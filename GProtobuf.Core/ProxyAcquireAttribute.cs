using System;

namespace GProtobuf
{
    /// <summary>
    /// Marks an optional static factory method on a proxy type that produces a fresh proxy instance during deserialization.
    /// When present, the generator calls this method instead of <c>new ProxyType()</c> before populating fields, enabling
    /// pool-based reuse of proxy instances. The method must be static, take no parameters, and return the proxy type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ProxyAcquireAttribute : Attribute { }
}
