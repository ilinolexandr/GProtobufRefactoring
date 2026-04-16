using System.IO;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Infrastructure
{
    /// <summary>
    /// Reusable 8 KB temp buffer + shared pool for OnePass serialization benches.
    /// <para>
    /// Usage inside a benchmark method:
    /// <code>
    /// using var scope = new OnePassScope(OnePassHarness.Pool);
    /// var writer = new OnePassStreamWriter(stream, OnePassHarness.TempBuffer, scope.Pool);
    /// OnePassStreamWriters.WriteFooModel(ref writer, model);
    /// writer.Flush();
    /// </code>
    /// </para>
    /// <para>
    /// Mirrors the idiomatic high-perf path used in production code
    /// (e.g. FrozenDictBenchmark/DispatchBenchmark.cs:40), not the generated
    /// convenience wrapper that stack-allocs 256 bytes per call.
    /// </para>
    /// </summary>
    public static class OnePassHarness
    {
        public const int TempBufferSize = 8192;

        [System.ThreadStatic]
        private static byte[] _tempBuffer;

        public static byte[] TempBuffer => _tempBuffer ??= new byte[TempBufferSize];

        public static BufferChainPoolCache Pool => BufferChainPoolCache.Shared;
    }
}
