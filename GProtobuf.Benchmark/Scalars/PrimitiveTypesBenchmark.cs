using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Scalars
{
    [BenchmarkCategory("CiFast")]
    public class PrimitiveTypesBenchmark : SerdeBenchmarkBase<PrimitiveTypesModel>
    {
        protected override PrimitiveTypesModel BuildModel() => new()
        {
            IntValue       = 12345,
            LongValue      = 9876543210L,
            FloatValue     = 3.14159f,
            DoubleValue    = 2.71828,
            BoolValue      = true,
            StringValue    = "Benchmark test string with some reasonable length for testing purposes",
            ByteArrayValue = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 },
            FixedIntValue    = -12345,
            FixedLongValue   = -9876543210L,
            ZigZagIntValue   = -12345,
            ZigZagLongValue  = -9876543210L
        };

        protected override byte[] PreSerialize(PrimitiveTypesModel model)
        {
            using var ms = new MemoryStream();
            Models.Serialization.Serializers.Serialize(ms, model);
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
            Models.Serialization.OnePassStreamWriters.WritePrimitiveTypesModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        // ── Deserialize (no OnePass — read path is unchanged) ────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public PrimitiveTypesModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<PrimitiveTypesModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public PrimitiveTypesModel GProtobuf_De()
            => Models.Serialization.Deserializers.DeserializePrimitiveTypesModel(Serialized);

        // ── SizeCalc (two-pass hot path observability) ───────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Serialization.SizeCalculators.CalculatePrimitiveTypesModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
