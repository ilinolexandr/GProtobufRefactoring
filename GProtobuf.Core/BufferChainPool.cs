using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GProtobuf.Core
{
    /// <summary>
    /// Append-only byte sink backed by a chain of 8 KB segments rented from
    /// <see cref="ArrayPool{T}.Shared"/>. Replaces <see cref="MemoryStream"/>'s
    /// power-of-two growing <c>byte[]</c> buffer for the nested sub-message sink
    /// in <see cref="OnePassStreamWriter"/>.
    /// <para>
    /// Segments are sized below the LOH threshold and live in a globally shared
    /// <see cref="ArrayPool{T}"/>, so repeated serializations re-use the same
    /// backing storage instead of allocating fresh Gen2-bound <c>byte[]</c>s.
    /// </para>
    /// </summary>
    internal sealed class BufferChain
    {
        internal const int SegmentSize = 8 * 1024;

        private byte[][] _segments = new byte[4][];
        private int _segmentCount;
        private int _currentOffset;
        private long _length;

        public long Length => _length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return;

            // Fast path: fits in the current segment.
            if (_segmentCount > 0)
            {
                int space = SegmentSize - _currentOffset;
                if (data.Length <= space)
                {
                    data.CopyTo(_segments[_segmentCount - 1].AsSpan(_currentOffset));
                    _currentOffset += data.Length;
                    _length += data.Length;
                    return;
                }
            }

            WriteSlow(data);
        }

        private void WriteSlow(ReadOnlySpan<byte> data)
        {
            if (_segmentCount == 0)
                RentNewSegment();

            while (!data.IsEmpty)
            {
                int space = SegmentSize - _currentOffset;
                if (space == 0)
                {
                    RentNewSegment();
                    space = SegmentSize;
                }

                int toCopy = data.Length <= space ? data.Length : space;
                data.Slice(0, toCopy).CopyTo(_segments[_segmentCount - 1].AsSpan(_currentOffset));
                _currentOffset += toCopy;
                _length += toCopy;
                data = data.Slice(toCopy);
            }
        }

        private void RentNewSegment()
        {
            if (_segmentCount == _segments.Length)
                Array.Resize(ref _segments, _segments.Length * 2);

            _segments[_segmentCount++] = ArrayPool<byte>.Shared.Rent(SegmentSize);
            _currentOffset = 0;
        }

        public void CopyTo(Stream target)
        {
            int count = _segmentCount;
            if (count == 0) return;

            var segments = _segments;
            // All fully-filled segments except the last.
            for (int i = 0; i < count - 1; i++)
                target.Write(segments[i], 0, SegmentSize);

            // Last (partial) segment.
            target.Write(segments[count - 1], 0, _currentOffset);
        }

        public void Reset()
        {
            var segments = _segments;
            int count = _segmentCount;
            for (int i = 0; i < count; i++)
            {
                ArrayPool<byte>.Shared.Return(segments[i], clearArray: false);
                segments[i] = null;
            }

            _segmentCount = 0;
            _currentOffset = 0;
            _length = 0;
        }
    }

    /// <summary>
    /// <see cref="Stream"/> adapter over <see cref="BufferChain"/>. Drop-in
    /// replacement for <see cref="MemoryStream"/> in the
    /// <see cref="OnePassStreamWriter"/> nested sub-message path: supports write +
    /// <see cref="CopyTo(Stream)"/>, plus the single seek operation the writer
    /// uses (<c>Position = 0</c> before copying the buffered content).
    /// </summary>
    internal sealed class BufferChainStream : Stream
    {
        private readonly BufferChain _chain = new();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _chain.Length;

        // Position getter reports current buffered length; setter is a no-op that
        // accepts 0 (the only value OnePassStreamWriter.EndSubMessage assigns).
        // Reading is not supported, so re-positioning for future reads is N/A.
        public override long Position
        {
            get => _chain.Length;
            set
            {
                if (value != 0 && value != _chain.Length)
                    throw new NotSupportedException("BufferChainStream only supports Position = 0 or the current length.");
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
            => _chain.Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
            => _chain.Write(buffer);

        public override void WriteByte(byte value)
        {
            Span<byte> one = stackalloc byte[1] { value };
            _chain.Write(one);
        }

        // Override the virtual (Stream, int) entry — the no-arg CopyTo(Stream)
        // forwards into it — so the byte-for-byte chain emission happens without
        // ever going through the (unsupported) Read path.
        public override void CopyTo(Stream destination, int bufferSize)
            => _chain.CopyTo(destination);

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        /// <summary>
        /// Returns all rented segments to <see cref="ArrayPool{T}.Shared"/> and
        /// resets the stream to an empty state. Called by <see cref="BufferChainPool.Return"/>.
        /// </summary>
        public void Reset() => _chain.Reset();
    }

    /// <summary>
    /// Pool of <see cref="BufferChainStream"/> wrappers.
    /// <para>
    /// NOT thread-safe. Intended to be rented from
    /// <see cref="BufferChainPoolCache"/> for the duration of a single
    /// serialization call, so <see cref="Rent"/>/<see cref="Return"/> can run
    /// without atomic operations on every nested sub-message.
    /// </para>
    /// <para>
    /// The wrapper object is the only thing pooled here; the actual segment
    /// bytes live in <see cref="ArrayPool{T}.Shared"/> and are rented/returned
    /// by <see cref="BufferChainStream.Reset"/>.
    /// </para>
    /// </summary>
    public sealed class BufferChainPool
    {
        private readonly BufferChainStream[] _items;
        private int _count;

        public BufferChainPool(int capacity = 8)
        {
            if (capacity < 1) capacity = 1;
            _items = new BufferChainStream[capacity];
        }

        internal BufferChainStream Rent()
        {
            int c = _count;
            if (c > 0)
            {
                int idx = c - 1;
                var item = _items[idx];
                _items[idx] = null;
                _count = idx;
                return item;
            }

            return new BufferChainStream();
        }

        internal void Return(BufferChainStream stream)
        {
            stream.Reset();

            int c = _count;
            var items = _items;
            if ((uint)c < (uint)items.Length)
            {
                items[c] = stream;
                _count = c + 1;
            }
            // else: inner pool full — let GC collect the excess wrapper.
            // Segments were already returned to ArrayPool by Reset().
        }
    }

    /// <summary>
    /// Thread-safe cache of <see cref="BufferChainPool"/> instances.
    /// <para>
    /// Contention (Interlocked CAS) occurs only on <see cref="Rent"/>/<see cref="Return"/>
    /// — i.e. once per serialization call — while <see cref="BufferChainPool.Rent"/>/
    /// <see cref="BufferChainPool.Return"/> inside that call remain atomic-free.
    /// </para>
    /// </summary>
    public sealed class BufferChainPoolCache
    {
        public static readonly BufferChainPoolCache Shared = new(maximumRetained: 8);

        private BufferChainPool _firstItem;
        private readonly Slot[] _items;

        public BufferChainPoolCache(uint maximumRetained = 8)
        {
            if (maximumRetained == 0) maximumRetained = 1;
            _items = new Slot[maximumRetained - 1];
        }

        public BufferChainPool Rent()
        {
            var item = _firstItem;
            if (item != null && Interlocked.CompareExchange(ref _firstItem, null, item) == item)
                return item;

            var items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                item = items[i].Element;
                if (item != null && Interlocked.CompareExchange(ref items[i].Element, null, item) == item)
                    return item;
            }

            return new BufferChainPool();
        }

        public void Return(BufferChainPool pool)
        {
            if (pool == null) return;

            if (Interlocked.CompareExchange(ref _firstItem, pool, null) == null)
                return;

            var items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                if (Interlocked.CompareExchange(ref items[i].Element, pool, null) == null)
                    return;
            }
            // Outer pool full — the inner pool (and its retained wrappers) will be GC'd.
        }

        private struct Slot
        {
            public BufferChainPool Element;
        }
    }
}
