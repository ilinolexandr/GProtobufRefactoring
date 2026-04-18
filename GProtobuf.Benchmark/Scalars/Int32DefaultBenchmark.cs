using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Scalars;
using GProtobuf.Core;
using static GProtobuf.Benchmark.Infrastructure.TestDataFactory;

namespace GProtobuf.Benchmark.Scalars
{
    /// <summary>
    /// int32 default varint encoding. Parameterized across varint boundaries
    /// (0 / 1 byte / 2 byte / MaxValue / NegativeOne [10 bytes!] / MinValue).
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class Int32DefaultBenchmark : SerdeBenchmarkBase<Int32DefaultListModel>
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

        protected override Int32DefaultListModel BuildModel()
        {
            var list = new System.Collections.Generic.List<int>(Count);
            var v = Int32(Shape);
            for (int i = 0; i < Count; i++) list.Add(v);
            return new Int32DefaultListModel { Values = list };
        }

        protected override byte[] PreSerialize(Int32DefaultListModel model)
        {
            using var ms = new MemoryStream();
            Models.Scalars.Serialization.Serializers.Serialize(ms, model);
            return ms.ToArray();
        }

        // ── Serialize ─────────────────────────────────────────────────────────

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
            Models.Scalars.Serialization.OnePassStreamWriters.WriteInt32DefaultListModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Scalars.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public Int32DefaultListModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<Int32DefaultListModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public Int32DefaultListModel GProtobuf_De()
            => Models.Scalars.Serialization.Deserializers.DeserializeInt32DefaultListModel(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Scalars.Serialization.SizeCalculators.CalculateInt32DefaultListModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
