// DUAL-ATTRIBUTE MODELS — see PrimitiveTypesModel.cs header for rationale.
//
// Dedicated models for DictionaryDeepDiveBenchmark. Each type combo isolates
// a different wire-format cost:
//   <int,int>       — fixed-small entry (~4 bytes), per-entry framing dominates
//   <int,string>    — varint key + length-delimited value (parameterized length)
//   <string,string> — length-delimited on both sides
//   <long,byte[]>   — wide varint key + binary payload (parameterized length)

using System.Collections.Generic;

namespace GProtobuf.Benchmark.Models.Collections
{
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class DictionaryIntIntModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public Dictionary<int, int> Map { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class DictionaryStringStringModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public Dictionary<string, string> Map { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class DictionaryLongBytesModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public Dictionary<long, byte[]> Map { get; set; }
    }
}
