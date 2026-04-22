using System.IO;
using GProtobuf.Benchmark.AmbientScope.Serialization;
using GProtobuf.Benchmark.Infrastructure;

namespace GProtobuf.Benchmark.AmbientScope;

public class DeviceValueBenchmark_AdHoc : AmbientScopeBenchmarkBase<BenchDeviceValue>
{
    protected override BenchDeviceValue BuildModel() => new()
    {
        ValueType = 3,
        Primitive = new BenchPrimitive
        {
            ValueTypeId = 1,
            ValueBytes = 0x40490FDB,
            StringPayload = null,
        },
        Timestamp = 1_750_000_000L,
    };

    protected override byte[] PreSerialize(BenchDeviceValue model)
    {
        using var ms = new MemoryStream();
        Serializers.Serialize(ms, model);
        return ms.ToArray();
    }

    protected override long SerializeViaGProto()
    {
        Stream.Position = 0;
        Serializers.Serialize(Stream, Model);
        return Stream.Position;
    }
}

public class DeviceValueBenchmark_LongLived : AmbientScopeLongLivedBenchmarkBase<BenchDeviceValue>
{
    protected override BenchDeviceValue BuildModel() => new()
    {
        ValueType = 3,
        Primitive = new BenchPrimitive
        {
            ValueTypeId = 1,
            ValueBytes = 0x40490FDB,
            StringPayload = null,
        },
        Timestamp = 1_750_000_000L,
    };

    protected override byte[] PreSerialize(BenchDeviceValue model)
    {
        using var ms = new MemoryStream();
        Serializers.Serialize(ms, model);
        return ms.ToArray();
    }

    protected override long SerializeViaGProto()
    {
        Stream.Position = 0;
        Serializers.Serialize(Stream, Model);
        return Stream.Position;
    }
}
