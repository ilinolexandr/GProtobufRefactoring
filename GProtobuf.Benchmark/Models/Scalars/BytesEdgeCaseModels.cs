// DUAL-ATTRIBUTE MODELS — see PrimitiveTypesModel.cs header for rationale.

namespace GProtobuf.Benchmark.Models.Scalars
{
    /// <summary>
    /// Single byte[] payload. Parameterized size sweeps stackalloc (≤256 B) →
    /// ArrayPool (≤LOH) → LOH (≥85 KB) boundaries to expose buffer-strategy
    /// thresholds in GProtobuf's read/write paths.
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class BytesPayloadModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public byte[] Value { get; set; }
    }
}
