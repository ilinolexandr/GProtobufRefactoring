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

                case CollectionKind.ImmutableList:
                case CollectionKind.ImmutableHashSet:
                case CollectionKind.ImmutableSortedSet:
                    // .Count is O(1) for all of them (tree root / internal count field)
                    return $"{varName}.Count";

                case CollectionKind.ImmutableArray:
                    // ImmutableArray<T>.Length is O(1)
                    return $"{varName}.Length";

                case CollectionKind.ImmutableQueue:
                case CollectionKind.ImmutableStack:
                    // ImmutableQueue/Stack expose only IsEmpty — no O(1) Count/Length.
                    // Return null so callers take the slow (enumerating) write path.
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// True for System.Collections.Immutable kinds: they cannot be mutated in place, always
        /// accumulate into temp storage on read, and are finalized via a freeze expression.
        /// </summary>
        public static bool IsImmutable(CollectionKind kind)
        {
            return kind == CollectionKind.ImmutableList
                || kind == CollectionKind.ImmutableArray
                || kind == CollectionKind.ImmutableHashSet
                || kind == CollectionKind.ImmutableSortedSet
                || kind == CollectionKind.ImmutableQueue
                || kind == CollectionKind.ImmutableStack;
        }

        /// <summary>
        /// Returns the freeze expression converting an accumulated IEnumerable&lt;T&gt; source
        /// (temp List&lt;T&gt;, T[], ...) into the immutable collection; null for non-immutable kinds.
        /// NB: for ImmutableArray prefer the zero-copy
        /// <c>ImmutableCollectionsMarshal.AsImmutableArray(exactArray)</c> when an exactly-sized
        /// T[] is available — this generic CreateRange always copies.
        /// </summary>
        public static string GetFreezeExpression(CollectionKind kind, string sourceExpr)
        {
            switch (kind)
            {
                case CollectionKind.ImmutableList:
                    return $"global::System.Collections.Immutable.ImmutableList.CreateRange({sourceExpr})";
                case CollectionKind.ImmutableArray:
                    return $"global::System.Collections.Immutable.ImmutableArray.CreateRange({sourceExpr})";
                case CollectionKind.ImmutableHashSet:
                    return $"global::System.Collections.Immutable.ImmutableHashSet.CreateRange({sourceExpr})";
                case CollectionKind.ImmutableSortedSet:
                    return $"global::System.Collections.Immutable.ImmutableSortedSet.CreateRange({sourceExpr})";
                case CollectionKind.ImmutableQueue:
                    // FIFO: CreateRange enqueues in source order → enumeration order == wire order.
                    return $"global::System.Collections.Immutable.ImmutableQueue.CreateRange({sourceExpr})";
                case CollectionKind.ImmutableStack:
                    // LIFO: the temp source holds wire order (top-first). CreateRange pushes in source
                    // order (last = top), which would invert enumeration — reverse first so the rebuilt
                    // stack enumerates top-first exactly as written.
                    return $"global::System.Collections.Immutable.ImmutableStack.CreateRange(global::System.Linq.Enumerable.Reverse({sourceExpr}))";
                default:
                    return null;
            }
        }
    }
}
