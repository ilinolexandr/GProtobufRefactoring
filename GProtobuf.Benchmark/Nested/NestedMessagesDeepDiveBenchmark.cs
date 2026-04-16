using System.IO;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models.Messages;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Nested
{
    // =====================================================================
    //  NestedMessagesDeepDiveBenchmark
    //
    //  Diagnostic sweep built to localize the OnePass-vs-TwoPass regression
    //  seen in NestedMessagesBenchmark (OnePass 2.25x slower at ~85 nested
    //  frames per payload).
    //
    //  Depth-only axis is already covered by DeepNestedMessageBenchmark
    //  (Depth 1..25) and NestingOverheadBenchmark (Depth 1..16). This file
    //  fills the axes those don't cover:
    //
    //      BreadthScalar — varint-only leaves, breadth sweeps per-frame cost
    //      BreadthString — +short UTF-8 string per leaf
    //      BreadthBytes  — +256 B payload per leaf
    //      SingleFrame   — exactly one sub-message, content length sweeps
    //                      so frame-fixed cost is isolated from payload copy
    //
    //  Each class reports Serialize (OnePass vs TwoPass-Stream vs pbnet),
    //  Deserialize, and SizeCalc (TwoPass pre-pass) so the time TwoPass
    //  spends in WriteSizeCalculator is visible against full serialize time.
    //
    //  Scope: diagnostics only. No generator/writer changes here.
    // =====================================================================

    // ── Breadth, scalar-only leaves ──────────────────────────────────────
    [BenchmarkCategory("NestedMessagesDeepDive")]
    public class BreadthScalarDeepDiveBenchmark : SerdeBenchmarkBase<WideFanoutRoot>
    {
        [Params(1, 4, 16, 64)]
        public int Breadth;

        protected override WideFanoutRoot BuildModel()
        {
            var nodes = new System.Collections.Generic.List<WideNode>(Breadth);
            for (int i = 0; i < Breadth; i++) nodes.Add(new WideNode { Id = i + 1 });
            return new WideFanoutRoot { Nodes = nodes };
        }

        protected override byte[] PreSerialize(WideFanoutRoot model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.SerializeWideFanoutRoot(ms, model);
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteWideFanoutRoot(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.SerializeWideFanoutRoot(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public WideFanoutRoot ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<WideFanoutRoot>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public WideFanoutRoot GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeWideFanoutRoot(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateWideFanoutRootContentSize(ref calc, Model);
            return calc.Length;
        }
    }

    // ── Breadth, +short string per leaf ─────────────────────────────────
    [BenchmarkCategory("NestedMessagesDeepDive")]
    public class BreadthStringDeepDiveBenchmark : SerdeBenchmarkBase<WideFanoutRoot>
    {
        [Params(1, 4, 16, 64)]
        public int Breadth;

        // Short fixed ASCII body — keeps string-encoding cost constant per leaf
        // so the breadth axis shows purely the per-frame scaling.
        private const string NameBody = "node-payload";

        protected override WideFanoutRoot BuildModel()
        {
            var nodes = new System.Collections.Generic.List<WideNode>(Breadth);
            for (int i = 0; i < Breadth; i++)
                nodes.Add(new WideNode { Id = i + 1, Name = NameBody });
            return new WideFanoutRoot { Nodes = nodes };
        }

        protected override byte[] PreSerialize(WideFanoutRoot model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.SerializeWideFanoutRoot(ms, model);
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteWideFanoutRoot(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.SerializeWideFanoutRoot(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public WideFanoutRoot ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<WideFanoutRoot>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public WideFanoutRoot GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeWideFanoutRoot(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateWideFanoutRootContentSize(ref calc, Model);
            return calc.Length;
        }
    }

    // ── Breadth, +256 B payload per leaf ─────────────────────────────────
    [BenchmarkCategory("NestedMessagesDeepDive")]
    public class BreadthBytesDeepDiveBenchmark : SerdeBenchmarkBase<WideFanoutRoot>
    {
        [Params(1, 4, 16, 64)]
        public int Breadth;

        private const int PayloadLen = 256;

        protected override WideFanoutRoot BuildModel()
        {
            var nodes = new System.Collections.Generic.List<WideNode>(Breadth);
            for (int i = 0; i < Breadth; i++)
            {
                var data = new byte[PayloadLen];
                for (int b = 0; b < PayloadLen; b++) data[b] = (byte)((i + b) & 0xFF);
                nodes.Add(new WideNode { Id = i + 1, Data = data });
            }
            return new WideFanoutRoot { Nodes = nodes };
        }

        protected override byte[] PreSerialize(WideFanoutRoot model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.SerializeWideFanoutRoot(ms, model);
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteWideFanoutRoot(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.SerializeWideFanoutRoot(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public WideFanoutRoot ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<WideFanoutRoot>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public WideFanoutRoot GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeWideFanoutRoot(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateWideFanoutRootContentSize(ref calc, Model);
            return calc.Length;
        }
    }

    // ── Single frame — isolates one BeginSub/EndSub pair vs content size ─
    [BenchmarkCategory("NestedMessagesDeepDive")]
    public class SingleFrameDeepDiveBenchmark : SerdeBenchmarkBase<SingleFrameRoot>
    {
        // 0 keeps Payload null → inner message has zero content, so the measurement
        // is the bare cost of one rent/flush/copy/return cycle.
        // 4096 pushes child content past the 8 KB temp buffer threshold, so the
        // pooled BufferChainStream branch inside CopyTo matters.
        [Params(0, 16, 256, 4096)]
        public int PayloadLen;

        protected override SingleFrameRoot BuildModel()
        {
            var payload = PayloadLen == 0 ? null : new byte[PayloadLen];
            if (payload != null)
                for (int b = 0; b < PayloadLen; b++) payload[b] = (byte)(b & 0xFF);
            return new SingleFrameRoot { Frame = new PayloadMessage { Payload = payload } };
        }

        protected override byte[] PreSerialize(SingleFrameRoot model)
        {
            using var ms = new MemoryStream();
            Models.Messages.Serialization.Serializers.SerializeSingleFrameRoot(ms, model);
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
            Models.Messages.Serialization.OnePassStreamWriters.WriteSingleFrameRoot(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Messages.Serialization.Serializers.SerializeSingleFrameRoot(Stream, Model);
            return Stream.Length;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public SingleFrameRoot ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<SingleFrameRoot>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public SingleFrameRoot GProtobuf_De()
            => Models.Messages.Serialization.Deserializers.DeserializeSingleFrameRoot(Serialized);

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Messages.Serialization.SizeCalculators.CalculateSingleFrameRootContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
