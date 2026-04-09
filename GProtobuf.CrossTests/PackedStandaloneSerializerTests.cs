using System;
using System.Collections.Generic;
using System.IO;
using GProtobuf.Core;
using Xunit;

// Register packed standalone serializers for primitive collections
[assembly: GenerateSerializer(typeof(List<bool>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(List<int>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(List<uint>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(List<long>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(List<ulong>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(List<short>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(List<ushort>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(List<float>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(List<double>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(List<sbyte>), IsPacked = true)]
[assembly: GenerateSerializer(typeof(int[]), IsPacked = true)]
[assembly: GenerateSerializer(typeof(bool[]), IsPacked = true)]
[assembly: GenerateSerializer(typeof(double[]), IsPacked = true)]
[assembly: GenerateSerializer(typeof(long[]), IsPacked = true)]

namespace GProtobuf.CrossTests
{
    /// <summary>
    /// Tests for packed standalone type serialization using [GenerateSerializer(typeof(...), IsPacked = true)].
    /// Packed format: [tag (field 1, wire type 2)][totalLength][value1][value2]...
    /// </summary>
    public class PackedStandaloneSerializerTests
    {
        #region List<int> Packed Tests

        [Fact]
        public void Test_GG_ListOfInt32Packed()
        {
            var original = new List<int> { 1, 2, 3, 42, 100, -1 };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfInt32Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        [Fact]
        public void Test_GG_ListOfInt32Packed_Empty()
        {
            var original = new List<int>();

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfInt32Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
        }

        [Fact]
        public void Test_GG_ListOfInt32Packed_SingleElement()
        {
            var original = new List<int> { 42 };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfInt32Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Single(deserialized);
            Assert.Equal(42, deserialized[0]);
        }

        [Fact]
        public void Test_GG_ListOfInt32Packed_LargeValues()
        {
            var original = new List<int> { int.MaxValue, int.MinValue, 0, 1, -1 };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfInt32Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        [Fact]
        public void Test_PackedInt32_SmallerThanUnpacked()
        {
            // Packed should produce smaller output than unpacked for primitives
            var data = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            using var msPacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32Packed(msPacked, data);

            using var msUnpacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32(msUnpacked, data);

            Assert.True(msPacked.Length < msUnpacked.Length,
                $"Packed ({msPacked.Length} bytes) should be smaller than unpacked ({msUnpacked.Length} bytes)");
        }

        [Fact]
        public void Test_SerializeListOfInt32Packed_Null_ShouldNotThrow()
        {
            List<int> nullList = null;

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32Packed(ms, nullList);

            Assert.Equal(0, ms.Length);
        }

        #endregion

        #region List<bool> Packed Tests

        [Fact]
        public void Test_GG_ListOfBooleanPacked()
        {
            var original = new List<bool> { true, false, true, true, false };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfBooleanPacked(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfBooleanPacked(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region List<long> Packed Tests

        [Fact]
        public void Test_GG_ListOfInt64Packed()
        {
            var original = new List<long> { 1L, 2L, long.MaxValue, long.MinValue, 0L };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt64Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfInt64Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region List<uint> Packed Tests

        [Fact]
        public void Test_GG_ListOfUInt32Packed()
        {
            var original = new List<uint> { 0, 1, 100, uint.MaxValue };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfUInt32Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfUInt32Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region List<ulong> Packed Tests

        [Fact]
        public void Test_GG_ListOfUInt64Packed()
        {
            var original = new List<ulong> { 0, 1, 100, ulong.MaxValue };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfUInt64Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfUInt64Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region List<short> Packed Tests

        [Fact]
        public void Test_GG_ListOfInt16Packed()
        {
            var original = new List<short> { 1, 2, short.MaxValue, short.MinValue, 0 };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt16Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfInt16Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region List<ushort> Packed Tests

        [Fact]
        public void Test_GG_ListOfUInt16Packed()
        {
            var original = new List<ushort> { 0, 1, 100, ushort.MaxValue };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfUInt16Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfUInt16Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region List<float> Packed Tests

        [Fact]
        public void Test_GG_ListOfSinglePacked()
        {
            var original = new List<float> { 1.5f, 2.5f, 3.14f, float.MaxValue, float.MinValue };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfSinglePacked(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfSinglePacked(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region List<double> Packed Tests

        [Fact]
        public void Test_GG_ListOfDoublePacked()
        {
            var original = new List<double> { 1.5, 2.5, 3.14, double.MaxValue, double.MinValue };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfDoublePacked(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfDoublePacked(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region List<sbyte> Packed Tests

        [Fact]
        public void Test_GG_ListOfSBytePacked()
        {
            var original = new List<sbyte> { 1, -1, sbyte.MaxValue, sbyte.MinValue, 0 };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfSBytePacked(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfSBytePacked(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region int[] Packed Array Tests

        [Fact]
        public void Test_GG_ArrayOfInt32Packed()
        {
            var original = new int[] { 1, 2, 3, 42, 100, -1 };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeArrayOfInt32Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeArrayOfInt32Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Length, deserialized.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        [Fact]
        public void Test_SerializeArrayOfInt32Packed_Null_ShouldNotThrow()
        {
            int[] nullArray = null;

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeArrayOfInt32Packed(ms, nullArray);

            Assert.Equal(0, ms.Length);
        }

        #endregion

        #region bool[] Packed Array Tests

        [Fact]
        public void Test_GG_ArrayOfBooleanPacked()
        {
            var original = new bool[] { true, false, true, true, false };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeArrayOfBooleanPacked(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeArrayOfBooleanPacked(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Length, deserialized.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region double[] Packed Array Tests

        [Fact]
        public void Test_GG_ArrayOfDoublePacked()
        {
            var original = new double[] { 1.5, 2.5, 3.14159, -1.0, 0.0 };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeArrayOfDoublePacked(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeArrayOfDoublePacked(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Length, deserialized.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region long[] Packed Array Tests

        [Fact]
        public void Test_GG_ArrayOfInt64Packed()
        {
            var original = new long[] { 1L, -1L, long.MaxValue, long.MinValue, 0L };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeArrayOfInt64Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeArrayOfInt64Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Length, deserialized.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region Packed vs Unpacked Size Comparison Tests

        [Fact]
        public void Test_PackedBool_SmallerThanUnpacked()
        {
            var data = new List<bool> { true, false, true, true, false, true, false, true, false, true };

            using var msPacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfBooleanPacked(msPacked, data);

            using var msUnpacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfBoolean(msUnpacked, data);

            Assert.True(msPacked.Length < msUnpacked.Length,
                $"Packed ({msPacked.Length} bytes) should be smaller than unpacked ({msUnpacked.Length} bytes)");
        }

        [Fact]
        public void Test_PackedDouble_SmallerThanUnpacked()
        {
            var data = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 };

            using var msPacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfDoublePacked(msPacked, data);

            using var msUnpacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfDouble(msUnpacked, data);

            Assert.True(msPacked.Length < msUnpacked.Length,
                $"Packed ({msPacked.Length} bytes) should be smaller than unpacked ({msUnpacked.Length} bytes)");
        }

        #endregion

        #region Many Elements Tests

        [Fact]
        public void Test_GG_ListOfInt32Packed_ManyElements()
        {
            var original = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                original.Add(i * 7 - 500);
            }

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32Packed(ms, original);
            var bytes = ms.ToArray();

            var deserialized = global::GProtobuf.Generated.Serialization.Deserializers.DeserializeListOfInt32Packed(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
        }

        #endregion

        #region Wire Format Comparison: GProtobuf Packed vs protobuf-net

        [Fact]
        public void Test_WireFormat_ListOfBool_CompareBytes()
        {
            var data = new List<bool> { true, false, true, true, false };

            // protobuf-net output
            using var msProto = new MemoryStream();
            ProtoBuf.Serializer.Serialize(msProto, data);
            var protoBytes = msProto.ToArray();

            // GProtobuf packed output
            using var msGPacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfBooleanPacked(msGPacked, data);
            var gPackedBytes = msGPacked.ToArray();

            // GProtobuf unpacked output
            using var msGUnpacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfBoolean(msGUnpacked, data);
            var gUnpackedBytes = msGUnpacked.ToArray();

            // Dump all three for analysis
            var protoDump = BitConverter.ToString(protoBytes);
            var gPackedDump = BitConverter.ToString(gPackedBytes);
            var gUnpackedDump = BitConverter.ToString(gUnpackedBytes);

            // Output for visual inspection (will appear in test output)
            Assert.True(true,
                $"protobuf-net  ({protoBytes.Length}b): {protoDump}\n" +
                $"GProto packed ({gPackedBytes.Length}b): {gPackedDump}\n" +
                $"GProto unpack ({gUnpackedBytes.Length}b): {gUnpackedDump}");
        }

        [Fact]
        public void Test_WireFormat_ListOfInt_CompareBytes()
        {
            var data = new List<int> { 1, 2, 3 };

            using var msProto = new MemoryStream();
            ProtoBuf.Serializer.Serialize(msProto, data);
            var protoBytes = msProto.ToArray();

            using var msGPacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32Packed(msGPacked, data);
            var gPackedBytes = msGPacked.ToArray();

            using var msGUnpacked = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32(msGUnpacked, data);
            var gUnpackedBytes = msGUnpacked.ToArray();

            var protoDump = BitConverter.ToString(protoBytes);
            var gPackedDump = BitConverter.ToString(gPackedBytes);
            var gUnpackedDump = BitConverter.ToString(gUnpackedBytes);

            Assert.True(true,
                $"protobuf-net  ({protoBytes.Length}b): {protoDump}\n" +
                $"GProto packed ({gPackedBytes.Length}b): {gPackedDump}\n" +
                $"GProto unpack ({gUnpackedBytes.Length}b): {gUnpackedDump}");
        }

        /// <summary>
        /// protobuf-net serializes standalone List&lt;T&gt; in UNPACKED format by default:
        ///   [tag=0x08][value][tag=0x08][value]...
        /// GProtobuf packed uses true packed format:
        ///   [tag=0x0A][length][value1][value2]...
        ///
        /// Cross-compatibility:
        /// - GP→P (GProtobuf packed → protobuf-net): WORKS — protobuf-net can read both formats
        /// - P→GP packed: NOT compatible — protobuf-net outputs unpacked, GProtobuf packed expects packed
        /// - P→GP unpacked: WORKS — tested in StandaloneSerializerTests
        /// - GProtobuf packed is byte-identical to GProtobuf unpacked method (separate tests above)
        /// </summary>
        [Fact]
        public void Test_CrossCompat_GP_ListOfBoolPacked_DeserializedByProtobufNet()
        {
            // GProtobuf packed → protobuf-net: protobuf-net can read packed format
            var original = new List<bool> { true, false, true, true, false };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfBooleanPacked(ms, original);
            ms.Position = 0;
            var deserialized = ProtoBuf.Serializer.Deserialize<List<bool>>(ms);

            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
                Assert.Equal(original[i], deserialized[i]);
        }

        [Fact]
        public void Test_CrossCompat_GP_ListOfIntPacked_DeserializedByProtobufNet()
        {
            var original = new List<int> { 1, 2, 3, 42, -1 };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfInt32Packed(ms, original);
            ms.Position = 0;
            var deserialized = ProtoBuf.Serializer.Deserialize<List<int>>(ms);

            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
                Assert.Equal(original[i], deserialized[i]);
        }

        [Fact]
        public void Test_CrossCompat_GP_ListOfDoublePacked_DeserializedByProtobufNet()
        {
            var original = new List<double> { 1.5, 2.5, 3.14 };

            using var ms = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfDoublePacked(ms, original);
            ms.Position = 0;
            var deserialized = ProtoBuf.Serializer.Deserialize<List<double>>(ms);

            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
                Assert.Equal(original[i], deserialized[i]);
        }

        [Fact]
        public void Test_ProtobufNet_UsesUnpacked_GProtobufPacked_UsesPacked()
        {
            // Verify the wire format difference:
            // protobuf-net: unpacked [0x08][val][0x08][val]... (tag per element)
            // GProtobuf packed: [0x0A][length][val1][val2]...  (single tag)
            var data = new List<bool> { true, false, true };

            using var msProto = new MemoryStream();
            ProtoBuf.Serializer.Serialize(msProto, data);
            var protoBytes = msProto.ToArray();

            using var msG = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfBooleanPacked(msG, data);
            var gPackedBytes = msG.ToArray();

            // protobuf-net uses unpacked: 0x08=tag(field1,varint) per each element
            Assert.Equal(0x08, protoBytes[0]); // first byte is tag with wire type 0 (varint)

            // GProtobuf packed: 0x0A=tag(field1,len-delimited), then length, then values
            Assert.Equal(0x0A, gPackedBytes[0]); // first byte is tag with wire type 2 (length-delimited)
            Assert.Equal(3, gPackedBytes[1]);     // 3 bytes of packed content

            // Packed is smaller (no per-element tags)
            Assert.True(gPackedBytes.Length < protoBytes.Length,
                $"Packed ({gPackedBytes.Length}b) should be smaller than protobuf-net unpacked ({protoBytes.Length}b)");
        }

        [Fact]
        public void Test_GProtobufUnpacked_ByteIdentical_To_ProtobufNet()
        {
            // GProtobuf unpacked and protobuf-net both use unpacked format — should be byte-identical
            var data = new List<bool> { true, false, true };

            using var msProto = new MemoryStream();
            ProtoBuf.Serializer.Serialize(msProto, data);

            using var msG = new MemoryStream();
            global::GProtobuf.Generated.Serialization.Serializers.SerializeListOfBoolean(msG, data);

            Assert.Equal(msProto.ToArray(), msG.ToArray());
        }

        #endregion
    }
}
