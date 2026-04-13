using System;

namespace ProtoBuf
{
    /// <summary>
    /// Marks an optional instance method on a proxy type for cleanup/pooling after serialization or deserialization.
    /// The method must be an instance method, take no parameters, and return void.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ProxyReturnAttribute : Attribute { }
}
