namespace GProtobuf.Generator.Analysis
{
    /// <summary>
    /// Represents the kind of collection type for serialization
    /// </summary>
    internal enum CollectionKind
    {
        /// <summary>
        /// Not a collection type
        /// </summary>
        None,

        /// <summary>
        /// Array type (T[]) - deserialize to List&lt;T&gt; then convert to array
        /// </summary>
        Array,

        /// <summary>
        /// Interface collection type (ICollection&lt;T&gt;, IList&lt;T&gt;, IEnumerable&lt;T&gt;) - deserialize to List&lt;T&gt;
        /// </summary>
        InterfaceCollection,

        /// <summary>
        /// Concrete collection type (List&lt;T&gt;, MyCollection&lt;T&gt;) - deserialize to actual type
        /// </summary>
        ConcreteCollection,

        /// <summary>
        /// Custom collection type (non-generic class implementing ICollection&lt;T&gt; + Add method)
        /// Used for protobuf-net compatible collections without [ProtoContract]
        /// </summary>
        CustomCollection,

        /// <summary>
        /// Custom enumerable type (non-generic class implementing IEnumerable&lt;T&gt; + Add method)
        /// Used for protobuf-net compatible collections without [ProtoContract]
        /// </summary>
        CustomEnumerable,

        /// <summary>
        /// System.Collections.Immutable.ImmutableList&lt;T&gt; — accumulate into a temp list,
        /// freeze via ImmutableList.CreateRange. O(1) .Count; iterate with foreach only
        /// (the indexer is O(log n)).
        /// </summary>
        ImmutableList,

        /// <summary>
        /// System.Collections.Immutable.ImmutableArray&lt;T&gt; — struct wrapper over T[];
        /// build an exact array and wrap. O(1) .Length. Null-state is default/IsDefault,
        /// NOT null — never emit `!= null` checks for this kind.
        /// </summary>
        ImmutableArray,

        /// <summary>
        /// System.Collections.Immutable.ImmutableHashSet&lt;T&gt; — accumulate into a temp list,
        /// freeze via ImmutableHashSet.CreateRange. O(1) .Count. Enumeration order is hash-based.
        /// </summary>
        ImmutableHashSet,

        /// <summary>
        /// System.Collections.Immutable.ImmutableSortedSet&lt;T&gt; — accumulate into a temp list,
        /// freeze via ImmutableSortedSet.CreateRange. O(1) .Count. Enumerates ascending.
        /// </summary>
        ImmutableSortedSet,

        /// <summary>
        /// System.Collections.Immutable.ImmutableQueue&lt;T&gt; / IImmutableQueue&lt;T&gt; — FIFO.
        /// Accumulate into a temp list (wire order), freeze via ImmutableQueue.CreateRange
        /// (enqueues in source order, so enumeration order == wire order). NO O(1) Count
        /// (only IsEmpty) — falls back to the slow write path. 
        /// </summary>
        ImmutableQueue,

        /// <summary>
        /// System.Collections.Immutable.ImmutableStack&lt;T&gt; / IImmutableStack&lt;T&gt; — LIFO.
        /// Enumerates top-first, so the writer emits items top-first (matching pn 3.x). On read the
        /// temp list holds wire order [top..bottom]; ImmutableStack.CreateRange pushes in source order
        /// (last pushed = top), so the freeze MUST reverse the temp list first to restore the original
        /// top-first enumeration — see CollectionKindHelper.GetFreezeExpression. NO O(1) Count.
        /// </summary>
        ImmutableStack
    }
}
