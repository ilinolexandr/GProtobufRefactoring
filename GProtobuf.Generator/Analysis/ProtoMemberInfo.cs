namespace GProtobuf.Generator.Analysis
{
    internal class ProtoMemberInfo
    {
        public ProtoMemberInfo(int fieldId)
        {
            this.FieldId = fieldId;
        }

        public int FieldId { get; set; }

        public string Type { get; set; }

        public string Namespace { get; set; }

        public string Name { get; set; }

        public bool IsPacked { get; set; }

        public bool IsRequired { get; set; }

        public DataFormat DataFormat { get; set; }

        public bool IsNullable { get; set; }

        public bool IsInit { get; set; }

        public bool IsCollection { get; set; }

        public string CollectionElementType { get; set; }

        public CollectionKind CollectionKind { get; set; }

        public bool IsMap { get; set; }

        public string MapKeyType { get; set; }

        public string MapValueType { get; set; }

        public bool IsEnum { get; set; }

        public string EnumUnderlyingType { get; set; }

        public bool MapKeyIsEnum { get; set; }

        public string MapKeyEnumUnderlyingType { get; set; }

        public bool MapValueIsEnum { get; set; }

        public string MapValueEnumUnderlyingType { get; set; }

        public bool IsProtoVarint { get; set; }

        public ProtoVarintType ProtoVarintType { get; set; }

        public string ProtoVarintValueMember { get; set; }

        public bool ProtoVarintValueIsProperty { get; set; }
    }
}
