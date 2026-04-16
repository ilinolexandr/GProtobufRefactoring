using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace GProtobuf.Benchmark.Infrastructure
{
    /// <summary>
    /// Displays payload size in bytes produced by the benchmark's GlobalSetup.
    /// <para>
    /// BDN runs benchmarks in a child process, so an in-memory registry populated
    /// by [GlobalSetup] is not visible to the summary-rendering (parent) process.
    /// Instead this column reflects over the benchmark type, instantiates it,
    /// invokes the [GlobalSetup] method, and reads the <c>Serialized</c> field
    /// from <see cref="SerdeBenchmarkBase{T}"/>. Cached per (type, paramsDisplay).
    /// </para>
    /// </summary>
    public sealed class PayloadSizeColumn : IColumn
    {
        private static readonly ConcurrentDictionary<string, long?> _cache = new();

        public string Id => nameof(PayloadSizeColumn);
        public string ColumnName => "Bytes";
        public string Legend => "Payload size in bytes produced by the benchmark's GlobalSetup (pre-serialized buffer length).";
        public UnitType UnitType => UnitType.Size;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
            => GetValue(summary, benchmarkCase, SummaryStyle.Default);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        {
            var size = Compute(benchmarkCase);
            return size.HasValue
                ? size.Value.ToString(CultureInfo.InvariantCulture)
                : "-";
        }

        private static long? Compute(BenchmarkCase benchmarkCase)
        {
            var type = benchmarkCase.Descriptor.Type;
            var paramsDisplay = benchmarkCase.Parameters?.DisplayInfo ?? string.Empty;
            var key = type.FullName + "::" + paramsDisplay;

            return _cache.GetOrAdd(key, _ => ComputeCore(type, benchmarkCase));
        }

        private static long? ComputeCore(Type type, BenchmarkCase benchmarkCase)
        {
            try
            {
                var instance = Activator.CreateInstance(type);
                if (instance == null) return null;

                // Apply any [Params] properties.
                var items = benchmarkCase.Parameters?.Items;
                if (items != null)
                foreach (var p in items)
                {
                    var prop = type.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null && prop.CanWrite) prop.SetValue(instance, p.Value);
                    else
                    {
                        var field = type.GetField(p.Name, BindingFlags.Public | BindingFlags.Instance);
                        field?.SetValue(instance, p.Value);
                    }
                }

                // Invoke methods marked [GlobalSetup] (walking base-types).
                foreach (var m in EnumerateMethods(type))
                {
                    if (m.GetCustomAttribute<GlobalSetupAttribute>() != null)
                        m.Invoke(instance, null);
                }

                // Read protected `Serialized` field from SerdeBenchmarkBase<T>.
                var field2 = FindField(type, "Serialized");
                if (field2?.GetValue(instance) is byte[] bytes) return bytes.LongLength;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static System.Collections.Generic.IEnumerable<MethodInfo> EnumerateMethods(Type type)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (var m in t.GetMethods(flags)) yield return m;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var f = t.GetField(name, flags);
                if (f != null) return f;
            }
            return null;
        }

        public override string ToString() => ColumnName;
    }
}
