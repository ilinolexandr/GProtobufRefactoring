using System;
using System.IO;
using FluentAssertions;
using GProtobuf.CrossTests.TestModel;
using Xunit;

namespace GProtobuf.CrossTests
{
    public class DataTypeCollectionSerializationContainerTests
    {
        [Fact]
        public void Test_GG_SpanRoundTrip_WithNestedObjectAndCustomBuffer()
        {
            // Arrange
            var bufferData = new byte[] { 10, 20, 30, 40, 50 };
            var sourceStream = new MemoryStream(bufferData);
            var original = new DataTypeCollectionSerializationContainer(sourceStream, new DataTypeGenericParam
            {
                TypeId = 42,
                TypeName = "TestType"
            });

            // Act - serialize via stream writer, deserialize via span reader
            using var ms = new MemoryStream();
            global::GProtobuf.CrossTests.TestModel.Serialization.Serializers.SerializeDataTypeCollectionSerializationContainer(ms, original);
            var bytes = ms.ToArray();
            var deserialized = global::GProtobuf.CrossTests.TestModel.Serialization.Deserializers.DeserializeDataTypeCollectionSerializationContainer(bytes);

            // Assert
            deserialized.GenericParam.Should().NotBeNull();
            deserialized.GenericParam.TypeId.Should().Be(42);
            deserialized.GenericParam.TypeName.Should().Be("TestType");
            deserialized.GetReceivedData().Should().BeEquivalentTo(bufferData);
        }

        [Fact]
        public void Test_GG_StreamRoundTrip_WithNestedObjectAndCustomBuffer()
        {
            // Arrange
            var bufferData = new byte[] { 10, 20, 30, 40, 50 };
            var sourceStream = new MemoryStream(bufferData);
            var original = new DataTypeCollectionSerializationContainer(sourceStream, new DataTypeGenericParam
            {
                TypeId = 42,
                TypeName = "TestType"
            });

            // Act - serialize and deserialize via stream
            using var ms = new MemoryStream();
            global::GProtobuf.CrossTests.TestModel.Serialization.Serializers.SerializeDataTypeCollectionSerializationContainer(ms, original);
            ms.Position = 0;
            var deserialized = global::GProtobuf.CrossTests.TestModel.Serialization.Deserializers.DeserializeDataTypeCollectionSerializationContainer(ms);

            // Assert
            deserialized.GenericParam.Should().NotBeNull();
            deserialized.GenericParam.TypeId.Should().Be(42);
            deserialized.GenericParam.TypeName.Should().Be("TestType");
            deserialized.GetReceivedData().Should().BeEquivalentTo(bufferData);
        }

        [Fact]
        public void Test_GG_StreamPopulate_WithNestedObjectAndCustomBuffer()
        {
            // Arrange
            var bufferData = new byte[] { 1, 2, 3 };
            var sourceStream = new MemoryStream(bufferData);
            var original = new DataTypeCollectionSerializationContainer(sourceStream, new DataTypeGenericParam
            {
                TypeId = 7,
                TypeName = "Populated"
            });

            // Act - serialize, then populate existing instance via stream
            using var ms = new MemoryStream();
            global::GProtobuf.CrossTests.TestModel.Serialization.Serializers.SerializeDataTypeCollectionSerializationContainer(ms, original);
            var bytes = ms.ToArray();

            var target = new DataTypeCollectionSerializationContainer();
            global::GProtobuf.CrossTests.TestModel.Serialization.Deserializers.PopulateDataTypeCollectionSerializationContainer(bytes, target);

            // Assert
            target.GenericParam.Should().NotBeNull();
            target.GenericParam.TypeId.Should().Be(7);
            target.GenericParam.TypeName.Should().Be("Populated");
            target.GetReceivedData().Should().BeEquivalentTo(bufferData);
        }

        [Fact]
        public void Test_GG_StreamPopulate_ViaStream()
        {
            // Arrange
            var bufferData = new byte[] { 0xAA, 0xBB, 0xCC };
            var sourceStream = new MemoryStream(bufferData);
            var original = new DataTypeCollectionSerializationContainer(sourceStream, new DataTypeGenericParam
            {
                TypeId = 99,
                TypeName = "StreamPopulate"
            });

            // Act - serialize, then populate via Stream overload
            using var ms = new MemoryStream();
            global::GProtobuf.CrossTests.TestModel.Serialization.Serializers.SerializeDataTypeCollectionSerializationContainer(ms, original);
            ms.Position = 0;

            var target = new DataTypeCollectionSerializationContainer();
            global::GProtobuf.CrossTests.TestModel.Serialization.Deserializers.PopulateDataTypeCollectionSerializationContainer(ms, target);

            // Assert
            target.GenericParam.Should().NotBeNull();
            target.GenericParam.TypeId.Should().Be(99);
            target.GetReceivedData().Should().BeEquivalentTo(bufferData);
        }

        [Fact]
        public void Test_GG_OnlyNestedObject_NoCustomBuffer()
        {
            // Arrange - source stream is null, so custom buffer size = 0 and won't be serialized
            var original = new DataTypeCollectionSerializationContainer
            {
                GenericParam = new DataTypeGenericParam { TypeId = 5, TypeName = "NoBuffer" }
            };

            // Act
            using var ms = new MemoryStream();
            global::GProtobuf.CrossTests.TestModel.Serialization.Serializers.SerializeDataTypeCollectionSerializationContainer(ms, original);
            ms.Position = 0;
            var deserialized = global::GProtobuf.CrossTests.TestModel.Serialization.Deserializers.DeserializeDataTypeCollectionSerializationContainer(ms);

            // Assert
            deserialized.GenericParam.Should().NotBeNull();
            deserialized.GenericParam.TypeId.Should().Be(5);
            deserialized.GenericParam.TypeName.Should().Be("NoBuffer");
            deserialized.GetReceivedData().Should().BeNull();
        }

        [Fact]
        public void Test_GG_LargeCustomBuffer()
        {
            // Arrange
            var bufferData = new byte[4096];
            new Random(42).NextBytes(bufferData);
            var sourceStream = new MemoryStream(bufferData);
            var original = new DataTypeCollectionSerializationContainer(sourceStream, new DataTypeGenericParam
            {
                TypeId = 1,
                TypeName = "Large"
            });

            // Act - stream round trip
            using var ms = new MemoryStream();
            global::GProtobuf.CrossTests.TestModel.Serialization.Serializers.SerializeDataTypeCollectionSerializationContainer(ms, original);
            ms.Position = 0;
            var deserialized = global::GProtobuf.CrossTests.TestModel.Serialization.Deserializers.DeserializeDataTypeCollectionSerializationContainer(ms);

            // Assert
            deserialized.GenericParam.TypeId.Should().Be(1);
            deserialized.GetReceivedData().Should().BeEquivalentTo(bufferData);
        }
    }
}
