// DUAL-ATTRIBUTE MODELS — see PrimitiveTypesModel.cs header for rationale.

using System.Collections.Generic;

namespace GProtobuf.Benchmark.Models
{
    /// <summary>
    /// Model designed to test ObjectArrayBuilder performance.
    /// Without ObjectArrayBuilder: List&lt;T&gt; grows 4→8→16→32→64→128 (many allocations).
    /// With ObjectArrayBuilder: single pooled buffer, only final array allocated.
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class ObjectArrayBuilderTestModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public SimpleItem[] Items { get; set; }

        [ProtoMember(2)]
        [ProtoBuf.ProtoMember(2)]
        public NestedItem[] NestedItems { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class SimpleItem
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public string Name { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public int Value { get; set; }
        [ProtoMember(3)] [ProtoBuf.ProtoMember(3)] public double Score { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class NestedItem
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public string Title { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public SimpleItem Inner { get; set; }
        [ProtoMember(3)] [ProtoBuf.ProtoMember(3)] public List<string> Tags { get; set; }
    }
}
