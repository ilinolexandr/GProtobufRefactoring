using System.IO;
using FluentAssertions;
using GProtobuf.Core;

namespace GProtobuf.CrossTests.Refactored;

public class BufferChainTests
{
    [Fact]
    public void Write_SingleSmallSpan_LengthMatches()
    {
        var chain = new BufferChain();
        byte[] data = [1, 2, 3, 4, 5];

        chain.Write(data);

        chain.Length.Should().Be(5);
    }

    [Fact]
    public void Write_EmptySpan_DoesNothing()
    {
        var chain = new BufferChain();
        chain.Write(System.ReadOnlySpan<byte>.Empty);

        chain.Length.Should().Be(0);
    }

    [Fact]
    public void Write_CrossingSegmentBoundary_PreservesByteOrder()
    {
        var chain = new BufferChain();

        // First write fills most of segment 1.
        byte[] first = new byte[BufferChain.SegmentSize - 10];
        for (int i = 0; i < first.Length; i++) first[i] = (byte)(i & 0xFF);
        chain.Write(first);

        // Second write straddles segments 1 → 2 (spans last 10 bytes of seg 1 + 40 bytes of seg 2).
        byte[] second = new byte[50];
        for (int i = 0; i < second.Length; i++) second[i] = (byte)(200 + i);
        chain.Write(second);

        chain.Length.Should().Be(first.Length + second.Length);

        using var ms = new MemoryStream();
        chain.CopyTo(ms);
        var bytes = ms.ToArray();

        bytes.Length.Should().Be(first.Length + second.Length);
        bytes.AsSpan(0, first.Length).SequenceEqual(first).Should().BeTrue();
        bytes.AsSpan(first.Length, second.Length).SequenceEqual(second).Should().BeTrue();
    }

    [Fact]
    public void Write_ManySegmentsViaSingleLargeSpan_CopiesContiguously()
    {
        // Span large enough to require growing the _segments array beyond its
        // initial 4 slots, exercising Array.Resize.
        int totalBytes = BufferChain.SegmentSize * 6 + 123;
        byte[] data = new byte[totalBytes];
        for (int i = 0; i < data.Length; i++) data[i] = (byte)(i * 31);

        var chain = new BufferChain();
        chain.Write(data);

        using var ms = new MemoryStream();
        chain.CopyTo(ms);

        ms.ToArray().Should().Equal(data);
    }

    [Fact]
    public void CopyTo_MatchesMemoryStreamOutput_ForSameInputSequence()
    {
        var chain = new BufferChain();
        using var reference = new MemoryStream();

        var rng = new System.Random(42);
        for (int i = 0; i < 200; i++)
        {
            byte[] chunk = new byte[rng.Next(1, 513)];
            rng.NextBytes(chunk);
            chain.Write(chunk);
            reference.Write(chunk, 0, chunk.Length);
        }

        chain.Length.Should().Be(reference.Length);

        using var output = new MemoryStream();
        chain.CopyTo(output);

        output.ToArray().Should().Equal(reference.ToArray());
    }

    [Fact]
    public void Reset_TwiceIsIdempotent()
    {
        var chain = new BufferChain();
        chain.Write(new byte[BufferChain.SegmentSize * 2 + 17]);
        chain.Length.Should().BeGreaterThan(0);

        chain.Reset();
        chain.Reset(); // must not double-return rented segments to ArrayPool

        chain.Length.Should().Be(0);
    }

    [Fact]
    public void Reset_AllowsReuse()
    {
        var chain = new BufferChain();

        byte[] first = new byte[1000];
        for (int i = 0; i < first.Length; i++) first[i] = (byte)i;
        chain.Write(first);

        chain.Reset();

        byte[] second = new byte[2000];
        for (int i = 0; i < second.Length; i++) second[i] = (byte)(255 - (i & 0xFF));
        chain.Write(second);

        chain.Length.Should().Be(second.Length);

        using var ms = new MemoryStream();
        chain.CopyTo(ms);
        ms.ToArray().Should().Equal(second);
    }

    [Fact]
    public void BufferChainStream_CopyTo_RoundTripsThroughMemoryStream()
    {
        var stream = new BufferChainStream();

        byte[] payload = new byte[BufferChain.SegmentSize + 250];
        for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i ^ 0xA5);
        stream.Write(payload, 0, payload.Length);

        stream.Length.Should().Be(payload.Length);

        // OnePassStreamWriter.EndSubMessage sets Position = 0 before CopyTo.
        stream.Position = 0;

        using var dest = new MemoryStream();
        stream.CopyTo(dest);

        dest.ToArray().Should().Equal(payload);
    }

    [Fact]
    public void BufferChainPoolCache_RentReturn_ReturnsSamePool()
    {
        var cache = new BufferChainPoolCache(maximumRetained: 4);

        var p1 = cache.Rent();
        cache.Return(p1);
        var p2 = cache.Rent();

        p2.Should().BeSameAs(p1);
    }
}
