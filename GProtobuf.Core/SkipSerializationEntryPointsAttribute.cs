using System;

namespace ProtoBuf
{
    /// <summary>
    /// When applied to a type, suppresses generation of public entry-point methods
    /// (Deserialize, Serialize, Populate, Read, Write, Calculate) for this type.
    /// Internal helper methods (OwnFields, Content, AsParent, WrapperSize) are still generated.
    /// Use on derived types that are only accessed via base-type polymorphic dispatch.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class SkipSerializationEntryPointsAttribute : Attribute { }
}
