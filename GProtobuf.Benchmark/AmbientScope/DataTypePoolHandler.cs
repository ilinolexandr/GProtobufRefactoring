using System;
using System.Collections.Generic;

namespace GProtobuf.Benchmark.AmbientScope;

/// <summary>Benchmark pool handler with two modes: LongLived (Before/After are bool-check-and-return) and AdHoc (pool rent/return from a shared LIFO cache).</summary>
public static class DataTypePoolHandler
{
    [ThreadStatic] private static Stack<BenchPrimitiveProxy> _proxyPool;
    [ThreadStatic] private static bool _longLivedThread;

    private static readonly Stack<Stack<BenchPrimitiveProxy>> _sharedPoolCache = new();
    private static readonly object _cacheLock = new();

    public static void MarkThreadAsLongLived()
    {
        _longLivedThread = true;
        _proxyPool ??= new Stack<BenchPrimitiveProxy>();
    }

    public static void UnmarkCurrentThread() => _longLivedThread = false;

    public static void Before()
    {
        if (_longLivedThread) return;
        lock (_cacheLock)
        {
            _proxyPool = _sharedPoolCache.Count > 0
                ? _sharedPoolCache.Pop()
                : new Stack<BenchPrimitiveProxy>();
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

    public static BenchPrimitiveProxy RentProxy()
    {
        var pool = _proxyPool;
        return pool is { Count: > 0 } ? pool.Pop() : new BenchPrimitiveProxy();
    }

    public static void ReturnProxy(BenchPrimitiveProxy proxy)
    {
        var pool = _proxyPool;
        pool?.Push(proxy);
    }
}
