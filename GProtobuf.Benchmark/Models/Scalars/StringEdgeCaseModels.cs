// DUAL-ATTRIBUTE MODELS — see PrimitiveTypesModel.cs header for rationale.

using System.Collections.Generic;

namespace GProtobuf.Benchmark.Models.Scalars
{
    /// <summary>
    /// 50 strings of uniform shape — amplifies per-string cost above BDN noise floor.
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class StringListModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public List<string> Values { get; set; }
    }

    /// <summary>
    /// Single long string — isolates per-string UTF-8 encode/decode cost
    /// without 50x list overhead.
    /// </summary>
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class SingleStringModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public string Value { get; set; }
    }
}
