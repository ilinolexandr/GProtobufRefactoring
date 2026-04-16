using System;

namespace GProtobuf
{
    /// <summary>Callback method invoked immediately before serialization of the declaring type.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ProtoBeforeSerializationAttribute : Attribute { }
}
