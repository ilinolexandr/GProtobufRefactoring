using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Messages;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Messages
{
    // =====================================================================
    //  ObjectArrayBuilderDeepDiveBenchmark
    //
    //  Diagnostic sweep built to localize the OnePass-vs-TwoPass regression
    //  observed in ObjectArrayBuilderBenchmark (OnePass 2.1-2.65x slower
    //  than TwoPass across N = 10..500, and 1.7x slower than protobuf-net
    //  at N = 500).
    //
    //  Split by element shape into four classes so the per-element framing
    //  cost is not mixed with the per-field payload cost inside one
    //  [Params] matrix:
    //
    //      ArrayIntOnlyDeepDive           — single int per element; isolates
    //                                       sub-message framing + buffer-
    //                                       chain rent/return cost.
    //      ArrayFivePrimitiveDeepDive     — 5 primitives per element;
    //                                       compared to IntOnly reveals
    //                                       per-field write cost when
    //                                       framing is held constant.
    //      ArrayNestedSubmessageDeepDive  — element contains another sub-
    //                                       message; exposes cost of
    //                                       nested length-delimited writes
    //                                       (hypothesis a: OnePass pays an
    //                                       extra shift/copy to materialize
    //                                       each prefix length).
    //      ArrayOfArraysDeepDive          — two levels of repeated sub-
    //                                       message; amplifies the pool-
    //                                       rental pattern (hypothesis b:
    //                                       BufferChainPool rent/return
    //                                       per object). See also
    //                                       FLUSHLOCK_ARRAYPOOL_ANALYSIS.md
    //                                       in the repo root.
    //
    //  Each class measures Serialize (OnePass vs TwoPass-Stream vs pbnet),
    //  Deserialize (Span), and SizeCalc (TwoPass pre-pass) separately so
    //  the time TwoPass spends in WriteSizeCalculator is visible against
    //  full serialize time.
    //
    //  Length sweep goes deliberately beyond the 500-item ceiling of the
    //  original benchmark (up to 10k where payload permits) so the curve
    //  past pool-saturation is visible.
    //
    //  Scope: diagnostics only. No optimizations here, no production code
    //  changes.
    // =====================================================================

    // ── Single-int element ───────────────────────────────────────────────
    [BenchmarkCategory("ObjectArrayDeepDive")]
    public class ArrayIntOnlyDeepDiveBenchmark : SerdeBenchmarkBase<ArrayIntOnlyModel>
    {
        [Params(1, 10, 100, 1000, 10000)]
        public int N;

        protected override ArrayIntOnlyModel BuildModel()
        {
            var items = new IntOnlyItem[N];
            for (int i = 0; i < N; i++) items[i] = new IntOnlyItem { Value = i * 31 + 7 };
            return new ArrayIntOnlyModel { Items = items };
        }

        protected override byte[] PreSerialize(ArrayIntOnlyModel model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.Serialize(ms, model);
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteArrayIntOnlyModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public ArrayIntOnlyModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<ArrayIntOnlyModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public ArrayIntOnlyModel GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeArrayIntOnlyModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateArrayIntOnlyModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }

    // ── Five primitive fields per element ────────────────────────────────
    [BenchmarkCategory("ObjectArrayDeepDive")]
    public class ArrayFivePrimitiveDeepDiveBenchmark : SerdeBenchmarkBase<ArrayFivePrimitiveModel>
    {
        [Params(1, 10, 100, 1000, 5000)]
        public int N;

        protected override ArrayFivePrimitiveModel BuildModel()
        {
            var items = new FivePrimitiveItem[N];
            for (int i = 0; i < N; i++)
            {
                items[i] = new FivePrimitiveItem
                {
                    A = i,
                    B = (long)i << 16,
                    C = i * 1.5,
                    D = i * 0.25f,
                    E = (i & 1) == 0
                };
            }
            return new ArrayFivePrimitiveModel { Items = items };
        }

        protected override byte[] PreSerialize(ArrayFivePrimitiveModel model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.Serialize(ms, model);
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteArrayFivePrimitiveModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public ArrayFivePrimitiveModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<ArrayFivePrimitiveModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public ArrayFivePrimitiveModel GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeArrayFivePrimitiveModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateArrayFivePrimitiveModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }

    // ── Element with a nested submessage ─────────────────────────────────
    [BenchmarkCategory("ObjectArrayDeepDive")]
    public class ArrayNestedSubmessageDeepDiveBenchmark : SerdeBenchmarkBase<ArrayNestedSubmessageModel>
    {
        [Params(1, 10, 100, 1000)]
        public int N;

        protected override ArrayNestedSubmessageModel BuildModel()
        {
            var items = new NestedSubmessageItem[N];
            for (int i = 0; i < N; i++)
            {
                items[i] = new NestedSubmessageItem
                {
                    Id = i,
                    Inner = new InnerLeaf { Value = i * 7, Label = "leaf" }
                };
            }
            return new ArrayNestedSubmessageModel { Items = items };
        }

        protected override byte[] PreSerialize(ArrayNestedSubmessageModel model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.Serialize(ms, model);
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteArrayNestedSubmessageModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public ArrayNestedSubmessageModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<ArrayNestedSubmessageModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public ArrayNestedSubmessageModel GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeArrayNestedSubmessageModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateArrayNestedSubmessageModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }

    // ── Array of arrays (two levels of repeated submessage) ──────────────
    [BenchmarkCategory("ObjectArrayDeepDive")]
    public class ArrayOfArraysDeepDiveBenchmark : SerdeBenchmarkBase<ArrayOfArraysModel>
    {
        // Outer array length × inner array length sweep.
        [Params(10, 100)]
        public int Outer;

        [Params(10, 100)]
        public int Inner;

        protected override ArrayOfArraysModel BuildModel()
        {
            var buckets = new InnerBucket[Outer];
            for (int o = 0; o < Outer; o++)
            {
                var items = new IntOnlyItem[Inner];
                for (int i = 0; i < Inner; i++)
                    items[i] = new IntOnlyItem { Value = o * Inner + i };
                buckets[o] = new InnerBucket { Items = items };
            }
            return new ArrayOfArraysModel { Buckets = buckets };
        }

        protected override byte[] PreSerialize(ArrayOfArraysModel model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.Serialize(ms, model);
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteArrayOfArraysModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public ArrayOfArraysModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<ArrayOfArraysModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public ArrayOfArraysModel GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeArrayOfArraysModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateArrayOfArraysModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
