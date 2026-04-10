using FluentAssertions;
using GProtobuf.Tests;
using GProtobuf.Tests.Serialization;
using ProtoBuf;

// Реєстрація proxy маппінгу для нового [ProxyAcquire] feature
[assembly: SerializationProxy(typeof(GProtobuf.Tests.AcquireExternalPoint), typeof(GProtobuf.Tests.AcquirePointProxy))]

namespace GProtobuf.Tests;

// ==================== Зовнішній тип ====================

/// <summary>
/// Зовнішній тип без [ProtoContract]. Class — щоб proxy міг бути class (для pool reuse через
/// reference equality). Codegen для class proxy очікує що original теж reference type.
/// </summary>
public sealed class AcquireExternalPoint
{
    public int X { get; set; }
    public int Y { get; set; }

    public AcquireExternalPoint() { }
    public AcquireExternalPoint(int x, int y) { X = x; Y = y; }
}

// ==================== Proxy з [ProxyAcquire] ====================

/// <summary>
/// Proxy для AcquireExternalPoint який демонструє pool-based [ProxyAcquire].
/// Веде статистику викликів Acquire/Wrap/Convert/Return для верифікації.
/// </summary>
[ProtoContract]
public sealed class AcquirePointProxy
{
    // Лічильники для тестів
    public static int AcquireCallCount;
    public static int WrapCallCount;
    public static int ConvertCallCount;
    public static int ReturnCallCount;

    // Простий стек-пул для верифікації reuse
    public static readonly System.Collections.Generic.Stack<AcquirePointProxy> Pool = new();

    public static void ResetCounters()
    {
        AcquireCallCount = 0;
        WrapCallCount = 0;
        ConvertCallCount = 0;
        ReturnCallCount = 0;
        Pool.Clear();
    }

    [ProtoMember(1)] public int X { get; set; }
    [ProtoMember(2)] public int Y { get; set; }

    [ProxyWrap]
    public static AcquirePointProxy Wrap(AcquireExternalPoint source)
    {
        WrapCallCount++;
        return new AcquirePointProxy { X = source.X, Y = source.Y };
    }

    [ProxyAcquire]
    public static AcquirePointProxy Acquire()
    {
        AcquireCallCount++;
        return Pool.Count > 0 ? Pool.Pop() : new AcquirePointProxy();
    }

    [ProxyConvert]
    public AcquireExternalPoint Convert()
    {
        ConvertCallCount++;
        return new AcquireExternalPoint(X, Y);
    }

    [ProxyReturn]
    public void Return()
    {
        ReturnCallCount++;
        // Skidaty stan i povernuty u pool dlya naseidiya Acquire
        X = 0;
        Y = 0;
        Pool.Push(this);
    }
}

// ==================== Тестові повідомлення ====================

[ProtoContract]
public sealed class AcquireSingleMessage
{
    [ProtoMember(1)] public AcquireExternalPoint Point { get; set; }
    [ProtoMember(2)] public string? Tag { get; set; }
}

[ProtoContract]
public sealed class AcquireCollectionMessage
{
    [ProtoMember(1)] public List<AcquireExternalPoint>? Points { get; set; }
}

[ProtoContract]
public sealed class AcquireNestedInner
{
    [ProtoMember(1)] public AcquireExternalPoint Origin { get; set; }
}

[ProtoContract]
public sealed class AcquireNestedOuter
{
    [ProtoMember(1)] public AcquireNestedInner? Inner { get; set; }
}

// ==================== Тести ====================

public sealed class ProxyAcquireRoundtripTests : BaseSerializationTest
{
    // Кейс A1: Span-path roundtrip — Acquire викликається замість new()
    [Fact]
    public void Acquire_SinglePoint_SpanPath_RoundTrip()
    {
        AcquirePointProxy.ResetCounters();

        var model = new AcquireSingleMessage
        {
            Point = new AcquireExternalPoint(11, 22),
            Tag = "single"
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeAcquireSingleMessage);
        var deserialized = Deserializers.DeserializeAcquireSingleMessage(data);

        deserialized.Point.X.Should().Be(11);
        deserialized.Point.Y.Should().Be(22);
        deserialized.Tag.Should().Be("single");

        AcquirePointProxy.AcquireCallCount.Should().BeGreaterThan(0,
            "[ProxyAcquire] має бути викликаний замість new() під час десеріалізації");
    }

    // Кейс A2: Pool reuse — instance, що ми поклали в пул, повертається з Acquire
    [Fact]
    public void Acquire_PoolReuse_ReturnsSameInstance()
    {
        AcquirePointProxy.ResetCounters();

        var sentinel = new AcquirePointProxy();
        AcquirePointProxy.Pool.Push(sentinel);

        var model = new AcquireSingleMessage
        {
            Point = new AcquireExternalPoint(7, 8),
            Tag = "pool"
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeAcquireSingleMessage);
        Deserializers.DeserializeAcquireSingleMessage(data);

        // Sentinel мав бути взятий з пулу під час десеріалізації, заповнений, потім
        // повернутий у пул через [ProxyReturn]. Перевіряємо що він знову у пулі.
        AcquirePointProxy.AcquireCallCount.Should().Be(1);
        AcquirePointProxy.Pool.Should().Contain(sentinel,
            "[ProxyReturn] має повернути той самий instance назад у пул");
    }

    // Кейс A3: Stream-path roundtrip — Acquire працює і для StreamReader
    [Fact]
    public void Acquire_SinglePoint_StreamPath_RoundTrip()
    {
        AcquirePointProxy.ResetCounters();

        var model = new AcquireSingleMessage
        {
            Point = new AcquireExternalPoint(33, 44),
            Tag = "stream"
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeAcquireSingleMessage);
        using var ms = new System.IO.MemoryStream(data);
        var deserialized = Deserializers.DeserializeAcquireSingleMessage(ms);

        deserialized.Point.X.Should().Be(33);
        deserialized.Point.Y.Should().Be(44);
        deserialized.Tag.Should().Be("stream");

        AcquirePointProxy.AcquireCallCount.Should().BeGreaterThan(0,
            "[ProxyAcquire] має бути викликаний у StreamReader шляху");
    }

    // Кейс A4: Колекція — Acquire викликається на кожний елемент
    [Fact]
    public void Acquire_Collection_RoundTrip()
    {
        AcquirePointProxy.ResetCounters();

        var model = new AcquireCollectionMessage
        {
            Points = new List<AcquireExternalPoint>
            {
                new(1, 2),
                new(3, 4),
                new(5, 6)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeAcquireCollectionMessage);
        var deserialized = Deserializers.DeserializeAcquireCollectionMessage(data);

        deserialized.Points.Should().HaveCount(3);
        deserialized.Points![0].X.Should().Be(1);
        deserialized.Points[1].X.Should().Be(3);
        deserialized.Points[2].Y.Should().Be(6);

        AcquirePointProxy.AcquireCallCount.Should().BeGreaterThanOrEqualTo(3,
            "[ProxyAcquire] має бути викликаний для кожного елементу колекції");
    }

    // Кейс A5: Nested message з proxy полем
    [Fact]
    public void Acquire_NestedMessage_RoundTrip()
    {
        AcquirePointProxy.ResetCounters();

        var model = new AcquireNestedOuter
        {
            Inner = new AcquireNestedInner
            {
                Origin = new AcquireExternalPoint(99, 100)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeAcquireNestedOuter);
        var deserialized = Deserializers.DeserializeAcquireNestedOuter(data);

        deserialized.Inner.Should().NotBeNull();
        deserialized.Inner!.Origin.X.Should().Be(99);
        deserialized.Inner.Origin.Y.Should().Be(100);

        AcquirePointProxy.AcquireCallCount.Should().BeGreaterThan(0);
    }

    // Кейс A6: Lifecycle — Acquire → Convert → Return викликаються рівно по разу для одного поля
    [Fact]
    public void Acquire_Lifecycle_AcquireConvertReturn_AllCalledOnce()
    {
        AcquirePointProxy.ResetCounters();

        var model = new AcquireSingleMessage
        {
            Point = new AcquireExternalPoint(123, 456)
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeAcquireSingleMessage);

        // Reset після серіалізації, бо Wrap і ще щось могли інкрементуватися під час write-path.
        AcquirePointProxy.AcquireCallCount = 0;
        AcquirePointProxy.ConvertCallCount = 0;
        AcquirePointProxy.ReturnCallCount = 0;

        Deserializers.DeserializeAcquireSingleMessage(data);

        AcquirePointProxy.AcquireCallCount.Should().Be(1, "одне proxy поле → один Acquire виклик");
        AcquirePointProxy.ConvertCallCount.Should().Be(1, "одне proxy поле → один Convert виклик");
        AcquirePointProxy.ReturnCallCount.Should().Be(1, "одне proxy поле → один Return виклик");
    }
}
