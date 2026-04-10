using System;
using System.Collections.Generic;
using GProtobuf.Generator.Analysis;
using GProtobuf.Generator.Attributes;
using GProtobuf.Generator.V2.Handlers.Core;
using GProtobuf.Generator.WireFormat;

namespace GProtobuf.Generator.V2.Helpers
{
    /// <summary>
    /// Shared helper for ObjectArrayBuilder code generation decisions and code generation.
    /// Centralizes the logic for determining when to use ObjectArrayBuilder vs List&lt;T&gt;.
    /// </summary>
    internal static class ObjectArrayBuilderHelper
    {
        /// <summary>
        /// Initial capacity for ObjectArrayBuilder instances.
        /// </summary>
        internal const int InitialCapacity = 16;

        /// <summary>
        /// Determines if a collection member should use ObjectArrayBuilder instead of List&lt;T&gt;.
        /// ObjectArrayBuilder is used for reference types (classes) to leverage unified ArrayPool&lt;object&gt;.
        /// </summary>
        /// <param name="member">The proto member attribute describing the collection.</param>
        /// <param name="registry">The type registry for looking up type information.</param>
        /// <returns>True if ObjectArrayBuilder should be used, false for List&lt;T&gt;.</returns>
        internal static bool ShouldUseObjectArrayBuilder(ProtoMemberAttribute member, TypeRegistry registry)
        {
            if (member?.CollectionElementType == null)
                return false;

            return ShouldUseObjectArrayBuilder(member.CollectionElementType, registry);
        }

        /// <summary>
        /// Determines if a collection with the given element type should use ObjectArrayBuilder.
        /// </summary>
        /// <param name="elementTypeName">The fully qualified element type name.</param>
        /// <param name="registry">The type registry for looking up type information.</param>
        /// <returns>True if ObjectArrayBuilder should be used, false for List&lt;T&gt;.</returns>
        internal static bool ShouldUseObjectArrayBuilder(string elementTypeName, TypeRegistry registry)
        {
            if (string.IsNullOrEmpty(elementTypeName))
                return false;

            // String is a reference type and satisfies the where T : class constraint.
            // Route it to ObjectArrayBuilder before the IsSimpleType check below.
            var normalized = TypeMapping.NormalizeTypeName(elementTypeName);
            if (normalized == "System.String")
                return true;

            // Simple types (primitives, DateTime, Guid, etc.) - use List<T>
            if (TypeMapping.IsSimpleType(elementTypeName))
                return false;

            // Check if it's a known type in registry
            var typeDef = registry?.GetByFullName(elementTypeName);
            if (typeDef != null)
            {
                // Structs cannot use ObjectArrayBuilder (has constraint where T : class)
                if (typeDef.IsStruct)
                    return false;

                // Enums use primitive handling
                if (typeDef.IsEnum)
                    return false;

                // Classes - use ObjectArrayBuilder
                return true;
            }

            // Unknown types - default to false to be safe
            return false;
        }

        /// <summary>
        /// Checks whether the collection kind requires temporary storage at method level
        /// (Array or IEnumerable&lt;T&gt;). These are the kinds that ALWAYS go through a method-level
        /// _builder_X / _tempList_X variable regardless of element type, because Array and
        /// IEnumerable&lt;T&gt; cannot be incrementally appended.
        /// </summary>
        internal static bool IsArrayOrIEnumerable(ProtoMemberAttribute member)
        {
            if (member == null || !member.IsCollection)
                return false;

            if (member.CollectionKind == CollectionKind.Array)
                return true;

            if (member.CollectionKind == CollectionKind.InterfaceCollection && member.Type != null)
            {
                var normalized = TypeMapping.NormalizeTypeName(member.Type);
                return normalized.StartsWith("System.Collections.Generic.IEnumerable<")
                    && !member.Type.Contains("ICollection")
                    && !member.Type.Contains("IList");
            }

            return false;
        }

        /// <summary>
        /// Phase 2: Checks whether the collection kind is eligible for ObjectArrayBuilder routing
        /// EVEN THOUGH it could otherwise be appended in place.
        /// True for: List&lt;T&gt; (exact, not subclass), IList&lt;T&gt;, ICollection&lt;T&gt;.
        /// False for: HashSet, custom List subclasses, custom HashSet subclasses, any non-Add collection.
        ///
        /// These kinds go through the builder only when the element type is also builder-eligible
        /// (class or string) — otherwise the existing per-Add path is faster.
        /// </summary>
        internal static bool IsAppendableBuilderCandidate(ProtoMemberAttribute member)
        {
            if (member == null || !member.IsCollection || member.Type == null)
                return false;

            var normalizedType = TypeMapping.NormalizeTypeName(member.Type);

            if (member.CollectionKind == CollectionKind.ConcreteCollection)
            {
                // Custom List subclasses (e.g., MyList : List<T>) must NOT be routed through
                // the builder — ToList() on the builder would lose the original concrete type.
                // NB: TypeHelper.IsCustomListType also returns true for IList<T> because it
                // matches anything containing "List<" — so we only consult it for ConcreteCollection.
                if (TypeHelper.IsCustomListType(normalizedType) || TypeHelper.IsCustomHashSetType(normalizedType))
                    return false;
                if (TypeHelper.IsHashSetType(normalizedType))
                    return false;

                // Only exact System.Collections.Generic.List<T> qualifies.
                return normalizedType == "System.Collections.Generic.List"
                    || normalizedType.Contains("System.Collections.Generic.List<")
                    || normalizedType.StartsWith("List<");
            }

            if (member.CollectionKind == CollectionKind.InterfaceCollection)
            {
                // IList<T> / ICollection<T> — but NOT IEnumerable<T> (handled by IsArrayOrIEnumerable).
                // Note: a leading 'I' before List/Collection is what distinguishes interfaces.
                return normalizedType.Contains("IList<")
                    || normalizedType.Contains("ICollection<");
            }

            return false;
        }

        /// <summary>
        /// Returns true when the field should be registered in the method-level builder/templist filter.
        /// This is the unified eligibility check used by all generator filter sites.
        /// - Array / IEnumerable&lt;T&gt;: always (element-type-agnostic; primitives still go through templist).
        /// - List&lt;T&gt; / IList&lt;T&gt; / ICollection&lt;T&gt;: only when element type is builder-eligible (class/string).
        /// </summary>
        internal static bool IsFieldNeedingTempListOrBuilder(ProtoMemberAttribute member, TypeRegistry registry)
        {
            if (IsArrayOrIEnumerable(member))
                return true;

            if (IsAppendableBuilderCandidate(member) && ShouldUseObjectArrayBuilder(member, registry))
                return true;

            return false;
        }

        /// <summary>
        /// Returns true when the field will be backed by a class-level ObjectArrayBuilder
        /// at the read site (so PrimitiveHandler / CollectionHandler should write directly into
        /// <c>_builder_{Name}</c> instead of a local temp list or instance.X.Add).
        /// </summary>
        internal static bool ShouldUseObjectArrayBuilderForRead(ProtoMemberAttribute member, TypeRegistry registry)
        {
            if (!ShouldUseObjectArrayBuilder(member, registry))
                return false;
            return IsArrayOrIEnumerable(member) || IsAppendableBuilderCandidate(member);
        }

        /// <summary>
        /// Determines if a collection member needs a class-level _tempList_ declaration.
        /// Returns true only for element types that are handled by CollectionHandler or TupleHandler
        /// (structs, tuples, DateTime arrays).
        /// Returns false for:
        /// - Classes (use ObjectArrayBuilder via _builder_)
        /// - Primitives (PrimitiveHandler uses local storage)
        /// - Enums (PrimitiveHandler uses local storage)
        /// </summary>
        /// <param name="member">The proto member attribute describing the collection.</param>
        /// <param name="registry">The type registry for looking up type information.</param>
        /// <returns>True if _tempList_ declaration is needed, false otherwise.</returns>
        internal static bool NeedsTempListDeclaration(ProtoMemberAttribute member, TypeRegistry registry)
        {
            if (member?.CollectionElementType == null)
                return false;

            // Classes use ObjectArrayBuilder, not _tempList_
            if (ShouldUseObjectArrayBuilder(member, registry))
                return false;

            // Primitives: PrimitiveHandler uses local storage (UnmanagedArrayBuilder or local tempList)
            // This includes: int, long, string, Guid, TimeSpan, byte, etc.
            if (TypeMapping.IsNonPackedArrayType(member.CollectionElementType))
                return false;

            // Enums: PrimitiveHandler uses local storage
            var normalizedType = TypeMapping.NormalizeTypeName(member.CollectionElementType);
            if (registry?.IsEnum(member.CollectionElementType) == true ||
                registry?.IsEnum(normalizedType) == true)
                return false;

            // Remaining types need _tempList_: structs, tuples, DateTime, etc.
            return true;
        }

        /// <summary>
        /// Generates declaration code for ObjectArrayBuilder fields.
        /// </summary>
        /// <param name="sb">The string builder to write to.</param>
        /// <param name="members">The collection members using ObjectArrayBuilder.</param>
        /// <param name="getElementType">Function to get the element type for a member.</param>
        /// <param name="targetVarForPreSeed">
        /// Optional target object name (e.g. <c>"instance"</c>) used to MERGE-safely pre-seed
        /// the builder from the target's existing collection contents. When non-null and the
        /// member is an appendable kind (List/IList/ICollection), the generator emits:
        /// <code>
        /// if (target.X != null)
        /// {
        ///     foreach (var item in target.X) _builder_X.Add(item);
        /// }
        /// </code>
        /// For Array and IEnumerable&lt;T&gt; pre-seed is skipped because those kinds were already
        /// fully replaced under the previous code path (no merge with existing data).
        /// Pass <c>null</c> when the target is a freshly constructed local (e.g. <c>"result"</c>
        /// in <c>ReadXxxContent</c>) — pre-seed would always be a no-op there.
        /// </param>
        internal static void GenerateDeclarations(
            StringBuilderWithIndent sb,
            IReadOnlyList<ProtoMemberAttribute> members,
            Func<ProtoMemberAttribute, string> getElementType,
            string targetVarForPreSeed = null)
        {
            if (members == null || members.Count == 0)
                return;

            foreach (var member in members)
            {
                var elementType = getElementType(member);
                sb.AppendIndentedLine($"var _builder_{member.Name} = new global::GProtobuf.Core.ObjectArrayBuilder<{elementType}>({InitialCapacity});");

                // Pre-seed only for appendable kinds (List/IList/ICollection) where the caller may
                // have passed a pre-populated instance whose existing items must be preserved.
                if (!string.IsNullOrEmpty(targetVarForPreSeed) && IsAppendableBuilderCandidate(member))
                {
                    sb.AppendIndentedLine($"if ({targetVarForPreSeed}.{member.Name} != null)");
                    sb.StartNewBlock();
                    sb.AppendIndentedLine($"foreach (var __preSeedItem in {targetVarForPreSeed}.{member.Name})");
                    sb.StartNewBlock();
                    sb.AppendIndentedLine($"_builder_{member.Name}.Add(__preSeedItem);");
                    sb.EndBlock();
                    sb.EndBlock();
                }
            }
        }

        /// <summary>
        /// Generates finalization code for List&lt;T&gt; temp list fields.
        /// Converts to array if needed and assigns to target property.
        /// </summary>
        /// <param name="sb">The string builder to write to.</param>
        /// <param name="members">The collection members using temp lists.</param>
        /// <param name="targetVar">The target variable name (e.g., "result" or "instance").</param>
        /// <param name="isArrayType">Optional function to check if member should use ToArray().
        /// If null, defaults to checking CollectionKind == Array.</param>
        internal static void GenerateTempListFinalization(
            StringBuilderWithIndent sb,
            IReadOnlyList<ProtoMemberAttribute> members,
            string targetVar,
            Func<ProtoMemberAttribute, bool> isArrayType = null)
        {
            if (members == null || members.Count == 0)
                return;

            sb.AppendNewLine();
            foreach (var member in members)
            {
                sb.AppendIndentedLine($"if (_tempList_{member.Name} != null)");
                sb.StartNewBlock();

                bool useToArray = isArrayType != null
                    ? isArrayType(member)
                    : member.CollectionKind == CollectionKind.Array;

                if (useToArray)
                {
                    sb.AppendIndentedLine($"{targetVar}.{member.Name} = _tempList_{member.Name}.ToArray();");
                }
                else
                {
                    sb.AppendIndentedLine($"{targetVar}.{member.Name} = _tempList_{member.Name};");
                }

                sb.EndBlock();
            }
        }

        /// <summary>
        /// Generates conversion code for ObjectArrayBuilder fields (inside try block).
        /// Converts collected items to array/list and assigns to target property.
        /// </summary>
        /// <param name="sb">The string builder to write to.</param>
        /// <param name="members">The collection members using ObjectArrayBuilder.</param>
        /// <param name="targetVar">The target variable name (e.g., "result" or "instance").</param>
        /// <param name="isArrayType">Optional function to check if member should use ToArray().
        /// If null, defaults to checking CollectionKind == Array.</param>
        internal static void GenerateConversion(
            StringBuilderWithIndent sb,
            IReadOnlyList<ProtoMemberAttribute> members,
            string targetVar,
            Func<ProtoMemberAttribute, bool> isArrayType = null)
        {
            if (members == null || members.Count == 0)
                return;

            sb.AppendNewLine();
            foreach (var member in members)
            {
                sb.AppendIndentedLine($"if (_builder_{member.Name}.Count > 0)");
                sb.StartNewBlock();

                bool useToArray = isArrayType != null
                    ? isArrayType(member)
                    : member.CollectionKind == CollectionKind.Array;

                if (useToArray)
                {
                    sb.AppendIndentedLine($"{targetVar}.{member.Name} = _builder_{member.Name}.ToArray();");
                }
                else
                {
                    sb.AppendIndentedLine($"{targetVar}.{member.Name} = _builder_{member.Name}.ToList();");
                }

                sb.EndBlock();
            }
        }

        /// <summary>
        /// Generates dispose code for ObjectArrayBuilder fields (inside finally block).
        /// </summary>
        /// <param name="sb">The string builder to write to.</param>
        /// <param name="members">The collection members using ObjectArrayBuilder.</param>
        internal static void GenerateDispose(
            StringBuilderWithIndent sb,
            IReadOnlyList<ProtoMemberAttribute> members)
        {
            if (members == null || members.Count == 0)
                return;

            foreach (var member in members)
            {
                sb.AppendIndentedLine($"_builder_{member.Name}.Dispose();");
            }
        }
    }
}
