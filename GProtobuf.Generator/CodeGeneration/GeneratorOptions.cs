namespace GProtobuf.Generator.CodeGeneration
{
    /// <summary>
    /// Options extracted from [assembly: GProtobufOptions(...)] attribute.
    /// Controls which code generators are enabled.
    /// </summary>
    internal sealed record GeneratorOptions
    {
        /// <summary>
        /// Enable generation of SpanReader-based deserialization methods.
        /// </summary>
        public bool GenerateSpanReader { get; init; } = true;

        /// <summary>
        /// Enable generation of StreamReader-based deserialization methods.
        /// </summary>
        public bool GenerateStreamReader { get; init; } = true;

        /// <summary>
        /// Enable generation of StreamWriter-based serialization methods.
        /// </summary>
        public bool GenerateStreamWriter { get; init; } = true;

        /// <summary>
        /// Enable generation of IBufferWriter-based serialization methods.
        /// </summary>
        public bool GenerateBufferWriter { get; init; } = true;

        /// <summary>
        /// Enable generation of OnePassStreamWriter-based serialization methods.
        /// </summary>
        public bool GenerateOnePassStreamWriter { get; init; } = false;

        /// <summary>
        /// Enable generation of StackBufferWriter-based serialization methods.
        /// Zero-allocation serialization for IoT devices with messages &lt;512 bytes.
        /// </summary>
        public bool GenerateStackBufferWriter { get; init; } = true;

        /// <summary>
        /// Enable string pooling for deserialization to reduce allocations.
        /// Uses StringPool for repeated string values (device IDs, sensor types).
        /// </summary>
        public bool UseStringPooling { get; init; } = false;

        /// <summary>
        /// If true, emit the old typed entry-point names (e.g. SerializeFoo, SerializeToArrayFoo).
        /// If false (default), emit overloaded names (Serialize, SerializeTo, SerializeToArray) with
        /// the OnePass suffix kept only when both 2-pass and 1-pass stream writers are generated
        /// (otherwise the two OnePass entry points would collide with the 2-pass overloads).
        /// </summary>
        public bool UseTypedSerializerNames { get; init; } = false;

        /// <summary>
        /// Default options with all generators enabled.
        /// </summary>
        public static GeneratorOptions Default { get; } = new();
    }
}
