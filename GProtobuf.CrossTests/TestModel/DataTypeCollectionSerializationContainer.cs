using System;
using System.IO;
using GProtobuf;

namespace GProtobuf.CrossTests.TestModel
{
    /// <summary>
    /// Nested object used as ProtoMember(1) in the container.
    /// </summary>
    [ProtoContract]
    public partial class DataTypeGenericParam
    {
        [ProtoMember(1)]
        public int TypeId { get; set; }

        [ProtoMember(2)]
        public string TypeName { get; set; }
    }

    /// <summary>
    /// Test model combining a regular ProtoMember (nested object) with a ProtoBuffer custom buffer.
    /// Mirrors the production DataTypeCollectionSerializationContainer pattern:
    /// - [ProtoMember(1)] = nested object field
    /// - [ProtoBuffer(2)] = custom buffer backed by MemoryStream
    /// </summary>
    [ProtoContract]
    public partial class DataTypeCollectionSerializationContainer
    {
        private MemoryStream _sourceStream;
        private MemoryStream _receivedStream;

        public DataTypeCollectionSerializationContainer()
        {
        }

        public DataTypeCollectionSerializationContainer(MemoryStream stream, DataTypeGenericParam dataType)
        {
            GenericParam = dataType;
            _sourceStream = stream;
        }

        [ProtoMember(1)]
        public DataTypeGenericParam GenericParam { get; set; }

        [ProtoBuffer(2, ProtoBufferOperation.GetSize)]
        public int GetSerializedSize()
        {
            return (int)(_sourceStream?.Length ?? 0);
        }

        [ProtoBuffer(2, ProtoBufferOperation.Write)]
        public void FillBuffer(Span<byte> buffer)
        {
            if (_sourceStream == null) return;
            _sourceStream.Position = 0;
            _sourceStream.ReadExactly(buffer);
        }

        [ProtoBuffer(2, ProtoBufferOperation.Read)]
        public void ReadFromBuffer(ReadOnlySpan<byte> buffer)
        {
            _receivedStream = new MemoryStream();
            _receivedStream.Write(buffer);
        }

        /// <summary>
        /// Returns the received buffer data as byte[] for test verification.
        /// </summary>
        public byte[] GetReceivedData()
        {
            if (_receivedStream == null) return null;
            return _receivedStream.ToArray();
        }
    }
}
