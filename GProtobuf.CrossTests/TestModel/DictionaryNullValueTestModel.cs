using GProtobuf;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace GProtobuf.Tests.TestModel
{
    // ═══════════════════════════════════════════════════════════════
    // Basic Dictionary with null values
    // ═══════════════════════════════════════════════════════════════

    [ProtoContract]
    public partial class DictWithStringNullValues
    {
        [ProtoMember(1)]
        public Dictionary<int, string> Items { get; set; }
    }

    [ProtoContract]
    public partial class DictWithObjectNullValues
    {
        [ProtoMember(1)]
        public Dictionary<string, NullValueNestedObject> Items { get; set; }
    }

    [ProtoContract]
    public partial class NullValueNestedObject
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public int Value { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // ConcurrentDictionary with null values
    // ═══════════════════════════════════════════════════════════════

    [ProtoContract]
    public partial class ConcurrentDictWithStringNullValues
    {
        [ProtoMember(1)]
        public ConcurrentDictionary<int, string> Items { get; set; }
    }

    [ProtoContract]
    public partial class ConcurrentDictWithObjectNullValues
    {
        [ProtoMember(1)]
        public ConcurrentDictionary<string, NullValueNestedObject> Items { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Dict<string, string> with null values
    // ═══════════════════════════════════════════════════════════════

    [ProtoContract]
    public partial class DictStringKeyStringNullValue
    {
        [ProtoMember(1)]
        public Dictionary<string, string> Items { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Nullable value types
    // ═══════════════════════════════════════════════════════════════

    [ProtoContract]
    public partial class DictWithNullableIntNullValues
    {
        [ProtoMember(1)]
        public Dictionary<int, int?> Items { get; set; }
    }

    [ProtoContract]
    public partial class DictWithNullableLongNullValues
    {
        [ProtoMember(1)]
        public Dictionary<int, long?> Items { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Dict<int, byte[]> with null values
    // ═══════════════════════════════════════════════════════════════

    [ProtoContract]
    public partial class DictWithByteArrayNullValues
    {
        [ProtoMember(1)]
        public Dictionary<int, byte[]> Items { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 2-Level Nested: Dict<K, Dict<K2, V>> with null values
    // ═══════════════════════════════════════════════════════════════

    [ProtoContract]
    public partial class DictLevel2NullValues_IntStringString
    {
        [ProtoMember(1)]
        public Dictionary<int, Dictionary<string, string>> Items { get; set; }
    }

    [ProtoContract]
    public partial class DictLevel2NullValues_StringIntObject
    {
        [ProtoMember(1)]
        public Dictionary<string, Dictionary<int, NullValueNestedObject>> Items { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Deep Object Nesting with Dictionaries
    // ═══════════════════════════════════════════════════════════════

    [ProtoContract]
    public partial class NullTestLevel1Container
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public Dictionary<int, NullTestLevel2Container> Children { get; set; }
    }

    [ProtoContract]
    public partial class NullTestLevel2Container
    {
        [ProtoMember(1)]
        public string Description { get; set; }

        [ProtoMember(2)]
        public Dictionary<string, NullTestLevel3Container> Items { get; set; }
    }

    [ProtoContract]
    public partial class NullTestLevel3Container
    {
        [ProtoMember(1)]
        public int Value { get; set; }

        [ProtoMember(2)]
        public Dictionary<int, string> Data { get; set; }
    }

    [ProtoContract]
    public partial class DeepNestedNullTestModel
    {
        [ProtoMember(1)]
        public Dictionary<string, NullTestLevel1Container> Root { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Dict<K, List<V>> with null lists
    // ═══════════════════════════════════════════════════════════════

    [ProtoContract]
    public partial class DictWithListNullValues
    {
        [ProtoMember(1)]
        public Dictionary<int, List<string>> Items { get; set; }
    }

    [ProtoContract]
    public partial class DictWithListOfObjectsNullValues
    {
        [ProtoMember(1)]
        public Dictionary<string, List<NullValueNestedObject>> Items { get; set; }
    }
}
