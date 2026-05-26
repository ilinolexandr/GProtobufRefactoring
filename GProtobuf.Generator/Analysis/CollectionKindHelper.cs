namespace GProtobuf.Generator.Analysis
{
    /// <summary>
    /// Utility methods for working with <see cref="CollectionKind"/> in code generation.
    /// </summary>
    internal static class CollectionKindHelper
    {
        /// <summary>
        /// Returns an O(1) count accessor expression for a collection kind, or <c>null</c> when
        /// no cheap accessor is available (plain <c>IEnumerable&lt;T&gt;</c> — counting would
        /// require enumeration and defeats the purpose of any caller-side fast path).
        /// </summary>
        /// <param name="varName">
        /// The variable name of the collection in the emitted code (typically <c>"collection"</c>).
        /// </param>
        /// <param name="kind">The classified collection kind from <see cref="CollectionKind"/>.</param>
        /// <param name="collectionTypeName">
        /// The full type name (e.g. <c>System.Collections.Generic.IEnumerable&lt;int&gt;</c>);
        /// used to detect plain IEnumerable variants that lack a cheap <c>.Count</c>.
        /// </param>
        public static string TryGetCheapCountExpression(string varName, CollectionKind kind, string collectionTypeName)
        {
            switch (kind)
            {
                case CollectionKind.Array:
                    return $"{varName}.Length";

                case CollectionKind.ConcreteCollection:
                    // List<T>, HashSet<T>, custom concrete collections all expose .Count
                    return $"{varName}.Count";

                case CollectionKind.InterfaceCollection:
                    // IList<T> / ICollection<T> / IReadOnlyList<T> / IReadOnlyCollection<T> have .Count
                    // IEnumerable<T> does NOT — fall back to the slow path
                    if (collectionTypeName != null &&
                        (collectionTypeName.Contains("IEnumerable<") ||
                         collectionTypeName.Contains("System.Collections.Generic.IEnumerable<")))
                    {
                        return null;
                    }
                    return $"{varName}.Count";

                case CollectionKind.CustomCollection:
                    // Custom non-generic ICollection<T>-based — has .Count
                    return $"{varName}.Count";

                case CollectionKind.CustomEnumerable:
                    // Custom IEnumerable<T>-only — no guaranteed .Count
                    return null;

                default:
                    return null;
            }
        }
    }
}
