// DUAL-ATTRIBUTE MODEL.
//
// GProtobuf.Core defines ProtoContract/ProtoMember in the `GProtobuf` namespace.
// Because this file sits under `GProtobuf.Benchmark.Models`, name resolution
// walks the enclosing `GProtobuf` namespace first and binds `[ProtoContract]`
// to GProtobuf's attribute — protobuf-net never sees its own contract.
//
// To let protobuf-net also recognize the model we apply both attribute sets
// via explicit `ProtoBuf.`-qualified references.

using DataFormat = GProtobuf.DataFormat;

namespace GProtobuf.Benchmark.Models
{
    [ProtoContract]                     // GProtobuf (enclosing-namespace lookup)
    [ProtoBuf.ProtoContract]            // protobuf-net
    public class PrimitiveTypesModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public int IntValue { get; set; }

        [ProtoMember(2)]
        [ProtoBuf.ProtoMember(2)]
        public long LongValue { get; set; }

        [ProtoMember(3)]
        [ProtoBuf.ProtoMember(3)]
        public float FloatValue { get; set; }

        [ProtoMember(4)]
        [ProtoBuf.ProtoMember(4)]
        public double DoubleValue { get; set; }

        [ProtoMember(5)]
        [ProtoBuf.ProtoMember(5)]
        public bool BoolValue { get; set; }

        [ProtoMember(6)]
        [ProtoBuf.ProtoMember(6)]
        public string StringValue { get; set; }

        [ProtoMember(7)]
        [ProtoBuf.ProtoMember(7)]
        public byte[] ByteArrayValue { get; set; }

        [ProtoMember(8, DataFormat = DataFormat.FixedSize)]
        [ProtoBuf.ProtoMember(8, DataFormat = ProtoBuf.DataFormat.FixedSize)]
        public int FixedIntValue { get; set; }

        [ProtoMember(9, DataFormat = DataFormat.FixedSize)]
        [ProtoBuf.ProtoMember(9, DataFormat = ProtoBuf.DataFormat.FixedSize)]
        public long FixedLongValue { get; set; }

        [ProtoMember(10, DataFormat = DataFormat.ZigZag)]
        [ProtoBuf.ProtoMember(10, DataFormat = ProtoBuf.DataFormat.ZigZag)]
        public int ZigZagIntValue { get; set; }

        [ProtoMember(11, DataFormat = DataFormat.ZigZag)]
        [ProtoBuf.ProtoMember(11, DataFormat = ProtoBuf.DataFormat.ZigZag)]
        public long ZigZagLongValue { get; set; }
    }
}
