using System;
using System.Reflection;
using BenchmarkDotNet.Running;
using GProtobuf.Benchmark.Infrastructure;

namespace GProtobuf.Benchmark
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var profile = BenchmarkConfig.ResolveProfileFromArgs(args);
            var config = new BenchmarkConfig(profile);

            Console.WriteLine($"[GProtobuf.Benchmark] profile = {profile}");
            Console.WriteLine("   Change with env GPROTOBUF_BENCH_MODE={dev|ci-fast|full} or flag --profile <mode>");
            Console.WriteLine();

            // Drop --profile <value> from args since BDN doesn't know it.
            var bdnArgs = StripProfileArg(args);

            // BDN drops into an interactive prompt when no selection arg is present.
            // That hangs non-TTY runs (CI, captured output). Default to '--filter *'
            // so unattended runs execute the full suite instead of waiting for stdin.
            bdnArgs = EnsureSelectionArg(bdnArgs);

            BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(bdnArgs, config);
        }

        private static string[] StripProfileArg(string[] args)
        {
            var list = new System.Collections.Generic.List<string>(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--profile", StringComparison.OrdinalIgnoreCase))
                {
                    i++; // skip value too
                    continue;
                }
                list.Add(args[i]);
            }
            return list.ToArray();
        }

        private static readonly string[] SelectionArgs =
        {
            "--filter", "-f", "--list", "--help", "-h", "--version", "--info"
        };

        private static string[] EnsureSelectionArg(string[] args)
        {
            foreach (var a in args)
                foreach (var s in SelectionArgs)
                    if (a.Equals(s, StringComparison.OrdinalIgnoreCase))
                        return args;

            Console.WriteLine("[GProtobuf.Benchmark] no --filter specified → defaulting to '--filter *' (use --filter <pattern> to narrow)");
            var extended = new string[args.Length + 2];
            Array.Copy(args, extended, args.Length);
            extended[args.Length]     = "--filter";
            extended[args.Length + 1] = "*";
            return extended;
        }
    }
}
