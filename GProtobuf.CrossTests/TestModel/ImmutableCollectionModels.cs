using GProtobuf;
using System;
using System.Collections.Immutable;

namespace GProtobuf.CrossTests.TestModel
{
    /// <summary>
    /// ImmutableList&lt;int&gt; — non-packed (explicit IsPacked = false) and packed variants.
    /// Wire format is identical to List&lt;int&gt;; protobuf-net 2.3.7 supports ImmutableList
    /// natively (ImmutableCollectionDecorator), so PG/GP oracle tests apply.
    /// </summary>
    [ProtoContract]
    public partial class ImmutableListIntModel
    {
        [ProtoMember(1)]
        public ImmutableList<int> Values { get; set; }

        [ProtoMember(2, IsPacked = true)]
        public ImmutableList<int> PackedValues { get; set; }
    }

    /// <summary>
    /// ImmutableList with string and double elements (non-packed string, packed-by-default double).
    /// </summary>
    [ProtoContract]
    public partial class ImmutableListMixedModel
    {
        [ProtoMember(1)]
        public ImmutableList<string> Names { get; set; }

        [ProtoMember(2, IsPacked = true)]
        public ImmutableList<double> Scores { get; set; }
    }

    /// <summary>
    /// ImmutableList of nested messages — exercises the complex-element read path
    /// (ObjectArrayBuilder accumulation + ImmutableList.CreateRange freeze).
    /// </summary>
    [ProtoContract]
    public partial class ImmutableListNestedModel
    {
        [ProtoMember(1)]
        public ImmutableList<ImmutableElementMsg> Items { get; set; }

        [ProtoMember(2)]
        public int Tag { get; set; }
    }

    [ProtoContract]
    public partial class ImmutableElementMsg
    {
        [ProtoMember(1)]
        public int K { get; set; }

        [ProtoMember(2)]
        public string S { get; set; }
    }

    /// <summary>
    /// ImmutableArray&lt;int&gt;/&lt;double&gt; — packed fixed-size elements exercise the bulk-copy
    /// read fast path (MemoryMarshal.Cast → exact T[] → zero-copy ImmutableCollectionsMarshal wrap).
    /// NOTE: protobuf-net 2.3.7 cannot roundtrip ImmutableArray (its read path throws NRE),
    /// so only GG + byte-level GP comparison tests apply.
    /// </summary>
    [ProtoContract]
    public partial class ImmutableArrayModel
    {
        [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.FixedSize)]
        public ImmutableArray<int> FixedValues { get; set; }

        [ProtoMember(2, IsPacked = true)]
        public ImmutableArray<double> Scores { get; set; }

        [ProtoMember(3)]
        public ImmutableArray<int> VarintValues { get; set; }
    }

    /// <summary>
    /// Byte-level reference for ImmutableArrayModel: same field ids/formats with plain arrays.
    /// Arrays are oracle-verified elsewhere, so byte-equality GProtobuf(ImmutableArrayModel) ==
    /// GProtobuf(ImmutableArrayEquivalentModel) proves the wire format without a pn roundtrip
    /// (pn 2.3.7 cannot roundtrip ImmutableArray).
    /// </summary>
    [ProtoContract]
    public partial class ImmutableArrayEquivalentModel
    {
        [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.FixedSize)]
        public int[] FixedValues { get; set; }

        [ProtoMember(2, IsPacked = true)]
        public double[] Scores { get; set; }

        [ProtoMember(3)]
        public int[] VarintValues { get; set; }
    }

    /// <summary>
    /// Immutable sets (Phase 2a). ImmutableHashSet enumerates in hash order (content-equality
    /// tests only); ImmutableSortedSet enumerates ascending (deterministic — byte-level tests OK).
    /// Both supported by pn 2.3.7 natively.
    /// </summary>
    [ProtoContract]
    public partial class ImmutableSetModel
    {
        [ProtoMember(1)]
        public ImmutableHashSet<int> Ints { get; set; }

        [ProtoMember(2)]
        public ImmutableSortedSet<int> SortedInts { get; set; }

        [ProtoMember(3)]
        public ImmutableHashSet<string> Names { get; set; }
    }

    /// <summary>
    /// Immutable dictionaries (Phase 2c). Read path uses SetItem accumulation
    /// (target = (target ?? Empty).SetItem(k, v)); both supported by pn 2.3.7.
    /// ImmutableDictionary enumerates in hash order (content tests only);
    /// ImmutableSortedDictionary enumerates by key — deterministic byte-level tests OK.
    /// </summary>
    [ProtoContract]
    public partial class ImmutableDictionaryModel
    {
        [ProtoMember(1)]
        public ImmutableDictionary<int, string> Map { get; set; }

        [ProtoMember(2)]
        public ImmutableSortedDictionary<int, string> SortedMap { get; set; }
    }

    /// <summary>
    /// Immutable collection as an init-only member — composes the deferred-init /
    /// constructor-injection paths with the freeze finalization.
    /// </summary>
    [ProtoContract]
    public partial class ImmutableInitModel
    {
        [ProtoMember(1)]
        public ImmutableList<int> Values { get; init; }

        [ProtoMember(2)]
        public string Label { get; init; }
    }

    /// <summary>
    /// Packing × DataFormat coverage (mirrors the IMMUTABLE-USAGE.md "Series" example):
    /// 8-byte FixedSize ImmutableArray (non-double bulk-copy path) and ZigZag ImmutableList
    /// (ZigZag was previously never exercised on any immutable collection).
    /// </summary>
    [ProtoContract]
    public partial class ImmutableSeriesModel
    {
        [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.FixedSize)]
        public ImmutableArray<long> Ticks { get; init; }

        [ProtoMember(2, IsPacked = true, DataFormat = DataFormat.ZigZag)]
        public ImmutableList<int> Deltas { get; init; }

        [ProtoMember(3, IsPacked = true, DataFormat = DataFormat.ZigZag)]
        public ImmutableArray<long> ZigZagLongs { get; init; }
    }
}
