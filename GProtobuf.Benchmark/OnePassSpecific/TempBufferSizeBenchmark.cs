using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Scalars;
using GProtobuf.Core;
using static GProtobuf.Benchmark.Infrastructure.TestDataFactory;

namespace GProtobuf.Benchmark.OnePassSpecific
{
    /// <summary>
    /// Directly tests the <b>R-002</b> hypothesis: does the 8 KB temp buffer
    /// in <see cref="OnePassHarness"/> overflow on 100 × int32 NegativeOne
    /// (10-byte varint each → 1 KB payload + tag overhead), forcing a
    /// <c>BufferChainPool</c> rental?
    /// <para>
    /// Model is fixed: <see cref="Int32DefaultListModel"/> populated with
    /// 100 copies of <see cref="VarintShape.NegativeOne"/> (-1). Only the
    /// OnePass temp buffer size varies.
    /// </para>
    /// <para>
    /// Expected pattern if R-002 is correct: allocation is high at small
    /// buffer sizes (rental every iteration), drops sharply once buffer ≥
    /// payload size, and stays flat thereafter.
    /// </para>
    /// </summary>
    [BenchmarkCategory("OnePassSpecific")]
    public class TempBufferSizeBenchmark : SerdeBenchmarkBase<Int32DefaultListModel>
    {
        private const int Count = 100;

        [Params(256, 1024, 4096, 8192, 16384, 65536)]
        public int BufferSize;

        private byte[] _tempBuffer;

        protected override Int32DefaultListModel BuildModel()
        {
            var list = new List<int>(Count);
            var v = Int32(VarintShape.NegativeOne);
            for (int i = 0; i < Count; i++) list.Add(v);
            return new Int32DefaultListModel { Values = list };
        }

        protected override byte[] PreSerialize(Int32DefaultListModel model)
        {
            using var ms = new MemoryStream();
            Models.Scalars.Serialization.Serializers.Serialize(ms, model);
            return ms.ToArray();
        }

        public override void Setup()
        {
            base.Setup();
            _tempBuffer = new byte[BufferSize];
        }

        // ── Only OnePass is measured — TempBufferSize only affects OnePass. ──

        [Benchmark(Baseline = true), BenchmarkCategory("Serialize")]
        public long GProtobuf_OnePass_Ser()
        {
            using var scope = new OnePassScope(OnePassHarness.Pool);
            var writer = new OnePassStreamWriter(Stream, _tempBuffer.AsSpan(), scope.Pool);
            Models.Scalars.Serialization.OnePassStreamWriters.WriteInt32DefaultListModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        // Reference point: TwoPass has no temp buffer, so its numbers should be
        // invariant to BufferSize Param. We run it to surface any BDN run-to-run noise.
        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser_Reference()
        {
            Models.Scalars.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }
    }
}
