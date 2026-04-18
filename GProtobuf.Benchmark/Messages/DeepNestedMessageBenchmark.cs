using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Messages;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Messages
{
    /// <summary>
    /// N-deep chain of self-referential <see cref="DeepNestedNode"/>.
    /// Parameter Depth sweeps 1 → 25 to surface OnePass BufferChainPool
    /// rental cost per nesting level vs TwoPass's pre-calculated size path.
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class DeepNestedMessageBenchmark : SerdeBenchmarkBase<DeepNestedNode>
    {
        [Params(1, 5, 10, 25)]
        public int Depth;

        protected override DeepNestedNode BuildModel()
        {
            var root = new DeepNestedNode { Value = 0 };
            var current = root;
            for (int i = 1; i < Depth; i++)
            {
                current.Child = new DeepNestedNode { Value = i };
                current = current.Child;
            }
            return root;
        }

        protected override byte[] PreSerialize(DeepNestedNode model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.Serialize(ms, model);
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteDeepNestedNode(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public DeepNestedNode ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<DeepNestedNode>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public DeepNestedNode GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeDeepNestedNode(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateDeepNestedNodeContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
