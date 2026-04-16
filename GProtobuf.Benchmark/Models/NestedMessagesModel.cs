// DUAL-ATTRIBUTE MODELS — see PrimitiveTypesModel.cs header for rationale.

using System.Collections.Generic;

namespace GProtobuf.Benchmark.Models
{
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class NestedMessagesModel
    {
        [ProtoMember(100)] [ProtoBuf.ProtoMember(100)] public string StringField1 { get; set; }
        [ProtoMember(101)] [ProtoBuf.ProtoMember(101)] public string StringField2 { get; set; }
        [ProtoMember(102)] [ProtoBuf.ProtoMember(102)] public string StringField3 { get; set; }
        [ProtoMember(103)] [ProtoBuf.ProtoMember(103)] public string StringField4 { get; set; }
        [ProtoMember(104)] [ProtoBuf.ProtoMember(104)] public string StringField5 { get; set; }

        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public PersonModel Person { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public List<PersonModel> People { get; set; }
        [ProtoMember(3)] [ProtoBuf.ProtoMember(3)] public AddressModel Address { get; set; }
        [ProtoMember(4)] [ProtoBuf.ProtoMember(4)] public List<AddressModel> Addresses { get; set; }
        [ProtoMember(5)] [ProtoBuf.ProtoMember(5)] public CompanyModel Company { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class PersonModel
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public string FirstName { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public string LastName { get; set; }
        [ProtoMember(3)] [ProtoBuf.ProtoMember(3)] public int Age { get; set; }
        [ProtoMember(4)] [ProtoBuf.ProtoMember(4)] public string Email { get; set; }
        [ProtoMember(5)] [ProtoBuf.ProtoMember(5)] public AddressModel Address { get; set; }
        [ProtoMember(6)] [ProtoBuf.ProtoMember(6)] public List<string> PhoneNumbers { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class AddressModel
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public string Street { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public string City { get; set; }
        [ProtoMember(3)] [ProtoBuf.ProtoMember(3)] public string State { get; set; }
        [ProtoMember(4)] [ProtoBuf.ProtoMember(4)] public string ZipCode { get; set; }
        [ProtoMember(5)] [ProtoBuf.ProtoMember(5)] public string Country { get; set; }
    }

    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class CompanyModel
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public string Name { get; set; }
        [ProtoMember(2)] [ProtoBuf.ProtoMember(2)] public AddressModel HeadquartersAddress { get; set; }
        [ProtoMember(3)] [ProtoBuf.ProtoMember(3)] public List<PersonModel> Employees { get; set; }
        [ProtoMember(4)] [ProtoBuf.ProtoMember(4)] public List<AddressModel> Offices { get; set; }
        [ProtoMember(5)] [ProtoBuf.ProtoMember(5)] public int FoundedYear { get; set; }
    }
}
