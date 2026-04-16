using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Scalars;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Collections
{
    /// <summary>
    /// Packed int32 list serialization/deserialization, scanned across
    /// N = 0, 1, 10, 1000, 100000. Reveals OnePass pool-growth behavior
    /// and deserialize allocation patterns as list size crosses buffer
    /// and LOH thresholds.
    /// <para>
    /// Reuses the <see cref="Int32FixedListModel"/> wire format for
    /// direct comparison with Shape-parameterized Int32FixedBenchmark.
    /// </para>
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class PackedInt32ScalingBenchmark : SerdeBenchmarkBase<Int32FixedListModel>
    {
        [Params(0, 1, 10, 1000, 100_000)]
        public int N;

        protected override Int32FixedListModel BuildModel()
        {
            var list = new List<int>(N);
            for (int i = 0; i < N; i++) list.Add(i);
            return new Int32FixedListModel { Values = list };
        }

        protected override byte[] PreSerialize(Int32FixedListModel model)
        {
            using var ms = new MemoryStream();
            Models.Scalars.Serialization.Serializers.SerializeInt32FixedListModel(ms, model);
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

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public Int32FixedListModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<Int32FixedListModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public Int32FixedListModel GProtobuf_De()
            => Models.Scalars.Serialization.Deserializers.DeserializeInt32FixedListModel(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Scalars.Serialization.SizeCalculators.CalculateInt32FixedListModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
