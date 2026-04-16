using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Nested
{
    // =====================================================================
    //  FramingIsolationBenchmark
    //
    //  Narrowest possible probe of the OnePass per-frame overhead.
    //  No generator-emitted code, no model tree — a hand-written loop that
    //  calls OnePassStreamWriter's public Begin/EndSubMessage API directly,
    //  so the measured cost is exactly the sub-message framing, not any
    //  enclosing serializer dispatch.
    //
    //  Three cases per frame count:
    //      Flat_WriteInt    — N × WriteVarInt32Field (no framing at all)
    //                         Establishes the pure write-throughput baseline.
    //      Framed_Empty     — N × (tag + BeginSub + EndSub)
    //                         Exercises pool-wrapper rent, two Flush calls
    //                         and the length-prefix write, but NEVER calls
    //                         BufferChain.Write, so ArrayPool.Rent is skipped.
    //      Framed_WriteInt  — N × (tag + BeginSub + WriteVarInt32Field + EndSub)
    //                         Same as Framed_Empty plus one inner Write, which
    //                         triggers RentNewSegment → ArrayPool.Rent(8192) →
    //                         CopyTo → ArrayPool.Return on every frame.
    //
    //  The two gaps answer two distinct questions:
    //      Framed_Empty  − Flat_WriteInt    = bookkeeping-only frame cost
    //      Framed_WriteInt − Framed_Empty   = ArrayPool rent + CopyTo round-trip
    //
    //  The second gap is the hypothesized primary bottleneck.
    // =====================================================================
    [BenchmarkCategory("NestedMessagesDeepDive", "FramingIsolation")]
    [MemoryDiagnoser(displayGenColumns: false)]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class FramingIsolationBenchmark
    {
        [Params(1, 10, 100, 1000)]
        public int Frames;

        // Oversized so Stream never grows during a run (would pollute the measurement).
        private readonly MemoryStream Stream = new(1 << 20);

        [IterationSetup]
        public void Reset()
        {
            Stream.Position = 0;
            Stream.SetLength(0);
        }

        [GlobalCleanup]
        public void Cleanup() => Stream.Dispose();

        // Baseline: no sub-message framing — straight line of varint writes.
        [Benchmark(Baseline = true), BenchmarkCategory("FramingIsolation")]
        public long Flat_WriteInt()
        {
            using var scope = new OnePassScope(OnePassHarness.Pool);
            var writer = new OnePassStreamWriter(Stream, OnePassHarness.TempBuffer, scope.Pool);
            for (int i = 0; i < Frames; i++)
                writer.WriteVarInt32Field(0x08, 42); // field 1, varint
            writer.Flush();
            return Stream.Length;
        }

        // Frame wrapper with zero payload — isolates Begin/End bookkeeping
        // from the ArrayPool segment cost because BufferChain.Write is never
        // invoked while inside the frame.
        [Benchmark, BenchmarkCategory("FramingIsolation")]
        public long Framed_Empty()
        {
            using var scope = new OnePassScope(OnePassHarness.Pool);
            var writer = new OnePassStreamWriter(Stream, OnePassHarness.TempBuffer, scope.Pool);
            for (int i = 0; i < Frames; i++)
            {
                writer.WriteSingleByte(0x0A); // field 1, LEN wire type
                writer.BeginSubMessage();
                writer.EndSubMessage();
            }
            writer.Flush();
            return Stream.Length;
        }

        // One tiny write inside each frame flips the ArrayPool segment
        // rent/CopyTo/return cycle on — delta vs Framed_Empty is the
        // direct cost of that cycle.
        [Benchmark, BenchmarkCategory("FramingIsolation")]
        public long Framed_WriteInt()
        {
            using var scope = new OnePassScope(OnePassHarness.Pool);
            var writer = new OnePassStreamWriter(Stream, OnePassHarness.TempBuffer, scope.Pool);
            for (int i = 0; i < Frames; i++)
            {
                writer.WriteSingleByte(0x0A); // field 1, LEN wire type (outer tag)
                writer.BeginSubMessage();
                writer.WriteVarInt32Field(0x08, 42); // inner field 1, varint
                writer.EndSubMessage();
            }
            writer.Flush();
            return Stream.Length;
        }
    }
}
