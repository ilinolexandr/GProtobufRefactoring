using System;

namespace GProtobuf
{
    /// <summary>
    /// Assembly-level pairing: serialize instances of <see cref="OriginalType"/> through the proxy
    /// <see cref="ProxyType"/> (which must carry [ProtoContract] and [ProxyWrap]/[ProxyConvert]).
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class SerializationProxyAttribute : Attribute
    {
        public SerializationProxyAttribute(Type originalType, Type proxyType)
        {
            OriginalType = originalType;
            ProxyType = proxyType;
        }

        public Type OriginalType { get; }
        public Type ProxyType { get; }
    }
}
