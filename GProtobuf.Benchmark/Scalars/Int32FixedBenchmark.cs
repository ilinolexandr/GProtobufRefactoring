using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Scalars;
using GProtobuf.Core;
using static GProtobuf.Benchmark.Infrastructure.TestDataFactory;

namespace GProtobuf.Benchmark.Scalars
{
    /// <summary>
    /// int32 fixed32 encoding (constant 4 bytes per value). Payload size is
    /// invariant across Shape; Mean should also be ~constant — serves as
    /// an invariance check and a per-value cost baseline vs varint encodings.
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class Int32FixedBenchmark : SerdeBenchmarkBase<Int32FixedListModel>
    {
        public const int Count = 100;

        [Params(
            VarintShape.Zero,
            VarintShape.MaxValue,
            VarintShape.NegativeOne,
            VarintShape.MinValue)]
        public VarintShape Shape;

        protected override Int32FixedListModel BuildModel()
        {
            var list = new System.Collections.Generic.List<int>(Count);
            var v = Int32(Shape);
            for (int i = 0; i < Count; i++) list.Add(v);
            return new Int32FixedListModel { Values = list };
        }

        protected override byte[] PreSerialize(Int32FixedListModel model)
        {
            using var ms = new MemoryStream();
            Models.Scalars.Serialization.Serializers.SerializeInt32FixedListModel(ms, model);
            return ms.ToArray();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Serialize")]
        public long ProtobufNet_Ser()
        {
            global::ProtoBuf.Serializer.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_OnePass_Ser()
        {
            using var scope = new OnePassScope(OnePassHarness.Pool);
            var writer = new OnePassStreamWriter(Stream, OnePassHarness.TempBuffer, scope.Pool);
            Models.Scalars.Serialization.OnePassStreamWriters.WriteInt32FixedListModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Scalars.Serialization.Serializers.SerializeInt32FixedListModel(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public Int32FixedListModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<Int32FixedListModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public Int32FixedListModel GProtobuf_De()
            => Models.Scalars.Serialization.Deserializers.DeserializeInt32FixedListModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Scalars.Serialization.SizeCalculators.CalculateInt32FixedListModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
