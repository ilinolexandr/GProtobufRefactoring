using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Collections;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Collections
{
    // =====================================================================
    //  DictionaryDeepDiveBenchmark
    //
    //  Diagnostic sweep built to localize the OnePass-vs-TwoPass regression
    //  observed in DictionaryScalingBenchmark (OnePass 2.0-2.4x slower than
    //  TwoPass across N = 1..100, TwoPass 1.82x slower than protobuf-net at
    //  N = 1000).
    //
    //  Split by key/value shape into four classes so the wire-format cost is
    //  not mixed inside one [Params] matrix:
    //
    //      IntIntDeepDive         — varint key + varint value (tiny entries)
    //      IntStringDeepDive      — varint key + length-delimited value
    //      StringStringDeepDive   — length-delimited on both sides
    //      LongBytesDeepDive      — wide varint key + raw bytes payload
    //
    //  Each class measures Serialize (OnePass vs TwoPass-Stream vs pbnet),
    //  Deserialize, and SizeCalc (TwoPass pre-pass) separately so the time
    //  TwoPass spends in WriteSizeCalculator is visible against full
    //  serialize time.
    //
    //  Scope: diagnostics only. No optimizations here.
    // =====================================================================

    // ── <int, int> ───────────────────────────────────────────────────────
    [BenchmarkCategory("DictionaryDeepDive")]
    public class DictionaryIntIntDeepDiveBenchmark : SerdeBenchmarkBase<DictionaryIntIntModel>
    {
        [Params(1, 10, 50, 100, 500, 1000, 5000)]
        public int N;

        protected override DictionaryIntIntModel BuildModel()
        {
            var map = new Dictionary<int, int>(N);
            for (int i = 0; i < N; i++) map[i] = i * 31 + 7;
            return new DictionaryIntIntModel { Map = map };
        }

        protected override byte[] PreSerialize(DictionaryIntIntModel model)
        {
            using var ms = new MemoryStream();
            Models.Collections.Serialization.Serializers.Serialize(ms, model);
            return ms.ToArray();
        }

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
            Models.Collections.Serialization.OnePassStreamWriters.WriteDictionaryIntIntModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Collections.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public DictionaryIntIntModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<DictionaryIntIntModel>((ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public DictionaryIntIntModel GProtobuf_De()
            => Models.Collections.Serialization.Deserializers.DeserializeDictionaryIntIntModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Collections.Serialization.SizeCalculators.CalculateDictionaryIntIntModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }

    // ── <int, string> — scans value length too ──────────────────────────
    [BenchmarkCategory("DictionaryDeepDive")]
    public class DictionaryIntStringDeepDiveBenchmark : SerdeBenchmarkBase<DictionaryIntStringModel>
    {
        [Params(1, 10, 100, 1000, 5000)]
        public int N;

        // ValueLen varies the UTF-8 payload size per entry so per-entry
        // overhead (framing + size calc) is separable from throughput.
        [Params(4, 16, 64, 256)]
        public int ValueLen;

        protected override DictionaryIntStringModel BuildModel()
        {
            var map = new Dictionary<int, string>(N);
            string body = new string('x', ValueLen);
            for (int i = 0; i < N; i++) map[i] = body;
            return new DictionaryIntStringModel { Map = map };
        }

        protected override byte[] PreSerialize(DictionaryIntStringModel model)
        {
            using var ms = new MemoryStream();
            Models.Collections.Serialization.Serializers.Serialize(ms, model);
            return ms.ToArray();
        }

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
            Models.Collections.Serialization.OnePassStreamWriters.WriteDictionaryIntStringModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Collections.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public DictionaryIntStringModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<DictionaryIntStringModel>((ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public DictionaryIntStringModel GProtobuf_De()
            => Models.Collections.Serialization.Deserializers.DeserializeDictionaryIntStringModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Collections.Serialization.SizeCalculators.CalculateDictionaryIntStringModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }

    // ── <string, string> ─────────────────────────────────────────────────
    [BenchmarkCategory("DictionaryDeepDive")]
    public class DictionaryStringStringDeepDiveBenchmark : SerdeBenchmarkBase<DictionaryStringStringModel>
    {
        [Params(1, 10, 100, 1000)]
        public int N;

        // Same length used for both key and value to keep the matrix small.
        [Params(4, 16, 64)]
        public int StringLen;

        protected override DictionaryStringStringModel BuildModel()
        {
            var map = new Dictionary<string, string>(N);
            string val = new string('x', StringLen);
            // Key = "k" + numeric suffix, padded to StringLen - 1 digits so
            // total key length ≈ StringLen. Uniqueness comes from the numeric
            // prefix; remaining bytes are filler.
            int padWidth = StringLen - 1;
            for (int i = 0; i < N; i++)
            {
                string num = i.ToString("D" + padWidth);
                map["k" + num] = val;
            }
            return new DictionaryStringStringModel { Map = map };
        }

        protected override byte[] PreSerialize(DictionaryStringStringModel model)
        {
            using var ms = new MemoryStream();
            Models.Collections.Serialization.Serializers.Serialize(ms, model);
            return ms.ToArray();
        }

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
            Models.Collections.Serialization.OnePassStreamWriters.WriteDictionaryStringStringModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Collections.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public DictionaryStringStringModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<DictionaryStringStringModel>((ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public DictionaryStringStringModel GProtobuf_De()
            => Models.Collections.Serialization.Deserializers.DeserializeDictionaryStringStringModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Collections.Serialization.SizeCalculators.CalculateDictionaryStringStringModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }

    // ── <long, byte[]> ───────────────────────────────────────────────────
    [BenchmarkCategory("DictionaryDeepDive")]
    public class DictionaryLongBytesDeepDiveBenchmark : SerdeBenchmarkBase<DictionaryLongBytesModel>
    {
        [Params(1, 10, 100, 1000)]
        public int N;

        [Params(4, 16, 64, 256)]
        public int PayloadLen;

        protected override DictionaryLongBytesModel BuildModel()
        {
            var map = new Dictionary<long, byte[]>(N);
            for (int i = 0; i < N; i++)
            {
                var payload = new byte[PayloadLen];
                for (int b = 0; b < PayloadLen; b++) payload[b] = (byte)(i + b);
                // Key space spread over wide varint lengths so the key itself is non-zero
                // and occupies multiple varint bytes for most entries.
                map[((long)i << 20) | 0xABCDEF] = payload;
            }
            return new DictionaryLongBytesModel { Map = map };
        }

        protected override byte[] PreSerialize(DictionaryLongBytesModel model)
        {
            using var ms = new MemoryStream();
            Models.Collections.Serialization.Serializers.Serialize(ms, model);
            return ms.ToArray();
        }

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
            Models.Collections.Serialization.OnePassStreamWriters.WriteDictionaryLongBytesModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Collections.Serialization.Serializers.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public DictionaryLongBytesModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<DictionaryLongBytesModel>((ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public DictionaryLongBytesModel GProtobuf_De()
            => Models.Collections.Serialization.Deserializers.DeserializeDictionaryLongBytesModel(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Collections.Serialization.SizeCalculators.CalculateDictionaryLongBytesModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
