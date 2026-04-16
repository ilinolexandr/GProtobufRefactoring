namespace GProtobuf
{
    /// <summary>Sub-format for serializing/deserializing numeric-like data on the wire.</summary>
    public enum DataFormat
    {
        /// <summary>Default encoding for the data-type (varint for integers).</summary>
        Default,

        /// <summary>ZigZag varint encoding for signed integers (sint32/sint64 wire form).</summary>
        ZigZag,

        /// <summary>Two's-complement varint for signed integers (always 10 bytes for negatives).</summary>
        TwosComplement,

        /// <summary>Fixed-width encoding (fixed32/fixed64 / sfixed32/sfixed64).</summary>
        FixedSize,

        /// <summary>Group-delimited sub-message framing (legacy).</summary>
        Group,

        /// <summary>Well-known standardized representation (Timestamp for DateTime, Duration for TimeSpan).</summary>
        WellKnown,
    }
}
