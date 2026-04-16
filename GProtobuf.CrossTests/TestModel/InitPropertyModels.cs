using GProtobuf;
using System.Collections.Generic;

namespace GProtobuf.CrossTests.TestModel
{
    /// <summary>
    /// Basic class with all init-only properties of various primitive types.
    /// Tests the deferred-construction code path for the most common case.
    /// </summary>
    [ProtoContract]
    public partial class InitOnlyPrimitivesModel
    {
        [ProtoMember(1)]
        public int IntValue { get; init; }

        [ProtoMember(2)]
        public long LongValue { get; init; }

        [ProtoMember(3)]
        public double DoubleValue { get; init; }

        [ProtoMember(4)]
        public bool BoolValue { get; init; }

        [ProtoMember(5)]
        public string StringValue { get; init; }

        [ProtoMember(6)]
        public byte[] BytesValue { get; init; }
    }

    /// <summary>
    /// Class mixing init-only and regular set properties.
    /// Tests that the deferred path correctly handles both kinds when init is present.
    /// </summary>
    [ProtoContract]
    public partial class MixedInitAndSetModel
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; init; }

        [ProtoMember(3)]
        public int Counter { get; set; }

        [ProtoMember(4)]
        public bool Flag { get; init; }
    }

    /// <summary>
    /// Class with an init-only nested object reference.
    /// Tests that complex types in init position go through the standard ReadContent path.
    /// </summary>
    [ProtoContract]
    public partial class InitWithNestedModel
    {
        [ProtoMember(1)]
        public InitOnlyPrimitivesModel Inner { get; init; }

        [ProtoMember(2)]
        public int Tag { get; init; }
    }

    /// <summary>
    /// Class with init-only collection. The collection reference itself is set during
    /// initialization, even though the collection contents are mutable.
    /// </summary>
    [ProtoContract]
    public partial class InitWithCollectionModel
    {
        [ProtoMember(1)]
        public List<int> Items { get; init; }

        [ProtoMember(2)]
        public string Label { get; init; }
    }

    /// <summary>
    /// Base type with init-only properties. Derived type uses regular setters.
    /// Tests deferred-init code path for the base portion of an inheritance chain.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(100, typeof(BaseInitDerivedNormalChild))]
    public partial class BaseInitDerivedNormalRoot
    {
        [ProtoMember(1)]
        public int BaseId { get; init; }

        [ProtoMember(2)]
        public string BaseName { get; init; }
    }

    [ProtoContract]
    public partial class BaseInitDerivedNormalChild : BaseInitDerivedNormalRoot
    {
        [ProtoMember(1)]
        public string ChildLabel { get; set; }

        [ProtoMember(2)]
        public int ChildCount { get; set; }
    }

    /// <summary>
    /// Base type with regular setters. Derived type uses init-only properties.
    /// Tests deferred-init code path for the derived portion of an inheritance chain.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(100, typeof(BaseNormalDerivedInitChild))]
    public partial class BaseNormalDerivedInitRoot
    {
        [ProtoMember(1)]
        public int BaseId { get; set; }

        [ProtoMember(2)]
        public string BaseName { get; set; }
    }

    [ProtoContract]
    public partial class BaseNormalDerivedInitChild : BaseNormalDerivedInitRoot
    {
        [ProtoMember(1)]
        public string ChildLabel { get; init; }

        [ProtoMember(2)]
        public int ChildCount { get; init; }
    }

    /// <summary>
    /// Both base and derived use init-only properties.
    /// Tests deferred-init code path on the entire inheritance chain.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(100, typeof(BaseInitDerivedInitChild))]
    public partial class BaseInitDerivedInitRoot
    {
        [ProtoMember(1)]
        public int BaseId { get; init; }

        [ProtoMember(2)]
        public string BaseName { get; init; }
    }

    [ProtoContract]
    public partial class BaseInitDerivedInitChild : BaseInitDerivedInitRoot
    {
        [ProtoMember(1)]
        public string ChildLabel { get; init; }

        [ProtoMember(2)]
        public int ChildCount { get; init; }
    }
}
