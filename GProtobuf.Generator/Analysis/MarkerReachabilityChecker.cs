using System;
using System.Collections.Generic;

namespace GProtobuf.Generator.Analysis
{
    /// <summary>
    /// Caches answers to <c>"does the graph rooted at type T reach marker M?"</c>.
    ///
    /// <para><b>Why a shared cache:</b></para>
    /// Two code paths ask the same question for the same registries during one generation
    /// pass — diagnostics (<c>RootsReachingMarker</c> for GPROTO026/029) and code emission
    /// (<see cref="V2.Helpers.AmbientHandlerEmitter"/>'s per-entry-point check). Without
    /// sharing, each pair <c>(type, marker)</c> would be recomputed at least twice; with
    /// sharing the second caller hits a warm cache. <see cref="AmbientHandlerReachabilityAnalyzer.ContainsMarker"/>
    /// remains the single source of truth for the actual graph walk.
    ///
    /// <para><b>Lifecycle:</b></para>
    /// One instance per <see cref="V2.ObjectTreeV2"/>. Built once <see cref="TypeRegistry"/>
    /// is fully populated; safe to query thereafter. Not thread-safe — generation pipelines
    /// run single-threaded per compilation.
    /// </summary>
    internal sealed class MarkerReachabilityChecker
    {
        private readonly TypeRegistry _typeRegistry;
        private readonly ProxyRegistry _proxyRegistry;
        private readonly Dictionary<(string TypeName, string MarkerName), bool> _cache
            = new Dictionary<(string, string), bool>();

        public MarkerReachabilityChecker(TypeRegistry typeRegistry, ProxyRegistry proxyRegistry)
        {
            _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
            _proxyRegistry = proxyRegistry; // null is valid (no proxies registered).
        }

        /// <summary>True if the graph rooted at <paramref name="typeFullName"/> reaches <paramref name="markerFullName"/>. Cached.</summary>
        public bool IsReachable(string typeFullName, string markerFullName)
        {
            if (string.IsNullOrEmpty(typeFullName) || string.IsNullOrEmpty(markerFullName))
                return false;

            var key = (typeFullName, markerFullName);
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            bool result = AmbientHandlerReachabilityAnalyzer.ContainsMarker(
                _typeRegistry, _proxyRegistry, typeFullName, markerFullName);
            _cache[key] = result;
            return result;
        }

        /// <summary>True if <paramref name="markerFullName"/> is reachable from any registered ProtoContract root.</summary>
        public bool IsReachableFromAnyRoot(string markerFullName)
        {
            if (string.IsNullOrEmpty(markerFullName))
                return false;

            foreach (var type in _typeRegistry.GetAllTypes())
            {
                if (IsReachable(type.FullName, markerFullName))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the full names of every registered ProtoContract root whose graph reaches
        /// <paramref name="markerFullName"/>. Empty if no root reaches it. Order matches
        /// <see cref="TypeRegistry.GetAllTypes"/>. Used by GPROTO029 to surface per-handler
        /// wrap counts.
        /// </summary>
        public IReadOnlyList<string> RootsReaching(string markerFullName)
        {
            if (string.IsNullOrEmpty(markerFullName))
                return Array.Empty<string>();

            var hits = new List<string>();
            foreach (var type in _typeRegistry.GetAllTypes())
            {
                if (IsReachable(type.FullName, markerFullName))
                    hits.Add(type.FullName);
            }
            return hits;
        }
    }
}
