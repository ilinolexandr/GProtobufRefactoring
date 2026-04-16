namespace GProtobuf.Generator.Analysis
{
    internal class ProtoIncludeInfo
    {
        public ProtoIncludeInfo(int fieldId, string type)
        {
            FieldId = fieldId;
            Type = type;
        }

        public int FieldId { get; set; }

        public string Type { get; set; }
    }
}
