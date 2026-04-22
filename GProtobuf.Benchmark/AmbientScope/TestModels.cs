using System.Collections.Generic;
using GProtobuf;
using GProtobuf.Benchmark.AmbientScope;

[assembly: SerializationProxy(typeof(BenchPrimitive), typeof(BenchPrimitiveProxy))]
[assembly: AmbientSerializationHandler(
    typeof(BenchPrimitive),
    typeof(DataTypePoolHandler),
    AutoReentrancyGuard = true)]

namespace GProtobuf.Benchmark.AmbientScope;

/// <summary>Marker interface carried by every model that needs pool-like setup.</summary>
public interface IBenchPooled { }

/// <summary>External (non-ProtoContract) primitive; the proxy below pulls from a thread-local pool.</summary>
public class BenchPrimitive : IBenchPooled
{
    public int ValueTypeId { get; set; }
    public long ValueBytes { get; set; }
    public string StringPayload { get; set; }
}

[ProtoContract]
public sealed class BenchPrimitiveProxy
{
    [ProtoMember(1)] public int ValueTypeId { get; set; }
    [ProtoMember(2)] public long ValueBytes { get; set; }
    [ProtoMember(3)] public string StringPayload { get; set; }

    [ProxyWrap]
    public static BenchPrimitiveProxy Wrap(BenchPrimitive src)
    {
        var proxy = DataTypePoolHandler.RentProxy();
        proxy.ValueTypeId = src.ValueTypeId;
        proxy.ValueBytes = src.ValueBytes;
        proxy.StringPayload = src.StringPayload;
        return proxy;
    }

    [ProxyAcquire]
    public static BenchPrimitiveProxy Acquire() =>
        DataTypePoolHandler.RentProxy();

    [ProxyConvert]
    public BenchPrimitive Convert() =>
        new()
        {
            ValueTypeId = ValueTypeId,
            ValueBytes = ValueBytes,
            StringPayload = StringPayload,
        };

    [ProxyReturn]
    public void Return()
    {
        ValueTypeId = 0;
        ValueBytes = 0;
        StringPayload = null;
        DataTypePoolHandler.ReturnProxy(this);
    }
}

[ProtoContract]
public class BenchDeviceValue
{
    [ProtoMember(1)] public int ValueType { get; set; }
    [ProtoMember(2)] public BenchPrimitive Primitive { get; set; }
    [ProtoMember(3)] public long Timestamp { get; set; }
}

[ProtoContract]
public class BenchDeviceValueList
{
    [ProtoMember(1)] public List<BenchDeviceValue> Values { get; set; } = new();
}

[ProtoContract]
public class BenchDeviceValueMap
{
    [ProtoMember(1)] public Dictionary<string, BenchDeviceValue> Values { get; set; } = new();
}
