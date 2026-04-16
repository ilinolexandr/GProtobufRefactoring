using System;

namespace GProtobuf
{
    /// <summary>Callback method invoked immediately after deserialization of the declaring type.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ProtoAfterDeserializationAttribute : Attribute { }
}
