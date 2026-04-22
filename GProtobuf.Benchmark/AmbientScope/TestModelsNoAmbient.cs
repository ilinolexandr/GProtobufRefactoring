using System;
using System.Collections.Generic;
using GProtobuf;
using GProtobuf.Benchmark.AmbientScope.NoAmbient;

[assembly: SerializationProxy(typeof(BenchPrimitiveNoAmbient), typeof(BenchPrimitiveNoAmbientProxy))]

namespace GProtobuf.Benchmark.AmbientScope.NoAmbient;

/// <summary>Parallel to BenchPrimitive but without <c>[AmbientSerializationHandler]</c>, so entry-points emit no wrap.</summary>
public class BenchPrimitiveNoAmbient
{
    public int ValueTypeId { get; set; }
    public long ValueBytes { get; set; }
    public string StringPayload { get; set; }
}

[ProtoContract]
public sealed class BenchPrimitiveNoAmbientProxy
{
    [ProtoMember(1)] public int ValueTypeId { get; set; }
    [ProtoMember(2)] public long ValueBytes { get; set; }
    [ProtoMember(3)] public string StringPayload { get; set; }

    [ProxyWrap]
    public static BenchPrimitiveNoAmbientProxy Wrap(BenchPrimitiveNoAmbient src)
    {
        var proxy = BenchPrimitiveNoAmbientPool.Rent();
        proxy.ValueTypeId = src.ValueTypeId;
        proxy.ValueBytes = src.ValueBytes;
        proxy.StringPayload = src.StringPayload;
        return proxy;
    }

    [ProxyAcquire]
    public static BenchPrimitiveNoAmbientProxy Acquire() => BenchPrimitiveNoAmbientPool.Rent();

    [ProxyConvert]
    public BenchPrimitiveNoAmbient Convert() => new()
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
        BenchPrimitiveNoAmbientPool.Return(this);
    }
}

[ProtoContract]
public class BenchDeviceValueNoAmbient
{
    [ProtoMember(1)] public int ValueType { get; set; }
    [ProtoMember(2)] public BenchPrimitiveNoAmbient Primitive { get; set; }
    [ProtoMember(3)] public long Timestamp { get; set; }
}

/// <summary>Plain ThreadStatic proxy pool — mirrors DataTypePoolHandler's pool so allocation behaviour is equalized across ambient and no-ambient benchmarks.</summary>
public static class BenchPrimitiveNoAmbientPool
{
    [ThreadStatic] private static Stack<BenchPrimitiveNoAmbientProxy> _pool;

    public static BenchPrimitiveNoAmbientProxy Rent()
    {
        var pool = _pool ??= new Stack<BenchPrimitiveNoAmbientProxy>();
        return pool.Count > 0 ? pool.Pop() : new BenchPrimitiveNoAmbientProxy();
    }

    public static void Return(BenchPrimitiveNoAmbientProxy proxy)
    {
        var pool = _pool ??= new Stack<BenchPrimitiveNoAmbientProxy>();
        pool.Push(proxy);
    }
}
