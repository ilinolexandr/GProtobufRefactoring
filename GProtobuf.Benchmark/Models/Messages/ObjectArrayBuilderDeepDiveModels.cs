// DUAL-ATTRIBUTE MODELS — see PrimitiveTypesModel.cs header for rationale.
//
// Dedicated models for ObjectArrayBuilderDeepDiveBenchmark. Each shape
// isolates a different per-element cost in the OnePass sub-message path:
//
//   IntOnlyItem           — minimal payload, exposes per-element framing
//                           (tag + length-prefix) and BufferChainPool
//                           rent/return cost in isolation.
//
//   FivePrimitiveItem     — 5 primitive fields of mixed wire type. Compared
//                           against IntOnlyItem shows per-field cost when
//                           element size grows but framing is identical.
//
//   NestedSubmessageItem  — contains another submessage. Exposes the
//                           cost of nested length-delimited writes
//                           (hypothesis: OnePass pays extra shift/copy
//                           to materialize the prefix length for each
//                           sub-sub-message).
//
//   ArrayOfArraysContainer — outer repeated field of inner messages, each
//                           holding its own repeated field. Amplifies the
//                           BufferChainPool pattern by chaining rentals
//                           at two levels.
//
// All models live in the same namespace as the existing deep-nested /
// inheritance models so the source generator emits serializers into
// GProtobuf.Benchmark.Models.Messages.Serialization alongside them.

using System.Collections.Generic;

namespace GProtobuf.Benchmark.Models.Messages
{
    // ── Int-only element ─────────────────────────────────────────────────
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class IntOnlyItem
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public int Value { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class ArrayIntOnlyModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public IntOnlyItem[] Items { get; set; }
    }

    // ── Five-primitive element ───────────────────────────────────────────
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class FivePrimitiveItem
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public int A { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public long B { get; set; }
        [ProtoMember(3)] [ProtoBuf.ProtoMember(3)] public double C { get; set; }
        [ProtoMember(4)] [ProtoBuf.ProtoMember(4)] public float D { get; set; }
        [ProtoMember(5)] [ProtoBuf.ProtoMember(5)] public bool E { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class ArrayFivePrimitiveModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public FivePrimitiveItem[] Items { get; set; }
    }

    // ── Nested submessage element ────────────────────────────────────────
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class InnerLeaf
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public int Value { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public string Label { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class NestedSubmessageItem
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public InnerLeaf Inner { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class ArrayNestedSubmessageModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public NestedSubmessageItem[] Items { get; set; }
    }

    // ── Array-of-arrays (two levels of submessage + repeated) ────────────
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class InnerBucket
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public IntOnlyItem[] Items { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class ArrayOfArraysModel
    {
        [ProtoMember(1)]
        [ProtoBuf.ProtoMember(1)]
        public InnerBucket[] Buckets { get; set; }
    }
}
