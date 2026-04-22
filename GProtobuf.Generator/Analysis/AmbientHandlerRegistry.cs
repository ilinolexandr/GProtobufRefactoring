using System.Collections.Generic;
using System.Linq;

namespace GProtobuf.Generator.Analysis
{
    /// <summary>All valid ambient-handler declarations, sorted deterministically.</summary>
    internal sealed class AmbientHandlerRegistry
    {
        private readonly List<AmbientHandlerDefinition> _all = new List<AmbientHandlerDefinition>();
        private AmbientHandlerDefinition[] _sorted;

        public void Register(AmbientHandlerDefinition def)
        {
            _all.Add(def);
            _sorted = null;
        }

        public bool IsEmpty => _all.Count == 0;

        /// <summary>Handlers ordered by (Marker, HandlerType, BeforeMethod), cached.</summary>
        public IReadOnlyList<AmbientHandlerDefinition> GetAllSorted()
        {
            var cached = _sorted;
            if (cached != null) return cached;

            cached = _all
                .OrderBy(x => x.MarkerFullName, System.StringComparer.Ordinal)
                .ThenBy(x => x.HandlerTypeFullName, System.StringComparer.Ordinal)
                .ThenBy(x => x.BeforeMethodName, System.StringComparer.Ordinal)
                .ToArray();
            _sorted = cached;
            return cached;
        }
    }
}
