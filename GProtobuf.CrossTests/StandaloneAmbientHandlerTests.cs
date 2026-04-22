using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GProtobuf;
using GProtobuf.Core;
using GProtobuf.CrossTests.AmbientTests;
using GProtobuf.CrossTests.AmbientTests.Serialization;

// Register standalone serializers whose element / value type carries the ambient marker.
[assembly: GenerateSerializer(typeof(List<AmbientMarker>))]
[assembly: GenerateSerializer(typeof(AmbientMarker[]))]
[assembly: GenerateSerializer(typeof(Dictionary<string, AmbientMarker>))]

namespace GProtobuf.CrossTests;

/// <summary>Ambient-handler wrap coverage for standalone List/Array/Dictionary entry-points.</summary>
[Collection("AmbientHandler")]
public sealed class StandaloneAmbientHandlerTests
{
    private static List<AmbientMarker> BuildList() => new()
    {
        new AmbientMarker { Value = 1 },
        new AmbientMarker { Value = 2 },
    };

    private static AmbientMarker[] BuildArray() => new[]
    {
        new AmbientMarker { Value = 10 },
        new AmbientMarker { Value = 20 },
    };

    private static Dictionary<string, AmbientMarker> BuildDict() => new()
    {
        ["a"] = new AmbientMarker { Value = 7 },
        ["b"] = new AmbientMarker { Value = 8 },
    };

    [Fact]
    public void Serialize_ListOfMarker_FiresHookOnce()
    {
        AmbientTestPoolHandler.Reset();
        AmbientTestPoolHandler.UnmarkLongLived();

        using var ms = new MemoryStream();
        Serializers.SerializeListOfAmbientMarker(ms, BuildList());

        AmbientTestPoolHandler.BeforeCount.Should().Be(1, "outer SerializeListOfAmbientMarker is the single wrapped entry-point");
        AmbientTestPoolHandler.AfterCount.Should().Be(1);
    }

    [Fact]
    public void Roundtrip_ListOfMarker_FiresHookOnceEachSide()
    {
        AmbientTestPoolHandler.Reset();
        AmbientTestPoolHandler.UnmarkLongLived();

        using var ms = new MemoryStream();
        Serializers.SerializeListOfAmbientMarker(ms, BuildList());
        var bytes = ms.ToArray();
        var back = Deserializers.DeserializeListOfAmbientMarker(bytes.AsSpan());

        back.Should().NotBeNull();
        back.Count.Should().Be(2);
        back[0].Value.Should().Be(1);
        back[1].Value.Should().Be(2);
        AmbientTestPoolHandler.BeforeCount.Should().Be(2, "serialize + deserialize each fire one outer hook");
        AmbientTestPoolHandler.AfterCount.Should().Be(2);
    }

    [Fact]
    public void Serialize_ArrayOfMarker_FiresHookOnce()
    {
        AmbientTestPoolHandler.Reset();
        AmbientTestPoolHandler.UnmarkLongLived();

        using var ms = new MemoryStream();
        Serializers.SerializeArrayOfAmbientMarker(ms, BuildArray());

        AmbientTestPoolHandler.BeforeCount.Should().Be(1);
        AmbientTestPoolHandler.AfterCount.Should().Be(1);
    }

    [Fact]
    public void Serialize_DictionaryStringToMarker_FiresHookOnce()
    {
        AmbientTestPoolHandler.Reset();
        AmbientTestPoolHandler.UnmarkLongLived();

        using var ms = new MemoryStream();
        Serializers.SerializeDictionaryOfStringAndAmbientMarker(ms, BuildDict());

        AmbientTestPoolHandler.BeforeCount.Should().Be(1, "value type carries marker — one wrap around the dictionary entry-point");
        AmbientTestPoolHandler.AfterCount.Should().Be(1);
    }

    [Fact]
    public void Roundtrip_DictionaryStringToMarker_FiresHookOnceEachSide()
    {
        AmbientTestPoolHandler.Reset();
        AmbientTestPoolHandler.UnmarkLongLived();

        using var ms = new MemoryStream();
        Serializers.SerializeDictionaryOfStringAndAmbientMarker(ms, BuildDict());
        var bytes = ms.ToArray();
        var back = Deserializers.DeserializeDictionaryOfStringAndAmbientMarker(bytes.AsSpan());

        back.Should().NotBeNull();
        back.Count.Should().Be(2);
        back["a"].Value.Should().Be(7);
        back["b"].Value.Should().Be(8);
        AmbientTestPoolHandler.BeforeCount.Should().Be(2);
        AmbientTestPoolHandler.AfterCount.Should().Be(2);
    }
}
