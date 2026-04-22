using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.AmbientScope.NoAmbient;
using GProtobuf.Benchmark.AmbientScope.NoAmbient.Serialization;
using GProtobuf.Benchmark.Infrastructure;

namespace GProtobuf.Benchmark.AmbientScope;

/// <summary>Baseline against a model with no ambient marker in its graph — generator emits no wrap.</summary>
public class DeviceValueBenchmark_NoAmbient : SerdeBenchmarkBase<BenchDeviceValueNoAmbient>
{
    protected override BenchDeviceValueNoAmbient BuildModel() => new()
    {
        ValueType = 3,
        Primitive = new BenchPrimitiveNoAmbient
        {
            ValueTypeId = 1,
            ValueBytes = 0x40490FDB,
            StringPayload = null,
        },
        Timestamp = 1_750_000_000L,
    };

    protected override byte[] PreSerialize(BenchDeviceValueNoAmbient model)
    {
        using var ms = new MemoryStream();
        Serializers.Serialize(ms, model);
        return ms.ToArray();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Serialize")]
    public long GProto_TwoPass_NoAmbient()
    {
        Stream.Position = 0;
        Serializers.Serialize(Stream, Model);
        return Stream.Position;
    }
}
