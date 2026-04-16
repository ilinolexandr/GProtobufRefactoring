using System;
using GProtobuf;

namespace GProtobuf.CrossTests.TestModel
{
    [ProtoContract]
    [ProtoInclude(100, typeof(ModelInh1))]
    [ProtoInclude(101, typeof(ModelInh2))]
    [ProtoInclude(102, typeof(ModelInh3))]
    public partial class ModelBase
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(100, typeof(Model15))]
    public partial class ModelInh1 : ModelBase
    {
        [ProtoMember(1)]
        public string Description { get; set; }

        [ProtoMember(2)]
        public Guid Guid { get; set; }
    }

    [ProtoContract]
    public partial class Model15 : ModelInh1
    {
        [ProtoMember(1)]
        public string Description15 { get; set; }

        [ProtoMember(2)]
        public Guid Guid15 { get; set; }
    }


    [ProtoContract]
    public partial class ModelInh2 : ModelBase
    {
        [ProtoMember(1)]
        public string Description1 { get; set; }

        [ProtoMember(2)]
        public Guid Guid1 { get; set; }
    }


    [ProtoContract]
    public partial class ModelInh3 : ModelBase
    {
        [ProtoMember(1)]
        public string Description2 { get; set; }

        [ProtoMember(2)]
        public Guid Guid2 { get; set; }
    }
}
