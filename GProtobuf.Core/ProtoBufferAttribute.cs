using System;

namespace GProtobuf
{
    /// <summary>Defines the type of operation for custom buffer serialization.</summary>
    public enum ProtoBufferOperation
    {
        /// <summary>Method returns the size of the buffer (int, no parameters).</summary>
        GetSize,

        /// <summary>Method writes data to the buffer (void, Span&lt;byte&gt; parameter).</summary>
        Write,

        /// <summary>Method reads data from the buffer (void, ReadOnlySpan&lt;byte&gt; parameter).</summary>
        Read
    }

    /// <summary>
    /// Marks a method as part of custom buffer serialization for a specific field.
    /// Three methods are typically paired (same tag): GetSize, Write, and optionally Read.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ProtoBufferAttribute : Attribute
    {
        public ProtoBufferAttribute(int tag, ProtoBufferOperation operation)
        {
            Tag = tag;
            Operation = operation;
        }

        /// <summary>Unique field ID (tag); all three operations for a field must share the same tag.</summary>
        public int Tag { get; }

        /// <summary>The type of operation this method performs.</summary>
        public ProtoBufferOperation Operation { get; }
    }
}
