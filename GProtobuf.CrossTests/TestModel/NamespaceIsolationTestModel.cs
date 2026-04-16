using GProtobuf;
namespace NmSpace1
{
    [ProtoContract]
    [ProtoInclude(100, typeof(Class12))]
    public partial class Class1
    {
        [ProtoMember(1)]
        public int Id { get; set; }
    }

    [ProtoContract]
    public partial class Class12 : Class1
    {
        [ProtoMember(1)]
        public string Name { get; set; }
    }
}

namespace NmSpace2
{
    [ProtoContract]
    public partial class Class2
    {
        [ProtoMember(1)]
        public int Id2 { get; set; }
    }
}
