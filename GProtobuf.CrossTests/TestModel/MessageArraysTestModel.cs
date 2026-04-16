using GProtobuf;
namespace GProtobuf.Tests.TestModel
{
    [ProtoContract]
    public partial class SimpleMessage
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public int Value { get; set; }
    }

    [ProtoContract]
    public partial class NestedMessage
    {
        [ProtoMember(1)]
        public string Title { get; set; }

        [ProtoMember(2)]
        public SimpleMessage Inner { get; set; }

        [ProtoMember(3)]
        public double Score { get; set; }
    }

    [ProtoContract]
    public partial class MessageArraysTestModel
    {
        [ProtoMember(1)]
        public SimpleMessage[] SimpleMessages { get; set; }

        [ProtoMember(2)]
        public NestedMessage[] NestedMessages { get; set; }

        [ProtoMember(3)]
        public SimpleMessage[] EmptyMessageArray { get; set; }

        [ProtoMember(4)]
        public SimpleMessage[] NullMessageArray { get; set; }

        [ProtoMember(5)]
        public SimpleMessage[] MessageArrayWithNulls { get; set; }

        [ProtoMember(6)]
        public NestedMessage[] ComplexMessageArray { get; set; }
    }
}