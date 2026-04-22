using System.IO;
using GProtobuf.Benchmark.Models;
using GProtobuf.Benchmark.Models.Serialization;

namespace GProtobuf.Benchmark.AmbientScope;

/// <summary>Regression control: model has no marker in its graph, so the generator must not emit any wrap.</summary>
public class NoMarkerRegressionBenchmark_AdHoc : AmbientScopeBenchmarkBase<PrimitiveTypesModel>
{
    protected override PrimitiveTypesModel BuildModel() => new()
    {
        IntValue = 42,
        LongValue = 9_999_999_999L,
        FloatValue = 3.14f,
        DoubleValue = 2.71828,
        BoolValue = true,
        StringValue = "hello-ambient",
        ByteArrayValue = new byte[] { 1, 2, 3, 4, 5 },
        FixedIntValue = 123_456,
        FixedLongValue = 987_654_321L,
        ZigZagIntValue = -100,
        ZigZagLongValue = -200L,
    };

    protected override byte[] PreSerialize(PrimitiveTypesModel model)
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

public class NoMarkerRegressionBenchmark_LongLived : AmbientScopeLongLivedBenchmarkBase<PrimitiveTypesModel>
{
    protected override PrimitiveTypesModel BuildModel() => new()
    {
        IntValue = 42,
        LongValue = 9_999_999_999L,
        FloatValue = 3.14f,
        DoubleValue = 2.71828,
        BoolValue = true,
        StringValue = "hello-ambient",
        ByteArrayValue = new byte[] { 1, 2, 3, 4, 5 },
        FixedIntValue = 123_456,
        FixedLongValue = 987_654_321L,
        ZigZagIntValue = -100,
        ZigZagLongValue = -200L,
    };

    protected override byte[] PreSerialize(PrimitiveTypesModel model)
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
