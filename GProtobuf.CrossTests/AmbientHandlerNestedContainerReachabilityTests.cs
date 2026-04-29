using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GProtobuf;
using GProtobuf.CrossTests.AmbientTests;
using GProtobuf.CrossTests.AmbientTests.Serialization;

namespace GProtobuf.CrossTests.AmbientTests
{
    /// <summary>
    /// Carrier whose graph reaches <see cref="AmbientMarker"/> only through nested
    /// non-<c>[ProtoContract]</c> generic containers — the shape that previously caused
    /// <see cref="GProtobuf.Generator.Analysis.AmbientHandlerReachabilityAnalyzer"/> to halt
    /// at the first unregistered container hop and silently drop the Before/After wrap.
    /// Mirrors the real-world <c>DeviceValuesDiff.instantValues</c> shape.
    /// </summary>
    [ProtoContract]
    public sealed partial class AmbientNestedContainerCarrier
    {
        [ProtoMember(1)] public Dictionary<int, Dictionary<uint, Dictionary<int, AmbientMarker>>> InstantValues { get; set; }
        [ProtoMember(2)] public Dictionary<int, List<AmbientMarker>> ListPerKey { get; set; }
        [ProtoMember(3)] public string Tag { get; set; }
    }
}

namespace GProtobuf.CrossTests
{
    /// <summary>
    /// Regression test for the nested-container reachability fix in
    /// <c>AmbientHandlerReachabilityAnalyzer.Descend</c>: even when the marker is reachable
    /// only via nested non-<c>[ProtoContract]</c> generic collections, the emitted entry-point
    /// must be wrapped with the handler's Before/After.
    /// </summary>
    [Collection("AmbientHandler")]
    public sealed class AmbientHandlerNestedContainerReachabilityTests
    {
        private static AmbientNestedContainerCarrier BuildCarrier() => new()
        {
            InstantValues = new Dictionary<int, Dictionary<uint, Dictionary<int, AmbientMarker>>>
            {
                [1] = new Dictionary<uint, Dictionary<int, AmbientMarker>>
                {
                    [10u] = new Dictionary<int, AmbientMarker>
                    {
                        [100] = new AmbientMarker { Value = 7 },
                    },
                },
            },
            ListPerKey = new Dictionary<int, List<AmbientMarker>>
            {
                [2] = new List<AmbientMarker> { new AmbientMarker { Value = 9 } },
            },
            Tag = "nested",
        };

        /// <summary>
        /// Outermost <c>Serialize</c> on a carrier whose marker is reachable only through
        /// <c>Dictionary&lt;int, Dictionary&lt;uint, Dictionary&lt;int, AmbientMarker&gt;&gt;&gt;</c>
        /// must fire the ambient handler exactly once. Pre-fix this counted zero because the
        /// walker halted at the second-hop unregistered <c>Dictionary&lt;uint, ...&gt;</c>.
        /// </summary>
        [Fact]
        public void Serialize_ReachesMarkerThroughNestedDictionaries_FiresHook()
        {
            AmbientTestPoolHandler.Reset();
            AmbientTestPoolHandler.UnmarkLongLived();

            using var ms = new MemoryStream();
            Serializers.Serialize(ms, BuildCarrier());

            AmbientTestPoolHandler.BeforeCount.Should().Be(1, "marker is reachable through nested non-contract containers");
            AmbientTestPoolHandler.AfterCount.Should().Be(1);
        }

        [Fact]
        public void Roundtrip_PreservesNestedContainerPayload()
        {
            AmbientTestPoolHandler.Reset();

            using var ms = new MemoryStream();
            Serializers.Serialize(ms, BuildCarrier());
            var bytes = ms.ToArray();

            var back = Deserializers.DeserializeAmbientNestedContainerCarrier(bytes.AsSpan());

            back.Tag.Should().Be("nested");
            back.InstantValues[1][10u][100].Value.Should().Be(7);
            back.ListPerKey[2][0].Value.Should().Be(9);
            AmbientTestPoolHandler.BeforeCount.Should().Be(2, "serialize + deserialize each fire one outer hook");
            AmbientTestPoolHandler.AfterCount.Should().Be(2);
        }
    }
}
