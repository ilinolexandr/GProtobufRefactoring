using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Collections;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Collections
{
    /// <summary>
    /// Dictionary&lt;int, string&gt; serialize/deserialize across a scaling
    /// sweep N = 0, 1, 10, 100, 1000. Each entry is a length-delimited
    /// map-entry sub-message, so per-entry overhead (varint length,
    /// tag bytes, sub-message framing) dominates.
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class DictionaryScalingBenchmark : SerdeBenchmarkBase<DictionaryIntStringModel>
    {
        [Params(0, 1, 10, 100, 1000)]
        public int N;

        protected override DictionaryIntStringModel BuildModel()
        {
            var map = new Dictionary<int, string>(N);
            for (int i = 0; i < N; i++) map[i] = $"value-{i}";
            return new DictionaryIntStringModel { Map = map };
        }

        protected override byte[] PreSerialize(DictionaryIntStringModel model)
        {
            using var ms = new MemoryStream();
            Models.Collections.Serialization.Serializers.SerializeDictionaryIntStringModel(ms, model);
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
            Models.Collections.Serialization.OnePassStreamWriters.WriteDictionaryIntStringModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Collections.Serialization.Serializers.SerializeDictionaryIntStringModel(Stream, Model);
            return Stream.Length;
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public DictionaryIntStringModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<DictionaryIntStringModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public DictionaryIntStringModel GProtobuf_De()
            => Models.Collections.Serialization.Deserializers.DeserializeDictionaryIntStringModel(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Collections.Serialization.SizeCalculators.CalculateDictionaryIntStringModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
