using FluentAssertions;
using GProtobuf.CrossTests.TestModel;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;
using TestModelSerializers = GProtobuf.CrossTests.TestModel.Serialization.Serializers;
using TestModelDeserializers = GProtobuf.CrossTests.TestModel.Serialization.Deserializers;

namespace GProtobuf.Tests;

/// <summary>
/// ImmutableQueue&lt;T&gt;/ImmutableStack&lt;T&gt; (+ IImmutableQueue/IImmutableStack) support.
///
/// Oracle = protobuf-net 3.2.46 (the version CrossTests references), which fully supports these
/// types — verified by throwaway probe: ImmutableQueue is byte-identical to List (FIFO); ImmutableStack
/// serializes top-first and round-trips ORDER-STABLY. (pn 2.3.7 throws "No serializer defined" for
/// both, so these are 3.x-oracle-only — no 2.3.7 path exists.)
///
/// Correctness criterion (project rule): gp == pn in BOTH values and bytes. The Stack tests are the
/// load-bearing ones: they pin the top-first enumeration AND byte order against pn so a future
/// regression in the freeze (reverse-before-CreateRange) is caught.
/// </summary>
public sealed class ImmutableQueueStackTests : BaseSerializationTest
{
    #region ImmutableQueue<int> (FIFO)

    private static ImmutableQueueIntModel CreateQueueIntModel() => new()
    {
        Values = ImmutableQueue.CreateRange(new[] { 1, 2, 3 }),       // enumerates 1,2,3
        PackedValues = ImmutableQueue.CreateRange(new[] { 10, 20, 30 }),
    };

    private static void AssertQueueIntModel(ImmutableQueueIntModel m)
    {
        m.Should().NotBeNull();
        m.Values.Should().Equal(1, 2, 3);
        m.PackedValues.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void ImmutableQueueInt_GG()
    {
        var data = SerializeWithGProtobuf(CreateQueueIntModel(), TestModelSerializers.Serialize);
        AssertQueueIntModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableQueueIntModel(bytes)));
    }

    [Fact]
    public void ImmutableQueueInt_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateQueueIntModel(), TestModelSerializers.Serialize);
        AssertQueueIntModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableQueueIntModel(stream)));
    }

    [Fact]
    public void ImmutableQueueInt_PG()
    {
        var data = SerializeWithProtobufNet(CreateQueueIntModel());
        AssertQueueIntModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableQueueIntModel(bytes)));
    }

    [Fact]
    public void ImmutableQueueInt_PG_Stream()
    {
        var data = SerializeWithProtobufNet(CreateQueueIntModel());
        AssertQueueIntModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableQueueIntModel(stream)));
    }

    [Fact]
    public void ImmutableQueueInt_GP()
    {
        var data = SerializeWithGProtobuf(CreateQueueIntModel(), TestModelSerializers.Serialize);
        AssertQueueIntModel(DeserializeWithProtobufNet<ImmutableQueueIntModel>(data));
    }

    [Fact]
    public void ImmutableQueueInt_BytesEqualPn()
    {
        var model = CreateQueueIntModel();
        var gp = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pn = SerializeWithProtobufNet(model);
        gp.Should().Equal(pn);
    }

    [Fact]
    public void ImmutableQueueInt_OnePass_ByteEqualTo2Pass()
    {
        var model = CreateQueueIntModel();
        var twoPass = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        using var ms = new MemoryStream();
        TestModelSerializers.SerializeOnePass(ms, model);
        ms.ToArray().Should().Equal(twoPass);
    }

    [Fact]
    public void ImmutableQueueInt_OnePass_BytesEqualPn()
    {
        var model = CreateQueueIntModel();
        using var ms = new MemoryStream();
        TestModelSerializers.SerializeOnePass(ms, model);
        ms.ToArray().Should().Equal(SerializeWithProtobufNet(model));
    }

    [Fact]
    public void ImmutableQueueInt_NullStaysNull_GG()
    {
        var data = SerializeWithGProtobuf(new ImmutableQueueIntModel(), TestModelSerializers.Serialize);
        var m = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableQueueIntModel(bytes));
        m.Values.Should().BeNull();
        m.PackedValues.Should().BeNull();
    }

    #endregion

    #region ImmutableStack<int> (LIFO — the ordering pin)

    // CreateRange([1,2,3]) pushes 1,2,3 → top=3 → enumerates 3,2,1.
    private static ImmutableStackIntModel CreateStackIntModel() => new()
    {
        Values = ImmutableStack.CreateRange(new[] { 1, 2, 3 }),
        PackedValues = ImmutableStack.CreateRange(new[] { 10, 20, 30 }),
    };

    private static void AssertStackIntModel(ImmutableStackIntModel m)
    {
        m.Should().NotBeNull();
        // Order-stable round-trip: top-first enumeration preserved exactly as pn does it.
        m.Values.Should().Equal(3, 2, 1);
        m.PackedValues.Should().Equal(30, 20, 10);
    }

    [Fact]
    public void ImmutableStackInt_GG()
    {
        var data = SerializeWithGProtobuf(CreateStackIntModel(), TestModelSerializers.Serialize);
        AssertStackIntModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableStackIntModel(bytes)));
    }

    [Fact]
    public void ImmutableStackInt_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateStackIntModel(), TestModelSerializers.Serialize);
        AssertStackIntModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableStackIntModel(stream)));
    }

    [Fact]
    public void ImmutableStackInt_PG()
    {
        var data = SerializeWithProtobufNet(CreateStackIntModel());
        AssertStackIntModel(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableStackIntModel(bytes)));
    }

    [Fact]
    public void ImmutableStackInt_PG_Stream()
    {
        var data = SerializeWithProtobufNet(CreateStackIntModel());
        AssertStackIntModel(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableStackIntModel(stream)));
    }

    [Fact]
    public void ImmutableStackInt_GP()
    {
        // gp-write → pn-read: pn must see the same top-first sequence (3,2,1).
        var data = SerializeWithGProtobuf(CreateStackIntModel(), TestModelSerializers.Serialize);
        AssertStackIntModel(DeserializeWithProtobufNet<ImmutableStackIntModel>(data));
    }

    [Fact]
    public void ImmutableStackInt_BytesEqualPn()
    {
        // The wire must be top-first AND byte-identical to pn (08 03 08 02 08 01 for Values).
        var model = CreateStackIntModel();
        var gp = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var pn = SerializeWithProtobufNet(model);
        gp.Should().Equal(pn);
    }

    [Fact]
    public void ImmutableStackInt_OnePass_ByteEqualTo2Pass()
    {
        var model = CreateStackIntModel();
        var twoPass = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        using var ms = new MemoryStream();
        TestModelSerializers.SerializeOnePass(ms, model);
        ms.ToArray().Should().Equal(twoPass);
    }

    [Fact]
    public void ImmutableStackInt_OnePass_BytesEqualPn()
    {
        var model = CreateStackIntModel();
        using var ms = new MemoryStream();
        TestModelSerializers.SerializeOnePass(ms, model);
        ms.ToArray().Should().Equal(SerializeWithProtobufNet(model));
    }

    [Fact]
    public void ImmutableStackInt_RoundTripOrderStable_GG()
    {
        // Independent of the helper: build a stack with a known top, round-trip, assert order is held.
        var stack = ImmutableStack<int>.Empty.Push(1).Push(2).Push(3); // top=3 → enumerates 3,2,1
        stack.Should().Equal(3, 2, 1);
        var model = new ImmutableStackIntModel { Values = stack };
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var back = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableStackIntModel(bytes));
        back.Values.Should().Equal(3, 2, 1);
    }

    #endregion

    #region Interface forms (IImmutableQueue / IImmutableStack)

    [Fact]
    public void ImmutableQueueInterface_GG_and_BytesEqualPn()
    {
        var model = new ImmutableQueueInterfaceModel { Values = ImmutableQueue.CreateRange(new[] { 7, 8, 9 }) };
        var gp = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        gp.Should().Equal(SerializeWithProtobufNet(model));
        var back = DeserializeWithGProtobuf(gp,
            bytes => TestModelDeserializers.DeserializeImmutableQueueInterfaceModel(bytes));
        back.Values.Should().Equal(7, 8, 9);
    }

    [Fact]
    public void ImmutableQueueInterface_PG()
    {
        var model = new ImmutableQueueInterfaceModel { Values = ImmutableQueue.CreateRange(new[] { 7, 8, 9 }) };
        var pn = SerializeWithProtobufNet(model);
        var back = DeserializeWithGProtobuf(pn,
            bytes => TestModelDeserializers.DeserializeImmutableQueueInterfaceModel(bytes));
        back.Values.Should().Equal(7, 8, 9);
    }

    [Fact]
    public void ImmutableStackInterface_GG_and_BytesEqualPn()
    {
        var model = new ImmutableStackInterfaceModel { Values = ImmutableStack.CreateRange(new[] { 7, 8, 9 }) }; // enumerates 9,8,7
        var gp = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        gp.Should().Equal(SerializeWithProtobufNet(model));
        var back = DeserializeWithGProtobuf(gp,
            bytes => TestModelDeserializers.DeserializeImmutableStackInterfaceModel(bytes));
        back.Values.Should().Equal(9, 8, 7);
    }

    [Fact]
    public void ImmutableStackInterface_GP()
    {
        var model = new ImmutableStackInterfaceModel { Values = ImmutableStack.CreateRange(new[] { 7, 8, 9 }) };
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var back = DeserializeWithProtobufNet<ImmutableStackInterfaceModel>(data);
        back.Values.Should().Equal(9, 8, 7);
    }

    #endregion

    #region String element + nested-message element

    [Fact]
    public void ImmutableQueueString_GG_PG_GP_BytesEqualPn()
    {
        var model = new ImmutableQueueStringModel { Names = ImmutableQueue.CreateRange(new[] { "a", "b", "c" }) };
        var gp = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        gp.Should().Equal(SerializeWithProtobufNet(model));

        DeserializeWithGProtobuf(gp, b => TestModelDeserializers.DeserializeImmutableQueueStringModel(b))
            .Names.Should().Equal("a", "b", "c");
        DeserializeWithGProtobuf(SerializeWithProtobufNet(model), b => TestModelDeserializers.DeserializeImmutableQueueStringModel(b))
            .Names.Should().Equal("a", "b", "c");
        DeserializeWithProtobufNet<ImmutableQueueStringModel>(gp)
            .Names.Should().Equal("a", "b", "c");
    }

    private static ImmutableQueueNestedModel CreateNestedModel() => new()
    {
        Items = ImmutableQueue.CreateRange(new[]
        {
            new QueueElementMsg { K = 1, S = "a" },
            new QueueElementMsg { K = 2, S = "b" },
        }),
        Tag = 42,
    };

    private static void AssertNested(ImmutableQueueNestedModel m)
    {
        m.Should().NotBeNull();
        m.Tag.Should().Be(42);
        m.Items.Select(i => i.K).Should().Equal(1, 2);
        m.Items.Select(i => i.S).Should().Equal("a", "b");
    }

    [Fact]
    public void ImmutableQueueNested_GG()
    {
        var data = SerializeWithGProtobuf(CreateNestedModel(), TestModelSerializers.Serialize);
        AssertNested(DeserializeWithGProtobuf(data, b => TestModelDeserializers.DeserializeImmutableQueueNestedModel(b)));
    }

    [Fact]
    public void ImmutableQueueNested_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateNestedModel(), TestModelSerializers.Serialize);
        AssertNested(DeserializeWithGProtobufStreamFromBytes(data, s => TestModelDeserializers.DeserializeImmutableQueueNestedModel(s)));
    }

    [Fact]
    public void ImmutableQueueNested_PG()
    {
        var data = SerializeWithProtobufNet(CreateNestedModel());
        AssertNested(DeserializeWithGProtobuf(data, b => TestModelDeserializers.DeserializeImmutableQueueNestedModel(b)));
    }

    [Fact]
    public void ImmutableQueueNested_GP()
    {
        var data = SerializeWithGProtobuf(CreateNestedModel(), TestModelSerializers.Serialize);
        AssertNested(DeserializeWithProtobufNet<ImmutableQueueNestedModel>(data));
    }

    [Fact]
    public void ImmutableQueueNested_BytesEqualPn()
    {
        var model = CreateNestedModel();
        SerializeWithGProtobuf(model, TestModelSerializers.Serialize)
            .Should().Equal(SerializeWithProtobufNet(model));
    }

    [Fact]
    public void ImmutableQueueNested_OnePass_ByteEqualTo2Pass()
    {
        var model = CreateNestedModel();
        var twoPass = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        using var ms = new MemoryStream();
        TestModelSerializers.SerializeOnePass(ms, model);
        ms.ToArray().Should().Equal(twoPass);
    }

    #endregion

    #region OnePass — priority write flow, direct vs pn oracle

    // OnePass is the primary production write path, so each element category gets a DIRECT OnePass→pn
    // (GP-via-OnePass) value/order pin plus a OnePass==pn byte pin — not just OnePass==2-pass (which is
    // only transitively oracle-checked). Stack cases are the load-bearing ordering pins on this flow.

    private static byte[] OnePassQueueInt(ImmutableQueueIntModel m) { using var ms = new MemoryStream(); TestModelSerializers.SerializeOnePass(ms, m); return ms.ToArray(); }
    private static byte[] OnePassStackInt(ImmutableStackIntModel m) { using var ms = new MemoryStream(); TestModelSerializers.SerializeOnePass(ms, m); return ms.ToArray(); }
    private static byte[] OnePassQueueString(ImmutableQueueStringModel m) { using var ms = new MemoryStream(); TestModelSerializers.SerializeOnePass(ms, m); return ms.ToArray(); }
    private static byte[] OnePassNested(ImmutableQueueNestedModel m) { using var ms = new MemoryStream(); TestModelSerializers.SerializeOnePass(ms, m); return ms.ToArray(); }
    private static byte[] OnePassStackIface(ImmutableStackInterfaceModel m) { using var ms = new MemoryStream(); TestModelSerializers.SerializeOnePass(ms, m); return ms.ToArray(); }

    [Fact]
    public void ImmutableQueueInt_OnePass_GP()
    {
        AssertQueueIntModel(DeserializeWithProtobufNet<ImmutableQueueIntModel>(OnePassQueueInt(CreateQueueIntModel())));
    }

    [Fact]
    public void ImmutableStackInt_OnePass_GP()
    {
        // OnePass write → pn read: pn must see the top-first sequence (3,2,1 / 30,20,10).
        AssertStackIntModel(DeserializeWithProtobufNet<ImmutableStackIntModel>(OnePassStackInt(CreateStackIntModel())));
    }

    [Fact]
    public void ImmutableQueueString_OnePass_ByteEqualTo2Pass_And_GP()
    {
        var model = new ImmutableQueueStringModel { Names = ImmutableQueue.CreateRange(new[] { "a", "b", "c" }) };
        OnePassQueueString(model).Should().Equal(SerializeWithGProtobuf(model, TestModelSerializers.Serialize));
        OnePassQueueString(model).Should().Equal(SerializeWithProtobufNet(model));
        DeserializeWithProtobufNet<ImmutableQueueStringModel>(OnePassQueueString(model)).Names.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void ImmutableQueueNested_OnePass_BytesEqualPn_And_GP()
    {
        var model = CreateNestedModel();
        OnePassNested(model).Should().Equal(SerializeWithProtobufNet(model));
        AssertNested(DeserializeWithProtobufNet<ImmutableQueueNestedModel>(OnePassNested(model)));
    }

    [Fact]
    public void ImmutableStackInterface_OnePass_ByteEqualTo2Pass_And_GP()
    {
        var model = new ImmutableStackInterfaceModel { Values = ImmutableStack.CreateRange(new[] { 7, 8, 9 }) }; // enumerates 9,8,7
        OnePassStackIface(model).Should().Equal(SerializeWithGProtobuf(model, TestModelSerializers.Serialize));
        OnePassStackIface(model).Should().Equal(SerializeWithProtobufNet(model));
        DeserializeWithProtobufNet<ImmutableStackInterfaceModel>(OnePassStackIface(model)).Values.Should().Equal(9, 8, 7);
    }

    [Fact]
    public void ImmutableQueue_StreamReadsPackedPayload_LikePn()
    {
        // pn writes Queue/Stack non-packed but READS a packed payload fine (dual-mode). gp's STREAM reader
        // matches that. field 1 packed = tag 0x0A, len 0x03, payload 01 02 03.
        var packed = new byte[] { 0x0A, 0x03, 0x01, 0x02, 0x03 };
        DeserializeWithProtobufNet<ImmutableQueueInterfaceModel>(packed).Values.Should().Equal(1, 2, 3); // pn baseline
        DeserializeWithGProtobufStreamFromBytes(packed, s => TestModelDeserializers.DeserializeImmutableQueueInterfaceModel(s))
            .Values.Should().Equal(1, 2, 3);
    }

    [Fact(Skip = "F3 (pre-existing, span-reader): a non-packed-declared primitive field has no WireType.Len " +
                 "branch in the span reader, so packed bytes throw 'Buffer overrun'. Shared with List<int>/int[], " +
                 "not Queue/Stack-specific. pn + gp-stream both accept packed; un-skip when F3 is fixed (span dual-mode).")]
    public void ImmutableQueue_SpanReadsPackedPayload_LikePn()
    {
        var packed = new byte[] { 0x0A, 0x03, 0x01, 0x02, 0x03 };
        DeserializeWithGProtobuf(packed, b => TestModelDeserializers.DeserializeImmutableQueueInterfaceModel(b))
            .Values.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void ImmutableQueueInt_OnePass_NullAndEmpty_ByteEqualTo2Pass()
    {
        // null → nothing; empty → nothing (non-packed empty emits no entries). OnePass must match 2-pass.
        var nullModel = new ImmutableQueueIntModel();
        OnePassQueueInt(nullModel).Should().Equal(SerializeWithGProtobuf(nullModel, TestModelSerializers.Serialize));

        var emptyModel = new ImmutableQueueIntModel { Values = ImmutableQueue<int>.Empty, PackedValues = ImmutableQueue<int>.Empty };
        OnePassQueueInt(emptyModel).Should().Equal(SerializeWithGProtobuf(emptyModel, TestModelSerializers.Serialize));
    }

    #endregion
}
