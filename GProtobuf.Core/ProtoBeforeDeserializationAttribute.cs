using System;

namespace GProtobuf
{
    /// <summary>Callback method invoked immediately before deserialization of the declaring type.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ProtoBeforeDeserializationAttribute : Attribute { }
}
