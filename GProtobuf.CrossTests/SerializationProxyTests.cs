using FluentAssertions;
using GProtobuf.Tests;
using GProtobuf.Tests.Serialization;
using ProtoBuf;

// Реєстрація proxy маппінгів (assembly-level атрибути мають стояти перед декларацією типів)
[assembly: SerializationProxy(typeof(ExternalVector3), typeof(Vector3Proxy))]
[assembly: SerializationProxy(typeof(ExternalColor), typeof(ColorProxy))]
[assembly: SerializationProxy(typeof(ExternalReadonlyPoint), typeof(ReadonlyPointProxy))]
[assembly: SerializationProxy(typeof(ExternalSize), typeof(SizeProxy))]

namespace GProtobuf.Tests;

// ==================== Зовнішні типи (імітація сторонніх бібліотек) ====================

/// <summary>Зовнішній struct без [ProtoContract] — базовий тип для proxy</summary>
public struct ExternalVector3
{
    public float X;
    public float Y;
    public float Z;

    public ExternalVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

/// <summary>Зовнішній class без [ProtoContract] — для тесту proxy на class</summary>
public class ExternalColor
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }
}

/// <summary>Зовнішній readonly struct — для тесту proxy з readonly полями</summary>
public readonly struct ExternalReadonlyPoint
{
    public readonly double X;
    public readonly double Y;

    public ExternalReadonlyPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}

// ==================== Proxy типи ====================

/// <summary>Proxy для ExternalVector3 (struct → struct)</summary>
[ProtoContract]
public struct Vector3Proxy
{
    /// <summary>Counter для верифікації що [ProxyReturn] реально викликається</summary>
    public static int ReturnCallCount;

    [ProtoMember(1)] public float X { get; set; }
    [ProtoMember(2)] public float Y { get; set; }
    [ProtoMember(3)] public float Z { get; set; }

    [ProxyWrap]
    public static Vector3Proxy FromOriginal(ExternalVector3 source)
        => new() { X = source.X, Y = source.Y, Z = source.Z };

    [ProxyConvert]
    public ExternalVector3 ToOriginal()
        => new(X, Y, Z);

    [ProxyReturn]
    public void Return()
    {
        ReturnCallCount++;
    }
}

/// <summary>Proxy для ExternalColor (class → class з null-handling)</summary>
[ProtoContract]
public class ColorProxy
{
    [ProtoMember(1)] public int R { get; set; }
    [ProtoMember(2)] public int G { get; set; }
    [ProtoMember(3)] public int B { get; set; }
    [ProtoMember(4)] public int A { get; set; }

    [ProxyWrap]
    public static ColorProxy FromOriginal(ExternalColor source)
        => new() { R = source.R, G = source.G, B = source.B, A = source.A };

    [ProxyConvert]
    public ExternalColor ToOriginal()
        => new() { R = (byte)R, G = (byte)G, B = (byte)B, A = (byte)A };
}

/// <summary>Proxy для ExternalReadonlyPoint (readonly struct → мутабельний struct)</summary>
[ProtoContract]
public struct ReadonlyPointProxy
{
    [ProtoMember(1)] public double X { get; set; }
    [ProtoMember(2)] public double Y { get; set; }

    [ProxyWrap]
    public static ReadonlyPointProxy FromOriginal(ExternalReadonlyPoint source)
        => new() { X = source.X, Y = source.Y };

    [ProxyConvert]
    public ExternalReadonlyPoint ToOriginal()
        => new(X, Y);
}

// ==================== Зовнішній тип для тесту 2-параметрового ProxyWrap ====================

/// <summary>Зовнішній struct для тесту pooling proxy</summary>
public struct ExternalSize
{
    public int Width;
    public int Height;

    public ExternalSize(int w, int h) { Width = w; Height = h; }
}

/// <summary>Proxy з 2-параметровим [ProxyWrap] (pooling pattern)</summary>
[ProtoContract]
public struct SizeProxy
{
    public static int CreateCallCount;

    [ProtoMember(1)] public int Width { get; set; }
    [ProtoMember(2)] public int Height { get; set; }

    [ProxyWrap]
    public static SizeProxy FromOriginal(ExternalSize source, SizeProxy reuse)
    {
        CreateCallCount++;
        reuse.Width = source.Width;
        reuse.Height = source.Height;
        return reuse;
    }

    [ProxyConvert]
    public ExternalSize ToOriginal() => new(Width, Height);
}

// ==================== Тестові повідомлення ====================

/// <summary>Кейс 1: Одне struct proxy поле</summary>
[ProtoContract]
public class ProxyTestMessage
{
    [ProtoMember(1)]
    public ExternalVector3 Position { get; set; }

    [ProtoMember(2)]
    public string? Name { get; set; }
}

/// <summary>Кейс 2: Колекція List з proxy</summary>
[ProtoContract]
public class ProxyCollectionTestMessage
{
    [ProtoMember(1)]
    public List<ExternalVector3>? Positions { get; set; }
}

/// <summary>Кейс 3: Масив з proxy (окремий code path через tempList/ObjectArrayBuilder)</summary>
[ProtoContract]
public class ProxyArrayTestMessage
{
    [ProtoMember(1)]
    public ExternalVector3[]? Points { get; set; }
}

/// <summary>Кейс 4: Nullable struct з proxy</summary>
[ProtoContract]
public class ProxyNullableTestMessage
{
    [ProtoMember(1)]
    public ExternalVector3? MaybePosition { get; set; }

    [ProtoMember(2)]
    public int Tag { get; set; }
}

/// <summary>Кейс 5: Class proxy з null-handling</summary>
[ProtoContract]
public class ProxyClassTestMessage
{
    [ProtoMember(1)]
    public ExternalColor? Color { get; set; }

    [ProtoMember(2)]
    public string? Label { get; set; }
}

/// <summary>Кейс 6: Кілька proxy полів різних типів</summary>
[ProtoContract]
public class ProxyMultiFieldTestMessage
{
    [ProtoMember(1)]
    public ExternalVector3 Position { get; set; }

    [ProtoMember(2)]
    public ExternalColor? Color { get; set; }

    [ProtoMember(3)]
    public ExternalReadonlyPoint Origin { get; set; }
}

/// <summary>Кейс 7: Вкладений proxy — Container → Inner → ExternalVector3</summary>
[ProtoContract]
public class ProxyNestedInner
{
    [ProtoMember(1)]
    public ExternalVector3 Offset { get; set; }

    [ProtoMember(2)]
    public float Scale { get; set; }
}

[ProtoContract]
public class ProxyNestedContainer
{
    [ProtoMember(1)]
    public ProxyNestedInner? Inner { get; set; }

    [ProtoMember(2)]
    public string? Description { get; set; }
}

/// <summary>Кейс 10: Мікс — proxy + звичайний [ProtoContract] + примітив</summary>
[ProtoContract]
public class RegularSubMessage
{
    [ProtoMember(1)]
    public int Value { get; set; }

    [ProtoMember(2)]
    public string? Text { get; set; }
}

[ProtoContract]
public class ProxyMixedTestMessage
{
    [ProtoMember(1)]
    public ExternalVector3 Position { get; set; }

    [ProtoMember(2)]
    public RegularSubMessage? SubMsg { get; set; }

    [ProtoMember(3)]
    public int Counter { get; set; }

    [ProtoMember(4)]
    public string? Label { get; set; }
}

/// <summary>Кейс 14: Великий proxy з 10+ полями</summary>
[ProtoContract]
public class LargeProxyContainer
{
    [ProtoMember(1)] public ExternalVector3 P1 { get; set; }
    [ProtoMember(2)] public ExternalVector3 P2 { get; set; }
    [ProtoMember(3)] public ExternalVector3 P3 { get; set; }
    [ProtoMember(4)] public ExternalVector3 P4 { get; set; }
    [ProtoMember(5)] public ExternalVector3 P5 { get; set; }
    [ProtoMember(6)] public string? Name { get; set; }
    [ProtoMember(7)] public int Count { get; set; }
    [ProtoMember(8)] public List<ExternalVector3>? Extra { get; set; }
}

/// <summary>Кейс 11: Dictionary з proxy value type (struct)</summary>
[ProtoContract]
public class ProxyDictStructTestMessage
{
    [ProtoMember(1)]
    public Dictionary<string, ExternalVector3>? Positions { get; set; }
}

/// <summary>Кейс 11b: Dictionary з proxy value type (class)</summary>
[ProtoContract]
public class ProxyDictClassTestMessage
{
    [ProtoMember(1)]
    public Dictionary<int, ExternalColor>? Colors { get; set; }
}

/// <summary>Кейс 15: Readonly struct proxy</summary>
[ProtoContract]
public class ProxyReadonlyStructTestMessage
{
    [ProtoMember(1)]
    public ExternalReadonlyPoint Point { get; set; }

    [ProtoMember(2)]
    public double Radius { get; set; }
}

// ==================== Edge Case моделі для аудиту ====================

/// <summary>Edge Case: HashSet з proxy типом</summary>
[ProtoContract]
public class ProxyHashSetTestMessage
{
    [ProtoMember(1)]
    public HashSet<ExternalVector3>? Positions { get; set; }
}

/// <summary>Edge Case: ProtoInclude ієрархія з proxy полем</summary>
[ProtoContract]
[ProtoInclude(10, typeof(DerivedWithProxy))]
public class BaseWithProxy
{
    [ProtoMember(1)]
    public string? Name { get; set; }
}

[ProtoContract]
public class DerivedWithProxy : BaseWithProxy
{
    [ProtoMember(1)]
    public ExternalVector3 Position { get; set; }

    [ProtoMember(2)]
    public List<ExternalVector3>? Points { get; set; }
}

/// <summary>Edge Case P7: Dictionary з масивом proxy типів як value</summary>
[ProtoContract]
public class ProxyDictArrayValueTestMessage
{
    [ProtoMember(1)]
    public Dictionary<string, ExternalVector3[]>? Data { get; set; }
}

/// <summary>Edge Case P7: Dictionary з List proxy типів як value</summary>
[ProtoContract]
public class ProxyDictListValueTestMessage
{
    [ProtoMember(1)]
    public Dictionary<string, List<ExternalVector3>>? Items { get; set; }
}

/// <summary>Edge Case P7: Nested Dictionary з proxy типом як inner value</summary>
[ProtoContract]
public class ProxyNestedDictTestMessage
{
    [ProtoMember(1)]
    public Dictionary<string, Dictionary<int, ExternalVector3>>? Data { get; set; }
}

/// <summary>Кейс P3: Proxy з 2-параметровим [ProxyWrap] (pooling)</summary>
[ProtoContract]
public class ProxyPoolingTestMessage
{
    [ProtoMember(1)]
    public ExternalSize Size { get; set; }

    [ProtoMember(2)]
    public List<ExternalSize>? Sizes { get; set; }
}

// ==================== Edge Case моделі A4 (тимчасові, для аудиту компіляції) ====================

// A4.2: Два proxy типи в одному Dictionary value
[ProtoContract]
public class ProxyTupleInDictTest
{
    [ProtoMember(1)]
    public Dictionary<string, ExternalColor>? Colors { get; set; }

    [ProtoMember(2)]
    public Dictionary<int, ExternalVector3>? Vectors { get; set; }
}

// A4.3: SKIPPED — Multi-level ProtoInclude (3+ levels) has a general deserialization issue
// (not proxy-specific). Nested ProtoInclude wrapper dispatching does not propagate
// to grandchild types. This is a separate issue from serialization proxy support.

// ==================== Тести ====================

public sealed class SerializationProxyTests : BaseSerializationTest
{
    // Кейс 1: Одне struct proxy поле
    [Fact]
    public void ProxyType_RoundTrip_StreamWriter()
    {
        var model = new ProxyTestMessage
        {
            Position = new ExternalVector3(1.5f, 2.5f, 3.5f),
            Name = "TestPoint"
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyTestMessage);
        var deserialized = Deserializers.DeserializeProxyTestMessage(data);

        data.Should().NotBeNull();
        data.Length.Should().BeGreaterThan(0);
        deserialized.Position.X.Should().Be(1.5f);
        deserialized.Position.Y.Should().Be(2.5f);
        deserialized.Position.Z.Should().Be(3.5f);
        deserialized.Name.Should().Be("TestPoint");
    }

    // Кейс 2: Колекція List<ExternalVector3>
    [Fact]
    public void ProxyType_Collection_RoundTrip()
    {
        var model = new ProxyCollectionTestMessage
        {
            Positions = new List<ExternalVector3>
            {
                new(1.0f, 2.0f, 3.0f),
                new(4.0f, 5.0f, 6.0f),
                new(7.0f, 8.0f, 9.0f)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyCollectionTestMessage);
        var deserialized = Deserializers.DeserializeProxyCollectionTestMessage(data);

        data.Should().NotBeNull();
        deserialized.Positions.Should().HaveCount(3);
        deserialized.Positions[0].X.Should().Be(1.0f);
        deserialized.Positions[1].Y.Should().Be(5.0f);
        deserialized.Positions[2].Z.Should().Be(9.0f);
    }

    // Кейс 3: Масив ExternalVector3[] (окремий code path через tempList/ObjectArrayBuilder)
    [Fact]
    public void ProxyType_Array_RoundTrip()
    {
        var model = new ProxyArrayTestMessage
        {
            Points = new[]
            {
                new ExternalVector3(10f, 20f, 30f),
                new ExternalVector3(40f, 50f, 60f)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyArrayTestMessage);
        var deserialized = Deserializers.DeserializeProxyArrayTestMessage(data);

        deserialized.Points.Should().HaveCount(2);
        deserialized.Points![0].X.Should().Be(10f);
        deserialized.Points[0].Y.Should().Be(20f);
        deserialized.Points[0].Z.Should().Be(30f);
        deserialized.Points[1].X.Should().Be(40f);
        deserialized.Points[1].Y.Should().Be(50f);
        deserialized.Points[1].Z.Should().Be(60f);
    }

    // Кейс 4: Nullable struct ExternalVector3? — перевірка що null пропускається
    [Fact]
    public void ProxyType_NullableStruct_WithValue_RoundTrip()
    {
        var model = new ProxyNullableTestMessage
        {
            MaybePosition = new ExternalVector3(1f, 2f, 3f),
            Tag = 42
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyNullableTestMessage);
        var deserialized = Deserializers.DeserializeProxyNullableTestMessage(data);

        deserialized.MaybePosition.Should().NotBeNull();
        deserialized.MaybePosition!.Value.X.Should().Be(1f);
        deserialized.MaybePosition.Value.Y.Should().Be(2f);
        deserialized.MaybePosition.Value.Z.Should().Be(3f);
        deserialized.Tag.Should().Be(42);
    }

    [Fact]
    public void ProxyType_NullableStruct_WithNull_RoundTrip()
    {
        var model = new ProxyNullableTestMessage
        {
            MaybePosition = null,
            Tag = 99
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyNullableTestMessage);
        var deserialized = Deserializers.DeserializeProxyNullableTestMessage(data);

        deserialized.MaybePosition.Should().BeNull();
        deserialized.Tag.Should().Be(99);
    }

    // Кейс 5: Class proxy — ExternalColor з null-handling
    [Fact]
    public void ProxyType_Class_WithValue_RoundTrip()
    {
        var model = new ProxyClassTestMessage
        {
            Color = new ExternalColor { R = 255, G = 128, B = 64, A = 200 },
            Label = "red-ish"
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyClassTestMessage);
        var deserialized = Deserializers.DeserializeProxyClassTestMessage(data);

        deserialized.Color.Should().NotBeNull();
        deserialized.Color!.R.Should().Be(255);
        deserialized.Color.G.Should().Be(128);
        deserialized.Color.B.Should().Be(64);
        deserialized.Color.A.Should().Be(200);
        deserialized.Label.Should().Be("red-ish");
    }

    [Fact]
    public void ProxyType_Class_WithNull_RoundTrip()
    {
        var model = new ProxyClassTestMessage
        {
            Color = null,
            Label = "none"
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyClassTestMessage);
        var deserialized = Deserializers.DeserializeProxyClassTestMessage(data);

        deserialized.Color.Should().BeNull();
        deserialized.Label.Should().Be("none");
    }

    // Кейс 6: Кілька proxy полів різних типів
    [Fact]
    public void ProxyType_MultiField_RoundTrip()
    {
        var model = new ProxyMultiFieldTestMessage
        {
            Position = new ExternalVector3(1f, 2f, 3f),
            Color = new ExternalColor { R = 10, G = 20, B = 30, A = 40 },
            Origin = new ExternalReadonlyPoint(100.5, 200.5)
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyMultiFieldTestMessage);
        var deserialized = Deserializers.DeserializeProxyMultiFieldTestMessage(data);

        deserialized.Position.X.Should().Be(1f);
        deserialized.Position.Y.Should().Be(2f);
        deserialized.Position.Z.Should().Be(3f);
        deserialized.Color.Should().NotBeNull();
        deserialized.Color!.R.Should().Be(10);
        deserialized.Color.A.Should().Be(40);
        deserialized.Origin.X.Should().Be(100.5);
        deserialized.Origin.Y.Should().Be(200.5);
    }

    // Кейс 7: Вкладений proxy — Container → Inner → ExternalVector3
    [Fact]
    public void ProxyType_Nested_RoundTrip()
    {
        var model = new ProxyNestedContainer
        {
            Inner = new ProxyNestedInner
            {
                Offset = new ExternalVector3(5f, 10f, 15f),
                Scale = 2.5f
            },
            Description = "nested test"
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyNestedContainer);
        var deserialized = Deserializers.DeserializeProxyNestedContainer(data);

        deserialized.Inner.Should().NotBeNull();
        deserialized.Inner!.Offset.X.Should().Be(5f);
        deserialized.Inner.Offset.Y.Should().Be(10f);
        deserialized.Inner.Offset.Z.Should().Be(15f);
        deserialized.Inner.Scale.Should().Be(2.5f);
        deserialized.Description.Should().Be("nested test");
    }

    // Кейс 8: Default values — серіалізація struct з усіма 0 (protobuf Level200 пропускає default)
    [Fact]
    public void ProxyType_DefaultValues_RoundTrip()
    {
        var model = new ProxyTestMessage
        {
            Position = new ExternalVector3(0f, 0f, 0f),
            Name = null
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyTestMessage);
        var deserialized = Deserializers.DeserializeProxyTestMessage(data);

        // protobuf Level200: поля з default значеннями можуть бути пропущені
        deserialized.Position.X.Should().Be(0f);
        deserialized.Position.Y.Should().Be(0f);
        deserialized.Position.Z.Should().Be(0f);
        deserialized.Name.Should().BeNull();
    }

    // Кейс 10: Мікс — proxy + звичайний [ProtoContract] + примітив
    [Fact]
    public void ProxyType_Mixed_RoundTrip()
    {
        var model = new ProxyMixedTestMessage
        {
            Position = new ExternalVector3(1.1f, 2.2f, 3.3f),
            SubMsg = new RegularSubMessage { Value = 42, Text = "hello" },
            Counter = 100,
            Label = "mixed"
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyMixedTestMessage);
        var deserialized = Deserializers.DeserializeProxyMixedTestMessage(data);

        deserialized.Position.X.Should().Be(1.1f);
        deserialized.Position.Y.Should().Be(2.2f);
        deserialized.Position.Z.Should().Be(3.3f);
        deserialized.SubMsg.Should().NotBeNull();
        deserialized.SubMsg!.Value.Should().Be(42);
        deserialized.SubMsg.Text.Should().Be("hello");
        deserialized.Counter.Should().Be(100);
        deserialized.Label.Should().Be("mixed");
    }

    // Кейс 12: Wire-сумісність з protobuf-net
    // Серіалізуємо з GProtobuf (через proxy), десеріалізуємо з protobuf-net (Vector3Proxy напряму)
    [Fact]
    public void ProxyType_WireCompatibility_GProtobufToProtobufNet()
    {
        var model = new ProxyTestMessage
        {
            Position = new ExternalVector3(1.5f, 2.5f, 3.5f),
            Name = "wiretest"
        };

        // Серіалізуємо з GProtobuf (proxy перетворює ExternalVector3 → Vector3Proxy під капотом)
        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyTestMessage);

        // Десеріалізуємо з protobuf-net, використовуючи message де Position = Vector3Proxy напряму
        var pnetResult = DeserializeWithProtobufNet<ProxyTestMessagePnet>(data);
        pnetResult.Position.Should().NotBeNull();
        pnetResult.Position!.Value.X.Should().Be(1.5f);
        pnetResult.Position.Value.Y.Should().Be(2.5f);
        pnetResult.Position.Value.Z.Should().Be(3.5f);
        pnetResult.Name.Should().Be("wiretest");
    }

    // Кейс 13: Порожня колекція — null або порожня
    [Fact]
    public void ProxyType_EmptyCollection_RoundTrip()
    {
        var model = new ProxyCollectionTestMessage
        {
            Positions = new List<ExternalVector3>()
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyCollectionTestMessage);
        var deserialized = Deserializers.DeserializeProxyCollectionTestMessage(data);

        // Порожній список → після десеріалізації може бути null (protobuf не пише порожні repeated)
        (deserialized.Positions == null || deserialized.Positions.Count == 0).Should().BeTrue();
    }

    [Fact]
    public void ProxyType_NullCollection_RoundTrip()
    {
        var model = new ProxyCollectionTestMessage
        {
            Positions = null
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyCollectionTestMessage);
        var deserialized = Deserializers.DeserializeProxyCollectionTestMessage(data);

        deserialized.Positions.Should().BeNull();
    }

    // Кейс 14: Великий контейнер з багатьма proxy полями та колекцією
    [Fact]
    public void ProxyType_LargeContainer_RoundTrip()
    {
        var model = new LargeProxyContainer
        {
            P1 = new ExternalVector3(1f, 1f, 1f),
            P2 = new ExternalVector3(2f, 2f, 2f),
            P3 = new ExternalVector3(3f, 3f, 3f),
            P4 = new ExternalVector3(4f, 4f, 4f),
            P5 = new ExternalVector3(5f, 5f, 5f),
            Name = "big",
            Count = 999,
            Extra = new List<ExternalVector3>
            {
                new(10f, 20f, 30f),
                new(40f, 50f, 60f)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeLargeProxyContainer);
        var deserialized = Deserializers.DeserializeLargeProxyContainer(data);

        deserialized.P1.X.Should().Be(1f);
        deserialized.P2.X.Should().Be(2f);
        deserialized.P3.X.Should().Be(3f);
        deserialized.P4.X.Should().Be(4f);
        deserialized.P5.X.Should().Be(5f);
        deserialized.Name.Should().Be("big");
        deserialized.Count.Should().Be(999);
        deserialized.Extra.Should().HaveCount(2);
        deserialized.Extra![0].X.Should().Be(10f);
        deserialized.Extra[1].Z.Should().Be(60f);
    }

    // Кейс 15: Readonly struct proxy
    [Fact]
    public void ProxyType_ReadonlyStruct_RoundTrip()
    {
        var model = new ProxyReadonlyStructTestMessage
        {
            Point = new ExternalReadonlyPoint(3.14, 2.72),
            Radius = 10.0
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyReadonlyStructTestMessage);
        var deserialized = Deserializers.DeserializeProxyReadonlyStructTestMessage(data);

        deserialized.Point.X.Should().Be(3.14);
        deserialized.Point.Y.Should().Be(2.72);
        deserialized.Radius.Should().Be(10.0);
    }

    // Кейс 11: Dictionary<string, ExternalVector3> — proxy struct value roundtrip
    [Fact]
    public void ProxyType_DictStruct_RoundTrip()
    {
        var model = new ProxyDictStructTestMessage
        {
            Positions = new Dictionary<string, ExternalVector3>
            {
                ["origin"] = new ExternalVector3(0f, 0f, 0f),
                ["target"] = new ExternalVector3(1f, 2f, 3f),
                ["offset"] = new ExternalVector3(10f, 20f, 30f)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyDictStructTestMessage);
        var deserialized = Deserializers.DeserializeProxyDictStructTestMessage(data);

        deserialized.Positions.Should().HaveCount(3);
        deserialized.Positions!["origin"].X.Should().Be(0f);
        deserialized.Positions["target"].Y.Should().Be(2f);
        deserialized.Positions["offset"].Z.Should().Be(30f);
    }

    // Кейс 11b: Dictionary<int, ExternalColor> — proxy class value roundtrip
    [Fact]
    public void ProxyType_DictClass_RoundTrip()
    {
        var model = new ProxyDictClassTestMessage
        {
            Colors = new Dictionary<int, ExternalColor>
            {
                [1] = new ExternalColor { R = 255, G = 0, B = 0, A = 255 },
                [2] = new ExternalColor { R = 0, G = 255, B = 0, A = 128 }
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyDictClassTestMessage);
        var deserialized = Deserializers.DeserializeProxyDictClassTestMessage(data);

        deserialized.Colors.Should().HaveCount(2);
        deserialized.Colors![1].R.Should().Be(255);
        deserialized.Colors[1].G.Should().Be(0);
        deserialized.Colors[2].G.Should().Be(255);
        deserialized.Colors[2].A.Should().Be(128);
    }

    // Кейс 11c: Wire compatibility — GProtobuf Dictionary proxy → protobuf-net Dictionary<string, Vector3Proxy>
    [Fact]
    public void ProxyType_DictStruct_WireCompatibility()
    {
        var model = new ProxyDictStructTestMessage
        {
            Positions = new Dictionary<string, ExternalVector3>
            {
                ["p1"] = new ExternalVector3(1.5f, 2.5f, 3.5f)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyDictStructTestMessage);
        var pnetResult = DeserializeWithProtobufNet<ProxyDictStructMessagePnet>(data);
        pnetResult.Positions.Should().HaveCount(1);
        pnetResult.Positions!["p1"].X.Should().Be(1.5f);
        pnetResult.Positions["p1"].Y.Should().Be(2.5f);
        pnetResult.Positions["p1"].Z.Should().Be(3.5f);
    }

    // Кейс 9: [ProxyReturn] end-to-end — перевірка що Return() реально викликається
    [Fact]
    public void ProxyReturn_CalledDuringSerialization()
    {
        Vector3Proxy.ReturnCallCount = 0;

        var model = new ProxyTestMessage
        {
            Position = new ExternalVector3(1f, 2f, 3f),
            Name = "return-test"
        };

        // Серіалізація: SizeCalc pass створює proxy + Writer pass створює proxy
        // Кожен pass може викликати Return() якщо [ProxyReturn] визначено
        SerializeWithGProtobuf(model, Serializers.SerializeProxyTestMessage);

        Vector3Proxy.ReturnCallCount.Should().BeGreaterThan(0,
            "[ProxyReturn] має бути викликаний під час серіалізації");
    }

    // Edge Case: HashSet<ExternalVector3> roundtrip
    [Fact]
    public void ProxyType_HashSet_RoundTrip()
    {
        var model = new ProxyHashSetTestMessage
        {
            Positions = new HashSet<ExternalVector3>
            {
                new(1.0f, 2.0f, 3.0f),
                new(4.0f, 5.0f, 6.0f)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyHashSetTestMessage);
        var deserialized = Deserializers.DeserializeProxyHashSetTestMessage(data);

        deserialized.Positions.Should().HaveCount(2);
    }

    // Edge Case: ProtoInclude з proxy полем
    [Fact]
    public void ProxyType_ProtoInclude_RoundTrip()
    {
        var model = new DerivedWithProxy
        {
            Name = "derived",
            Position = new ExternalVector3(1f, 2f, 3f),
            Points = new List<ExternalVector3>
            {
                new(10f, 20f, 30f),
                new(40f, 50f, 60f)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeDerivedWithProxy);
        var deserialized = Deserializers.DeserializeBaseWithProxy(data);

        deserialized.Should().BeOfType<DerivedWithProxy>();
        var derived = (DerivedWithProxy)deserialized;
        derived.Name.Should().Be("derived");
        derived.Position.X.Should().Be(1f);
        derived.Position.Y.Should().Be(2f);
        derived.Position.Z.Should().Be(3f);
        derived.Points.Should().HaveCount(2);
        derived.Points![0].X.Should().Be(10f);
        derived.Points[1].Z.Should().Be(60f);
    }

    // Edge Case P7: Dictionary<string, ExternalVector3[]>
    [Fact]
    public void ProxyType_DictArrayValue_RoundTrip()
    {
        var model = new ProxyDictArrayValueTestMessage
        {
            Data = new Dictionary<string, ExternalVector3[]>
            {
                ["a"] = new[] { new ExternalVector3(1f, 2f, 3f), new ExternalVector3(4f, 5f, 6f) },
                ["b"] = new[] { new ExternalVector3(7f, 8f, 9f) }
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyDictArrayValueTestMessage);
        var deserialized = Deserializers.DeserializeProxyDictArrayValueTestMessage(data);

        deserialized.Data.Should().HaveCount(2);
        deserialized.Data!["a"].Should().HaveCount(2);
        deserialized.Data["a"][0].X.Should().Be(1f);
        deserialized.Data["a"][1].Z.Should().Be(6f);
        deserialized.Data["b"].Should().HaveCount(1);
        deserialized.Data["b"][0].Y.Should().Be(8f);
    }

    // Edge Case P7: Dictionary<string, List<ExternalVector3>>
    [Fact]
    public void ProxyType_DictListValue_RoundTrip()
    {
        var model = new ProxyDictListValueTestMessage
        {
            Items = new Dictionary<string, List<ExternalVector3>>
            {
                ["x"] = new List<ExternalVector3> { new(1f, 2f, 3f), new(4f, 5f, 6f) },
                ["y"] = new List<ExternalVector3> { new(7f, 8f, 9f) }
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyDictListValueTestMessage);
        var deserialized = Deserializers.DeserializeProxyDictListValueTestMessage(data);

        deserialized.Items.Should().HaveCount(2);
        deserialized.Items!["x"].Should().HaveCount(2);
        deserialized.Items["x"][0].X.Should().Be(1f);
        deserialized.Items["x"][1].Z.Should().Be(6f);
        deserialized.Items["y"].Should().HaveCount(1);
        deserialized.Items["y"][0].Y.Should().Be(8f);
    }

    // Edge Case P7: Dictionary<string, Dictionary<int, ExternalVector3>>
    [Fact]
    public void ProxyType_NestedDict_RoundTrip()
    {
        var model = new ProxyNestedDictTestMessage
        {
            Data = new Dictionary<string, Dictionary<int, ExternalVector3>>
            {
                ["inner"] = new Dictionary<int, ExternalVector3>
                {
                    [1] = new ExternalVector3(10f, 20f, 30f),
                    [2] = new ExternalVector3(40f, 50f, 60f)
                }
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyNestedDictTestMessage);
        var deserialized = Deserializers.DeserializeProxyNestedDictTestMessage(data);

        deserialized.Data.Should().HaveCount(1);
        deserialized.Data!["inner"].Should().HaveCount(2);
        deserialized.Data["inner"][1].X.Should().Be(10f);
        deserialized.Data["inner"][2].Z.Should().Be(60f);
    }

    // Кейс P3: 2-параметровий [ProxyWrap] — pooling proxy roundtrip
    [Fact]
    public void ProxyType_TwoParamCreate_RoundTrip()
    {
        SizeProxy.CreateCallCount = 0;

        var model = new ProxyPoolingTestMessage
        {
            Size = new ExternalSize(800, 600),
            Sizes = new List<ExternalSize>
            {
                new(1920, 1080),
                new(2560, 1440)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyPoolingTestMessage);
        var deserialized = Deserializers.DeserializeProxyPoolingTestMessage(data);

        deserialized.Size.Width.Should().Be(800);
        deserialized.Size.Height.Should().Be(600);
        deserialized.Sizes.Should().HaveCount(2);
        deserialized.Sizes![0].Width.Should().Be(1920);
        deserialized.Sizes[0].Height.Should().Be(1080);
        deserialized.Sizes[1].Width.Should().Be(2560);
        deserialized.Sizes[1].Height.Should().Be(1440);

        // Verify FromOriginal was called (with 2 params — source + default reuse)
        SizeProxy.CreateCallCount.Should().BeGreaterThan(0,
            "[ProxyWrap] with 2 parameters should be called during serialization");
    }

    // Edge Case A4.2: Two proxy types in separate dictionaries
    [Fact]
    public void ProxyType_TupleInDict_RoundTrip()
    {
        var model = new ProxyTupleInDictTest
        {
            Colors = new Dictionary<string, ExternalColor>
            {
                ["red"] = new ExternalColor { R = 255, G = 0, B = 0, A = 255 }
            },
            Vectors = new Dictionary<int, ExternalVector3>
            {
                [1] = new ExternalVector3(1f, 2f, 3f)
            }
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyTupleInDictTest);
        var deserialized = Deserializers.DeserializeProxyTupleInDictTest(data);

        deserialized.Colors.Should().HaveCount(1);
        deserialized.Colors!["red"].R.Should().Be(255);
        deserialized.Vectors.Should().HaveCount(1);
        deserialized.Vectors![1].X.Should().Be(1f);
        deserialized.Vectors[1].Y.Should().Be(2f);
        deserialized.Vectors[1].Z.Should().Be(3f);
    }

    [Fact]
    public void ProxyReturn_CalledDuringDeserialization()
    {
        var model = new ProxyTestMessage
        {
            Position = new ExternalVector3(1f, 2f, 3f),
            Name = "return-test"
        };

        var data = SerializeWithGProtobuf(model, Serializers.SerializeProxyTestMessage);

        Vector3Proxy.ReturnCallCount = 0;

        Deserializers.DeserializeProxyTestMessage(data);

        Vector3Proxy.ReturnCallCount.Should().BeGreaterThan(0,
            "[ProxyReturn] має бути викликаний під час десеріалізації");
    }
}

// Допоміжна модель для wire-compatibility тесту з protobuf-net
// Замість ExternalVector3 використовується Vector3Proxy напряму
[ProtoContract]
public class ProxyTestMessagePnet
{
    [ProtoMember(1)]
    public Vector3Proxy? Position { get; set; }

    [ProtoMember(2)]
    public string? Name { get; set; }
}

// Допоміжна модель для Dictionary wire-compatibility тесту з protobuf-net
[ProtoContract]
public class ProxyDictStructMessagePnet
{
    [ProtoMember(1)]
    public Dictionary<string, Vector3Proxy>? Positions { get; set; }
}
