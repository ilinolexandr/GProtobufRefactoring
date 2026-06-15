using GProtobuf;
using System.Collections.Immutable;

namespace GProtobuf.CrossTests.TestModel
{
    // System.Collections.Immutable ImmutableQueue<T>/ImmutableStack<T> + their interface forms.
    //
    // Oracle: protobuf-net 3.x (the version GProtobuf.CrossTests references — 3.2.46) SUPPORTS these
    // (explicit CreateImmutableQueue/Stack factories in RepeatedSerializers.cs). pn 2.3.7 does NOT
    // (throws "No serializer defined" — Queue/Stack lack the Builder pattern its decorator needs),
    // so these models are 3.x-oracle-only.
    //
    // ImmutableQueue is FIFO: CreateRange([1,2,3]) enumerates 1,2,3 → wire == List<int>.
    // ImmutableStack is LIFO: CreateRange([1,2,3]) enumerates top-first 3,2,1 → wire is [3,2,1].
    // pn round-trips Stack ORDER-STABLY (3,2,1 → 3,2,1); GProtobuf must match (the freeze reverses
    // the temp list before ImmutableStack.CreateRange — see CollectionKindHelper.GetFreezeExpression).

    [ProtoContract]
    public partial class ImmutableQueueIntModel
    {
        [ProtoMember(1)]
        public ImmutableQueue<int> Values { get; set; }

        [ProtoMember(2, IsPacked = true)]
        public ImmutableQueue<int> PackedValues { get; set; }
    }

    [ProtoContract]
    public partial class ImmutableStackIntModel
    {
        [ProtoMember(1)]
        public ImmutableStack<int> Values { get; set; }

        [ProtoMember(2, IsPacked = true)]
        public ImmutableStack<int> PackedValues { get; set; }
    }

    [ProtoContract]
    public partial class ImmutableQueueInterfaceModel
    {
        [ProtoMember(1)]
        public IImmutableQueue<int> Values { get; set; }
    }

    [ProtoContract]
    public partial class ImmutableStackInterfaceModel
    {
        [ProtoMember(1)]
        public IImmutableStack<int> Values { get; set; }
    }

    [ProtoContract]
    public partial class ImmutableQueueStringModel
    {
        [ProtoMember(1)]
        public ImmutableQueue<string> Names { get; set; }
    }

    [ProtoContract]
    public partial class ImmutableQueueNestedModel
    {
        [ProtoMember(1)]
        public ImmutableQueue<QueueElementMsg> Items { get; set; }

        [ProtoMember(2)]
        public int Tag { get; set; }
    }

    [ProtoContract]
    public partial class QueueElementMsg
    {
        [ProtoMember(1)]
        public int K { get; set; }

        [ProtoMember(2)]
        public string S { get; set; }
    }
}
