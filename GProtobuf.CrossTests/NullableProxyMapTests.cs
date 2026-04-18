using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GProtobuf.Tests.Serialization;
using GProtobuf;
namespace GProtobuf.Tests;

// ==================== Reproduction model ====================

/// <summary>
/// Reproduction for bug: Dictionary&lt;K, V?&gt; where V is a struct with [SerializationProxy].
/// Generator emits FromOriginal(value) without .Value unwrap and without HasValue check,
/// causing CS1503 in OnePassStreamWriter map entry method.
/// </summary>
[ProtoContract]
public partial class NullableProxyMapTestMessage
{
    [ProtoMember(1)]
    public Dictionary<int, ExternalVector3?>? NullableVectors { get; set; }
}


// ==================== Tests ====================

public sealed class NullableProxyMapTests : BaseSerializationTest
{
    private static NullableProxyMapTestMessage MakeModel()
    {
        return new NullableProxyMapTestMessage
        {
            NullableVectors = new Dictionary<int, ExternalVector3?>
            {
                [1] = new ExternalVector3(1f, 2f, 3f),
                [2] = null,
                [3] = new ExternalVector3(4f, 5f, 6f),
            }
        };
    }

    private static void AssertModel(NullableProxyMapTestMessage deserialized)
    {
        deserialized.NullableVectors.Should().NotBeNull();
        // null values should be skipped (entry contains key only)
        deserialized.NullableVectors!.Should().ContainKey(1);
        deserialized.NullableVectors[1].Should().NotBeNull();
        deserialized.NullableVectors[1]!.Value.X.Should().Be(1f);
        deserialized.NullableVectors[1]!.Value.Y.Should().Be(2f);
        deserialized.NullableVectors[1]!.Value.Z.Should().Be(3f);

        deserialized.NullableVectors.Should().ContainKey(3);
        deserialized.NullableVectors[3]!.Value.X.Should().Be(4f);

        // key 2 had null value -> entry should round-trip with default(T?) == null
        if (deserialized.NullableVectors.TryGetValue(2, out var v2))
        {
            v2.Should().BeNull();
        }
    }

    [Fact]
    public void NullableProxyMap_StreamWriter_RoundTrip()
    {
        var model = MakeModel();
        var data = SerializeWithGProtobuf(model, Serializers.Serialize);
        var deserialized = Deserializers.DeserializeNullableProxyMapTestMessage(data);
        AssertModel(deserialized);
    }

    [Fact]
    public void NullableProxyMap_OnePass_RoundTrip()
    {
        var model = MakeModel();
        using var ms = new MemoryStream();
        Serializers.SerializeOnePass(ms, model);
        var data = ms.ToArray();
        var deserialized = Deserializers.DeserializeNullableProxyMapTestMessage(data);
        AssertModel(deserialized);
    }
}
