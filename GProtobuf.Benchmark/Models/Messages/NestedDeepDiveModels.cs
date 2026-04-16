// DUAL-ATTRIBUTE MODELS — see PrimitiveTypesModel.cs header for rationale.

using System.Collections.Generic;

namespace GProtobuf.Benchmark.Models.Messages
{
    // Flat leaf node used by WideFanoutRoot. Name/Data are optional so a single
    // type covers scalar-only / +string / +bytes payload shapes by null-skipping.
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class WideNode
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public string Name { get; set; }
        [ProtoMember(3)] [ProtoBuf.ProtoMember(3)] public byte[] Data { get; set; }
    }

    // Holds a list of sibling sub-messages. Each element triggers a
    // BeginSubMessage/EndSubMessage pair in OnePass → isolates per-frame
    // cost (pool rent + Flush + CopyTo + pool return) as a function of breadth.
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class WideFanoutRoot
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public List<WideNode> Nodes { get; set; }
    }

    // Single bytes payload wrapped in a message — used to measure the cost of
    // exactly one BeginSubMessage/EndSubMessage pair against varying content size.
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class PayloadMessage
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public byte[] Payload { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class SingleFrameRoot
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public PayloadMessage Frame { get; set; }
    }
}
