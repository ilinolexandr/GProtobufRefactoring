using GProtobuf;
using System;
using System.Collections.Immutable;

namespace GProtobuf.CrossTests.TestModel
{
    /// <summary>
    /// Immutable rework of the production ImmutableValuesSegment/ImmutableValueSet pair
    /// (originally: DataType[] CurrentValues, ListDictionary&lt;DeviceValueStatisticsFunction, DataType[]&gt;
    /// StatisticValues, List&lt;ImmutableValuesSegment&gt; Segments — all mutable with set-properties).
    /// Exercises the immutable feature set end-to-end on a realistic shape:
    /// init-only members + ImmutableArray of messages + enum-keyed ImmutableDictionary with
    /// array-of-message values + ImmutableList of nested immutable objects + DateTime/TimeSpan.
    /// </summary>
    public enum DeviceValueEnumFunction
    {
        Min = 0,
        Max = 1,
        Average = 2,
        Sum = 3,
    }

    /// <summary>Stand-in for the production DataType payload (nested message).</summary>
    [ProtoContract]
    public sealed partial class ImmutableDataType
    {
        [ProtoMember(1)]
        public long Value { get; init; }

        [ProtoMember(2)]
        public int Scale { get; init; }
    }

    [ProtoContract]
    public sealed partial class ImmutableValuesSegment
    {
        [ProtoMember(1)]
        public DateTime FirstValueStartTime { get; init; }

        [ProtoMember(2)]
        public ImmutableArray<ImmutableDataType> CurrentValues { get; init; }

        [ProtoMember(3)]
        public ImmutableDictionary<DeviceValueEnumFunction, ImmutableDataType[]> StatisticValues { get; init; }
    }

    [ProtoContract]
    public sealed partial class ImmutableValueSet
    {
        [ProtoMember(1)]
        public string ProviderId { get; init; } = "";

        [ProtoMember(2)]
        public string ValueKind { get; init; } = "";

        [ProtoMember(3)]
        public string Unit { get; init; } = "";

        [ProtoMember(4)]
        public TimeSpan Granularity { get; init; }

        [ProtoMember(5)]
        public ImmutableList<ImmutableValuesSegment> Segments { get; init; }
    }
}
