using System.Collections.Generic;

namespace GProtobuf.Generator.Analysis
{
    /// <summary>
    /// Registry of serialization proxy mappings. Maps original type full names to their proxy definitions.
    /// </summary>
    public sealed class ProxyRegistry
    {
        private readonly Dictionary<string, ProxyDefinition> _proxies = new Dictionary<string, ProxyDefinition>();

        public void Register(ProxyDefinition proxy)
        {
            _proxies[proxy.OriginalTypeFullName] = proxy;
        }

        public ProxyDefinition GetProxy(string originalTypeFullName)
        {
            return _proxies.TryGetValue(originalTypeFullName, out var proxy) ? proxy : null;
        }

        public bool HasProxy(string originalTypeFullName)
        {
            return _proxies.ContainsKey(originalTypeFullName);
        }

        public IEnumerable<ProxyDefinition> GetAll()
        {
            return _proxies.Values;
        }
    }
}
