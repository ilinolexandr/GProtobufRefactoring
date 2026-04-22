using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;

namespace GProtobuf.Benchmark.AmbientScope;

/// <summary>Base with manual-hook baseline; concrete subclasses add Generated-hook variants for AdHoc vs LongLived comparison.</summary>
public abstract class AmbientScopeBenchmarkBase<T> : SerdeBenchmarkBase<T>
{
    protected abstract long SerializeViaGProto();

    [Benchmark(Baseline = true), BenchmarkCategory("Serialize")]
    public long GProto_TwoPass() => SerializeViaGProto();

    [Benchmark, BenchmarkCategory("Serialize")]
    public long GProto_ManualHooks()
    {
        DataTypePoolHandler.Before();
        try { return SerializeViaGProto(); }
        finally { DataTypePoolHandler.After(); }
    }
}

public abstract class AmbientScopeLongLivedBenchmarkBase<T> : AmbientScopeBenchmarkBase<T>
{
    public override void Setup()
    {
        base.Setup();
        DataTypePoolHandler.MarkThreadAsLongLived();
    }
}
