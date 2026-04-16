// DUAL-ATTRIBUTE MODEL — see PrimitiveTypesModel.cs header for rationale.

namespace GProtobuf.Benchmark.Models.Messages
{
    /// <summary>
    /// Single-field model used to measure dispatch overhead.
    /// With default value (0), protobuf skips the field and the payload
    /// is effectively empty — everything measured is library boilerplate.
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class MinimalMessageModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public int Value { get; set; }
    }
}
