// DUAL-ATTRIBUTE MODELS — see PrimitiveTypesModel.cs header for rationale.

using System.Collections.Generic;
using DataFormat = GProtobuf.DataFormat;

namespace GProtobuf.Benchmark.Models.Scalars
{
    /// <summary>
    /// 100 int32 values, default varint encoding (not packed — one tag+value per element).
    /// Packing is exercised separately via the ZigZag/Fixed variants.
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class Int32DefaultListModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public List<int> Values { get; set; }
    }

    /// <summary>
    /// 100 int32 values, ZigZag varint encoding (efficient for negatives).
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class Int32ZigZagListModel
    {
        [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
        [ProtoBuf.ProtoMember(1, DataFormat = ProtoBuf.DataFormat.ZigZag)]
        public List<int> Values { get; set; }
    }

    /// <summary>
    /// 100 int32 values, fixed 32-bit encoding (constant 4 bytes per value regardless of magnitude).
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class Int32FixedListModel
    {
        [ProtoMember(1, DataFormat = DataFormat.FixedSize, IsPacked = true)]
        [ProtoBuf.ProtoMember(1, DataFormat = ProtoBuf.DataFormat.FixedSize, IsPacked = true)]
        public List<int> Values { get; set; }
    }
}
