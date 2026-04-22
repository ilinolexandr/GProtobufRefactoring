using System.Collections.Generic;
using GProtobuf;
using GProtobuf.Benchmark.AmbientScope.NoGuard;

// Structural clone of BenchPrimitive / BenchDeviceValue with AutoReentrancyGuard = false so the
// delta between the two variants isolates the cost of the depth counter itself.
[assembly: SerializationProxy(typeof(BenchPrimitiveNoGuard), typeof(BenchPrimitiveNoGuardProxy))]
[assembly: AmbientSerializationHandler(
    typeof(BenchPrimitiveNoGuard),
    typeof(DataTypePoolHandlerNoGuard),
    AutoReentrancyGuard = false)]

namespace GProtobuf.Benchmark.AmbientScope.NoGuard;

public class BenchPrimitiveNoGuard
{
    public int ValueTypeId { get; set; }
    public long ValueBytes { get; set; }
    public string StringPayload { get; set; }
}

[ProtoContract]
public sealed class BenchPrimitiveNoGuardProxy
{
    [ProtoMember(1)] public int ValueTypeId { get; set; }
    [ProtoMember(2)] public long ValueBytes { get; set; }
    [ProtoMember(3)] public string StringPayload { get; set; }

    [ProxyWrap]
    public static BenchPrimitiveNoGuardProxy Wrap(BenchPrimitiveNoGuard src)
    {
        var proxy = DataTypePoolHandlerNoGuard.RentProxy();
        proxy.ValueTypeId = src.ValueTypeId;
        proxy.ValueBytes = src.ValueBytes;
        proxy.StringPayload = src.StringPayload;
        return proxy;
    }

    [ProxyAcquire]
    public static BenchPrimitiveNoGuardProxy Acquire() =>
        DataTypePoolHandlerNoGuard.RentProxy();

    [ProxyConvert]
    public BenchPrimitiveNoGuard Convert() =>
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
        DataTypePoolHandlerNoGuard.ReturnProxy(this);
    }
}

[ProtoContract]
public class BenchDeviceValueNoGuard
{
    [ProtoMember(1)] public int ValueType { get; set; }
    [ProtoMember(2)] public BenchPrimitiveNoGuard Primitive { get; set; }
    [ProtoMember(3)] public long Timestamp { get; set; }
}

/// <summary>Separate handler + ThreadStatic state so the NoGuard registration doesn't cross-contaminate DataTypePoolHandler's pool.</summary>
public static class DataTypePoolHandlerNoGuard
{
    [ThreadStatic] private static Stack<BenchPrimitiveNoGuardProxy> _proxyPool;
    [ThreadStatic] private static bool _longLivedThread;

    private static readonly Stack<Stack<BenchPrimitiveNoGuardProxy>> _sharedPoolCache = new();
    private static readonly object _cacheLock = new();

    public static void MarkThreadAsLongLived()
    {
        _longLivedThread = true;
        _proxyPool ??= new Stack<BenchPrimitiveNoGuardProxy>();
    }

    public static void UnmarkCurrentThread() => _longLivedThread = false;

    public static void Before()
    {
        if (_longLivedThread) return;
        lock (_cacheLock)
        {
            _proxyPool = _sharedPoolCache.Count > 0
                ? _sharedPoolCache.Pop()
                : new Stack<BenchPrimitiveNoGuardProxy>();
        }
    }

    public static void After()
    {
        if (_longLivedThread) return;
        var pool = _proxyPool;
        if (pool == null) return;
        _proxyPool = null;
        lock (_cacheLock)
        {
            _sharedPoolCache.Push(pool);
        }
    }

    public static BenchPrimitiveNoGuardProxy RentProxy()
    {
        var pool = _proxyPool;
        return pool is { Count: > 0 } ? pool.Pop() : new BenchPrimitiveNoGuardProxy();
    }

    public static void ReturnProxy(BenchPrimitiveNoGuardProxy proxy)
    {
        var pool = _proxyPool;
        pool?.Push(proxy);
    }
}
