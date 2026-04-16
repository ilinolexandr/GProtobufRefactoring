namespace GProtobuf.Generator.Analysis
{
    /// <summary>
    /// Specifies the varint encoding type for ProtoVarint structs.
    /// </summary>
    internal enum ProtoVarintType
    {
        /// <summary>
        /// Unsigned 32-bit integer (varint encoding).
        /// </summary>
        UInt32,

        /// <summary>
        /// Signed 32-bit integer (varint encoding, inefficient for negative numbers).
        /// </summary>
        Int32,

        /// <summary>
        /// Signed 32-bit integer with ZigZag encoding (efficient for negative numbers).
        /// </summary>
        SInt32,

        /// <summary>
        /// Unsigned 64-bit integer (varint encoding).
        /// </summary>
        UInt64,

        /// <summary>
        /// Signed 64-bit integer (varint encoding, inefficient for negative numbers).
        /// </summary>
        Int64,

        /// <summary>
        /// Signed 64-bit integer with ZigZag encoding (efficient for negative numbers).
        /// </summary>
        SInt64
    }
}
