using System;
using System.Collections.Generic;
using GProtobuf.Generator.WireFormat;

namespace GProtobuf.Generator.Analysis
{
    /// <summary>
    /// Classifies "transparent" generic containers (Dictionary, List, Tuple, Nullable, etc.) by
    /// open-name match against a fixed catalog and exposes their type arguments.
    ///
    /// Used by <see cref="AmbientHandlerReachabilityAnalyzer"/> to keep walking the type graph
    /// across non-<c>[ProtoContract]</c> generic collection types, where the registry has no
    /// <see cref="TypeDefinition"/> to descend through. Pure string-in / string-out — no Roslyn
    /// dependency, no plumbing churn.
    /// </summary>
    /// <remarks>
    /// Match rule is exact open-name only (after <see cref="TypeMapping.NormalizeTypeName"/>),
    /// so a user <c>[ProtoContract]</c> type that *also* implements <c>IEnumerable&lt;X&gt;</c>
    /// is not transparently walked — it falls into the standard registered-type path of the
    /// caller before this classifier ever runs.
    /// </remarks>
    internal static class TransparentContainerClassifier
    {
        // Arity-2 dictionaries: recurse into both K and V.
        private static readonly HashSet<string> Dictionaries = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Collections.Generic.Dictionary",
            "System.Collections.Generic.IDictionary",
            "System.Collections.Generic.IReadOnlyDictionary",
            "System.Collections.Generic.SortedDictionary",
            "System.Collections.Generic.SortedList",
            "System.Collections.Concurrent.ConcurrentDictionary",
            "System.Collections.Immutable.ImmutableDictionary",
            "System.Collections.Immutable.ImmutableSortedDictionary",
            "System.Collections.Generic.KeyValuePair",
            // Project-specific (lives at TapModels/TapHome.Core.Lib.Model/ListDictionary.cs).
            "TapHome.Core.Lib.Model.ListDictionary",
        };

        // Arity-1 collections / wrappers: recurse into the single type argument.
        private static readonly HashSet<string> SingleArgContainers = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Collections.Generic.List",
            "System.Collections.Generic.IList",
            "System.Collections.Generic.IReadOnlyList",
            "System.Collections.Generic.ICollection",
            "System.Collections.Generic.IReadOnlyCollection",
            "System.Collections.Generic.IEnumerable",
            "System.Collections.Generic.HashSet",
            "System.Collections.Generic.SortedSet",
            "System.Collections.Generic.Queue",
            "System.Collections.Generic.Stack",
            "System.Collections.Generic.LinkedList",
            "System.Collections.ObjectModel.Collection",
            "System.Collections.ObjectModel.ReadOnlyCollection",
            "System.Collections.ObjectModel.ObservableCollection",
            "System.Collections.Concurrent.ConcurrentBag",
            "System.Collections.Concurrent.ConcurrentQueue",
            "System.Collections.Concurrent.ConcurrentStack",
            "System.Collections.Immutable.ImmutableArray",
            "System.Collections.Immutable.ImmutableList",
            "System.Collections.Immutable.ImmutableHashSet",
            "System.Collections.Immutable.ImmutableSortedSet",
            "System.Collections.Immutable.ImmutableQueue",
            "System.Collections.Immutable.ImmutableStack",
            "System.ArraySegment",
            "System.Memory",
            "System.ReadOnlyMemory",
            "System.Span",
            "System.ReadOnlySpan",
            "System.Lazy",
        };

        private const string TupleOpenName = "System.Tuple";
        private const string ValueTupleOpenName = "System.ValueTuple";
        private const string NullableOpenName = "System.Nullable";

        /// <summary>
        /// Returns <c>true</c> if <paramref name="typeFullName"/> is a transparent container whose
        /// type arguments should be walked individually (e.g. <c>Dictionary&lt;K,V&gt;</c>,
        /// <c>List&lt;T&gt;</c>, <c>T[]</c>, <c>Tuple&lt;...&gt;</c>, <c>Nullable&lt;T&gt;</c>).
        /// </summary>
        public static bool TryGetTypeArguments(string typeFullName, out IReadOnlyList<string> typeArguments)
        {
            typeArguments = null;
            if (string.IsNullOrEmpty(typeFullName))
                return false;

            var normalized = TypeMapping.NormalizeTypeName(typeFullName);

            // Arrays: T[] -> recurse into T. Multi-dim arrays (T[,]) are out of scope for protobuf.
            if (normalized.Length >= 2 && normalized[normalized.Length - 1] == ']' && normalized[normalized.Length - 2] == '[')
            {
                var element = normalized.Substring(0, normalized.Length - 2);
                typeArguments = new[] { element };
                return true;
            }

            int openBracket = normalized.IndexOf('<');
            if (openBracket < 0)
                return false;

            int closeBracket = normalized.LastIndexOf('>');
            if (closeBracket <= openBracket)
                return false;

            string outer = normalized.Substring(0, openBracket);

            bool match =
                Dictionaries.Contains(outer)
                || SingleArgContainers.Contains(outer)
                || outer == TupleOpenName
                || outer == ValueTupleOpenName
                || outer == NullableOpenName;

            if (!match)
                return false;

            string inside = normalized.Substring(openBracket + 1, closeBracket - openBracket - 1);
            typeArguments = SplitTopLevelTypeArguments(inside);
            return typeArguments.Count > 0;
        }

        /// <summary>
        /// Depth-aware split of a generic type-argument list at top-level commas.
        /// Mirrors the splitter used by <c>TypeHelper.ParseDictionaryTypes</c> and
        /// <c>TypeMapping.NormalizeGenericTypeName</c>.
        /// </summary>
        private static IReadOnlyList<string> SplitTopLevelTypeArguments(string argsString)
        {
            var result = new List<string>(2);
            int depth = 0;
            int start = 0;

            for (int i = 0; i < argsString.Length; i++)
            {
                char c = argsString[i];
                if (c == '<')
                {
                    depth++;
                }
                else if (c == '>')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    var arg = argsString.Substring(start, i - start).Trim();
                    if (arg.Length > 0)
                        result.Add(arg);
                    start = i + 1;
                }
            }

            if (start < argsString.Length)
            {
                var arg = argsString.Substring(start).Trim();
                if (arg.Length > 0)
                    result.Add(arg);
            }

            return result;
        }
    }
}
