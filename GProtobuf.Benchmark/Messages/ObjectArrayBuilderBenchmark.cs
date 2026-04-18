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
            Models.Serialization.Serializers.Serialize(ms, model);

            // POC parity check: verify the hand-written pre-calc OnePass path
            // produces byte-identical output. Fails loudly at GlobalSetup time
            // rather than producing silently wrong benchmark numbers.
            var twopassBytes = ms.ToArray();
            using var pocMs = new MemoryStream();
            var pocWriter = new OnePassStreamWriter(pocMs, OnePassHarness.TempBuffer, null!);
            WriteObjectArrayBuilderTestModel_PreCalc(ref pocWriter, model);
            pocWriter.Flush();
            var pocBytes = pocMs.ToArray();
            if (!pocBytes.AsSpan().SequenceEqual(twopassBytes))
                throw new System.InvalidOperationException(
                    $"POC byte-parity failed: POC={pocBytes.Length}B TwoPass={twopassBytes.Length}B");

            return twopassBytes;
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
            Models.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        // ── POC: OnePass with per-sub-message pre-calc ────────────────────────
        //
        // Hand-written replacement of generator output that bypasses
        // OnePassStreamWriter.BeginSubMessage / EndSubMessage entirely.
        // Instead, for each sub-message we:
        //   1) call the already-generated SizeCalculator to learn the length,
        //   2) write the length varint directly into the parent span buffer,
        //   3) write the fields directly (no child BufferChainStream, no CopyTo).
        //
        // Uses OnePassStreamWriters.WriteSimpleItem as-is (it only writes
        // primitive fields — no BeginSubMessage inside). NestedItem is
        // reimplemented locally so its inner sub-message field also takes the
        // pre-calc path.
        //
        // Pass/fail criterion: mean ≈ TwoPass_Stream_Ser on this benchmark.
        // If yes, the same transformation in OnePassStreamWriterGenerator will
        // close the regression for all repeated-message writes.
        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_OnePass_PreCalc_Ser()
        {
            var writer = new OnePassStreamWriter(Stream, OnePassHarness.TempBuffer, null!);
            WriteObjectArrayBuilderTestModel_PreCalc(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        private static void WriteObjectArrayBuilderTestModel_PreCalc(
            ref OnePassStreamWriter writer,
            ObjectArrayBuilderTestModel instance)
        {
            if (instance == null) return;

            var items = instance.Items;
            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    if (item is null) continue;

                    writer.WriteSingleByte(0x0A);
                    var itemCalc = new WriteSizeCalculator();
                    Models.Serialization.SizeCalculators.CalculateSimpleItemContentSize(ref itemCalc, item);
                    writer.WriteVarUInt32((uint)itemCalc.Length);
                    // WriteSimpleItem writes three primitive fields directly; no BeginSubMessage.
                    Models.Serialization.OnePassStreamWriters.WriteSimpleItem(ref writer, item);
                }
            }

            var nestedItems = instance.NestedItems;
            if (nestedItems != null)
            {
                for (int i = 0; i < nestedItems.Length; i++)
                {
                    var item = nestedItems[i];
                    if (item is null) continue;

                    writer.WriteSingleByte(0x12);
                    var itemCalc = new WriteSizeCalculator();
                    Models.Serialization.SizeCalculators.CalculateNestedItemContentSize(ref itemCalc, item);
                    writer.WriteVarUInt32((uint)itemCalc.Length);
                    WriteNestedItem_PreCalc(ref writer, item);
                }
            }
        }

        private static void WriteNestedItem_PreCalc(
            ref OnePassStreamWriter writer,
            NestedItem instance)
        {
            if (instance == null) return;

            writer.WriteStringField(0x0A, instance.Title);

            var inner = instance.Inner;
            if (inner != null)
            {
                writer.WriteSingleByte(0x12);
                var innerCalc = new WriteSizeCalculator();
                Models.Serialization.SizeCalculators.CalculateSimpleItemContentSize(ref innerCalc, inner);
                writer.WriteVarUInt32((uint)innerCalc.Length);
                Models.Serialization.OnePassStreamWriters.WriteSimpleItem(ref writer, inner);
            }

            var tags = instance.Tags;
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    var tag = tags[i];
                    if (tag == null) continue;
                    writer.WriteSingleByte(0x1A);
                    writer.WriteString(tag);
                }
            }
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
