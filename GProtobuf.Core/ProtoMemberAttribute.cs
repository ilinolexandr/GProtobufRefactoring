using System;

namespace GProtobuf
{
    /// <summary>Marks a field or property for protobuf serialization with a unique field ID (tag).</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ProtoMemberAttribute : Attribute
    {
        public ProtoMemberAttribute(int tag)
        {
            Tag = tag;
        }

        /// <summary>Field ID (tag) used in the wire format.</summary>
        public int Tag { get; }

        /// <summary>Optional schema name for the member (currently informational).</summary>
        public string Name { get; set; }

        /// <summary>For repeated primitive fields: use packed encoding.</summary>
        public bool IsPacked { get; set; }

        /// <summary>Marks the member as required (validation hint).</summary>
        public bool IsRequired { get; set; }

        /// <summary>Wire encoding override (Default, ZigZag, FixedSize, …).</summary>
        public DataFormat DataFormat { get; set; }
    }
}
