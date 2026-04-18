using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Scalars;
using static GProtobuf.Benchmark.Infrastructure.TestDataFactory;

namespace GProtobuf.Benchmark.TwoPassSpecific
{
    /// <summary>
    /// Isolates the TwoPass write-sink choice: <see cref="MemoryStream"/> vs
    /// <c>ArrayBufferWriter&lt;byte&gt;</c>. Both share the same SizeCalc prepass —
    /// the difference is purely how the second pass emits bytes.
    /// <para>
    /// Parameterised by <c>N</c> to surface the crossover curve: at small N
    /// the per-call Stream overhead dominates; at large N the BufferWriter's
    /// pre-sized contiguous buffer wins over fragmented Stream.Write calls.
    /// </para>
    /// <para>
    /// Not part of the main comparison surface. The canonical within-GProtobuf
    /// pairing is <c>GProtobuf_OnePass_Ser</c> vs <c>GProtobuf_TwoPass_Stream_Ser</c>
    /// (same <see cref="Stream"/> sink). BufferWriter is an optional alternative
    /// sink for TwoPass only, measured here in isolation.
    /// </para>
    /// </summary>
    [BenchmarkCategory("TwoPassSpecific")]
    public class BufferWriterVsStreamBenchmark : SerdeBenchmarkBase<Int32DefaultListModel>
    {
        [Params(10, 100, 1000, 10000)]
        public int N;

        protected override Int32DefaultListModel BuildModel()
        {
            var list = new List<int>(N);
            var v = Int32(VarintShape.MaxValue);
            for (int i = 0; i < N; i++) list.Add(v);
            return new Int32DefaultListModel { Values = list };
        }

        protected override byte[] PreSerialize(Int32DefaultListModel model)
        {
            using var ms = new MemoryStream();
            Models.Scalars.Serialization.Serializers.Serialize(ms, model);
            return ms.ToArray();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Scalars.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_BufferWriter_Ser()
        {
            Models.Scalars.Serialization.Serializers.Serialize(BufferWriter, Model);
            return BufferWriter.WrittenCount;
        }
    }
}
