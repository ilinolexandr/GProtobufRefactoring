using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Collections
{
    [BenchmarkCategory("CiFast")]
    public class CollectionsBenchmark : SerdeBenchmarkBase<CollectionsModel>
    {
        protected override CollectionsModel BuildModel()
        {
            var intData    = Enumerable.Range(1, 300).ToList();
            var longData   = Enumerable.Range(1, 100).Select(i => (long)i * 1_000_000).ToList();
            var floatData  = Enumerable.Range(1, 100).Select(i => i * 1.5f).ToList();
            var doubleData = Enumerable.Range(1, 100).Select(i => i * 2.5).ToList();
            var stringData = Enumerable.Range(1, 50).Select(i => $"String item number {i} with some content").ToList();

            return new CollectionsModel
            {
                IntList             = intData,
                LongList            = longData,
                FloatList           = floatData,
                DoubleList          = doubleData,
                StringList          = stringData,
                IntArray            = intData.ToArray(),
                FloatArray          = floatData.ToArray(),
                DoubleArray         = doubleData.ToArray(),
                StringArray         = stringData.ToArray(),
                PackedFixedIntList  = intData.Take(50).ToList(),
                PackedZigZagIntList = intData.Select(i => i % 2 == 0 ? i : -i).ToList()
            };
        }

        protected override byte[] PreSerialize(CollectionsModel model)
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
            Models.Serialization.OnePassStreamWriters.WriteCollectionsModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public CollectionsModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<CollectionsModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public CollectionsModel GProtobuf_De()
            => Models.Serialization.Deserializers.DeserializeCollectionsModel(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Serialization.SizeCalculators.CalculateCollectionsModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
