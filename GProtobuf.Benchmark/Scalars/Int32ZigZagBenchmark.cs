using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Scalars;
using GProtobuf.Core;
using static GProtobuf.Benchmark.Infrastructure.TestDataFactory;

namespace GProtobuf.Benchmark.Scalars
{
    /// <summary>
    /// int32 ZigZag varint encoding. Expected to outperform Default encoding
    /// on NegativeOne / MinValue (2-byte vs 10-byte per element) while
    /// matching on non-negative values.
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class Int32ZigZagBenchmark : SerdeBenchmarkBase<Int32ZigZagListModel>
    {
        public const int Count = 100;

        [Params(
            VarintShape.Zero,
            VarintShape.Boundary1Byte,
            VarintShape.Boundary2Byte,
            VarintShape.MaxValue,
            VarintShape.NegativeOne,
            VarintShape.MinValue)]
        public VarintShape Shape;

        protected override Int32ZigZagListModel BuildModel()
        {
            var list = new System.Collections.Generic.List<int>(Count);
            var v = Int32(Shape);
            for (int i = 0; i < Count; i++) list.Add(v);
            return new Int32ZigZagListModel { Values = list };
        }

        protected override byte[] PreSerialize(Int32ZigZagListModel model)
        {
            using var ms = new MemoryStream();
            Models.Scalars.Serialization.Serializers.SerializeInt32ZigZagListModel(ms, model);
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
            Models.Scalars.Serialization.OnePassStreamWriters.WriteInt32ZigZagListModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Scalars.Serialization.Serializers.SerializeInt32ZigZagListModel(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public Int32ZigZagListModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<Int32ZigZagListModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public Int32ZigZagListModel GProtobuf_De()
            => Models.Scalars.Serialization.Deserializers.DeserializeInt32ZigZagListModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Scalars.Serialization.SizeCalculators.CalculateInt32ZigZagListModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
