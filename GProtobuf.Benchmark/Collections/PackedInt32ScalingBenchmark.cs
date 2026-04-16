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
    /// <para>
    /// Each benchmark uses <c>OperationsPerInvoke = <see cref="Ops"/></c>
    /// with an inner loop so a single BDN iteration amortizes the per-op cost
    /// over enough work to dwarf GC pauses and JIT tier-up transients.
    /// Under <c>[MemoryDiagnoser]</c>, BDN forces <c>InvocationCount=1</c>,
    /// which otherwise means each iteration measures one single-digit-μs op —
    /// see PACKED_INT32_SCALING_INVESTIGATION.md for the noise-vs-signal
    /// analysis this was written against.
    /// </para>
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class PackedInt32ScalingBenchmark : SerdeBenchmarkBase<Int32FixedListModel>
    {
        private const int Ops = 1000;

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

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops), BenchmarkCategory("Serialize")]
        public long ProtobufNet_Ser()
        {
            long last = 0;
            for (int i = 0; i < Ops; i++)
            {
                Stream.SetLength(0);
                global::ProtoBuf.Serializer.Serialize(Stream, Model);
                last = Stream.Length;
            }
            return last;
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Serialize")]
        public long GProtobuf_OnePass_Ser()
        {
            long last = 0;
            for (int i = 0; i < Ops; i++)
            {
                Stream.SetLength(0);
                using var scope = new OnePassScope(OnePassHarness.Pool);
                var writer = new OnePassStreamWriter(Stream, OnePassHarness.TempBuffer, scope.Pool);
                Models.Scalars.Serialization.OnePassStreamWriters.WriteInt32FixedListModel(ref writer, Model);
                writer.Flush();
                last = Stream.Length;
            }
            return last;
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            long last = 0;
            for (int i = 0; i < Ops; i++)
            {
                Stream.SetLength(0);
                Models.Scalars.Serialization.Serializers.SerializeInt32FixedListModel(Stream, Model);
                last = Stream.Length;
            }
            return last;
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops), BenchmarkCategory("Deserialize")]
        public Int32FixedListModel ProtobufNet_De()
        {
            Int32FixedListModel last = null;
            for (int i = 0; i < Ops; i++)
                last = global::ProtoBuf.Serializer.Deserialize<Int32FixedListModel>((System.ReadOnlySpan<byte>)Serialized);
            return last;
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Deserialize")]
        public Int32FixedListModel GProtobuf_De()
        {
            Int32FixedListModel last = null;
            for (int i = 0; i < Ops; i++)
                last = Models.Scalars.Serialization.Deserializers.DeserializeInt32FixedListModel(Serialized);
            return last;
        }

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            int last = 0;
            for (int i = 0; i < Ops; i++)
            {
                var calc = new WriteSizeCalculator();
                Models.Scalars.Serialization.SizeCalculators.CalculateInt32FixedListModelContentSize(ref calc, Model);
                last = calc.Length;
            }
            return last;
        }
    }
}
