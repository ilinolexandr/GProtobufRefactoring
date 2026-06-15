using FluentAssertions;
using GProtobuf.CrossTests.TestModel;
using System;
using System.Collections.Immutable;
using System.IO;
using Xunit;
using TestModelSerializers = GProtobuf.CrossTests.TestModel.Serialization.Serializers;
using TestModelDeserializers = GProtobuf.CrossTests.TestModel.Serialization.Deserializers;

namespace GProtobuf.Tests;

/// <summary>
/// Integration tests for the immutable rework of the production ImmutableValueSet shape:
/// init-only members + ImmutableArray&lt;message&gt; + enum-keyed ImmutableDictionary with
/// array-of-message values + ImmutableList of nested immutable objects + DateTime/TimeSpan.
/// </summary>
public sealed class ImmutableValuesImmutableTests : BaseSerializationTest
{
    private static readonly DateTime T0 = new(2026, 6, 12, 10, 30, 0, DateTimeKind.Utc);

    private static ImmutableValueSet CreateSet() => new()
    {
        ProviderId = "provider-7",
        ValueKind = "temperature",
        Unit = "°C",
        Granularity = TimeSpan.FromMinutes(15),
        Segments = ImmutableList.Create(
            new ImmutableValuesSegment
            {
                FirstValueStartTime = T0,
                CurrentValues = ImmutableArray.Create(
                    new ImmutableDataType { Value = 215, Scale = 1 },
                    new ImmutableDataType { Value = 221, Scale = 1 }),
                StatisticValues = ImmutableDictionary<DeviceValueEnumFunction, ImmutableDataType[]>.Empty
                    .Add(DeviceValueEnumFunction.Min, new[] { new ImmutableDataType { Value = 198, Scale = 1 } })
                    .Add(DeviceValueEnumFunction.Max, new[]
                    {
                        new ImmutableDataType { Value = 240, Scale = 1 },
                        new ImmutableDataType { Value = 245, Scale = 1 },
                    }),
            },
            new ImmutableValuesSegment
            {
                FirstValueStartTime = T0.AddHours(1),
                CurrentValues = ImmutableArray<ImmutableDataType>.Empty,
                StatisticValues = null,
            }),
    };

    private static void AssertSet(ImmutableValueSet d)
    {
        d.Should().NotBeNull();
        d.ProviderId.Should().Be("provider-7");
        d.ValueKind.Should().Be("temperature");
        d.Unit.Should().Be("°C");
        d.Granularity.Should().Be(TimeSpan.FromMinutes(15));

        d.Segments.Should().HaveCount(2);

        var s0 = d.Segments[0];
        s0.FirstValueStartTime.Should().Be(T0);
        s0.CurrentValues.IsDefault.Should().BeFalse();
        s0.CurrentValues.Should().HaveCount(2);
        s0.CurrentValues[0].Value.Should().Be(215);
        s0.CurrentValues[1].Value.Should().Be(221);
        s0.StatisticValues.Should().HaveCount(2);
        s0.StatisticValues[DeviceValueEnumFunction.Min].Should().HaveCount(1);
        s0.StatisticValues[DeviceValueEnumFunction.Min][0].Value.Should().Be(198);
        s0.StatisticValues[DeviceValueEnumFunction.Max].Should().HaveCount(2);
        s0.StatisticValues[DeviceValueEnumFunction.Max][1].Value.Should().Be(245);

        var s1 = d.Segments[1];
        s1.FirstValueStartTime.Should().Be(T0.AddHours(1));
        // Empty ImmutableArray writes nothing → reads back as default (documented semantics).
        s1.CurrentValues.IsDefault.Should().BeTrue();
        s1.StatisticValues.Should().BeNull();
    }

    [Fact]
    public void ImutableValues_GG()
    {
        var data = SerializeWithGProtobuf(CreateSet(), TestModelSerializers.Serialize);
        AssertSet(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableValueSet(bytes)));
    }

    [Fact]
    public void ImutableValues_GG_Stream()
    {
        var data = SerializeWithGProtobuf(CreateSet(), TestModelSerializers.Serialize);
        AssertSet(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableValueSet(stream)));
    }

    [Fact]
    public void ImutableValues_PG()
    {
        var data = SerializeWithProtobufNet(CreateSet());
        AssertSet(DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableValueSet(bytes)));
    }

    [Fact]
    public void ImutableValues_PG_Stream()
    {
        var data = SerializeWithProtobufNet(CreateSet());
        AssertSet(DeserializeWithGProtobufStreamFromBytes(data,
            stream => TestModelDeserializers.DeserializeImmutableValueSet(stream)));
    }

    [Fact]
    public void ImutableValues_GP()
    {
        var data = SerializeWithGProtobuf(CreateSet(), TestModelSerializers.Serialize);
        AssertSet(DeserializeWithProtobufNet<ImmutableValueSet>(data));
    }

    private static byte[] OnePass(ImmutableValueSet model)
    {
        using var ms = new MemoryStream();
        TestModelSerializers.SerializeOnePass(ms, model);
        return ms.ToArray();
    }

    [Fact]
    public void ImutableValues_OnePass_ByteEqualTo2Pass()
    {
        var model = CreateSet();
        OnePass(model).Should().Equal(SerializeWithGProtobuf(model, TestModelSerializers.Serialize));
    }

    [Fact]
    public void ImutableValues_OnePass_GP()
    {
        AssertSet(DeserializeWithProtobufNet<ImmutableValueSet>(OnePass(CreateSet())));
    }

    [Fact]
    public void ImutableValues_OnePass_GG_Span()
    {
        AssertSet(DeserializeWithGProtobuf(OnePass(CreateSet()),
            bytes => TestModelDeserializers.DeserializeImmutableValueSet(bytes)));
    }

    [Fact]
    public void ImutableValues_OnePass_GG_Stream()
    {
        AssertSet(DeserializeWithGProtobufStreamFromBytes(OnePass(CreateSet()),
            stream => TestModelDeserializers.DeserializeImmutableValueSet(stream)));
    }

    [Fact]
    public void ImutableValues_Defaults_GG()
    {
        // All-default instance: empty strings, default TimeSpan, null list → minimal wire.
        var data = SerializeWithGProtobuf(new ImmutableValueSet(), TestModelSerializers.Serialize);
        var d = DeserializeWithGProtobuf(data,
            bytes => TestModelDeserializers.DeserializeImmutableValueSet(bytes));
        d.Segments.Should().BeNull();
        d.Granularity.Should().Be(TimeSpan.Zero);
    }
}
