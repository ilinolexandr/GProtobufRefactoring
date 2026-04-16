using System;

namespace GProtobuf
{
    /// <summary>Specifies the varint encoding type for [ProtoVarint] structs/classes.</summary>
    public enum ProtoVarintType
    {
        /// <summary>Unsigned 32-bit integer (varint encoding).</summary>
        UInt32,

        /// <summary>Signed 32-bit integer (varint encoding; inefficient for negative values).</summary>
        Int32,

        /// <summary>Signed 32-bit integer with ZigZag encoding (efficient for negative values).</summary>
        SInt32,

        /// <summary>Unsigned 64-bit integer (varint encoding).</summary>
        UInt64,

        /// <summary>Signed 64-bit integer (varint encoding; inefficient for negative values).</summary>
        Int64,

        /// <summary>Signed 64-bit integer with ZigZag encoding (efficient for negative values).</summary>
        SInt64
    }

    /// <summary>
    /// Marks a struct or class to be serialized as a varint instead of a nested message.
    /// The type must have a constructor with [ProtoVarintConstructor] and a member with [ProtoVarintValue].
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ProtoVarintAttribute : Attribute
    {
        public ProtoVarintAttribute(ProtoVarintType type = ProtoVarintType.UInt32)
        {
            Type = type;
        }

        /// <summary>The varint encoding type for this struct.</summary>
        public ProtoVarintType Type { get; }
    }

    /// <summary>
    /// Marks a constructor used for deserializing a [ProtoVarint] type.
    /// The constructor must have exactly one parameter matching the ProtoVarintType.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class ProtoVarintConstructorAttribute : Attribute { }

    /// <summary>
    /// Marks a method, property, or field that returns the underlying value for serialization.
    /// The return type must match the ProtoVarintType specified on the struct.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ProtoVarintValueAttribute : Attribute { }
}
