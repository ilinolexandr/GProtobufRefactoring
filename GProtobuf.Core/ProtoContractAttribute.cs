using System;

namespace GProtobuf
{
    /// <summary>Marks a type as protobuf-serializable; discovered by the GProtobuf source generator.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ProtoContractAttribute : Attribute
    {
        /// <summary>Optional schema name for the type (currently informational).</summary>
        public string Name { get; set; }

        /// <summary>When true, the generator emits a recursion-depth guard to protect against stack overflow.</summary>
        public bool EnableRecursionGuard { get; set; }

        /// <summary>
        /// When true, suppresses generation of public entry-point methods (Deserialize/Serialize/…).
        /// The type is still usable via polymorphic dispatch from base-type serializers.
        /// </summary>
        public bool SkipEntryPoints { get; set; }

        /// <summary>
        /// When true, unknown enum values round-trip through serialization instead of being rejected.
        /// Carried for API parity with protobuf-net; the GProtobuf generator currently treats it as informational,
        /// but the oracle-compat layer propagates it so wire-level tests match protobuf-net behavior.
        /// </summary>
        public bool EnumPassthru { get; set; }
    }
}
