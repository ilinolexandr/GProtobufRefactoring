using FluentAssertions;
using GProtobuf.CrossTests.TestModel;
using System.Collections.Generic;
using Xunit;
using TestModelSerializers = GProtobuf.CrossTests.TestModel.Serialization.Serializers;
using TestModelDeserializers = GProtobuf.CrossTests.TestModel.Serialization.Deserializers;

namespace GProtobuf.Tests;

/// <summary>
/// Tests for deserialization of types that use C# init-only properties.
/// Verifies that the deferred-construction code path correctly reads members into local
/// temp variables and constructs the instance via an object initializer expression.
/// </summary>
public sealed class InitPropertyTests : BaseSerializationTest
{
    #region InitOnlyPrimitivesModel

    private static InitOnlyPrimitivesModel CreatePrimitivesModel() => new()
    {
        IntValue = 42,
        LongValue = 12345678901234L,
        DoubleValue = 3.14159,
        BoolValue = true,
        StringValue = "init-only string",
        BytesValue = new byte[] { 0x01, 0x02, 0x03, 0x04 },
    };

    private static void AssertPrimitivesModel(InitOnlyPrimitivesModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.IntValue.Should().Be(42);
        deserialized.LongValue.Should().Be(12345678901234L);
        deserialized.DoubleValue.Should().Be(3.14159);
        deserialized.BoolValue.Should().BeTrue();
        deserialized.StringValue.Should().Be("init-only string");
        deserialized.BytesValue.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03, 0x04 });
    }

    [Fact]
    public void InitOnlyPrimitives_PG()
    {
        var model = CreatePrimitivesModel();
        var data = SerializeWithProtobufNet(model);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeInitOnlyPrimitivesModel(bytes));
        AssertPrimitivesModel(deserialized);
    }

    [Fact]
    public void InitOnlyPrimitives_GG()
    {
        var model = CreatePrimitivesModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeInitOnlyPrimitivesModel(bytes));
        AssertPrimitivesModel(deserialized);
    }

    [Fact]
    public void InitOnlyPrimitives_GP()
    {
        var model = CreatePrimitivesModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithProtobufNet<InitOnlyPrimitivesModel>(data);
        AssertPrimitivesModel(deserialized);
    }

    [Fact]
    public void InitOnlyPrimitives_GG_Stream()
    {
        var model = CreatePrimitivesModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeInitOnlyPrimitivesModel(stream));
        AssertPrimitivesModel(deserialized);
    }

    [Fact]
    public void InitOnlyPrimitives_PG_Stream()
    {
        var model = CreatePrimitivesModel();
        var data = SerializeWithProtobufNet(model);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeInitOnlyPrimitivesModel(stream));
        AssertPrimitivesModel(deserialized);
    }

    #endregion

    #region MixedInitAndSetModel

    private static MixedInitAndSetModel CreateMixedModel() => new()
    {
        Id = 7,
        Name = "mixed-init",
        Counter = 99,
        Flag = true,
    };

    private static void AssertMixedModel(MixedInitAndSetModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Id.Should().Be(7);
        deserialized.Name.Should().Be("mixed-init");
        deserialized.Counter.Should().Be(99);
        deserialized.Flag.Should().BeTrue();
    }

    [Fact]
    public void MixedInitAndSet_PG()
    {
        var model = CreateMixedModel();
        var data = SerializeWithProtobufNet(model);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeMixedInitAndSetModel(bytes));
        AssertMixedModel(deserialized);
    }

    [Fact]
    public void MixedInitAndSet_GG()
    {
        var model = CreateMixedModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeMixedInitAndSetModel(bytes));
        AssertMixedModel(deserialized);
    }

    [Fact]
    public void MixedInitAndSet_GP()
    {
        var model = CreateMixedModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithProtobufNet<MixedInitAndSetModel>(data);
        AssertMixedModel(deserialized);
    }

    #endregion

    #region InitWithNestedModel

    private static InitWithNestedModel CreateNestedModel() => new()
    {
        Tag = 99,
        Inner = new InitOnlyPrimitivesModel
        {
            IntValue = 1,
            LongValue = 2L,
            DoubleValue = 0.5,
            BoolValue = false,
            StringValue = "nested",
            BytesValue = new byte[] { 0xAA },
        },
    };

    private static void AssertNestedModel(InitWithNestedModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Tag.Should().Be(99);
        deserialized.Inner.Should().NotBeNull();
        deserialized.Inner.IntValue.Should().Be(1);
        deserialized.Inner.LongValue.Should().Be(2L);
        deserialized.Inner.DoubleValue.Should().Be(0.5);
        deserialized.Inner.BoolValue.Should().BeFalse();
        deserialized.Inner.StringValue.Should().Be("nested");
        deserialized.Inner.BytesValue.Should().BeEquivalentTo(new byte[] { 0xAA });
    }

    [Fact]
    public void InitWithNested_PG()
    {
        var model = CreateNestedModel();
        var data = SerializeWithProtobufNet(model);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeInitWithNestedModel(bytes));
        AssertNestedModel(deserialized);
    }

    [Fact]
    public void InitWithNested_GG()
    {
        var model = CreateNestedModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeInitWithNestedModel(bytes));
        AssertNestedModel(deserialized);
    }

    [Fact]
    public void InitWithNested_GP()
    {
        var model = CreateNestedModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithProtobufNet<InitWithNestedModel>(data);
        AssertNestedModel(deserialized);
    }

    #endregion

    #region InitWithCollectionModel

    private static InitWithCollectionModel CreateCollectionModel() => new()
    {
        Items = new List<int> { 10, 20, 30, 40 },
        Label = "with-collection",
    };

    private static void AssertCollectionModel(InitWithCollectionModel deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Label.Should().Be("with-collection");
        deserialized.Items.Should().BeEquivalentTo(new[] { 10, 20, 30, 40 });
    }

    [Fact]
    public void InitWithCollection_PG()
    {
        var model = CreateCollectionModel();
        var data = SerializeWithProtobufNet(model);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeInitWithCollectionModel(bytes));
        AssertCollectionModel(deserialized);
    }

    [Fact]
    public void InitWithCollection_GG()
    {
        var model = CreateCollectionModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeInitWithCollectionModel(bytes));
        AssertCollectionModel(deserialized);
    }

    [Fact]
    public void InitWithCollection_GP()
    {
        var model = CreateCollectionModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithProtobufNet<InitWithCollectionModel>(data);
        AssertCollectionModel(deserialized);
    }

    [Fact]
    public void InitWithCollection_GG_Stream()
    {
        var model = CreateCollectionModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeInitWithCollectionModel(stream));
        AssertCollectionModel(deserialized);
    }

    [Fact]
    public void InitWithNested_GG_Stream()
    {
        var model = CreateNestedModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeInitWithNestedModel(stream));
        AssertNestedModel(deserialized);
    }

    [Fact]
    public void MixedInitAndSet_GG_Stream()
    {
        var model = CreateMixedModel();
        var data = SerializeWithGProtobuf(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeMixedInitAndSetModel(stream));
        AssertMixedModel(deserialized);
    }

    #endregion

    #region BaseInitDerivedNormal (init in base, regular setters in derived)

    private static BaseInitDerivedNormalChild CreateBaseInitDerivedNormalModel() => new()
    {
        BaseId = 11,
        BaseName = "base-init",
        ChildLabel = "child-label",
        ChildCount = 99,
    };

    private static void AssertBaseInitDerivedNormalModel(BaseInitDerivedNormalRoot deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Should().BeOfType<BaseInitDerivedNormalChild>();
        var derived = (BaseInitDerivedNormalChild)deserialized;
        derived.BaseId.Should().Be(11);
        derived.BaseName.Should().Be("base-init");
        derived.ChildLabel.Should().Be("child-label");
        derived.ChildCount.Should().Be(99);
    }

    [Fact]
    public void BaseInitDerivedNormal_PG()
    {
        var model = CreateBaseInitDerivedNormalModel();
        var data = SerializeWithProtobufNet<BaseInitDerivedNormalRoot>(model);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeBaseInitDerivedNormalRoot(bytes));
        AssertBaseInitDerivedNormalModel(deserialized);
    }

    [Fact]
    public void BaseInitDerivedNormal_GG()
    {
        var model = CreateBaseInitDerivedNormalModel();
        var data = SerializeWithGProtobuf<BaseInitDerivedNormalRoot>(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeBaseInitDerivedNormalRoot(bytes));
        AssertBaseInitDerivedNormalModel(deserialized);
    }

    [Fact]
    public void BaseInitDerivedNormal_GP()
    {
        var model = CreateBaseInitDerivedNormalModel();
        var data = SerializeWithGProtobuf<BaseInitDerivedNormalRoot>(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithProtobufNet<BaseInitDerivedNormalRoot>(data);
        AssertBaseInitDerivedNormalModel(deserialized);
    }

    [Fact]
    public void BaseInitDerivedNormal_GG_Stream()
    {
        var model = CreateBaseInitDerivedNormalModel();
        var data = SerializeWithGProtobuf<BaseInitDerivedNormalRoot>(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeBaseInitDerivedNormalRoot(stream));
        AssertBaseInitDerivedNormalModel(deserialized);
    }

    [Fact]
    public void BaseInitDerivedNormal_PG_Stream()
    {
        var model = CreateBaseInitDerivedNormalModel();
        var data = SerializeWithProtobufNet<BaseInitDerivedNormalRoot>(model);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeBaseInitDerivedNormalRoot(stream));
        AssertBaseInitDerivedNormalModel(deserialized);
    }

    #endregion

    #region BaseNormalDerivedInit (regular setters in base, init in derived)

    private static BaseNormalDerivedInitChild CreateBaseNormalDerivedInitModel() => new()
    {
        BaseId = 22,
        BaseName = "base-normal",
        ChildLabel = "child-init",
        ChildCount = 77,
    };

    private static void AssertBaseNormalDerivedInitModel(BaseNormalDerivedInitRoot deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Should().BeOfType<BaseNormalDerivedInitChild>();
        var derived = (BaseNormalDerivedInitChild)deserialized;
        derived.BaseId.Should().Be(22);
        derived.BaseName.Should().Be("base-normal");
        derived.ChildLabel.Should().Be("child-init");
        derived.ChildCount.Should().Be(77);
    }

    [Fact]
    public void BaseNormalDerivedInit_PG()
    {
        var model = CreateBaseNormalDerivedInitModel();
        var data = SerializeWithProtobufNet<BaseNormalDerivedInitRoot>(model);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeBaseNormalDerivedInitRoot(bytes));
        AssertBaseNormalDerivedInitModel(deserialized);
    }

    [Fact]
    public void BaseNormalDerivedInit_GG()
    {
        var model = CreateBaseNormalDerivedInitModel();
        var data = SerializeWithGProtobuf<BaseNormalDerivedInitRoot>(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeBaseNormalDerivedInitRoot(bytes));
        AssertBaseNormalDerivedInitModel(deserialized);
    }

    [Fact]
    public void BaseNormalDerivedInit_GP()
    {
        var model = CreateBaseNormalDerivedInitModel();
        var data = SerializeWithGProtobuf<BaseNormalDerivedInitRoot>(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithProtobufNet<BaseNormalDerivedInitRoot>(data);
        AssertBaseNormalDerivedInitModel(deserialized);
    }

    [Fact]
    public void BaseNormalDerivedInit_GG_Stream()
    {
        var model = CreateBaseNormalDerivedInitModel();
        var data = SerializeWithGProtobuf<BaseNormalDerivedInitRoot>(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeBaseNormalDerivedInitRoot(stream));
        AssertBaseNormalDerivedInitModel(deserialized);
    }

    [Fact]
    public void BaseNormalDerivedInit_PG_Stream()
    {
        var model = CreateBaseNormalDerivedInitModel();
        var data = SerializeWithProtobufNet<BaseNormalDerivedInitRoot>(model);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeBaseNormalDerivedInitRoot(stream));
        AssertBaseNormalDerivedInitModel(deserialized);
    }

    #endregion

    #region BaseInitDerivedInit (init in both base and derived)

    private static BaseInitDerivedInitChild CreateBaseInitDerivedInitModel() => new()
    {
        BaseId = 33,
        BaseName = "base-init-both",
        ChildLabel = "child-init-both",
        ChildCount = 55,
    };

    private static void AssertBaseInitDerivedInitModel(BaseInitDerivedInitRoot deserialized)
    {
        deserialized.Should().NotBeNull();
        deserialized.Should().BeOfType<BaseInitDerivedInitChild>();
        var derived = (BaseInitDerivedInitChild)deserialized;
        derived.BaseId.Should().Be(33);
        derived.BaseName.Should().Be("base-init-both");
        derived.ChildLabel.Should().Be("child-init-both");
        derived.ChildCount.Should().Be(55);
    }

    [Fact]
    public void BaseInitDerivedInit_PG()
    {
        var model = CreateBaseInitDerivedInitModel();
        var data = SerializeWithProtobufNet<BaseInitDerivedInitRoot>(model);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeBaseInitDerivedInitRoot(bytes));
        AssertBaseInitDerivedInitModel(deserialized);
    }

    [Fact]
    public void BaseInitDerivedInit_GG()
    {
        var model = CreateBaseInitDerivedInitModel();
        var data = SerializeWithGProtobuf<BaseInitDerivedInitRoot>(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeBaseInitDerivedInitRoot(bytes));
        AssertBaseInitDerivedInitModel(deserialized);
    }

    [Fact]
    public void BaseInitDerivedInit_GP()
    {
        var model = CreateBaseInitDerivedInitModel();
        var data = SerializeWithGProtobuf<BaseInitDerivedInitRoot>(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithProtobufNet<BaseInitDerivedInitRoot>(data);
        AssertBaseInitDerivedInitModel(deserialized);
    }

    [Fact]
    public void BaseInitDerivedInit_GG_Stream()
    {
        var model = CreateBaseInitDerivedInitModel();
        var data = SerializeWithGProtobuf<BaseInitDerivedInitRoot>(model, TestModelSerializers.Serialize);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeBaseInitDerivedInitRoot(stream));
        AssertBaseInitDerivedInitModel(deserialized);
    }

    [Fact]
    public void BaseInitDerivedInit_PG_Stream()
    {
        var model = CreateBaseInitDerivedInitModel();
        var data = SerializeWithProtobufNet<BaseInitDerivedInitRoot>(model);
        var deserialized = DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeBaseInitDerivedInitRoot(stream));
        AssertBaseInitDerivedInitModel(deserialized);
    }

    #endregion

}
