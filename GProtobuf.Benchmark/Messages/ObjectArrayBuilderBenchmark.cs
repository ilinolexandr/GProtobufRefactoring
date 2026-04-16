using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Messages
{
    /// <summary>
    /// Measures allocation reduction when deserializing arrays of class objects
    /// via ObjectArrayBuilder (vs default List&lt;T&gt;.Add growth pattern).
    /// Parameterized by item count to expose scaling behavior.
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class ObjectArrayBuilderBenchmark : SerdeBenchmarkBase<ObjectArrayBuilderTestModel>
    {
        [Params(10, 50, 100, 500)]
        public int ItemCount { get; set; }

        protected override ObjectArrayBuilderTestModel BuildModel() => new()
        {
            Items = Enumerable.Range(1, ItemCount).Select(i => new SimpleItem
            {
                Name  = $"Item {i}",
                Value = i * 100,
                Score = i * 1.5
            }).ToArray(),

            NestedItems = Enumerable.Range(1, ItemCount / 2).Select(i => new NestedItem
            {
                Title = $"Nested {i}",
                Inner = new SimpleItem { Name = $"Inner {i}", Value = i, Score = i * 0.5 },
                Tags  = new List<string> { $"tag{i}", $"tag{i + 1}" }
            }).ToArray()
        };

        protected override byte[] PreSerialize(ObjectArrayBuilderTestModel model)
        {
            using var ms = new MemoryStream();
            Models.Serialization.Serializers.SerializeObjectArrayBuilderTestModel(ms, model);
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
            Models.Serialization.OnePassStreamWriters.WriteObjectArrayBuilderTestModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Serialization.Serializers.SerializeObjectArrayBuilderTestModel(Stream, Model);
            return Stream.Length;
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public ObjectArrayBuilderTestModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<ObjectArrayBuilderTestModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public ObjectArrayBuilderTestModel GProtobuf_De_Span()
            => Models.Serialization.Deserializers.DeserializeObjectArrayBuilderTestModel(Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public ObjectArrayBuilderTestModel GProtobuf_De_Stream()
        {
            Stream.Position = 0;
            Stream.SetLength(Serialized.Length);
            Serialized.CopyTo(Stream.GetBuffer(), 0);
            Stream.Position = 0;
            return Models.Serialization.Deserializers.DeserializeObjectArrayBuilderTestModel((System.IO.Stream)Stream);
        }

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Serialization.SizeCalculators.CalculateObjectArrayBuilderTestModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
