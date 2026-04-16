using GProtobuf;
namespace GProtobuf.Tests.TestModel
{
    [ProtoContract]
    public partial class SimpleTypesZigZag
    {
        [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
        public long LongValue { get; set; }
    }
}