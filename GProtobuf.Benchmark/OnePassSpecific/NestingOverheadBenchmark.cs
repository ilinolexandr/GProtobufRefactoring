using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Messages;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.OnePassSpecific
{
    /// <summary>
    /// Fine-grained per-level scan of <see cref="DeepNestedNode"/> up to the
    /// <see cref="OnePassStreamWriter"/> inline-stack cap of 16 (R-007).
    /// <para>
    /// Shows exactly where OnePass <c>BeginSubMessage</c> /
    /// <c>BufferChainPool.Rent</c> starts dominating the cost versus
    /// TwoPass's precomputed-size path. Payload is trivial (one int per
    /// level) so measurements isolate per-level framing overhead.
    /// </para>
    /// </summary>
    [BenchmarkCategory("OnePassSpecific")]
    public class NestingOverheadBenchmark : SerdeBenchmarkBase<DeepNestedNode>
    {
        [Params(1, 2, 4, 8, 12, 16)]
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

        [Benchmark(Baseline = true), BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.Serialize(Stream, Model);
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
    }
}
