using GProtobuf;
using System;

namespace GProtobuf.Tests.TestModel
{
    [ProtoContract] // BCL Guid format now supported in V2
    public partial class GuidTypesModel
    {
        [ProtoMember(1)]
        public Guid GuidValue { get; set; }

        [ProtoMember(2)]
        public Guid EmptyGuidValue { get; set; }

        [ProtoMember(3)]
        public Guid AnotherGuidValue { get; set; }
    }
}