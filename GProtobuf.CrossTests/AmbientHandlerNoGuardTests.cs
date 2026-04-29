using System.IO;
using System.Threading;
using FluentAssertions;
using GProtobuf;
using GProtobuf.CrossTests.NoGuardTests;
using GProtobuf.CrossTests.NoGuardTests.Serialization;

// Second handler registered with AutoReentrancyGuard = false — this is the mode TapHome's DataTypePoolHandler uses.
[assembly: AmbientSerializationHandler(
    typeof(NoGuardMarker),
    typeof(NoGuardPoolHandler),
    AutoReentrancyGuard = false)]
[assembly: SerializationProxy(typeof(NoGuardMarker), typeof(NoGuardMarkerProxy))]

namespace GProtobuf.CrossTests.NoGuardTests
{
    public sealed class NoGuardMarker
    {
        public int X { get; set; }
    }

    [ProtoContract]
    public sealed partial class NoGuardMarkerProxy
    {
        [ProtoMember(1)] public int X { get; set; }

        [ProxyWrap]
        public static NoGuardMarkerProxy Wrap(NoGuardMarker src) => new() { X = src.X };

        [ProxyConvert]
        public NoGuardMarker Convert() => new() { X = X };
    }

    [ProtoContract]
    public sealed partial class NoGuardCarrier
    {
        [ProtoMember(1)] public NoGuardMarker Marker { get; set; }
        [ProtoMember(2)] public string Tag { get; set; }
    }

    public static class NoGuardPoolHandler
    {
        public static long BeforeCount;
        public static long AfterCount;

        public static void Reset()
        {
            Interlocked.Exchange(ref BeforeCount, 0);
            Interlocked.Exchange(ref AfterCount, 0);
        }

        public static void Before() => Interlocked.Increment(ref BeforeCount);
        public static void After() => Interlocked.Increment(ref AfterCount);
    }
}

namespace GProtobuf.CrossTests
{
    /// <summary>Verifies generator emits wrap WITHOUT reentrancy guard when AutoReentrancyGuard=false.</summary>
    [Collection("AmbientHandler")]
    public sealed class AmbientHandlerNoGuardTests
    {
        private static NoGuardCarrier BuildCarrier() => new()
        {
            Marker = new NoGuardMarker { X = 42 },
            Tag = "no-guard",
        };

        /// <summary>Single Serialize still fires exactly one Before/After — guard-off doesn't duplicate.</summary>
        [Fact]
        public void Serialize_FiresHookExactlyOnce()
        {
            NoGuardPoolHandler.Reset();

            using var ms = new MemoryStream();
            Serializers.Serialize(ms, BuildCarrier());

            NoGuardPoolHandler.BeforeCount.Should().Be(1);
            NoGuardPoolHandler.AfterCount.Should().Be(1);
        }

        /// <summary>Manual outer Before + inner Serialize: without guard, both fire — that's the contract of AutoReentrancyGuard=false.</summary>
        [Fact]
        public void ManualOuterScope_InnerSerialize_FiresAgain()
        {
            NoGuardPoolHandler.Reset();

            NoGuardPoolHandler.Before();
            try
            {
                using var ms = new MemoryStream();
                Serializers.Serialize(ms, BuildCarrier());
            }
            finally
            {
                NoGuardPoolHandler.After();
            }

            NoGuardPoolHandler.BeforeCount.Should().Be(2,
                "AutoReentrancyGuard=false: inner Serialize is not suppressed by an outer manual scope");
            NoGuardPoolHandler.AfterCount.Should().Be(2);
        }

        /// <summary>Roundtrip still preserves payload through un-guarded wrap.</summary>
        [Fact]
        public void Roundtrip_PreservesPayload()
        {
            NoGuardPoolHandler.Reset();

            using var ms = new MemoryStream();
            Serializers.Serialize(ms, BuildCarrier());
            var bytes = ms.ToArray();

            var back = Deserializers.DeserializeNoGuardCarrier(bytes.AsSpan());

            back.Marker.X.Should().Be(42);
            back.Tag.Should().Be("no-guard");
            NoGuardPoolHandler.BeforeCount.Should().Be(2, "one Before on serialize, one on deserialize");
            NoGuardPoolHandler.AfterCount.Should().Be(2);
        }
    }
}
