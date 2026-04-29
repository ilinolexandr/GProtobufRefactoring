using System.Collections.Generic;
using System.Linq;
using GProtobuf.Generator.Analysis;
using GProtobuf.Generator.Utilities;

namespace GProtobuf.Generator.V2.Helpers
{
    /// <summary>One handler opened in a prologue, paired with the outer-flag local the prologue declared.</summary>
    internal readonly struct HandlerSlot
    {
        public readonly AmbientHandlerDefinition Handler;
        public readonly string OuterLocal;
        public HandlerSlot(AmbientHandlerDefinition handler, string outerLocal)
        {
            Handler = handler;
            OuterLocal = outerLocal;
        }
    }

    /// <summary>Handle returned by a prologue and consumed by the matching epilogue to close handlers in LIFO order.</summary>
    internal readonly struct HandlerEmissionHandle
    {
        public readonly IReadOnlyList<HandlerSlot> Slots;
        public HandlerEmissionHandle(IReadOnlyList<HandlerSlot> slots) { Slots = slots; }
        public bool IsEmpty => Slots == null || Slots.Count == 0;
    }

    /// <summary>Emits the Before/After wrap for entry-points whose graph reaches a registered marker.</summary>
    internal sealed class AmbientHandlerEmitter
    {
        private readonly AmbientHandlerRegistry _registry;
        private readonly MarkerReachabilityChecker _reachabilityChecker;

        // (typeFullName, forSerialization) → resolved handler list. Cached because a single
        // ProtoContract type's handlers are queried multiple times across spans/streams/buffers.
        // The (type, marker) reachability check it depends on is itself cached inside the
        // shared MarkerReachabilityChecker.
        private readonly Dictionary<(string TypeName, bool ForSer), List<AmbientHandlerDefinition>> _handlerCache
            = new Dictionary<(string, bool), List<AmbientHandlerDefinition>>();

        public AmbientHandlerEmitter(AmbientHandlerRegistry registry, MarkerReachabilityChecker reachabilityChecker)
        {
            _registry = registry;
            _reachabilityChecker = reachabilityChecker;
        }

        public bool HasHandlersFor(TypeDefinition type, bool forSerialization)
        {
            if (_registry == null || _registry.IsEmpty || type == null) return false;
            return ResolveHandlers(type, forSerialization).Count > 0;
        }

        public bool HasHandlersForTypeName(string typeFullName, bool forSerialization)
        {
            if (_registry == null || _registry.IsEmpty || string.IsNullOrEmpty(typeFullName)) return false;
            return ResolveHandlersByTypeName(typeFullName, forSerialization).Count > 0;
        }

        public HandlerEmissionHandle EmitPrologue(StringBuilderWithIndent sb, TypeDefinition type, bool forSerialization, int startIndex = 0)
        {
            var handlers = ResolveHandlers(type, forSerialization);
            return EmitPrologueCore(sb, handlers, startIndex);
        }

        public HandlerEmissionHandle EmitPrologueByTypeName(StringBuilderWithIndent sb, string typeFullName, bool forSerialization, int startIndex = 0)
        {
            var handlers = ResolveHandlersByTypeName(typeFullName, forSerialization);
            return EmitPrologueCore(sb, handlers, startIndex);
        }

        public void EmitEpilogue(StringBuilderWithIndent sb, HandlerEmissionHandle handle)
        {
            if (handle.IsEmpty) return;

            for (int i = handle.Slots.Count - 1; i >= 0; i--)
            {
                var slot = handle.Slots[i];
                EmitSingleEpilogue(sb, slot.Handler, slot.OuterLocal);
            }
        }

        /// <summary>Emits the body as an arrow form when no handlers match, or a wrapped block otherwise.</summary>
        public void EmitEntryPointBody(StringBuilderWithIndent sb, TypeDefinition type, bool forSerialization, string singleStatement)
        {
            var handlers = ResolveHandlers(type, forSerialization);
            EmitEntryPointBodyCore(sb, handlers, singleStatement);
        }

        public void EmitEntryPointBodyByTypeName(StringBuilderWithIndent sb, string typeFullName, bool forSerialization, string singleStatement)
        {
            var handlers = ResolveHandlersByTypeName(typeFullName, forSerialization);
            EmitEntryPointBodyCore(sb, handlers, singleStatement);
        }

        private void EmitEntryPointBodyCore(StringBuilderWithIndent sb, List<AmbientHandlerDefinition> handlers, string singleStatement)
        {
            string body = singleStatement.EndsWith(";", System.StringComparison.Ordinal)
                ? singleStatement.Substring(0, singleStatement.Length - 1)
                : singleStatement;

            if (handlers.Count == 0)
            {
                sb.AppendIndentedLine($"    => {body};");
                return;
            }

            sb.StartNewBlock();
            var handle = EmitPrologueCore(sb, handlers, startIndex: 0);
            sb.AppendIndentedLine(body + ";");
            EmitEpilogue(sb, handle);
            sb.EndBlock();
        }

        private HandlerEmissionHandle EmitPrologueCore(StringBuilderWithIndent sb, List<AmbientHandlerDefinition> handlers, int startIndex)
        {
            if (handlers == null || handlers.Count == 0)
                return default;

            var slots = new HandlerSlot[handlers.Count];
            for (int i = 0; i < handlers.Count; i++)
            {
                var handler = handlers[i];
                var outerLocal = OuterLocalPrefix + (startIndex + i);
                slots[i] = new HandlerSlot(handler, outerLocal);
                EmitSinglePrologue(sb, handler, outerLocal);
            }

            return new HandlerEmissionHandle(slots);
        }

        private const string OuterLocalPrefix = "__gprotoOuter";

        // Before is inside the outer try (so Exit still runs on throw) but outside the inner try
        // (so a throw from Before skips After — state we never set up).
        private static void EmitSinglePrologue(StringBuilderWithIndent sb, AmbientHandlerDefinition handler, string outerLocal)
        {
            string handlerRef = handler.HandlerGlobalReference;
            bool hasAfter = !string.IsNullOrEmpty(handler.AfterMethodName);

            if (handler.AutoReentrancyGuard)
            {
                string guardName = AmbientHandlerGuardGenerator.GetGuardTypeReference(handler);
                sb.AppendIndentedLine($"bool {outerLocal} = {guardName}.Enter();");
                sb.AppendIndentedLine("try");
                sb.StartNewBlock();
                sb.AppendIndentedLine($"if ({outerLocal}) {handlerRef}.{handler.BeforeMethodName}();");

                if (hasAfter)
                {
                    sb.AppendIndentedLine("try");
                    sb.StartNewBlock();
                }
            }
            else
            {
                sb.AppendIndentedLine($"{handlerRef}.{handler.BeforeMethodName}();");
                if (hasAfter)
                {
                    sb.AppendIndentedLine("try");
                    sb.StartNewBlock();
                }
            }
        }

        private static void EmitSingleEpilogue(StringBuilderWithIndent sb, AmbientHandlerDefinition handler, string outerLocal)
        {
            string handlerRef = handler.HandlerGlobalReference;
            bool hasAfter = !string.IsNullOrEmpty(handler.AfterMethodName);

            if (handler.AutoReentrancyGuard)
            {
                if (hasAfter)
                {
                    sb.EndBlock();
                    sb.AppendIndentedLine("finally");
                    sb.StartNewBlock();
                    sb.AppendIndentedLine($"if ({outerLocal}) {handlerRef}.{handler.AfterMethodName}();");
                    sb.EndBlock();
                }

                string guardName = AmbientHandlerGuardGenerator.GetGuardTypeReference(handler);
                sb.EndBlock();
                sb.AppendIndentedLine("finally");
                sb.StartNewBlock();
                sb.AppendIndentedLine($"{guardName}.Exit();");
                sb.EndBlock();
            }
            else if (hasAfter)
            {
                sb.EndBlock();
                sb.AppendIndentedLine("finally");
                sb.StartNewBlock();
                sb.AppendIndentedLine($"{handlerRef}.{handler.AfterMethodName}();");
                sb.EndBlock();
            }
        }

        private List<AmbientHandlerDefinition> ResolveHandlers(TypeDefinition type, bool forSerialization)
            => ResolveHandlersByTypeName(type.FullName, forSerialization);

        private static readonly List<AmbientHandlerDefinition> EmptyHandlers = new List<AmbientHandlerDefinition>(0);

        private List<AmbientHandlerDefinition> ResolveHandlersByTypeName(string typeFullName, bool forSerialization)
        {
            if (_registry == null || _registry.IsEmpty || string.IsNullOrEmpty(typeFullName))
                return EmptyHandlers;

            var cacheKey = (typeFullName, forSerialization);
            if (_handlerCache.TryGetValue(cacheKey, out var cached))
                return cached;

            List<AmbientHandlerDefinition> result = null;
            foreach (var handler in _registry.GetAllSorted())
            {
                if (forSerialization && !handler.IncludeSerialization) continue;
                if (!forSerialization && !handler.IncludeDeserialization) continue;

                // Reachability cache is shared with ObjectTreeV2's diagnostic-time
                // RootsReachingMarker, so any pair already computed there hits a warm cache here.
                if (_reachabilityChecker.IsReachable(typeFullName, handler.MarkerFullName))
                {
                    (result ??= new List<AmbientHandlerDefinition>()).Add(handler);
                }
            }

            var finalResult = result ?? EmptyHandlers;
            _handlerCache[cacheKey] = finalResult;
            return finalResult;
        }
    }
}
