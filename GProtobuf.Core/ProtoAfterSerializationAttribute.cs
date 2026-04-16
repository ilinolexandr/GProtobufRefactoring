using System;

namespace GProtobuf
{
    /// <summary>Callback method invoked immediately after serialization of the declaring type.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ProtoAfterSerializationAttribute : Attribute { }
}
