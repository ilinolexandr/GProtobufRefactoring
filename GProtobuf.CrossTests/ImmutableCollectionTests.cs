using FluentAssertions;
using GProtobuf.CrossTests.TestModel;
using System.Collections.Immutable;
using System.IO;
using Xunit;
using TestModelSerializers = GProtobuf.CrossTests.TestModel.Serialization.Serializers;
using TestModelDeserializers = GProtobuf.CrossTests.TestModel.Serialization.Deserializers;

namespace GProtobuf.Tests;

/// <summary>
/// Tests for System.Collections.Immutable members (Phase 1: ImmutableList&lt;T&gt; + ImmutableArray&lt;T&gt;).
///
/// Correctness criterion: protobuf-net 2.3.7 is the reference; gp must produce identical values
/// AND bytes. Tests assert gp == pn; cases blocked by an unfixed bug are [Fact(Skip="Fx: …")].
///
/// Oracle status (protobuf-net-2.3.7-facts.md, Immutable collections audit):
/// - ImmutableList&lt;T&gt; is fully supported by pn 2.3.7 → PG/GP roundtrip + byte tests apply.
/// - PACKED PRIMITIVE ImmutableArray&lt;T&gt; (e.g. ImmutableArray&lt;int&gt;) is broken in pn 2.3.7
///   (read NRE, packed-write InvalidProgramException) → no pn roundtrip; the oracle is byte-equality
///   vs the plain int[]/double[] equivalent model (which IS pn-verified). Non-packed message-element
///   ImmutableArray (see FutureValuesImmutableTests) DOES round-trip through pn.
/// </summary>
public sealed class ImmutableCollectionTests : BaseSerializationTest
{
    #region ImmutableListIntModel

    private static ImmutableListIntModel CreateListIntModel() => new()
    {
        Values = ImmutableList.Create(1, -2, int.MaxValue),
        PackedValues = ImmutableList.Create(10, 20, 30),
    };

    private static void AssertListIntModel(ImmutableListIntModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Values.Should().Equal(1, -2, int.MaxValue);
        deserialized.PackedValues.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void ImmutableListInt_GG()
    {
        var data = SerializeWithGProtobuf(CreateListIntModel(), TestModelSerializers.Serialize);
        AssertListIntModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableListIntModel(bytes)));
    }

    [Fact]
    public void ImmutableListInt_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateListIntModel(), TestModelSerializers.Serialize);
        AssertListIntModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableListIntModel(stream)));
    }

    [Fact]
    public void ImmutableListInt_PG()
    {
        var data = SerializeWithProtobufNet(CreateListIntModel());
        AssertListIntModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableListIntModel(bytes)));
    }

    [Fact]
    public void ImmutableListInt_PG_Stream()
    {
        var data = SerializeWithProtobufNet(CreateListIntModel());
        AssertListIntModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableListIntModel(stream)));
    }

    [Fact]
    public void ImmutableListInt_GP()
    {
        var data = SerializeWithGProtobuf(CreateListIntModel(), TestModelSerializers.Serialize);
        AssertListIntModel(DeserializeWithProtobufNet<ImmutableListIntModel>(data));
    }

    [Fact]
    public void ImmutableListInt_OnePass_ByteEqualTo2Pass()
    {
        var model = CreateListIntModel();
        var twoPass = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        using var ms = new MemoryStream();
        TestModelSerializers.SerializeOnePass(ms, model);
        ms.ToArray().Should().Equal(twoPass);
    }

    [Fact]
    public void ImmutableListInt_NullStaysNull_GG()
    {
        var data = SerializeWithGProtobuf(new ImmutableListIntModel(), TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableListIntModel(bytes));
        deserialized.Values.Should().BeNull();
        deserialized.PackedValues.Should().BeNull();
    }

    [Fact]
    public void ImmutableListInt_EmptyNonPacked_WritesNothing_GG()
    {
        // Empty non-packed collection emits zero wire entries → reader leaves the member null
        // (matches pn 2.3.7: empty collection serializes to zero bytes, reads back as null).
        var model = new ImmutableListIntModel { Values = ImmutableList<int>.Empty };
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableListIntModel(bytes));
        deserialized.Values.Should().BeNull();
    }

    #endregion

    #region ImmutableListMixedModel (string + packed double)

    private static ImmutableListMixedModel CreateMixedModel() => new()
    {
        Names = ImmutableList.Create("alpha", "beta", "gamma"),
        Scores = ImmutableList.Create(1.5, -2.25, 0.0),
    };

    private static void AssertMixedModel(ImmutableListMixedModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Names.Should().Equal("alpha", "beta", "gamma");
        deserialized.Scores.Should().Equal(1.5, -2.25, 0.0);
    }

    [Fact]
    public void ImmutableListMixed_GG()
    {
        var data = SerializeWithGProtobuf(CreateMixedModel(), TestModelSerializers.Serialize);
        AssertMixedModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableListMixedModel(bytes)));
    }

    [Fact]
    public void ImmutableListMixed_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateMixedModel(), TestModelSerializers.Serialize);
        AssertMixedModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableListMixedModel(stream)));
    }

    [Fact]
    public void ImmutableListMixed_PG()
    {
        var data = SerializeWithProtobufNet(CreateMixedModel());
        AssertMixedModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableListMixedModel(bytes)));
    }

    [Fact]
    public void ImmutableListMixed_GP()
    {
        var data = SerializeWithGProtobuf(CreateMixedModel(), TestModelSerializers.Serialize);
        AssertMixedModel(DeserializeWithProtobufNet<ImmutableListMixedModel>(data));
    }

    #endregion

    #region ImmutableListNestedModel (complex elements)

    private static ImmutableListNestedModel CreateNestedModel() => new()
    {
        Tag = 7,
        Items = ImmutableList.Create(
            new ImmutableElementMsg { K = 1, S = "a" },
            new ImmutableElementMsg { K = 2, S = "b" }),
    };

    private static void AssertNestedModel(ImmutableListNestedModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Tag.Should().Be(7);
        deserialized.Items.Should().HaveCount(2);
        deserialized.Items[0].K.Should().Be(1);
        deserialized.Items[0].S.Should().Be("a");
        deserialized.Items[1].K.Should().Be(2);
        deserialized.Items[1].S.Should().Be("b");
    }

    [Fact]
    public void ImmutableListNested_GG()
    {
        var data = SerializeWithGProtobuf(CreateNestedModel(), TestModelSerializers.Serialize);
        AssertNestedModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableListNestedModel(bytes)));
    }

    [Fact]
    public void ImmutableListNested_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateNestedModel(), TestModelSerializers.Serialize);
        AssertNestedModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableListNestedModel(stream)));
    }

    [Fact]
    public void ImmutableListNested_PG()
    {
        var data = SerializeWithProtobufNet(CreateNestedModel());
        AssertNestedModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableListNestedModel(bytes)));
    }

    [Fact]
    public void ImmutableListNested_GP()
    {
        var data = SerializeWithGProtobuf(CreateNestedModel(), TestModelSerializers.Serialize);
        AssertNestedModel(DeserializeWithProtobufNet<ImmutableListNestedModel>(data));
    }

    #endregion

    #region ImmutableArrayModel (GG + byte-equality vs plain arrays; pn cannot roundtrip)

    private static ImmutableArrayModel CreateArrayModel() => new()
    {
        FixedValues = ImmutableArray.Create(1, -1, int.MaxValue),
        Scores = ImmutableArray.Create(3.14, -1e10),
        VarintValues = ImmutableArray.Create(0, 127, 128),
    };

    private static void AssertArrayModel(ImmutableArrayModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.FixedValues.Should().Equal(1, -1, int.MaxValue);
        deserialized.Scores.Should().Equal(3.14, -1e10);
        deserialized.VarintValues.Should().Equal(0, 127, 128);
    }

    [Fact]
    public void ImmutableArray_GG()
    {
        var data = SerializeWithGProtobuf(CreateArrayModel(), TestModelSerializers.Serialize);
        AssertArrayModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableArrayModel(bytes)));
    }

    [Fact]
    public void ImmutableArray_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateArrayModel(), TestModelSerializers.Serialize);
        AssertArrayModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableArrayModel(stream)));
    }

    [Fact]
    public void ImmutableArray_BytesEqualPn()
    {
        // pn 2.3.7 cannot round-trip ImmutableArray, but it CAN serialize the equivalent int[]/double[]
        // model. Oracle anchor: gp(ImmutableArray) bytes must equal pn(int[]-equivalent) bytes.
        // (Data uses FixedSize ints + non-negative varints, so the negative-varint divergence does
        // not apply here — verified gp==gp(int[])==pn(int[]) byte-identical.)
        var immutableBytes = SerializeWithGProtobuf(CreateArrayModel(), TestModelSerializers.Serialize);
        var pnArrayBytes = SerializeWithProtobufNet(new ImmutableArrayEquivalentModel
        {
            FixedValues = new[] { 1, -1, int.MaxValue },
            Scores = new[] { 3.14, -1e10 },
            VarintValues = new[] { 0, 127, 128 },
        });
        immutableBytes.Should().Equal(pnArrayBytes);
    }

    [Fact]
    public void ImmutableArray_OnePass_ByteEqualTo2Pass()
    {
        var model = CreateArrayModel();
        var twoPass = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        using var ms = new MemoryStream();
        TestModelSerializers.SerializeOnePass(ms, model);
        ms.ToArray().Should().Equal(twoPass);
    }

    [Fact]
    public void ImmutableArray_DefaultIsSkippedAndStaysDefault_GG()
    {
        // default(ImmutableArray<T>): serialization must skip it (no NRE — unlike pn 2.3.7),
        // and reading an absent field leaves the member default.
        var data = SerializeWithGProtobuf(new ImmutableArrayModel(), TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableArrayModel(bytes));
        deserialized.FixedValues.IsDefault.Should().BeTrue();
        deserialized.Scores.IsDefault.Should().BeTrue();
        deserialized.VarintValues.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ImmutableArray_EmptyRoundtripSemantics_GG()
    {
        // Post-F4(b) pinning. Decision A: no read-normalization → gp == pn 3.2.46.
        // - PACKED empty member writes tag+len0 (0A 00) → reads back as Empty (safe, round-trips).
        // - NON-PACKED empty member writes nothing → indistinguishable from default → reads back
        //   default (same as the oracle pn 3.2.46; the unset/empty distinction is unrepresentable).
        var model = new ImmutableArrayModel
        {
            FixedValues = ImmutableArray<int>.Empty,    // field 1, packed (FixedSize)
            Scores = ImmutableArray<double>.Empty,      // field 2, packed
            VarintValues = ImmutableArray<int>.Empty,   // field 3, non-packed
        };
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var d = DeserializeWithGProtobuf(data, b => TestModelDeserializers.DeserializeImmutableArrayModel(b));

        d.FixedValues.IsDefault.Should().BeFalse();
        d.FixedValues.IsEmpty.Should().BeTrue();
        d.Scores.IsDefault.Should().BeFalse();
        d.Scores.IsEmpty.Should().BeTrue();
        d.VarintValues.IsDefault.Should().BeTrue();
    }

    [Fact] // F4 FIXED: write guard is now !IsDefault, so empty packed ImmutableArray writes tag+len0.
    public void ImmutableArray_Empty_BytesEqualPn()
    {
        // Oracle: gp(ImmutableArray.Empty) packed members must equal pn(int[]-equivalent Empty),
        // which is 0A-00-12-00 (FixedValues+Scores packed headers; VarintValues non-packed → nothing).
        var model = new ImmutableArrayModel
        {
            FixedValues = ImmutableArray<int>.Empty,
            Scores = ImmutableArray<double>.Empty,
            VarintValues = ImmutableArray<int>.Empty,
        };
        var gpBytes = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pnBytes = SerializeWithProtobufNet(new ImmutableArrayEquivalentModel
        {
            FixedValues = System.Array.Empty<int>(),
            Scores = System.Array.Empty<double>(),
            VarintValues = System.Array.Empty<int>(),
        });
        gpBytes.Should().Equal(pnBytes);
    }

    #endregion

    #region Byte-equality with protobuf-net 2.3.7 (oracle) — items / null / empty flows

    [Fact]
    public void ImmutableListInt_NonNegativeItems_BytesEqualPn()
    {
        // Non-negative subset only: proves gp==pn bytes when no negative int32 is present.
        // The general case (with negatives) is the SKIPPED test below — do not widen this data
        // set, or it would mask the negative-varint divergence instead of exercising it.
        var model = new ImmutableListIntModel
        {
            Values = ImmutableList.Create(1, 2, int.MaxValue),
            PackedValues = ImmutableList.Create(10, 20, 30),
        };
        var gpBytes = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pnBytes = SerializeWithProtobufNet(model);
        gpBytes.Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableListMixed_Items_BytesEqualPn()
    {
        var model = CreateMixedModel();
        var gpBytes = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pnBytes = SerializeWithProtobufNet(model);
        gpBytes.Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableListNested_Items_BytesEqualPn()
    {
        var model = CreateNestedModel();
        var gpBytes = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pnBytes = SerializeWithProtobufNet(model);
        gpBytes.Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableListInt_Null_BytesEqualPn()
    {
        // null collections: both serializers must emit nothing.
        var model = new ImmutableListIntModel();
        SerializeWithGProtobuf(model, TestModelSerializers.Serialize).Should().BeEmpty();
        SerializeWithProtobufNet(model).Should().BeEmpty();
    }

    [Fact]
    public void ImmutableListInt_EmptyNonPacked_BytesEqualPn()
    {
        // Empty non-packed collection: zero wire entries on both sides.
        var model = new ImmutableListIntModel { Values = ImmutableList<int>.Empty };
        SerializeWithGProtobuf(model, TestModelSerializers.Serialize).Should().BeEmpty();
        SerializeWithProtobufNet(model).Should().BeEmpty();
    }

    [Fact]
    public void ImmutableListInt_EmptyPacked_BytesEqualPn()
    {
        // Empty PACKED collection — pn 2.3.7 writes the header tag+len0 (e.g. 12 00), NOT nothing;
        // gp must produce the identical bytes. (ImmutableList Empty correctly emits the header,
        // unlike the ImmutableArray Empty case — see the skipped F4 test.)
        var model = new ImmutableListIntModel { PackedValues = ImmutableList<int>.Empty };
        var gpBytes = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pnBytes = SerializeWithProtobufNet(model);
        gpBytes.Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableListInt_Null_PG_RoundsToNull()
    {
        // pn serializes null → zero bytes → gp reads null back.
        var pnBytes = SerializeWithProtobufNet(new ImmutableListIntModel());
        var deserialized = DeserializeWithGProtobuf(pnBytes,
            bytes => TestModelDeserializers.DeserializeImmutableListIntModel(bytes));
        deserialized.Values.Should().BeNull();
        deserialized.PackedValues.Should().BeNull();
    }

    [Fact]
    public void ImmutableListInt_Null_GP_RoundsToNull()
    {
        // gp serializes null → zero bytes → pn reads null back.
        var gpBytes = SerializeWithGProtobuf(new ImmutableListIntModel(), TestModelSerializers.Serialize);
        var deserialized = DeserializeWithProtobufNet<ImmutableListIntModel>(gpBytes);
        deserialized.Values.Should().BeNull();
        deserialized.PackedValues.Should().BeNull();
    }

    [Fact]
    public void ImmutableListInt_Empty_RoundtripSemanticsMatchPn()
    {
        // Empty semantics differ per packing and are IDENTICAL between gp and pn:
        // - non-packed empty → zero wire entries → reads back as null;
        // - packed empty → header (tag + len 0) IS written → reads back as Empty list.
        var model = new ImmutableListIntModel
        {
            Values = ImmutableList<int>.Empty,
            PackedValues = ImmutableList<int>.Empty,
        };
        var gpBytes = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pnBytes = SerializeWithProtobufNet(model);
        gpBytes.Should().Equal(pnBytes);

        // gp bytes → pn reader
        var pnRead = DeserializeWithProtobufNet<ImmutableListIntModel>(gpBytes);
        pnRead.Values.Should().BeNull();
        pnRead.PackedValues.Should().BeEmpty();

        // pn bytes → gp reader (span + stream)
        var gpRead = DeserializeWithGProtobuf(pnBytes,
            bytes => TestModelDeserializers.DeserializeImmutableListIntModel(bytes));
        gpRead.Values.Should().BeNull();
        gpRead.PackedValues.Should().BeEmpty();

        var gpStreamRead = DeserializeWithGProtobufStreamFromBytes(pnBytes,
            stream => TestModelDeserializers.DeserializeImmutableListIntModel(stream));
        gpStreamRead.Values.Should().BeNull();
        gpStreamRead.PackedValues.Should().BeEmpty();
    }

    #endregion

    #region OnePass — primary production flow: byte-equality with 2-pass for all element kinds

    private static byte[] SerializeOnePass<T>(T model) where T : class
    {
        using var ms = new MemoryStream();
        // Dynamic dispatch picks the right overload of the type-overloaded SerializeOnePass.
        TestModelSerializers.SerializeOnePass(ms, (dynamic)model);
        return ms.ToArray();
    }

    [Fact]
    public void ImmutableListMixed_OnePass_ByteEqualTo2Pass()
    {
        // Strings exercise the OnePass length-prefix machinery (BeginSubMessage/padded varint).
        var model = CreateMixedModel();
        SerializeOnePass(model).Should().Equal(
            SerializeWithGProtobuf(model, TestModelSerializers.Serialize));
    }

    [Fact]
    public void ImmutableListNested_OnePass_ByteEqualTo2Pass()
    {
        // Complex elements go through OnePass's own GenerateComplexCollectionWrite variant.
        var model = CreateNestedModel();
        SerializeOnePass(model).Should().Equal(
            SerializeWithGProtobuf(model, TestModelSerializers.Serialize));
    }

    [Fact]
    public void ImmutableListNested_OnePass_GP()
    {
        // OnePass output must be readable by the oracle directly.
        var model = CreateNestedModel();
        AssertNestedModel(DeserializeWithProtobufNet<ImmutableListNestedModel>(SerializeOnePass(model)));
    }

    [Fact]
    public void ImmutableListInt_NullAndEmpty_OnePass_ByteEqualTo2Pass()
    {
        // null → nothing; non-packed empty → nothing; packed empty → tag + len 0.
        var nullModel = new ImmutableListIntModel();
        SerializeOnePass(nullModel).Should().BeEmpty();

        var emptyModel = new ImmutableListIntModel
        {
            Values = ImmutableList<int>.Empty,
            PackedValues = ImmutableList<int>.Empty,
        };
        SerializeOnePass(emptyModel).Should().Equal(
            SerializeWithGProtobuf(emptyModel, TestModelSerializers.Serialize));
    }

    [Fact]
    public void ImmutableArray_Default_OnePass_WritesNothing()
    {
        // default(ImmutableArray) is genuinely "unset" (pn can't represent it at all) → no bytes.
        // This is NOT a divergence: an all-default model has no members to write.
        SerializeOnePass(new ImmutableArrayModel()).Should().BeEmpty();
    }

    [Fact] // F4 FIXED: OnePass empty packed ImmutableArray now writes tag+len0.
    public void ImmutableArray_Empty_OnePass_BytesEqualPn()
    {
        var emptyModel = new ImmutableArrayModel
        {
            FixedValues = ImmutableArray<int>.Empty,
            Scores = ImmutableArray<double>.Empty,
            VarintValues = ImmutableArray<int>.Empty,
        };
        var pnBytes = SerializeWithProtobufNet(new ImmutableArrayEquivalentModel
        {
            FixedValues = System.Array.Empty<int>(),
            Scores = System.Array.Empty<double>(),
            VarintValues = System.Array.Empty<int>(),
        });
        SerializeOnePass(emptyModel).Should().Equal(pnBytes);
    }

    #endregion

    #region ImmutableSetModel (Phase 2a: ImmutableHashSet + ImmutableSortedSet)

    private static ImmutableSetModel CreateSetModel() => new()
    {
        Ints = ImmutableHashSet.Create(1, 2, 3),
        SortedInts = ImmutableSortedSet.Create(30, 10, 20),
        Names = ImmutableHashSet.Create("alpha", "beta"),
    };

    private static void AssertSetModel(ImmutableSetModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Ints.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        deserialized.SortedInts.Should().Equal(10, 20, 30); // sorted set enumerates ascending
        deserialized.Names.Should().BeEquivalentTo(new[] { "alpha", "beta" });
    }

    [Fact]
    public void ImmutableSet_GG()
    {
        var data = SerializeWithGProtobuf(CreateSetModel(), TestModelSerializers.Serialize);
        AssertSetModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableSetModel(bytes)));
    }

    [Fact]
    public void ImmutableSet_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateSetModel(), TestModelSerializers.Serialize);
        AssertSetModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableSetModel(stream)));
    }

    [Fact]
    public void ImmutableSet_PG()
    {
        var data = SerializeWithProtobufNet(CreateSetModel());
        AssertSetModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableSetModel(bytes)));
    }

    [Fact]
    public void ImmutableSet_GP()
    {
        var data = SerializeWithGProtobuf(CreateSetModel(), TestModelSerializers.Serialize);
        AssertSetModel(DeserializeWithProtobufNet<ImmutableSetModel>(data));
    }

    [Fact]
    public void ImmutableSortedSet_BytesEqualPn()
    {
        // Sorted set enumerates ascending on both sides → deterministic byte identity.
        // (Hash sets enumerate in hash order — byte comparison is not meaningful for them.)
        var model = new ImmutableSetModel { SortedInts = ImmutableSortedSet.Create(30, 10, 20) };
        var gpBytes = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pnBytes = SerializeWithProtobufNet(model);
        gpBytes.Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableSet_OnePass_ByteEqualTo2Pass()
    {
        var model = CreateSetModel();
        SerializeOnePass(model).Should().Equal(
            SerializeWithGProtobuf(model, TestModelSerializers.Serialize));
    }

    [Fact]
    public void ImmutableSet_NullAndEmpty_GG()
    {
        // null → nothing on wire → null back.
        var nullRead = DeserializeWithGProtobuf(
            SerializeWithGProtobuf(new ImmutableSetModel(), TestModelSerializers.Serialize),
            bytes => TestModelDeserializers.DeserializeImmutableSetModel(bytes));
        nullRead.Ints.Should().BeNull();
        nullRead.SortedInts.Should().BeNull();
        nullRead.Names.Should().BeNull();

        // empty non-packed → zero wire entries → null back (same as pn).
        var emptyModel = new ImmutableSetModel
        {
            Ints = ImmutableHashSet<int>.Empty,
            SortedInts = ImmutableSortedSet<int>.Empty,
            Names = ImmutableHashSet<string>.Empty,
        };
        var emptyBytes = SerializeWithGProtobuf(emptyModel, TestModelSerializers.Serialize);
        emptyBytes.Should().BeEmpty();
        SerializeWithProtobufNet(emptyModel).Should().BeEmpty();
    }

    #endregion

    #region ImmutableDictionaryModel (Phase 2c)

    private static ImmutableDictionaryModel CreateDictionaryModel() => new()
    {
        Map = ImmutableDictionary<int, string>.Empty.Add(1, "a").Add(2, "b"),
        SortedMap = ImmutableSortedDictionary<int, string>.Empty.Add(20, "y").Add(10, "x"),
    };

    private static void AssertDictionaryModel(ImmutableDictionaryModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Map.Should().HaveCount(2);
        deserialized.Map[1].Should().Be("a");
        deserialized.Map[2].Should().Be("b");
        deserialized.SortedMap.Should().HaveCount(2);
        deserialized.SortedMap[10].Should().Be("x");
        deserialized.SortedMap[20].Should().Be("y");
    }

    [Fact]
    public void ImmutableDictionary_GG()
    {
        var data = SerializeWithGProtobuf(CreateDictionaryModel(), TestModelSerializers.Serialize);
        AssertDictionaryModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableDictionaryModel(bytes)));
    }

    [Fact]
    public void ImmutableDictionary_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateDictionaryModel(), TestModelSerializers.Serialize);
        AssertDictionaryModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableDictionaryModel(stream)));
    }

    [Fact]
    public void ImmutableDictionary_PG()
    {
        var data = SerializeWithProtobufNet(CreateDictionaryModel());
        AssertDictionaryModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableDictionaryModel(bytes)));
    }

    [Fact]
    public void ImmutableDictionary_PG_Stream()
    {
        var data = SerializeWithProtobufNet(CreateDictionaryModel());
        AssertDictionaryModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableDictionaryModel(stream)));
    }

    [Fact]
    public void ImmutableDictionary_GP()
    {
        var data = SerializeWithGProtobuf(CreateDictionaryModel(), TestModelSerializers.Serialize);
        AssertDictionaryModel(DeserializeWithProtobufNet<ImmutableDictionaryModel>(data));
    }

    [Fact]
    public void ImmutableSortedDictionary_BytesEqualPn()
    {
        // Sorted dictionary enumerates by key on both sides → deterministic byte identity.
        var model = new ImmutableDictionaryModel
        {
            SortedMap = ImmutableSortedDictionary<int, string>.Empty.Add(2, "b").Add(1, "a"),
        };
        var gpBytes = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pnBytes = SerializeWithProtobufNet(model);
        gpBytes.Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableDictionary_OnePass_ByteEqualTo2Pass()
    {
        var model = CreateDictionaryModel();
        SerializeOnePass(model).Should().Equal(
            SerializeWithGProtobuf(model, TestModelSerializers.Serialize));
    }

    [Fact]
    public void ImmutableDictionary_NullAndEmpty_GG()
    {
        // null → nothing on wire → null back.
        var nullRead = DeserializeWithGProtobuf(
            SerializeWithGProtobuf(new ImmutableDictionaryModel(), TestModelSerializers.Serialize),
            bytes => TestModelDeserializers.DeserializeImmutableDictionaryModel(bytes));
        nullRead.Map.Should().BeNull();
        nullRead.SortedMap.Should().BeNull();

        // empty map → zero wire entries → null back (matches pn).
        var emptyModel = new ImmutableDictionaryModel
        {
            Map = ImmutableDictionary<int, string>.Empty,
            SortedMap = ImmutableSortedDictionary<int, string>.Empty,
        };
        SerializeWithGProtobuf(emptyModel, TestModelSerializers.Serialize).Should().BeEmpty();
        SerializeWithProtobufNet(emptyModel).Should().BeEmpty();
    }

    #endregion

    #region ImmutableInitModel (init-only members)

    [Fact]
    public void ImmutableInit_GG()
    {
        var model = new ImmutableInitModel
        {
            Values = ImmutableList.Create(5, 6, 7),
            Label = "init-immutable",
        };
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableInitModel(bytes));
        deserialized.Values.Should().Equal(5, 6, 7);
        deserialized.Label.Should().Be("init-immutable");
    }

    [Fact]
    public void ImmutableInit_PG()
    {
        var model = new ImmutableInitModel
        {
            Values = ImmutableList.Create(5, 6, 7),
            Label = "init-immutable",
        };
        var data = SerializeWithProtobufNet(model);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableInitModel(bytes));
        deserialized.Values.Should().Equal(5, 6, 7);
        deserialized.Label.Should().Be("init-immutable");
    }

    #endregion

    #region OnePass — DIRECT oracle verification (OnePass output vs pn, not just vs 2-pass)

    // The primary production flow is OnePass. The tests above mostly assert OnePass == 2-pass
    // (transitive to pn only where a separate 2-pass == pn test exists on the same data). These
    // tests pin OnePass DIRECTLY against pn 2.3.7: bytes where enumeration order is deterministic,
    // values (OnePass → pn read) for every type incl. hash-ordered sets/dictionaries.

    // --- OnePass bytes == pn bytes (deterministic order; no negative int32) ---

    [Fact]
    public void ImmutableListInt_NonNegative_OnePass_BytesEqualPn()
    {
        var model = new ImmutableListIntModel
        {
            Values = ImmutableList.Create(1, 2, int.MaxValue),
            PackedValues = ImmutableList.Create(10, 20, 30),
        };
        SerializeOnePass(model).Should().Equal(SerializeWithProtobufNet(model));
    }

    [Fact]
    public void ImmutableListMixed_OnePass_BytesEqualPn()
    {
        var model = CreateMixedModel();
        SerializeOnePass(model).Should().Equal(SerializeWithProtobufNet(model));
    }

    [Fact]
    public void ImmutableListNested_OnePass_BytesEqualPn()
    {
        var model = CreateNestedModel();
        SerializeOnePass(model).Should().Equal(SerializeWithProtobufNet(model));
    }

    [Fact]
    public void ImmutableArray_OnePass_BytesEqualPn()
    {
        // pn can't round-trip packed-primitive ImmutableArray, but it serializes the int[] equivalent;
        // OnePass(ImmutableArray) bytes must equal pn(int[]-equivalent).
        var pnBytes = SerializeWithProtobufNet(new ImmutableArrayEquivalentModel
        {
            FixedValues = new[] { 1, -1, int.MaxValue },
            Scores = new[] { 3.14, -1e10 },
            VarintValues = new[] { 0, 127, 128 },
        });
        SerializeOnePass(CreateArrayModel()).Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableSortedSet_OnePass_BytesEqualPn()
    {
        var model = new ImmutableSetModel { SortedInts = ImmutableSortedSet.Create(30, 10, 20) };
        SerializeOnePass(model).Should().Equal(SerializeWithProtobufNet(model));
    }

    [Fact]
    public void ImmutableSortedDictionary_OnePass_BytesEqualPn()
    {
        var model = new ImmutableDictionaryModel
        {
            SortedMap = ImmutableSortedDictionary<int, string>.Empty.Add(2, "b").Add(1, "a"),
        };
        SerializeOnePass(model).Should().Equal(SerializeWithProtobufNet(model));
    }

    // --- OnePass → pn value roundtrip (works regardless of byte order; covers hash types) ---

    [Fact]
    public void ImmutableListInt_OnePass_GP()
    {
        AssertListIntModel(DeserializeWithProtobufNet<ImmutableListIntModel>(SerializeOnePass(CreateListIntModel())));
    }

    [Fact]
    public void ImmutableListMixed_OnePass_GP()
    {
        AssertMixedModel(DeserializeWithProtobufNet<ImmutableListMixedModel>(SerializeOnePass(CreateMixedModel())));
    }

    [Fact]
    public void ImmutableSet_OnePass_GP()
    {
        // Hash-ordered sets: byte order is non-deterministic, so verify pn reads OnePass content back.
        AssertSetModel(DeserializeWithProtobufNet<ImmutableSetModel>(SerializeOnePass(CreateSetModel())));
    }

    [Fact]
    public void ImmutableDictionary_OnePass_GP()
    {
        // Hash-ordered dictionary: verify pn reads OnePass content back.
        AssertDictionaryModel(DeserializeWithProtobufNet<ImmutableDictionaryModel>(SerializeOnePass(CreateDictionaryModel())));
    }

    // --- OnePass output → gp readers (direct GG-via-OnePass roundtrip, span + stream) ---
    // NB: there is no "pn → OnePass" flow — OnePass is a writer; reading is always via Span/Stream.

    [Fact]
    public void ImmutableListInt_OnePass_GG_Span()
    {
        AssertListIntModel(DeserializeWithGProtobuf(SerializeOnePass(CreateListIntModel()),
            bytes => TestModelDeserializers.DeserializeImmutableListIntModel(bytes)));
    }

    [Fact]
    public void ImmutableListInt_OnePass_GG_Stream()
    {
        AssertListIntModel(DeserializeWithGProtobufStreamFromBytes(SerializeOnePass(CreateListIntModel()),
            stream => TestModelDeserializers.DeserializeImmutableListIntModel(stream)));
    }

    [Fact]
    public void ImmutableSet_OnePass_GG_Stream()
    {
        AssertSetModel(DeserializeWithGProtobufStreamFromBytes(SerializeOnePass(CreateSetModel()),
            stream => TestModelDeserializers.DeserializeImmutableSetModel(stream)));
    }

    [Fact]
    public void ImmutableDictionary_OnePass_GG_Stream()
    {
        AssertDictionaryModel(DeserializeWithGProtobufStreamFromBytes(SerializeOnePass(CreateDictionaryModel()),
            stream => TestModelDeserializers.DeserializeImmutableDictionaryModel(stream)));
    }

    // --- Closest thing to "pn ↔ OnePass": pn-sourced data → gp read → OnePass re-write == pn bytes.
    //     Proves OnePass writes pn-faithful output even on data that originated from the oracle
    //     (catches any read→write asymmetry in the primary flow). Deterministic, non-negative data.

    [Fact]
    public void ImmutableListInt_PnSourced_OnePassRewrite_BytesEqualPn()
    {
        var model = new ImmutableListIntModel
        {
            Values = ImmutableList.Create(1, 2, int.MaxValue),
            PackedValues = ImmutableList.Create(10, 20, 30),
        };
        var pnBytes = SerializeWithProtobufNet(model);
        var gpRead = DeserializeWithGProtobuf(pnBytes,
            bytes => TestModelDeserializers.DeserializeImmutableListIntModel(bytes));
        SerializeOnePass(gpRead).Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableListMixed_PnSourced_OnePassRewrite_BytesEqualPn()
    {
        var pnBytes = SerializeWithProtobufNet(CreateMixedModel());
        var gpRead = DeserializeWithGProtobuf(pnBytes,
            bytes => TestModelDeserializers.DeserializeImmutableListMixedModel(bytes));
        SerializeOnePass(gpRead).Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableListNested_PnSourced_OnePassRewrite_BytesEqualPn()
    {
        var pnBytes = SerializeWithProtobufNet(CreateNestedModel());
        var gpRead = DeserializeWithGProtobuf(pnBytes,
            bytes => TestModelDeserializers.DeserializeImmutableListNestedModel(bytes));
        SerializeOnePass(gpRead).Should().Equal(pnBytes);
    }

    [Fact]
    public void ImmutableSortedDictionary_PnSourced_OnePassRewrite_BytesEqualPn()
    {
        var model = new ImmutableDictionaryModel
        {
            SortedMap = ImmutableSortedDictionary<int, string>.Empty.Add(2, "b").Add(1, "a"),
        };
        var pnBytes = SerializeWithProtobufNet(model);
        var gpRead = DeserializeWithGProtobuf(pnBytes,
            bytes => TestModelDeserializers.DeserializeImmutableDictionaryModel(bytes));
        SerializeOnePass(gpRead).Should().Equal(pnBytes);
    }

    #endregion

    #region Merge into existing instance (existingInstance overload) — F1 pre-seed for sets/dicts/array

    // Stream-reader merge (F1): seeds the temp list / dictionary from the existing instance, so
    // deserializing into a pre-populated immutable member appends/unions instead of dropping.
    // (SerializeOnePass just produces the input wire here — equal to 2-pass bytes.)
    [Fact]
    public void ImmutableHashSet_Merge_Stream_Unions()
    {
        var wire = SerializeOnePass(new ImmutableSetModel { Ints = ImmutableHashSet.Create(2, 3) });
        using var ms = new MemoryStream(wire);
        var r = TestModelDeserializers.DeserializeImmutableSetModel(ms, new ImmutableSetModel { Ints = ImmutableHashSet.Create(1, 2) });
        r.Ints.Should().BeEquivalentTo(new[] { 1, 2, 3 }); // dedup at freeze
    }

    [Fact]
    public void ImmutableSortedSet_Merge_Stream_Unions()
    {
        var wire = SerializeOnePass(new ImmutableSetModel { SortedInts = ImmutableSortedSet.Create(30, 10) });
        using var ms = new MemoryStream(wire);
        var r = TestModelDeserializers.DeserializeImmutableSetModel(ms, new ImmutableSetModel { SortedInts = ImmutableSortedSet.Create(20, 10) });
        r.SortedInts.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void ImmutableDictionary_Merge_Stream_AddsAndOverwrites()
    {
        var wire = SerializeOnePass(new ImmutableDictionaryModel { Map = ImmutableDictionary<int, string>.Empty.Add(2, "b").Add(1, "z") });
        using var ms = new MemoryStream(wire);
        var r = TestModelDeserializers.DeserializeImmutableDictionaryModel(ms,
            new ImmutableDictionaryModel { Map = ImmutableDictionary<int, string>.Empty.Add(1, "a") });
        r.Map.Should().HaveCount(2);
        r.Map[1].Should().Be("z"); // SetItem overwrites the existing key
        r.Map[2].Should().Be("b"); // and adds the new one
    }

    [Fact]
    public void ImmutableDictionary_Merge_Span_AddsAndOverwrites()
    {
        // The dictionary SetItem accumulation runs identically in the span reader.
        var wire = SerializeOnePass(new ImmutableDictionaryModel { Map = ImmutableDictionary<int, string>.Empty.Add(2, "b").Add(1, "z") });
        var r = TestModelDeserializers.DeserializeImmutableDictionaryModel(wire,
            new ImmutableDictionaryModel { Map = ImmutableDictionary<int, string>.Empty.Add(1, "a") });
        r.Map[1].Should().Be("z");
        r.Map[2].Should().Be("b");
    }

    [Fact]
    public void ImmutableArray_PackedFixed_Merge_Stream_Appends()
    {
        var wire = SerializeOnePass(new ImmutableArrayModel { FixedValues = ImmutableArray.Create(3, 4) });
        using var ms = new MemoryStream(wire);
        var r = TestModelDeserializers.DeserializeImmutableArrayModel(ms, new ImmutableArrayModel { FixedValues = ImmutableArray.Create(1, 2) });
        r.FixedValues.Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void ImmutableArray_PackedFixed_Merge_FromDefault_Stream()
    {
        // Existing member is default(ImmutableArray) → seed is empty → wire items only.
        var wire = SerializeOnePass(new ImmutableArrayModel { FixedValues = ImmutableArray.Create(3, 4) });
        using var ms = new MemoryStream(wire);
        var r = TestModelDeserializers.DeserializeImmutableArrayModel(ms, new ImmutableArrayModel());
        r.FixedValues.Should().Equal(3, 4);
    }

    #endregion

    #region Packing × DataFormat (FixedSize / ZigZag) — verifies the guide's Series example

    private static ImmutableSeriesModel CreateSeries() => new()
    {
        Ticks = ImmutableArray.Create(1L, -2L, long.MaxValue, long.MinValue),
        Deltas = ImmutableList.Create(1, -1, 2, -2),
        ZigZagLongs = ImmutableArray.Create(0L, -1L, long.MinValue),
    };

    private static void AssertSeries(ImmutableSeriesModel d)
    {
        d.Should().NotBeNull();
        d.Ticks.Should().Equal(1L, -2L, long.MaxValue, long.MinValue);
        d.Deltas.Should().Equal(1, -1, 2, -2);
        d.ZigZagLongs.Should().Equal(0L, -1L, long.MinValue);
    }

    [Fact]
    public void ImmutableSeries_GG()
        => AssertSeries(DeserializeWithGProtobuf(SerializeWithGProtobuf(CreateSeries(), TestModelSerializers.Serialize),
            bytes => TestModelDeserializers.DeserializeImmutableSeriesModel(bytes)));

    [Fact]
    public void ImmutableSeries_GG_Stream()
        => AssertSeries(DeserializeWithGProtobufStreamFromBytes(SerializeWithGProtobuf(CreateSeries(), TestModelSerializers.Serialize),
            stream => TestModelDeserializers.DeserializeImmutableSeriesModel(stream)));

    [Fact]
    public void ImmutableSeries_OnePass_ByteEqualTo2Pass()
        => SerializeOnePass(CreateSeries()).Should().Equal(SerializeWithGProtobuf(CreateSeries(), TestModelSerializers.Serialize));

    [Fact]
    public void ImmutableSeries_OnePass_GG_Stream()
        => AssertSeries(DeserializeWithGProtobufStreamFromBytes(SerializeOnePass(CreateSeries()),
            stream => TestModelDeserializers.DeserializeImmutableSeriesModel(stream)));

    [Fact]
    public void ImmutableSeries_BytesEqualPn()
    {
        // FixedSize + ZigZag are deterministic; gp must match the protobuf-net oracle byte-for-byte.
        var model = CreateSeries();
        SerializeWithGProtobuf(model, TestModelSerializers.Serialize).Should().Equal(SerializeWithProtobufNet(model));
    }

    #endregion
}
