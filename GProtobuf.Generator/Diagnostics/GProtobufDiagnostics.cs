using Microsoft.CodeAnalysis;

namespace GProtobuf.Generator.Diagnostics
{
    /// <summary>
    /// Single source of truth for every <see cref="DiagnosticDescriptor"/> the GProtobuf
    /// source generator emits.
    ///
    /// <para><b>Why centralize?</b></para>
    /// Roslyn caches diagnostics by descriptor reference; using one shared instance per ID
    /// keeps the analyzer infrastructure happy and eliminates ~28 inline
    /// <c>new DiagnosticDescriptor(...)</c> sites scattered across the generator. Severity
    /// is baked into the descriptor — call sites no longer compute it via string-matching
    /// the ID.
    ///
    /// <para><b>How to add a new diagnostic:</b></para>
    /// 1. Pick the next free GPROTO id and an opinionated semantic name.
    /// 2. Declare a <c>static readonly DiagnosticDescriptor</c> in the matching region below.
    /// 3. Use placeholders (<c>{0}</c>, <c>{1}</c>) in <c>messageFormat</c>; the call site
    ///    passes the values via <see cref="Diagnostic.Create(DiagnosticDescriptor, Location, object[])"/>.
    /// 4. Report it via <see cref="DiagnosticReporter.Report"/> for terse call sites, or
    ///    attach a <see cref="DiagnosticReport"/> to a <c>Diagnostics</c> collection on
    ///    a definition object (proxy/ambient handler) for deferred reporting.
    /// </summary>
    internal static class GProtobufDiagnostics
    {
        private const string Category = "GProtobuf";

        // Shared titles. Roslyn surfaces these in IDE Error List / build output.
        private const string TitleLifecycle = "GProtobuf Generator";
        private const string TitleTypeAnalysis = "GProtobuf Type Analysis";
        private const string TitleProxy = "GProtobuf Serialization Proxy";
        private const string TitleAmbient = "GProtobuf Ambient Serialization Handler";

        // ─── Generator lifecycle (GPROTO001, GPROTO002) ────────────────────────────────────

        /// <summary>GPROTO001 — generator started; reports counts of inputs and feature flags.</summary>
        public static readonly DiagnosticDescriptor GeneratorStarted = new(
            id: "GPROTO001",
            title: TitleLifecycle,
            messageFormat: "GProtobuf generator started with {0} enum types, {1} type definitions, {2} standalone types. Options: SpanReader={3}, StreamReader={4}, StreamWriter={5}, BufferWriter={6}, OnePassStreamWriter={7}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        /// <summary>GPROTO002 — generator finished; reports the count of generated files.</summary>
        public static readonly DiagnosticDescriptor GeneratorCompleted = new(
            id: "GPROTO002",
            title: TitleLifecycle,
            messageFormat: "GProtobuf generator completed successfully, generated {0} files",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        // ─── Type analysis (GPROTO003, GPROTO004, GPROTO005) ───────────────────────────────

        /// <summary>GPROTO003 — readonly [ProtoMember] field without a matching constructor.</summary>
        public static readonly DiagnosticDescriptor ReadonlyFieldsWithoutMatchingConstructor = new(
            id: "GPROTO003",
            title: "Readonly fields require a matching constructor",
            messageFormat: "Type '{0}' has readonly [ProtoMember] field(s) but no public constructor whose parameters match all ProtoMembers by name and type. Add such a constructor or remove the readonly modifier.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Readonly fields can only be assigned inside a constructor, so GProtobuf needs a constructor whose parameters correspond one-to-one to the [ProtoMember] fields.");

        /// <summary>GPROTO004 — init-only [ProtoMember] without parameterless or matching constructor.</summary>
        public static readonly DiagnosticDescriptor InitOnlyWithoutConstructor = new(
            id: "GPROTO004",
            title: "Init-only properties require a parameterless or matching constructor",
            messageFormat: "Type '{0}' has init-only [ProtoMember] property/properties but neither a public parameterless constructor nor a constructor whose parameters match all ProtoMembers was found. Add a parameterless constructor, add a matching constructor, or change the property to a regular setter.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "GProtobuf deserializes init-only properties either via the deferred object-initializer path (parameterless ctor) or via constructor injection (matching ctor).");

        /// <summary>GPROTO005 — get-only [ProtoMember] property silently ignored during deserialization.</summary>
        public static readonly DiagnosticDescriptor GetOnlyProtoMemberIgnored = new(
            id: "GPROTO005",
            title: "Get-only [ProtoMember] property is ignored during deserialization",
            messageFormat: "Property '{0}' on type '{1}' is marked with [ProtoMember] but has no setter — the field will be skipped during deserialization. Add a `set` or `init` accessor, or remove [ProtoMember].",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Get-only properties cannot be assigned post-construction; deserialized values would be lost while the serializer still emits the field, causing silent round-trip data loss.");

        // ─── Serialization proxy validation (GPROTO010-014, GPROTO018-020) ─────────────────

        /// <summary>GPROTO010 — proxy type missing [ProtoContract] attribute.</summary>
        public static readonly DiagnosticDescriptor ProxyMissingProtoContract = new(
            id: "GPROTO010",
            title: TitleProxy,
            messageFormat: "Proxy type '{0}' must have [ProtoContract] attribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>GPROTO011 — proxy type missing exactly one [ProxyWrap] method.</summary>
        public static readonly DiagnosticDescriptor ProxyMissingWrap = new(
            id: "GPROTO011",
            title: TitleProxy,
            messageFormat: "Proxy type '{0}' must have exactly one method with [ProxyWrap] attribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>GPROTO012 — [ProxyWrap] method has invalid shape.</summary>
        public static readonly DiagnosticDescriptor ProxyWrapInvalidShape = new(
            id: "GPROTO012",
            title: TitleProxy,
            messageFormat: "[ProxyWrap] method '{0}' in '{1}' must be static, take one parameter of type '{2}', and return '{1}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>GPROTO013 — proxy type missing exactly one [ProxyConvert] method.</summary>
        public static readonly DiagnosticDescriptor ProxyMissingConvert = new(
            id: "GPROTO013",
            title: TitleProxy,
            messageFormat: "Proxy type '{0}' must have exactly one method with [ProxyConvert] attribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>GPROTO014 — [ProxyConvert] method has invalid shape.</summary>
        public static readonly DiagnosticDescriptor ProxyConvertInvalidShape = new(
            id: "GPROTO014",
            title: TitleProxy,
            messageFormat: "[ProxyConvert] method '{0}' in '{1}' must be an instance method, take no parameters, and return '{2}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>GPROTO018 — [ProxyAcquire] cannot coexist with a matching constructor (Error: caller must remove one).</summary>
        public static readonly DiagnosticDescriptor ProxyAcquireConflictsWithMatchingConstructor = new(
            id: "GPROTO018",
            title: TitleProxy,
            messageFormat: "Proxy type '{0}' has [ProxyAcquire] but also a matching constructor that would be used for deserialization. Remove [ProxyAcquire] or remove the matching constructor.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "When the generator's ConstructorMatcher selects a matching constructor, the [ProxyAcquire] factory is bypassed — the user's pooling intent is silently lost. We surface this conflict as an error so the user explicitly chooses one path.");

        /// <summary>GPROTO019 — [ProxyAcquire] method has invalid shape.</summary>
        public static readonly DiagnosticDescriptor ProxyAcquireInvalidShape = new(
            id: "GPROTO019",
            title: TitleProxy,
            messageFormat: "[ProxyAcquire] method '{0}' in '{1}' must be {2}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>GPROTO020 — proxy type has more than one [ProxyAcquire] method.</summary>
        public static readonly DiagnosticDescriptor ProxyAcquireMultiple = new(
            id: "GPROTO020",
            title: TitleProxy,
            messageFormat: "Proxy type '{0}' must have at most one method with [ProxyAcquire] attribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // ─── Ambient serialization handler validation (GPROTO021-027) ──────────────────────

        /// <summary>GPROTO021 — handler is missing the named Before method (Error: handler is dropped).</summary>
        public static readonly DiagnosticDescriptor AmbientHandlerBeforeMissing = new(
            id: "GPROTO021",
            title: TitleAmbient,
            messageFormat: "AmbientSerializationHandler '{0}' has no method named '{1}'.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>GPROTO022 — handler's Before method has invalid shape (Error: handler is dropped).</summary>
        public static readonly DiagnosticDescriptor AmbientHandlerBeforeInvalidShape = new(
            id: "GPROTO022",
            title: TitleAmbient,
            messageFormat: "'{0}.{1}' must be 'public static void {1}()' with no parameters.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>GPROTO023 — handler is missing an explicitly-named After method (Error: handler is dropped).</summary>
        public static readonly DiagnosticDescriptor AmbientHandlerAfterMissing = new(
            id: "GPROTO023",
            title: TitleAmbient,
            messageFormat: "AmbientSerializationHandler '{0}' has no method named '{1}'. Set AfterMethod = null to opt out.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>GPROTO024 — handler's After method has invalid shape (Error: handler is dropped).</summary>
        public static readonly DiagnosticDescriptor AmbientHandlerAfterInvalidShape = new(
            id: "GPROTO024",
            title: TitleAmbient,
            messageFormat: "'{0}.{1}' must be 'public static void {1}()' with no parameters.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>GPROTO025 — duplicate [assembly: AmbientSerializationHandler] declaration (non-fatal: kept in registry).</summary>
        public static readonly DiagnosticDescriptor AmbientHandlerDuplicateDeclaration = new(
            id: "GPROTO025",
            title: TitleAmbient,
            messageFormat: "Duplicate AmbientSerializationHandler declaration for marker '{0}', handler '{1}', before-method '{2}'. Remove the redundant [assembly: AmbientSerializationHandler(...)] attribute.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>GPROTO026 — registered marker is not reachable from any ProtoContract graph; handler will never run.</summary>
        public static readonly DiagnosticDescriptor AmbientHandlerMarkerUnreachable = new(
            id: "GPROTO026",
            title: TitleAmbient,
            messageFormat: "AmbientSerializationHandler marker '{0}' is not reachable from any ProtoContract graph known to the generator — this handler will never run. If the marker is reached only via a custom generic container, add the container's open-name to GProtobuf.Generator.Analysis.TransparentContainerClassifier (recognized: Dictionary, List, ListDictionary, Tuple, Nullable, T[], and standard collection / immutable / concurrent variants).",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>GPROTO027 — handler shape suggests a scope type rather than a static class (non-fatal: kept in registry).</summary>
        public static readonly DiagnosticDescriptor AmbientHandlerShapeWarning = new(
            id: "GPROTO027",
            title: TitleAmbient,
            messageFormat: "AmbientSerializationHandler '{0}' is {1}. Use a static class with public static Before/After methods instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>GPROTO029 — coverage info: how many ProtoContract roots the handler wraps (Info-level spot-check).</summary>
        public static readonly DiagnosticDescriptor AmbientHandlerCoverageInfo = new(
            id: "GPROTO029",
            title: "GProtobuf Ambient Serialization Handler Coverage",
            messageFormat: "AmbientSerializationHandler '{0}' (marker '{1}') wraps {2} ProtoContract root(s): {3}. If this is unexpectedly low, your marker may be reached only via a custom generic container not in TransparentContainerClassifier's catalog.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        // ─── Catastrophic failure (GPROTO999) ──────────────────────────────────────────────

        /// <summary>GPROTO999 — unhandled exception inside the generator pipeline; surfaced for diagnosis.</summary>
        public static readonly DiagnosticDescriptor GeneratorFailure = new(
            id: "GPROTO999",
            title: "GProtobuf Generator Error",
            messageFormat: "GProtobuf generator failed: {0}. Stack: {1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }

    /// <summary>
    /// A deferred diagnostic: descriptor + the format args needed to instantiate it. Used for
    /// validation diagnostics collected during analysis (proxy / ambient handler) where the
    /// final <see cref="Diagnostic"/> is built once analysis completes and the
    /// <see cref="SourceProductionContext"/> is available.
    /// </summary>
    internal readonly struct DiagnosticReport
    {
        public DiagnosticDescriptor Descriptor { get; }
        public object[] MessageArgs { get; }

        public DiagnosticReport(DiagnosticDescriptor descriptor, params object[] args)
        {
            Descriptor = descriptor;
            MessageArgs = args ?? System.Array.Empty<object>();
        }

        public Diagnostic ToDiagnostic(Location location = null)
            => Diagnostic.Create(Descriptor, location ?? Location.None, MessageArgs);
    }

    /// <summary>Terse helpers for emitting diagnostics from the source generator pipeline.</summary>
    internal static class DiagnosticReporter
    {
        /// <summary>Emits a diagnostic at <see cref="Location.None"/> built from the given descriptor and args.</summary>
        public static void Report(this SourceProductionContext context, DiagnosticDescriptor descriptor, params object[] args)
            => context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, args));

        /// <summary>Emits a previously-collected <see cref="DiagnosticReport"/> at <see cref="Location.None"/>.</summary>
        public static void Report(this SourceProductionContext context, DiagnosticReport report)
            => context.ReportDiagnostic(report.ToDiagnostic());
    }
}
