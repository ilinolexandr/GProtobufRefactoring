using FluentAssertions;
using ProtoBuf;

// Assembly-level proxy registration for cross-namespace types
[assembly: SerializationProxy(typeof(TestProxies.CrossNsPoint), typeof(TestProxies.CrossNsPointProxy))]

namespace TestProxies
{
    /// <summary>Зовнішній тип в namespace TestProxies</summary>
    public struct CrossNsPoint
    {
        public int X;
        public int Y;

        public CrossNsPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>Proxy в namespace TestProxies — proxy і message в різних namespaces</summary>
    [ProtoContract]
    public struct CrossNsPointProxy
    {
        [ProtoMember(1)] public int X { get; set; }
        [ProtoMember(2)] public int Y { get; set; }

        [ProxyWrap]
        public static CrossNsPointProxy FromOriginal(CrossNsPoint source)
            => new() { X = source.X, Y = source.Y };

        [ProxyConvert]
        public CrossNsPoint ToOriginal() => new(X, Y);
    }
}

namespace TestMessages
{
    /// <summary>Message в namespace TestMessages, proxy field в TestProxies</summary>
    [ProtoContract]
    public class CrossNsTestMessage
    {
        [ProtoMember(1)]
        public TestProxies.CrossNsPoint Position { get; set; }

        [ProtoMember(2)]
        public int Tag { get; set; }
    }
}

namespace GProtobuf.Tests
{
    /// <summary>Cross-namespace proxy тест: proxy в TestProxies, message в TestMessages</summary>
    public sealed class CrossNamespaceProxyTests : BaseSerializationTest
    {
        [Fact]
        public void ProxyType_CrossNamespace_RoundTrip()
        {
            var model = new TestMessages.CrossNsTestMessage
            {
                Position = new TestProxies.CrossNsPoint(42, 99),
                Tag = 7
            };

            var data = SerializeWithGProtobuf(model, TestMessages.Serialization.Serializers.SerializeCrossNsTestMessage);
            var deserialized = TestMessages.Serialization.Deserializers.DeserializeCrossNsTestMessage(data);

            deserialized.Position.X.Should().Be(42);
            deserialized.Position.Y.Should().Be(99);
            deserialized.Tag.Should().Be(7);
        }
    }
}
