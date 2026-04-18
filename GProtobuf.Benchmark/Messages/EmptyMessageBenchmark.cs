using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Messages;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Messages
{
    /// <summary>
    /// Minimum message with a default-valued single field. Payload is ~0 bytes;
    /// Mean captures library dispatch / setup overhead only. Use as a
    /// floor value when comparing to larger messages.
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class EmptyMessageBenchmark : SerdeBenchmarkBase<MinimalMessageModel>
    {
        protected override MinimalMessageModel BuildModel() => new() { Value = 0 };

        protected override byte[] PreSerialize(MinimalMessageModel model)
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteMinimalMessageModel(ref writer, Model);
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
        public MinimalMessageModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<MinimalMessageModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public MinimalMessageModel GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeMinimalMessageModel(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateMinimalMessageModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
