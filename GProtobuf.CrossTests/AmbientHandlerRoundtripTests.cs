using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GProtobuf;
using GProtobuf.CrossTests;
using GProtobuf.CrossTests.AmbientTests;

// Guard enabled so re-entrancy and fault-recovery tests exercise the guarded emission.
[assembly: AmbientSerializationHandler(
    typeof(AmbientMarker),
    typeof(AmbientTestPoolHandler),
    AutoReentrancyGuard = true)]
[assembly: SerializationProxy(typeof(AmbientMarker), typeof(AmbientMarkerProxy))]

namespace GProtobuf.CrossTests.AmbientTests;

/// <summary>Marker type reachable via AmbientCarrier.Marker.</summary>
public sealed class AmbientMarker
{
    public int Value { get; set; }
}

/// <summary>Proxy for AmbientMarker (original type has no <c>[ProtoContract]</c>).</summary>
[ProtoContract]
public sealed partial class AmbientMarkerProxy
{
    [ProtoMember(1)] public int Value { get; set; }

    [ProxyWrap]
    public static AmbientMarkerProxy Wrap(AmbientMarker src)
        => new() { Value = src.Value };

    [ProxyConvert]
    public AmbientMarker Convert() => new() { Value = Value };
}

[ProtoContract]
public sealed partial class AmbientCarrier
{
    [ProtoMember(1)] public AmbientMarker Marker { get; set; }
    [ProtoMember(2)] public string Tag { get; set; }
}

/// <summary>Test double handler with counters and thread-state / fault-injection switches.</summary>
public static class AmbientTestPoolHandler
{
    [ThreadStatic] private static bool _markedLongLived;
    public static long BeforeCount;
    public static long AfterCount;
    public static long BeforeCount_OnAdHocThreads;
    public static long BeforeCount_OnLongLivedThreads;
    public static bool ThrowOnNextBefore;

    public static void MarkLongLived() => _markedLongLived = true;
    public static void UnmarkLongLived() => _markedLongLived = false;

    public static void Reset()
    {
        Interlocked.Exchange(ref BeforeCount, 0);
        Interlocked.Exchange(ref AfterCount, 0);
        Interlocked.Exchange(ref BeforeCount_OnAdHocThreads, 0);
        Interlocked.Exchange(ref BeforeCount_OnLongLivedThreads, 0);
        ThrowOnNextBefore = false;
    }

    public static void Before()
    {
        if (ThrowOnNextBefore)
        {
            ThrowOnNextBefore = false;
            throw new InvalidOperationException("injected-from-Before");
        }
        Interlocked.Increment(ref BeforeCount);
        if (_markedLongLived) Interlocked.Increment(ref BeforeCount_OnLongLivedThreads);
        else Interlocked.Increment(ref BeforeCount_OnAdHocThreads);
    }

    public static void After() => Interlocked.Increment(ref AfterCount);
}
