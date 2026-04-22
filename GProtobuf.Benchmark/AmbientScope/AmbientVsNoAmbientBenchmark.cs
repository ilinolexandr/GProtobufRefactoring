using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GProtobuf;
using GProtobuf.Benchmark.AmbientScope.NoAmbient;
using GProtobuf.Benchmark.AmbientScope.NoGuard;
using GProtobuf.Core;
using NoAmbientSerializers = GProtobuf.Benchmark.AmbientScope.NoAmbient.Serialization.Serializers;
using AmbientSerializers = GProtobuf.Benchmark.AmbientScope.Serialization.Serializers;
using NoGuardSerializers = GProtobuf.Benchmark.AmbientScope.NoGuard.Serialization.Serializers;

[assembly: GenerateSerializer(typeof(List<GProtobuf.Benchmark.AmbientScope.BenchPrimitive>))]
[assembly: GenerateSerializer(typeof(List<BenchPrimitiveNoAmbient>))]
[assembly: GenerateSerializer(typeof(Dictionary<string, GProtobuf.Benchmark.AmbientScope.BenchPrimitive>))]
[assembly: GenerateSerializer(typeof(Dictionary<string, BenchPrimitiveNoAmbient>))]

namespace GProtobuf.Benchmark.AmbientScope;

/// <summary>All Ambient-vs-NoAmbient variants (message + standalone List/Dict, two-pass + one-pass) in one table.</summary>
[MemoryDiagnoser(displayGenColumns: false)]
[Orderer(SummaryOrderPolicy.Declared)]
public class AmbientVsNoAmbientBenchmark
{
    private const int ElementCount = 10;

    private BenchDeviceValue _ambientModel;
    private BenchDeviceValueNoAmbient _noAmbientModel;
    private BenchDeviceValueNoGuard _noGuardModel;

    private List<BenchPrimitive> _ambientList;
    private List<BenchPrimitiveNoAmbient> _noAmbientList;
    private Dictionary<string, BenchPrimitive> _ambientDict;
    private Dictionary<string, BenchPrimitiveNoAmbient> _noAmbientDict;

    private readonly MemoryStream _stream = new(1 << 16);

    [GlobalSetup]
    public void Setup()
    {
        _ambientModel = new BenchDeviceValue
        {
            ValueType = 3,
            Primitive = new BenchPrimitive
            {
                ValueTypeId = 1,
                ValueBytes = 0x40490FDB,
                StringPayload = null,
            },
            Timestamp = 1_750_000_000L,
        };
        _noAmbientModel = new BenchDeviceValueNoAmbient
        {
            ValueType = 3,
            Primitive = new BenchPrimitiveNoAmbient
            {
                ValueTypeId = 1,
                ValueBytes = 0x40490FDB,
                StringPayload = null,
            },
            Timestamp = 1_750_000_000L,
        };
        _noGuardModel = new BenchDeviceValueNoGuard
        {
            ValueType = 3,
            Primitive = new BenchPrimitiveNoGuard
            {
                ValueTypeId = 1,
                ValueBytes = 0x40490FDB,
                StringPayload = null,
            },
            Timestamp = 1_750_000_000L,
        };

        _ambientList = new List<BenchPrimitive>(ElementCount);
        _noAmbientList = new List<BenchPrimitiveNoAmbient>(ElementCount);
        _ambientDict = new Dictionary<string, BenchPrimitive>(ElementCount);
        _noAmbientDict = new Dictionary<string, BenchPrimitiveNoAmbient>(ElementCount);

        for (int i = 0; i < ElementCount; i++)
        {
            _ambientList.Add(new BenchPrimitive
            {
                ValueTypeId = 1,
                ValueBytes = 0x40490FDB + i,
                StringPayload = null,
            });
            _noAmbientList.Add(new BenchPrimitiveNoAmbient
            {
                ValueTypeId = 1,
                ValueBytes = 0x40490FDB + i,
                StringPayload = null,
            });
            var key = "k" + i;
            _ambientDict[key] = new BenchPrimitive
            {
                ValueTypeId = 1,
                ValueBytes = 0x40490FDB + i,
                StringPayload = null,
            };
            _noAmbientDict[key] = new BenchPrimitiveNoAmbient
            {
                ValueTypeId = 1,
                ValueBytes = 0x40490FDB + i,
                StringPayload = null,
            };
        }

        // Warm every path so first iteration doesn't pay JIT + cold-pool cost.
        DataTypePoolHandler.MarkThreadAsLongLived();
        DataTypePoolHandlerNoGuard.MarkThreadAsLongLived();


        AmbientSerializers.Serialize(_stream, _ambientModel);
        _stream.Position = 0; _stream.SetLength(0);
        NoAmbientSerializers.Serialize(_stream, _noAmbientModel);
        _stream.Position = 0; _stream.SetLength(0);
        NoGuardSerializers.Serialize(_stream, _noGuardModel);
        _stream.Position = 0; _stream.SetLength(0);

        AmbientSerializers.SerializeListOfBenchPrimitive(_stream, _ambientList);
        _stream.Position = 0; _stream.SetLength(0);
        AmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitive(_stream, _ambientDict);
        _stream.Position = 0; _stream.SetLength(0);
        AmbientSerializers.SerializeListOfBenchPrimitiveOnePass(_stream, _ambientList);
        _stream.Position = 0; _stream.SetLength(0);
        AmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitiveOnePass(_stream, _ambientDict);
        _stream.Position = 0; _stream.SetLength(0);

        NoAmbientSerializers.SerializeListOfBenchPrimitiveNoAmbient(_stream, _noAmbientList);
        _stream.Position = 0; _stream.SetLength(0);
        NoAmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitiveNoAmbient(_stream, _noAmbientDict);
        _stream.Position = 0; _stream.SetLength(0);
        NoAmbientSerializers.SerializeListOfBenchPrimitiveNoAmbientOnePass(_stream, _noAmbientList);
        _stream.Position = 0; _stream.SetLength(0);
        NoAmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitiveNoAmbientOnePass(_stream, _noAmbientDict);
        _stream.Position = 0; _stream.SetLength(0);

        // Start from AdHoc; per-method setup flips as needed.
        DataTypePoolHandler.UnmarkCurrentThread();
        DataTypePoolHandlerNoGuard.UnmarkCurrentThread();
    }

    [IterationSetup]
    public void Reset()
    {
        _stream.Position = 0;
        _stream.SetLength(0);
    }

    // --- Single-message entry-points ---

    /// <summary>Baseline: generator emits plain body (no wrap).</summary>
    [Benchmark(Baseline = true)]
    public long Msg_NoAmbient()
    {
        _stream.Position = 0;
        NoAmbientSerializers.Serialize(_stream, _noAmbientModel);
        return _stream.Position;
    }

    /// <summary>Hot path: thread marked long-lived so Before/After degenerate to a bool check.</summary>
    [Benchmark]
    public long Msg_Ambient_LongLived()
    {
        DataTypePoolHandler.MarkThreadAsLongLived();
        _stream.Position = 0;
        AmbientSerializers.Serialize(_stream, _ambientModel);
        return _stream.Position;
    }

    /// <summary>Same hot path with <c>AutoReentrancyGuard = false</c>; delta vs LongLived is guard cost.</summary>
    [Benchmark]
    public long Msg_Ambient_LongLived_NoGuard()
    {
        DataTypePoolHandlerNoGuard.MarkThreadAsLongLived();
        _stream.Position = 0;
        NoGuardSerializers.Serialize(_stream, _noGuardModel);
        return _stream.Position;
    }

    /// <summary>Fresh thread: Before rents a pool from shared cache, After returns it.</summary>
    [Benchmark]
    public long Msg_Ambient_AdHoc()
    {
        DataTypePoolHandler.UnmarkCurrentThread();
        _stream.Position = 0;
        AmbientSerializers.Serialize(_stream, _ambientModel);
        return _stream.Position;
    }

    /// <summary>Caller already entered the scope; guard collapses inner Before/After to zero.</summary>
    [Benchmark]
    public long Msg_Ambient_LongLived_ManualHooks()
    {
        DataTypePoolHandler.MarkThreadAsLongLived();
        DataTypePoolHandler.Before();
        try
        {
            _stream.Position = 0;
            AmbientSerializers.Serialize(_stream, _ambientModel);
            return _stream.Position;
        }
        finally { DataTypePoolHandler.After(); }
    }

    // --- Standalone List/Dict entry-points, two-pass ---

    [Benchmark]
    public long List_NoAmbient()
    {
        _stream.Position = 0;
        NoAmbientSerializers.SerializeListOfBenchPrimitiveNoAmbient(_stream, _noAmbientList);
        return _stream.Position;
    }

    [Benchmark]
    public long List_Ambient_LongLived()
    {
        DataTypePoolHandler.MarkThreadAsLongLived();
        _stream.Position = 0;
        AmbientSerializers.SerializeListOfBenchPrimitive(_stream, _ambientList);
        return _stream.Position;
    }

    [Benchmark]
    public long List_Ambient_AdHoc()
    {
        DataTypePoolHandler.UnmarkCurrentThread();
        _stream.Position = 0;
        AmbientSerializers.SerializeListOfBenchPrimitive(_stream, _ambientList);
        return _stream.Position;
    }

    [Benchmark]
    public long Dict_NoAmbient()
    {
        _stream.Position = 0;
        NoAmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitiveNoAmbient(_stream, _noAmbientDict);
        return _stream.Position;
    }

    [Benchmark]
    public long Dict_Ambient_LongLived()
    {
        DataTypePoolHandler.MarkThreadAsLongLived();
        _stream.Position = 0;
        AmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitive(_stream, _ambientDict);
        return _stream.Position;
    }

    [Benchmark]
    public long Dict_Ambient_AdHoc()
    {
        DataTypePoolHandler.UnmarkCurrentThread();
        _stream.Position = 0;
        AmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitive(_stream, _ambientDict);
        return _stream.Position;
    }

    // --- Standalone List/Dict entry-points, one-pass ---

    [Benchmark]
    public long List_NoAmbient_OnePass()
    {
        _stream.Position = 0;
        NoAmbientSerializers.SerializeListOfBenchPrimitiveNoAmbientOnePass(_stream, _noAmbientList);
        return _stream.Position;
    }

    [Benchmark]
    public long List_Ambient_LongLived_OnePass()
    {
        DataTypePoolHandler.MarkThreadAsLongLived();
        _stream.Position = 0;
        AmbientSerializers.SerializeListOfBenchPrimitiveOnePass(_stream, _ambientList);
        return _stream.Position;
    }

    [Benchmark]
    public long List_Ambient_AdHoc_OnePass()
    {
        DataTypePoolHandler.UnmarkCurrentThread();
        _stream.Position = 0;
        AmbientSerializers.SerializeListOfBenchPrimitiveOnePass(_stream, _ambientList);
        return _stream.Position;
    }

    [Benchmark]
    public long Dict_NoAmbient_OnePass()
    {
        _stream.Position = 0;
        NoAmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitiveNoAmbientOnePass(_stream, _noAmbientDict);
        return _stream.Position;
    }

    [Benchmark]
    public long Dict_Ambient_LongLived_OnePass()
    {
        DataTypePoolHandler.MarkThreadAsLongLived();
        _stream.Position = 0;
        AmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitiveOnePass(_stream, _ambientDict);
        return _stream.Position;
    }

    [Benchmark]
    public long Dict_Ambient_AdHoc_OnePass()
    {
        DataTypePoolHandler.UnmarkCurrentThread();
        _stream.Position = 0;
        AmbientSerializers.SerializeDictionaryOfStringAndBenchPrimitiveOnePass(_stream, _ambientDict);
        return _stream.Position;
    }
}
