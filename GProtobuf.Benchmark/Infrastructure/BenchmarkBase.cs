using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace GProtobuf.Benchmark.Infrastructure
{
    /// <summary>
    /// Base for paired serialize+deserialize benchmarks on a single model.
    /// <para>
    /// Derived class implements <see cref="BuildModel"/> and <see cref="PreSerialize"/>.
    /// Provides reusable <see cref="Stream"/> and <see cref="BufferWriter"/> so
    /// fresh-buffer alloc isn't measured inside the hot loop.
    /// </para>
    /// </summary>
    [MemoryDiagnoser(displayGenColumns: false)]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public abstract class SerdeBenchmarkBase<T>
    {
        protected T Model = default!;
        protected byte[] Serialized = default!;

        protected readonly MemoryStream Stream = new(1 << 16);
        protected readonly ArrayBufferWriter<byte> BufferWriter = new(1 << 16);

        protected abstract T BuildModel();
        protected abstract byte[] PreSerialize(T model);

        [GlobalSetup]
        public virtual void Setup()
        {
            Model = BuildModel();
            Serialized = PreSerialize(Model);
            WarmupProtoBufNet(Model);
        }

        /// <summary>
        /// protobuf-net 3.x lazily builds the runtime contract on first use.
        /// Calling Serialize once here registers the type in RuntimeTypeModel.Default
        /// so the baseline benchmark methods don't throw
        /// <c>Type is not expected, and no contract can be inferred</c>.
        /// <para>
        /// Swallows exceptions by design — some models (e.g. inheritance with
        /// dual-attribute R-009 workaround) will throw here because protobuf-net
        /// lacks [ProtoBuf.ProtoInclude]. We want the rest of the bench class
        /// to still run, so the protobuf-net Serialize method simply fails
        /// later on its individual benchmark row (NA) instead of killing
        /// GlobalSetup for the whole class.
        /// </para>
        /// </summary>
        protected static void WarmupProtoBufNet(T model)
        {
            if (model is null) return;
            try
            {
                using var ms = new MemoryStream();
                global::ProtoBuf.Serializer.Serialize(ms, model);
            }
            catch
            {
                // Intentional: keep GlobalSetup alive even if protobuf-net can't
                // handle this specific model shape.
            }
        }

        [IterationSetup]
        public virtual void Reset()
        {
            Stream.Position = 0;
            Stream.SetLength(0);
            BufferWriter.ResetWrittenCount();
        }

        [GlobalCleanup]
        public virtual void Cleanup()
        {
            Stream.Dispose();
        }
    }
}
