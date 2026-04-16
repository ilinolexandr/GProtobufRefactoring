using System;

namespace GProtobuf
{
    /// <summary>Declares a derived type in a protobuf inheritance hierarchy with its own field ID.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class ProtoIncludeAttribute : Attribute
    {
        public ProtoIncludeAttribute(int tag, Type knownType)
        {
            Tag = tag;
            KnownType = knownType;
        }

        /// <summary>Field ID used for the length-delimited wrapper of the derived type.</summary>
        public int Tag { get; }

        /// <summary>Derived type covered by this [ProtoInclude].</summary>
        public Type KnownType { get; }
    }
}
