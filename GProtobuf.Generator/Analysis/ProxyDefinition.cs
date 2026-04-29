using System.Collections.Generic;
using GProtobuf.Generator.Diagnostics;

namespace GProtobuf.Generator.Analysis
{
    /// <summary>
    /// Represents a serialization proxy mapping from an original type to a proxy type.
    /// Created during source generation by analyzing [assembly: SerializationProxy] attributes.
    /// </summary>
    internal sealed class ProxyDefinition
    {
        /// <summary>
        /// Validation diagnostics collected during proxy analysis. Empty list means the proxy
        /// passed every check. Each entry holds a descriptor reference (severity baked in)
        /// plus the message-format args; the actual <see cref="Microsoft.CodeAnalysis.Diagnostic"/>
        /// is materialised once at report time.
        /// </summary>
        public List<DiagnosticReport> Diagnostics { get; set; } = new List<DiagnosticReport>();

        /// <summary>
        /// Returns true if proxy has no validation errors. A "valid" proxy here is defined as
        /// one whose diagnostics list is empty — analysis layers that allow non-fatal warnings
        /// must filter at the call site (see <see cref="ProxyDefinition.Diagnostics"/>).
        /// </summary>
        public bool IsValid => Diagnostics.Count == 0;

        /// <summary>
        /// Fully qualified name of the original type being proxied.
        /// </summary>
        public string OriginalTypeFullName { get; set; }

        /// <summary>
        /// Fully qualified name of the proxy type (must have [ProtoContract]).
        /// </summary>
        public string ProxyTypeFullName { get; set; }

        /// <summary>
        /// Short class name of the proxy type, used for generated method names.
        /// </summary>
        public string ProxyClassName { get; set; }

        /// <summary>
        /// Namespace where the proxy type is declared.
        /// </summary>
        public string ProxyNamespace { get; set; }

        /// <summary>
        /// Name of the static factory method marked with [ProxyWrap].
        /// Wraps an original type instance into a proxy instance for serialization.
        /// </summary>
        public string WrapMethodName { get; set; }

        /// <summary>
        /// Name of the optional static factory method marked with [ProxyAcquire].
        /// When non-null, the generator emits a call to this method instead of `new ProxyType()`
        /// when constructing a proxy instance during deserialization (enables pool reuse).
        /// </summary>
        public string AcquireMethodName { get; set; }

        /// <summary>
        /// Name of the instance method marked with [ProxyConvert].
        /// Converts proxy type back to original type.
        /// </summary>
        public string ConvertMethodName { get; set; }

        /// <summary>
        /// Name of the optional instance method marked with [ProxyReturn].
        /// Called after serialization/deserialization for cleanup/pooling. May be null.
        /// </summary>
        public string ReturnMethodName { get; set; }

        /// <summary>
        /// Number of parameters in the [ProxyWrap] method.
        /// 1 = standard (source only), 2 = pooling (source + reuse instance).
        /// </summary>
        public int WrapParameterCount { get; set; } = 1;

        /// <summary>
        /// Returns the extra arguments string for the [ProxyWrap] call.
        /// For 1-parameter: "" (empty), for 2-parameter: ", default" (passes default reuse instance).
        /// </summary>
        public string WrapExtraArgs => WrapParameterCount >= 2 ? ", default" : "";

        /// <summary>
        /// Whether the proxy type is a struct (affects null-checking and allocation).
        /// </summary>
        public bool IsStruct { get; set; }
    }
}
