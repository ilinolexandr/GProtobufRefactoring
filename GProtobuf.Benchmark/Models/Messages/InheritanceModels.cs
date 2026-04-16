// INHERITANCE MODELS — DUAL-ATTRIBUTE PATTERN NOT FULLY APPLIED.
//
// Dual attributes are applied to [ProtoContract] / [ProtoMember] as usual
// (see PrimitiveTypesModel.cs header).
//
// [ProtoInclude] is only applied ONCE (via GProtobuf's attribute, picked up
// by enclosing-namespace lookup). Adding a second [ProtoBuf.ProtoInclude]
// triggers generator bug R-009 — the generator's .Contains("ProtoIncludeAttribute")
// match treats both occurrences as distinct includes and emits duplicate
// switch cases, which fails to compile.
//
// Consequence: protobuf-net will NOT do polymorphic dispatch on these types.
// It serializes only the declared-type fields (base layer). The Inheritance
// benchmark's ProtoBufNet variant therefore measures a strictly smaller
// workload than GProtobuf — treat that baseline as an upper-bound reference
// rather than an apples-to-apples comparison until R-009 is fixed.

namespace GProtobuf.Benchmark.Models.Messages
{
    [ProtoContract]
    [ProtoInclude(100, typeof(InheritDerived))]
    [ProtoBuf.ProtoContract]
    public class InheritBase
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public string BaseField { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(101, typeof(InheritDoublyDerived))]
    [ProtoBuf.ProtoContract]
    public class InheritDerived : InheritBase
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public string DerivedField { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class InheritDoublyDerived : InheritDerived
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public string DoublyDerivedField { get; set; }
    }
}
