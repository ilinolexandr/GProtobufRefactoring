using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Messages;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Messages
{
    /// <summary>
    /// Serialization/deserialization of a 3-level ProtoInclude hierarchy
    /// via the base type. Level parameter picks which concrete type is
    /// instantiated — exposes polymorphic-dispatch cost growth.
    /// <para>
    /// Baseline = <c>GProtobuf_TwoPass_Stream_Ser</c> rather than
    /// protobuf-net because R-009 prevents applying [ProtoBuf.ProtoInclude]
    /// dual-attributes; protobuf-net therefore sees only base-type fields
    /// and does strictly less work. Its row is kept as a reference but
    /// does NOT represent an apples-to-apples comparison.
    /// </para>
    /// </summary>
    [BenchmarkCategory("CiFast")]
    public class InheritanceBenchmark : SerdeBenchmarkBase<InheritBase>
    {
        /// <summary>1 = base only; 2 = base + derived; 3 = base + derived + doubly-derived.</summary>
        [Params(1, 2, 3)]
        public int Level;

        protected override InheritBase BuildModel()
        {
            return Level switch
            {
                1 => new InheritBase
                {
                    BaseField = "base"
                },
                2 => new InheritDerived
                {
                    BaseField    = "base",
                    DerivedField = "derived"
                },
                3 => new InheritDoublyDerived
                {
                    BaseField          = "base",
                    DerivedField       = "derived",
                    DoublyDerivedField = "doubly-derived"
                },
                _ => throw new System.ArgumentOutOfRangeException(nameof(Level))
            };
        }

        protected override byte[] PreSerialize(InheritBase model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.Serialize(ms, model);
            return ms.ToArray();
        }

        // ── Serialize ─────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("Serialize")]
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteInheritBase(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("Deserialize")]
        public InheritBase ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<InheritBase>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public InheritBase GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeInheritBase(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateInheritBaseContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
