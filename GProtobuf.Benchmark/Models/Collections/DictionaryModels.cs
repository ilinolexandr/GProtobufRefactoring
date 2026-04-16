// DUAL-ATTRIBUTE MODELS — see PrimitiveTypesModel.cs header for rationale.

using System.Collections.Generic;

namespace GProtobuf.Benchmark.Models.Collections
{
    /// <summary>
    /// Dictionary&lt;int, string&gt; payload for N-scaling benchmarks.
    /// Tests protobuf map-entry wire format (repeated sub-message) under
    /// growing collection size.
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class DictionaryIntStringModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public Dictionary<int, string> Map { get; set; }
    }
}
