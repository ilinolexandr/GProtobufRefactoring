using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Scalars;
using GProtobuf.Core;
using static GProtobuf.Benchmark.Infrastructure.TestDataFactory;

namespace GProtobuf.Benchmark.Scalars
{
    /// <summary>
    /// 50-element list of uniform-shape strings. Parameters sweep length
    /// boundaries (varint-length prefix: 1-byte @63/64, 2-byte @127/128,
    /// 3-byte @16K+) and ASCII-vs-UTF-8 encoding cost.
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class StringEdgeCasesBenchmark : SerdeBenchmarkBase<StringListModel>
    {
        public const int Count = 50;

        [Params(
            StringShape.Empty,
            StringShape.OneCharAscii,
            StringShape.Short63Ascii,
            StringShape.Boundary128Ascii,
            StringShape.Long1KAscii,
            StringShape.ShortUtf8Multibyte,
            StringShape.LongUtf8Multibyte)]
        public StringShape Shape;

        protected override StringListModel BuildModel()
        {
            var list = new List<string>(Count);
            var s = String(Shape);
            for (int i = 0; i < Count; i++) list.Add(s);
            return new StringListModel { Values = list };
        }

        protected override byte[] PreSerialize(StringListModel model)
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
            Models.Scalars.Serialization.OnePassStreamWriters.WriteStringListModel(ref writer, Model);
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
        public StringListModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<StringListModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public StringListModel GProtobuf_De()
            => Models.Scalars.Serialization.Deserializers.DeserializeStringListModel(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Scalars.Serialization.SizeCalculators.CalculateStringListModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
