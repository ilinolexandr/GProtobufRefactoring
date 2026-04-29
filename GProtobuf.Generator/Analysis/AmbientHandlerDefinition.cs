using System.Collections.Generic;
using GProtobuf.Generator.Diagnostics;

namespace GProtobuf.Generator.Analysis
{
    /// <summary>One resolved <c>[assembly: AmbientSerializationHandler]</c> declaration.</summary>
    internal sealed class AmbientHandlerDefinition
    {
        /// <summary>
        /// Validation diagnostics attached while resolving the attribute. Each entry carries
        /// its <see cref="Microsoft.CodeAnalysis.DiagnosticDescriptor"/> reference, so callers
        /// classify fatal vs non-fatal by descriptor identity (e.g.
        /// <c>GProtobufDiagnostics.AmbientHandlerDuplicateDeclaration</c>) instead of string-
        /// matching the GPROTO id.
        /// </summary>
        public List<DiagnosticReport> Diagnostics { get; set; } = new List<DiagnosticReport>();

        /// <summary>Fully qualified marker type.</summary>
        public string MarkerFullName { get; set; }

        /// <summary>Fully qualified handler type.</summary>
        public string HandlerTypeFullName { get; set; }

        private string _handlerGlobalReference;
        /// <summary><c>"global::" + HandlerTypeFullName</c>, cached for emit-sites.</summary>
        public string HandlerGlobalReference
            => _handlerGlobalReference ??= "global::" + HandlerTypeFullName;

        /// <summary>Handler's before-method name.</summary>
        public string BeforeMethodName { get; set; } = "Before";

        /// <summary>Handler's after-method name, or null when opted out.</summary>
        public string AfterMethodName { get; set; }

        /// <summary>Emit a depth counter so hooks fire only at the outermost call.</summary>
        public bool AutoReentrancyGuard { get; set; }

        /// <summary>Wrap serialize entry-points.</summary>
        public bool IncludeSerialization { get; set; } = true;

        /// <summary>Wrap deserialize entry-points.</summary>
        public bool IncludeDeserialization { get; set; } = true;
    }
}
