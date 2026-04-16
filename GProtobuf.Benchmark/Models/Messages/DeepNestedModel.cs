// DUAL-ATTRIBUTE MODEL — see PrimitiveTypesModel.cs header for rationale.

namespace GProtobuf.Benchmark.Models.Messages
{
    /// <summary>
    /// Self-referential node. A chain of depth N is built by setting
    /// Child on N nested instances. Exposes per-level BeginSubMessage /
    /// EndSubMessage cost in OnePass vs TwoPass size-precomputation.
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class DeepNestedNode
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public int Value { get; set; }

        [ProtoMember(2)]
        [ProtoBuf.ProtoMember(2)]
        public DeepNestedNode Child { get; set; }
    }
}
