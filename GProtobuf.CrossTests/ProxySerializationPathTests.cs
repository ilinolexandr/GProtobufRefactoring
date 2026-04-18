using System.Buffers;
using FluentAssertions;
using GProtobuf.Tests.Serialization;

namespace GProtobuf.Tests;

/// <summary>
/// Тести різних serialization/deserialization paths для proxy типів.
/// Поточні тести покривають лише Stream serialize + SpanReader deserialize.
/// Ці тести додають покриття SerializeToArray, IBufferWriter, StackBufferWriter,
/// StreamReader deserialize, Populate, та existing instance paths.
/// </summary>
public sealed class ProxySerializationPathTests : BaseSerializationTest
{
    private static ProxyTestMessage CreateTestMessage() => new()
    {
        Position = new ExternalVector3(1.5f, 2.5f, 3.5f),
        Name = "PathTest"
    };

    private static ProxyCollectionTestMessage CreateCollectionMessage() => new()
    {
        Positions = new List<ExternalVector3>
        {
            new(1.0f, 2.0f, 3.0f),
            new(4.0f, 5.0f, 6.0f),
            new(7.0f, 8.0f, 9.0f)
        }
    };

    private static ProxyMultiFieldTestMessage CreateMultiFieldMessage() => new()
    {
        Position = new ExternalVector3(1f, 2f, 3f),
        Color = new ExternalColor { R = 10, G = 20, B = 30, A = 40 },
        Origin = new ExternalReadonlyPoint(100.5, 200.5)
    };

    private static void AssertTestMessage(ProxyTestMessage result)
    {
        result.Position.X.Should().Be(1.5f);
        result.Position.Y.Should().Be(2.5f);
        result.Position.Z.Should().Be(3.5f);
        result.Name.Should().Be("PathTest");
    }

    private static void AssertCollectionMessage(ProxyCollectionTestMessage result)
    {
        result.Positions.Should().HaveCount(3);
        result.Positions![0].X.Should().Be(1.0f);
        result.Positions[1].Y.Should().Be(5.0f);
        result.Positions[2].Z.Should().Be(9.0f);
    }

    private static void AssertMultiFieldMessage(ProxyMultiFieldTestMessage result)
    {
        result.Position.X.Should().Be(1f);
        result.Position.Y.Should().Be(2f);
        result.Position.Z.Should().Be(3f);
        result.Color.Should().NotBeNull();
        result.Color!.R.Should().Be(10);
        result.Color.A.Should().Be(40);
        result.Origin.X.Should().Be(100.5);
        result.Origin.Y.Should().Be(200.5);
    }

    // ==================== SerializeToArray path ====================

    [Fact]
    public void SerializeToArray_SingleProxy_RoundTrip()
    {
        var model = CreateTestMessage();
        var data = Serializers.SerializeToArray(model);
        var result = Deserializers.DeserializeProxyTestMessage(data);
        AssertTestMessage(result);
    }

    [Fact]
    public void SerializeToArray_Collection_RoundTrip()
    {
        var model = CreateCollectionMessage();
        var data = Serializers.SerializeToArray(model);
        var result = Deserializers.DeserializeProxyCollectionTestMessage(data);
        AssertCollectionMessage(result);
    }

    [Fact]
    public void SerializeToArray_MultiField_RoundTrip()
    {
        var model = CreateMultiFieldMessage();
        var data = Serializers.SerializeToArray(model);
        var result = Deserializers.DeserializeProxyMultiFieldTestMessage(data);
        AssertMultiFieldMessage(result);
    }

    // ==================== IBufferWriter path ====================

    [Fact]
    public void BufferWriter_SingleProxy_RoundTrip()
    {
        var model = CreateTestMessage();
        var bufferWriter = new ArrayBufferWriter<byte>();
        Serializers.Serialize(bufferWriter, model);
        var data = bufferWriter.WrittenSpan.ToArray();
        var result = Deserializers.DeserializeProxyTestMessage(data);
        AssertTestMessage(result);
    }

    [Fact]
    public void BufferWriter_Collection_RoundTrip()
    {
        var model = CreateCollectionMessage();
        var bufferWriter = new ArrayBufferWriter<byte>();
        Serializers.Serialize(bufferWriter, model);
        var data = bufferWriter.WrittenSpan.ToArray();
        var result = Deserializers.DeserializeProxyCollectionTestMessage(data);
        AssertCollectionMessage(result);
    }

    [Fact]
    public void BufferWriter_MultiField_RoundTrip()
    {
        var model = CreateMultiFieldMessage();
        var bufferWriter = new ArrayBufferWriter<byte>();
        Serializers.Serialize(bufferWriter, model);
        var data = bufferWriter.WrittenSpan.ToArray();
        var result = Deserializers.DeserializeProxyMultiFieldTestMessage(data);
        AssertMultiFieldMessage(result);
    }

    // ==================== StackBufferWriter (SerializeTo Span) path ====================

    [Fact]
    public void StackBufferWriter_SingleProxy_RoundTrip()
    {
        var model = CreateTestMessage();
        Span<byte> buffer = stackalloc byte[512];
        var written = Serializers.SerializeTo(buffer, model);
        var data = buffer.Slice(0, written).ToArray();
        var result = Deserializers.DeserializeProxyTestMessage(data);
        AssertTestMessage(result);
    }

    [Fact]
    public void StackBufferWriter_MultiField_RoundTrip()
    {
        var model = CreateMultiFieldMessage();
        Span<byte> buffer = stackalloc byte[512];
        var written = Serializers.SerializeTo(buffer, model);
        var data = buffer.Slice(0, written).ToArray();
        var result = Deserializers.DeserializeProxyMultiFieldTestMessage(data);
        AssertMultiFieldMessage(result);
    }

    // ==================== StreamReader deserialization path ====================

    [Fact]
    public void StreamReader_SingleProxy_RoundTrip()
    {
        var model = CreateTestMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);
        using var stream = new MemoryStream(data);
        var result = Deserializers.DeserializeProxyTestMessage(stream);
        AssertTestMessage(result);
    }

    [Fact]
    public void StreamReader_Collection_RoundTrip()
    {
        var model = CreateCollectionMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);
        using var stream = new MemoryStream(data);
        var result = Deserializers.DeserializeProxyCollectionTestMessage(stream);
        AssertCollectionMessage(result);
    }

    [Fact]
    public void StreamReader_MultiField_RoundTrip()
    {
        var model = CreateMultiFieldMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);
        using var stream = new MemoryStream(data);
        var result = Deserializers.DeserializeProxyMultiFieldTestMessage(stream);
        AssertMultiFieldMessage(result);
    }

    [Fact]
    public void StreamReader_WithCustomBuffer_SingleProxy_RoundTrip()
    {
        var model = CreateTestMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);
        using var stream = new MemoryStream(data);
        Span<byte> buffer = stackalloc byte[256];
        var result = Deserializers.DeserializeProxyTestMessage(stream, buffer);
        AssertTestMessage(result);
    }

    // ==================== Populate path (existing instance) ====================

    [Fact]
    public void Populate_SpanReader_ExistingInstance()
    {
        var model = CreateTestMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);

        var existing = new ProxyTestMessage { Name = "old", Position = new ExternalVector3(0, 0, 0) };
        var result = Deserializers.DeserializeProxyTestMessage((ReadOnlySpan<byte>)data, existing);

        AssertTestMessage(result);
    }

    [Fact]
    public void Populate_StreamReader_ExistingInstance()
    {
        var model = CreateTestMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);

        var existing = new ProxyTestMessage { Name = "old", Position = new ExternalVector3(0, 0, 0) };
        using var stream = new MemoryStream(data);
        var result = Deserializers.DeserializeProxyTestMessage(stream, existing);

        AssertTestMessage(result);
    }

    [Fact]
    public void Populate_Explicit_SpanReader()
    {
        var model = CreateTestMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);

        var instance = new ProxyTestMessage();
        Deserializers.PopulateProxyTestMessage((ReadOnlySpan<byte>)data, instance);

        AssertTestMessage(instance);
    }

    [Fact]
    public void Populate_Explicit_StreamReader()
    {
        var model = CreateTestMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);

        var instance = new ProxyTestMessage();
        using var stream = new MemoryStream(data);
        Deserializers.PopulateProxyTestMessage(stream, instance);

        AssertTestMessage(instance);
    }

    [Fact]
    public void Populate_Explicit_StreamReader_WithBuffer()
    {
        var model = CreateTestMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);

        var instance = new ProxyTestMessage();
        using var stream = new MemoryStream(data);
        Span<byte> buffer = stackalloc byte[256];
        Deserializers.PopulateProxyTestMessage(stream, buffer, instance);

        AssertTestMessage(instance);
    }

    // ==================== Cross-path: різні ser + різні deser ====================

    [Fact]
    public void SerializeToArray_DeserializeStream_CrossPath()
    {
        var model = CreateTestMessage();
        var data = Serializers.SerializeToArray(model);
        using var stream = new MemoryStream(data);
        var result = Deserializers.DeserializeProxyTestMessage(stream);
        AssertTestMessage(result);
    }

    [Fact]
    public void BufferWriter_DeserializeStream_CrossPath()
    {
        var model = CreateMultiFieldMessage();
        var bufferWriter = new ArrayBufferWriter<byte>();
        Serializers.Serialize(bufferWriter, model);
        using var stream = new MemoryStream(bufferWriter.WrittenSpan.ToArray());
        var result = Deserializers.DeserializeProxyMultiFieldTestMessage(stream);
        AssertMultiFieldMessage(result);
    }

    // ==================== Wire compatibility: protobuf-net → GProtobuf ====================

    [Fact]
    public void ProtobufNet_ToGProtobuf_SingleProxy_RoundTrip()
    {
        // Серіалізуємо protobuf-net через proxy тип напряму
        var pnetModel = new ProxyTestMessagePnet
        {
            Position = new Vector3Proxy { X = 1.5f, Y = 2.5f, Z = 3.5f },
            Name = "pnet-test"
        };
        var data = SerializeWithProtobufNet(pnetModel);

        // Десеріалізуємо з GProtobuf (proxy перетворить Vector3Proxy → ExternalVector3)
        var result = Deserializers.DeserializeProxyTestMessage(data);

        result.Position.X.Should().Be(1.5f);
        result.Position.Y.Should().Be(2.5f);
        result.Position.Z.Should().Be(3.5f);
        result.Name.Should().Be("pnet-test");
    }

    [Fact]
    public void ProtobufNet_ToGProtobuf_Dict_RoundTrip()
    {
        var pnetModel = new ProxyDictStructMessagePnet
        {
            Positions = new Dictionary<string, Vector3Proxy>
            {
                ["a"] = new Vector3Proxy { X = 1f, Y = 2f, Z = 3f },
                ["b"] = new Vector3Proxy { X = 4f, Y = 5f, Z = 6f }
            }
        };
        var data = SerializeWithProtobufNet(pnetModel);

        var result = Deserializers.DeserializeProxyDictStructTestMessage(data);

        result.Positions.Should().HaveCount(2);
        result.Positions!["a"].X.Should().Be(1f);
        result.Positions["b"].Z.Should().Be(6f);
    }

    // ==================== Великі колекції ====================

    [Fact]
    public void LargeCollection_1000Elements_AllPaths()
    {
        var positions = new List<ExternalVector3>();
        for (int i = 0; i < 1000; i++)
            positions.Add(new ExternalVector3(i, i * 2f, i * 3f));

        var model = new ProxyCollectionTestMessage { Positions = positions };

        // Stream serialize → SpanReader deserialize
        var dataStream = SerializeWithGProtobuf(model, Serializers.Serialize);
        var result1 = Deserializers.DeserializeProxyCollectionTestMessage(dataStream);
        result1.Positions.Should().HaveCount(1000);
        result1.Positions![0].X.Should().Be(0f);
        result1.Positions[999].X.Should().Be(999f);
        result1.Positions[500].Y.Should().Be(1000f);

        // SerializeToArray → StreamReader deserialize
        var dataArray = Serializers.SerializeToArray(model);
        using var stream = new MemoryStream(dataArray);
        var result2 = Deserializers.DeserializeProxyCollectionTestMessage(stream);
        result2.Positions.Should().HaveCount(1000);
        result2.Positions![999].Z.Should().Be(2997f);
    }

    // ==================== Два однакових proxy типи в одному message ====================

    [Fact]
    public void SerializeToArray_LargeContainer_RoundTrip()
    {
        var model = new LargeProxyContainer
        {
            P1 = new ExternalVector3(1f, 1f, 1f),
            P2 = new ExternalVector3(2f, 2f, 2f),
            P3 = new ExternalVector3(3f, 3f, 3f),
            P4 = new ExternalVector3(4f, 4f, 4f),
            P5 = new ExternalVector3(5f, 5f, 5f),
            Name = "array-path",
            Count = 777,
            Extra = new List<ExternalVector3> { new(10f, 20f, 30f) }
        };

        var data = Serializers.SerializeToArray(model);
        var result = Deserializers.DeserializeLargeProxyContainer(data);

        result.P1.X.Should().Be(1f);
        result.P5.Z.Should().Be(5f);
        result.Name.Should().Be("array-path");
        result.Count.Should().Be(777);
        result.Extra.Should().HaveCount(1);
        result.Extra![0].Y.Should().Be(20f);
    }

    // ==================== Dict proxy через різні paths ====================

    [Fact]
    public void SerializeToArray_DictProxy_RoundTrip()
    {
        var model = new ProxyDictStructTestMessage
        {
            Positions = new Dictionary<string, ExternalVector3>
            {
                ["origin"] = new ExternalVector3(0f, 0f, 0f),
                ["target"] = new ExternalVector3(1f, 2f, 3f)
            }
        };

        var data = Serializers.SerializeToArray(model);
        var result = Deserializers.DeserializeProxyDictStructTestMessage(data);

        result.Positions.Should().HaveCount(2);
        result.Positions!["target"].Y.Should().Be(2f);
    }

    [Fact]
    public void StreamReader_DictProxy_RoundTrip()
    {
        var model = new ProxyDictClassTestMessage
        {
            Colors = new Dictionary<int, ExternalColor>
            {
                [1] = new ExternalColor { R = 255, G = 0, B = 0, A = 255 },
                [2] = new ExternalColor { R = 0, G = 255, B = 0, A = 128 }
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.Serialize);
        using var stream = new MemoryStream(data);
        var result = Deserializers.DeserializeProxyDictClassTestMessage(stream);

        result.Colors.Should().HaveCount(2);
        result.Colors![1].R.Should().Be(255);
        result.Colors[2].G.Should().Be(255);
    }

    // ==================== ProxyReturn call count per path ====================

    [Fact]
    public void ProxyReturn_SerializeToArray_CallCount()
    {
        Vector3Proxy.ReturnCallCount = 0;
        var model = CreateTestMessage();

        Serializers.SerializeToArray(model);

        Vector3Proxy.ReturnCallCount.Should().BeGreaterThan(0,
            "[ProxyReturn] має бути викликаний в SerializeToArray path");
    }

    [Fact]
    public void ProxyReturn_StreamReader_Deserialization_CallCount()
    {
        var model = CreateTestMessage();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);

        Vector3Proxy.ReturnCallCount = 0;
        using var stream = new MemoryStream(data);
        Deserializers.DeserializeProxyTestMessage(stream);

        Vector3Proxy.ReturnCallCount.Should().BeGreaterThan(0,
            "[ProxyReturn] має бути викликаний при StreamReader десеріалізації");
    }

    // ==================== ProtoInclude через різні paths ====================

    // NOTE: SerializeToArray + ProtoInclude не підтримує roundtrip derived→base десеріалізацію.
    // Це загальне обмеження ProtoInclude, не специфічне для proxy.

    [Fact]
    public void SerializeToArray_Nullable_WithValue_RoundTrip()
    {
        var model = new ProxyNullableTestMessage
        {
            MaybePosition = new ExternalVector3(7f, 8f, 9f),
            Tag = 42
        };

        var data = Serializers.SerializeToArray(model);
        var result = Deserializers.DeserializeProxyNullableTestMessage(data);

        result.MaybePosition.Should().NotBeNull();
        result.MaybePosition!.Value.X.Should().Be(7f);
        result.MaybePosition.Value.Y.Should().Be(8f);
        result.MaybePosition.Value.Z.Should().Be(9f);
        result.Tag.Should().Be(42);
    }

    [Fact]
    public void StreamReader_ProtoInclude_WithProxy_RoundTrip()
    {
        var model = new DerivedWithProxy
        {
            Name = "derived-stream",
            Position = new ExternalVector3(4f, 5f, 6f),
            Points = new List<ExternalVector3> { new(7f, 8f, 9f), new(10f, 11f, 12f) }
        };

        var data = SerializeWithGProtobuf(model, Serializers.Serialize);
        using var stream = new MemoryStream(data);
        var result = Deserializers.DeserializeBaseWithProxy(stream);

        result.Should().BeOfType<DerivedWithProxy>();
        var derived = (DerivedWithProxy)result;
        derived.Name.Should().Be("derived-stream");
        derived.Position.Y.Should().Be(5f);
        derived.Points.Should().HaveCount(2);
    }

    // ==================== Pooling proxy через різні paths ====================

    [Fact]
    public void SerializeToArray_PoolingProxy_RoundTrip()
    {
        var model = new ProxyPoolingTestMessage
        {
            Size = new ExternalSize(1920, 1080),
            Sizes = new List<ExternalSize> { new(2560, 1440), new(3840, 2160) }
        };

        var data = Serializers.SerializeToArray(model);
        var result = Deserializers.DeserializeProxyPoolingTestMessage(data);

        result.Size.Width.Should().Be(1920);
        result.Size.Height.Should().Be(1080);
        result.Sizes.Should().HaveCount(2);
        result.Sizes![0].Width.Should().Be(2560);
        result.Sizes[1].Height.Should().Be(2160);
    }
}
