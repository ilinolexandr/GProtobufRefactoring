using System;

namespace GProtobuf.Core
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class GenerateSerializerAttribute : Attribute
    {
        public Type CollectionType { get; }

        /// <summary>
        /// When true, generates packed encoding methods (length-delimited with contiguous values).
        /// Only valid for collections of primitive types (int, long, bool, float, etc.) and enums.
        /// Packed methods have "Packed" suffix in their names (e.g., SerializeListOfInt32Packed).
        /// </summary>
        public bool IsPacked { get; set; }

        public GenerateSerializerAttribute(Type collectionType)
        {
            CollectionType = collectionType;
        }
    }
}
