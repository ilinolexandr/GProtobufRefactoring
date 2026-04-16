using System;

namespace GProtobuf
{
    /// <summary>Excludes a field or property from protobuf serialization.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ProtoIgnoreAttribute : Attribute { }
}
