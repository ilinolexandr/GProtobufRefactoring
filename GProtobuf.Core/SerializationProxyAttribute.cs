using System;

namespace ProtoBuf
{
    /// <summary>
    /// Registers a serialization proxy: when the original type appears as a field,
    /// the proxy type (which must have [ProtoContract]) is used for serialization instead.
    /// The proxy type must have methods marked with [ProxyWrap] and [ProxyConvert].
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class SerializationProxyAttribute : Attribute
    {
        public Type OriginalType { get; }
        public Type ProxyType { get; }

        public SerializationProxyAttribute(Type originalType, Type proxyType)
        {
            OriginalType = originalType;
            ProxyType = proxyType;
        }
    }
}
