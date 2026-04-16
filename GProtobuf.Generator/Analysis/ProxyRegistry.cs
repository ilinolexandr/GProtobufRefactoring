using System.Collections.Generic;

namespace GProtobuf.Generator.Analysis
{
    /// <summary>
    /// Registry of serialization proxy mappings. Maps original type full names to their proxy definitions.
    /// </summary>
    internal sealed class ProxyRegistry
    {
        private readonly Dictionary<string, ProxyDefinition> _proxies = new Dictionary<string, ProxyDefinition>();
        private readonly Dictionary<string, ProxyDefinition> _proxiesByProxyType = new Dictionary<string, ProxyDefinition>();

        public void Register(ProxyDefinition proxy)
        {
            _proxies[proxy.OriginalTypeFullName] = proxy;
            if (!string.IsNullOrEmpty(proxy.ProxyTypeFullName))
            {
                _proxiesByProxyType[proxy.ProxyTypeFullName] = proxy;
            }
        }

        public ProxyDefinition GetProxy(string originalTypeFullName)
        {
            return _proxies.TryGetValue(originalTypeFullName, out var proxy) ? proxy : null;
        }

        public bool HasProxy(string originalTypeFullName)
        {
            return _proxies.ContainsKey(originalTypeFullName);
        }

        /// <summary>
        /// Lookup by proxy type full name (reverse index). Used by deserialization codegen
        /// to detect when a [ProtoContract] type being read is itself a registered proxy
        /// (e.g., to call [ProxyAcquire] instead of `new()`).
        /// </summary>
        public ProxyDefinition GetProxyByProxyType(string proxyTypeFullName)
        {
            if (string.IsNullOrEmpty(proxyTypeFullName)) return null;
            return _proxiesByProxyType.TryGetValue(proxyTypeFullName, out var proxy) ? proxy : null;
        }

        public IEnumerable<ProxyDefinition> GetAll()
        {
            return _proxies.Values;
        }
    }
}
