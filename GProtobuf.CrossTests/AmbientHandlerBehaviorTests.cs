using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GProtobuf.CrossTests.AmbientTests;
using GProtobuf.CrossTests.AmbientTests.Serialization;

namespace GProtobuf.CrossTests;

/// <summary>Behavioural contract tests for AmbientSerializationHandler: reentrancy guard, thread-state, exception safety, cross-thread.</summary>
[Collection("AmbientHandler")]
public sealed class AmbientHandlerBehaviorTests
{
    private static AmbientCarrier BuildCarrier() => new()
    {
        Marker = new AmbientMarker { Value = 42 },
        Tag = "ambient-test",
    };

    [Fact]
    public void Serialize_FiresHookExactlyOnce()
    {
        AmbientTestPoolHandler.Reset();
        AmbientTestPoolHandler.UnmarkLongLived();

        using var ms = new MemoryStream();
        Serializers.Serialize(ms, BuildCarrier());

        AmbientTestPoolHandler.BeforeCount.Should().Be(1, "hook fires once per outer Serialize call");
        AmbientTestPoolHandler.AfterCount.Should().Be(1, "After fires once in finally");
    }

    /// <summary>Span roundtrip fires hooks once on each side.</summary>
    [Fact]
    public void Roundtrip_SpanPath_PreservesPayload()
    {
        AmbientTestPoolHandler.Reset();

        using var ms = new MemoryStream();
        Serializers.Serialize(ms, BuildCarrier());
        var bytes = ms.ToArray();

        var back = Deserializers.DeserializeAmbientCarrier(bytes.AsSpan());

        back.Marker.Value.Should().Be(42);
        back.Tag.Should().Be("ambient-test");
        AmbientTestPoolHandler.BeforeCount.Should().Be(2, "serialize + deserialize each fire one outer hook");
        AmbientTestPoolHandler.AfterCount.Should().Be(2);
    }

    /// <summary>Long-lived thread accounts as long-lived.</summary>
    [Fact]
    public void T19_Serialize_OnLongLivedThread_RegistersAsLongLived()
    {
        RunOnFreshThread(() =>
        {
            AmbientTestPoolHandler.Reset();
            AmbientTestPoolHandler.MarkLongLived();
            try
            {
                using var ms = new MemoryStream();
                Serializers.Serialize(ms, BuildCarrier());

                AmbientTestPoolHandler.BeforeCount.Should().Be(1);
                AmbientTestPoolHandler.BeforeCount_OnLongLivedThreads.Should().Be(1);
                AmbientTestPoolHandler.BeforeCount_OnAdHocThreads.Should().Be(0);
            }
            finally
            {
                AmbientTestPoolHandler.UnmarkLongLived();
            }
        });
    }

    /// <summary>Unmarked thread accounts as ad-hoc.</summary>
    [Fact]
    public void T20_Serialize_OnAdHocThread_RegistersAsAdHoc()
    {
        RunOnFreshThread(() =>
        {
            AmbientTestPoolHandler.Reset();
            AmbientTestPoolHandler.UnmarkLongLived();

            using var ms = new MemoryStream();
            Serializers.Serialize(ms, BuildCarrier());

            AmbientTestPoolHandler.BeforeCount_OnAdHocThreads.Should().Be(1);
            AmbientTestPoolHandler.BeforeCount_OnLongLivedThreads.Should().Be(0);
        });
    }

    /// <summary>Nested Serialize inside an already-entered guard scope fires Before/After exactly once total.</summary>
    [Fact]
    public void T21_ReentrantSerialize_AutoGuard_FiresOnce()
    {
        AmbientTestPoolHandler.Reset();
        AmbientTestPoolHandler.UnmarkLongLived();

        using var ms = new MemoryStream();
        global::GProtobuf.__Generated.AmbientGuards.__GProtoHandlerGuard_GProtobuf_CrossTests_AmbientTests_AmbientTestPoolHandler.Enter();
        AmbientTestPoolHandler.Before();
        try
        {
            Serializers.Serialize(ms, BuildCarrier());
        }
        finally
        {
            AmbientTestPoolHandler.After();
            global::GProtobuf.__Generated.AmbientGuards.__GProtoHandlerGuard_GProtobuf_CrossTests_AmbientTests_AmbientTestPoolHandler.Exit();
        }

        AmbientTestPoolHandler.BeforeCount.Should().Be(1, "guard prevents inner Before");
        AmbientTestPoolHandler.AfterCount.Should().Be(1, "guard prevents inner After");
    }

    /// <summary>Exception in Before() leaves the depth counter consistent for the next outer call.</summary>
    [Fact]
    public void T24_ExceptionInBefore_GuardRecovers()
    {
        RunOnFreshThread(() =>
        {
            AmbientTestPoolHandler.Reset();
            AmbientTestPoolHandler.ThrowOnNextBefore = true;

            using var ms = new MemoryStream();
            Action act = () => Serializers.Serialize(ms, BuildCarrier());
            act.Should().Throw<InvalidOperationException>().WithMessage("injected-from-Before");

            AmbientTestPoolHandler.AfterCount.Should().Be(0);

            AmbientTestPoolHandler.ThrowOnNextBefore = false;
            using var ms2 = new MemoryStream();
            Serializers.Serialize(ms2, BuildCarrier());

            AmbientTestPoolHandler.BeforeCount.Should().Be(1,
                "guard recovered after the throw so the next outer Serialize counted as outermost");
            AmbientTestPoolHandler.AfterCount.Should().Be(1);
        });
    }

    /// <summary>Fresh worker thread does not inherit MarkLongLived from another thread.</summary>
    [Fact]
    public async Task T27_CrossThread_NewThreadIsAdHoc()
    {
        RunOnFreshThread(() =>
        {
            AmbientTestPoolHandler.Reset();
            AmbientTestPoolHandler.MarkLongLived();
        });

        await Task.Run(() =>
        {
            using var ms = new MemoryStream();
            Serializers.Serialize(ms, BuildCarrier());

            AmbientTestPoolHandler.BeforeCount_OnAdHocThreads.Should().Be(1,
                "new thread should not inherit MarkLongLived from another thread");
            AmbientTestPoolHandler.BeforeCount_OnLongLivedThreads.Should().Be(0);
        });
    }

    private static void RunOnFreshThread(Action action)
    {
        Exception captured = null;
        var t = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { captured = ex; }
        });
        t.IsBackground = true;
        t.Start();
        t.Join();
        if (captured != null) throw captured;
    }
}
