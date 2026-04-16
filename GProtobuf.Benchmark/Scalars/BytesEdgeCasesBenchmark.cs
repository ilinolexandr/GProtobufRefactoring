using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Scalars;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Scalars
{
    /// <summary>
    /// Single byte[] serialization/deserialization across critical size
    /// boundaries: 0 (empty), 1 (tiny), 64 (stack-viable), 1024 (normal),
    /// 65536 (above stack, below LOH), 1048576 (LOH-territory).
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class BytesEdgeCasesBenchmark : SerdeBenchmarkBase<BytesPayloadModel>
    {
        [Params(0, 1, 64, 1024, 65536, 1048576)]
        public int Size;

        protected override BytesPayloadModel BuildModel()
        {
            return new BytesPayloadModel { Value = TestDataFactory.Bytes(Size) };
        }

        protected override byte[] PreSerialize(BytesPayloadModel model)
        {
            using var ms = new MemoryStream();
            Models.Scalars.Serialization.Serializers.SerializeBytesPayloadModel(ms, model);
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
            Models.Scalars.Serialization.OnePassStreamWriters.WriteBytesPayloadModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Scalars.Serialization.Serializers.SerializeBytesPayloadModel(Stream, Model);
            return Stream.Length;
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public BytesPayloadModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<BytesPayloadModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public BytesPayloadModel GProtobuf_De()
            => Models.Scalars.Serialization.Deserializers.DeserializeBytesPayloadModel(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Scalars.Serialization.SizeCalculators.CalculateBytesPayloadModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
