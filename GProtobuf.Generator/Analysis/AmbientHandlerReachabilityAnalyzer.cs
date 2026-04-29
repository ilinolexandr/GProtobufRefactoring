using System.Collections.Generic;

namespace GProtobuf.Generator.Analysis
{
    /// <summary>Walks a root type's proxy-transformed serialization graph to decide whether a marker type is reachable.</summary>
    internal static class AmbientHandlerReachabilityAnalyzer
    {
        public static bool ContainsMarker(
            TypeRegistry types,
            ProxyRegistry proxies,
            string rootTypeFullName,
            string markerFullName)
        {
            if (string.IsNullOrEmpty(rootTypeFullName) || string.IsNullOrEmpty(markerFullName))
                return false;

            var visited = new HashSet<string>();
            return Descend(types, proxies, rootTypeFullName, markerFullName, visited);
        }

        private static bool Descend(
            TypeRegistry types,
            ProxyRegistry proxies,
            string typeFullName,
            string markerFullName,
            HashSet<string> visited)
        {
            if (string.IsNullOrEmpty(typeFullName))
                return false;

            if (typeFullName == markerFullName)
                return true;

            if (!visited.Add(typeFullName))
                return false;

            // If the type is proxy-wrapped, descend through the proxy's members.
            var proxy = proxies?.GetProxy(typeFullName);
            var fieldSourceTypeName = proxy != null ? proxy.ProxyTypeFullName : typeFullName;

            var def = types.GetByFullName(fieldSourceTypeName);
            if (def == null)
            {
              
                if (TransparentContainerClassifier.TryGetTypeArguments(fieldSourceTypeName, out var typeArguments))
                {
                    foreach (var arg in typeArguments)
                    {
                        if (Descend(types, proxies, arg, markerFullName, visited))
                            return true;
                    }
                }
                return false;
            }

            if (def.ProtoMembers != null)
            {
                foreach (var member in def.ProtoMembers)
                {
                    if (MemberReachesMarker(types, proxies, member, markerFullName, visited))
                        return true;
                }
            }

            foreach (var derived in types.GetAllDerivedTypes(fieldSourceTypeName))
            {
                if (Descend(types, proxies, derived, markerFullName, visited))
                    return true;
            }

            return false;
        }

        private static bool MemberReachesMarker(
            TypeRegistry types,
            ProxyRegistry proxies,
            ProtoMemberInfo member,
            string markerFullName,
            HashSet<string> visited)
        {
            if (member.IsMap)
            {
                if (Descend(types, proxies, member.MapKeyType, markerFullName, visited))
                    return true;
                if (Descend(types, proxies, member.MapValueType, markerFullName, visited))
                    return true;
                return false;
            }

            if (member.IsCollection)
            {
                return Descend(types, proxies, member.CollectionElementType, markerFullName, visited);
            }

            return Descend(types, proxies, member.Type, markerFullName, visited);
        }
    }
}
