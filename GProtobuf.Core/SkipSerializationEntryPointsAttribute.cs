using System;

namespace GProtobuf
{
    /// <summary>Alternative to [ProtoContract(SkipEntryPoints = true)]; suppresses public entry-point methods.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class SkipSerializationEntryPointsAttribute : Attribute { }
}
