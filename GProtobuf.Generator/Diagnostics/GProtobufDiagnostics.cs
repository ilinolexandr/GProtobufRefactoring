using Microsoft.CodeAnalysis;

namespace GProtobuf.Generator.Diagnostics
{
    /// <summary>Centralized diagnostic descriptors for the GProtobuf source generator.</summary>
    internal static class GProtobufDiagnostics
    {
        private const string Category = "GProtobuf";

        /// <summary>GPROTO003 — readonly [ProtoMember] field without matching constructor.</summary>
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
    }
}
