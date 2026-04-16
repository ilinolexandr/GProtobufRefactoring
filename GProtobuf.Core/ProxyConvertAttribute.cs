using System;

namespace GProtobuf
{
    /// <summary>
    /// Marks an instance method on a proxy type that converts the proxy back to the original type.
    /// The method must be an instance method, take no parameters, and return the original type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ProxyConvertAttribute : Attribute { }
}
