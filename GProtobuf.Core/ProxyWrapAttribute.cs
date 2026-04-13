using System;

namespace ProtoBuf
{
    /// <summary>
    /// Marks a static factory method on a proxy type that wraps an original instance into a proxy instance for serialization.
    /// The method must be static, take one parameter of the original type, and return the proxy type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ProxyWrapAttribute : Attribute { }
}
