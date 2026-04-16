using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace GProtobuf.Benchmark.Infrastructure
{
    public sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig() : this(ResolveProfile()) { }

        public BenchmarkConfig(BenchmarkProfile profile)
        {
            AddJob(BuildJob(profile));

            foreach (var col in DefaultConfig.Instance.GetColumnProviders()) AddColumnProvider(col);
            foreach (var exp in DefaultConfig.Instance.GetExporters()) AddExporter(exp);
            foreach (var log in DefaultConfig.Instance.GetLoggers()) AddLogger(log);
            foreach (var an in DefaultConfig.Instance.GetAnalysers()) AddAnalyser(an);
            foreach (var val in DefaultConfig.Instance.GetValidators()) AddValidator(val);
            foreach (var diag in DefaultConfig.Instance.GetDiagnosers()) AddDiagnoser(diag);

            AddColumn(new PayloadSizeColumn());

            SummaryStyle = SummaryStyle.Default
                .WithRatioStyle(RatioStyle.Trend);
        }

        private static Job BuildJob(BenchmarkProfile profile) => profile switch
        {
            // Dev: in-process toolchain → no subprocess (avoids AV friction, faster turnaround).
            BenchmarkProfile.Dev    => Job.Default
                                          .WithToolchain(InProcessEmitToolchain.Instance)
                                          .WithWarmupCount(3).WithIterationCount(50)
                                          .WithId("Dev"),
            BenchmarkProfile.CiFast => Job.Default.WithWarmupCount(2).WithIterationCount(5).WithId("CiFast"),
            BenchmarkProfile.Full   => Job.Default.WithWarmupCount(3).WithIterationCount(10).WithId("Full"),
            _ => throw new ArgumentOutOfRangeException(nameof(profile))
        };

        public static BenchmarkProfile ResolveProfile()
        {
            var env = Environment.GetEnvironmentVariable("GPROTOBUF_BENCH_MODE");
            return env?.ToLowerInvariant() switch
            {
                "ci-fast" or "cifast" => BenchmarkProfile.CiFast,
                "full"                => BenchmarkProfile.Full,
                _                     => BenchmarkProfile.Dev
            };
        }

        public static BenchmarkProfile ResolveProfileFromArgs(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals("--profile", StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1].ToLowerInvariant() switch
                    {
                        "ci-fast" or "cifast" => BenchmarkProfile.CiFast,
                        "full"                => BenchmarkProfile.Full,
                        _                     => BenchmarkProfile.Dev
                    };
                }
            }
            return ResolveProfile();
        }
    }
}
