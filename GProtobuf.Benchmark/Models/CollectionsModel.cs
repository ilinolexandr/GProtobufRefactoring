// DUAL-ATTRIBUTE MODEL — see PrimitiveTypesModel.cs header for rationale.

using System.Collections.Generic;
using DataFormat = GProtobuf.DataFormat;

namespace GProtobuf.Benchmark.Models
{
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class CollectionsModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public List<int> IntList { get; set; }

        [ProtoMember(2)]
        [ProtoBuf.ProtoMember(2)]
        public List<long> LongList { get; set; }

        [ProtoMember(3)]
        [ProtoBuf.ProtoMember(3)]
        public List<float> FloatList { get; set; }

        [ProtoMember(4)]
        [ProtoBuf.ProtoMember(4)]
        public List<double> DoubleList { get; set; }

        [ProtoMember(5)]
        [ProtoBuf.ProtoMember(5)]
        public List<string> StringList { get; set; }

        [ProtoMember(6)]
        [ProtoBuf.ProtoMember(6)]
        public int[] IntArray { get; set; }

        [ProtoMember(7)]
        [ProtoBuf.ProtoMember(7)]
        public float[] FloatArray { get; set; }

        [ProtoMember(8)]
        [ProtoBuf.ProtoMember(8)]
        public double[] DoubleArray { get; set; }

        [ProtoMember(9)]
        [ProtoBuf.ProtoMember(9)]
        public string[] StringArray { get; set; }

        [ProtoMember(10, DataFormat = DataFormat.FixedSize, IsPacked = true)]
        [ProtoBuf.ProtoMember(10, DataFormat = ProtoBuf.DataFormat.FixedSize, IsPacked = true)]
        public List<int> PackedFixedIntList { get; set; }

        [ProtoMember(11, DataFormat = DataFormat.ZigZag)]
        [ProtoBuf.ProtoMember(11, DataFormat = ProtoBuf.DataFormat.ZigZag)]
        public List<int> PackedZigZagIntList { get; set; }
    }
}
