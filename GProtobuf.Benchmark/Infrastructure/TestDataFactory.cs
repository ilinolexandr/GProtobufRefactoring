using System;
using System.Text;

namespace GProtobuf.Benchmark.Infrastructure
{
    /// <summary>
    /// Deterministic factories for benchmark edge-case data.
    /// Single seed (42) for reproducibility.
    /// </summary>
    public static class TestDataFactory
    {
        public const int Seed = 42;

        public enum VarintShape
        {
            Zero,
            One,
            Boundary1Byte,   // 127  — still 1-byte varint
            Boundary2Byte,   // 128  — first 2-byte varint
            Boundary2To3,    // 16383
            MaxValue,
            NegativeOne,     // worst case: 10-byte int32 varint
            MinValue
        }

        public enum StringShape
        {
            Empty,
            OneCharAscii,
            Short63Ascii,
            Boundary64Ascii,
            Short127Ascii,
            Boundary128Ascii,
            Long1KAscii,
            Long64KAscii,
            ShortUtf8Multibyte,
            LongUtf8Multibyte
        }

        public static int Int32(VarintShape shape) => shape switch
        {
            VarintShape.Zero          => 0,
            VarintShape.One           => 1,
            VarintShape.Boundary1Byte => 127,
            VarintShape.Boundary2Byte => 128,
            VarintShape.Boundary2To3  => 16383,
            VarintShape.MaxValue      => int.MaxValue,
            VarintShape.NegativeOne   => -1,
            VarintShape.MinValue      => int.MinValue,
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };

        public static long Int64(VarintShape shape) => shape switch
        {
            VarintShape.Zero          => 0L,
            VarintShape.One           => 1L,
            VarintShape.Boundary1Byte => 127L,
            VarintShape.Boundary2Byte => 128L,
            VarintShape.Boundary2To3  => 16383L,
            VarintShape.MaxValue      => long.MaxValue,
            VarintShape.NegativeOne   => -1L,
            VarintShape.MinValue      => long.MinValue,
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };

        public static string String(StringShape shape) => shape switch
        {
            StringShape.Empty              => string.Empty,
            StringShape.OneCharAscii       => "x",
            StringShape.Short63Ascii       => new string('a', 63),
            StringShape.Boundary64Ascii    => new string('a', 64),
            StringShape.Short127Ascii      => new string('a', 127),
            StringShape.Boundary128Ascii   => new string('a', 128),
            StringShape.Long1KAscii        => new string('a', 1024),
            StringShape.Long64KAscii       => new string('a', 65536),
            StringShape.ShortUtf8Multibyte => "Привіт, світе! 😀",
            StringShape.LongUtf8Multibyte  => BuildUtf8Long(1024),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };

        public static byte[] Bytes(int size)
        {
            var arr = new byte[size];
            var rng = new Random(Seed);
            rng.NextBytes(arr);
            return arr;
        }

        public static int[] IntArray(int n, VarintShape shape = VarintShape.Boundary2Byte)
        {
            var arr = new int[n];
            var v = Int32(shape);
            for (int i = 0; i < n; i++) arr[i] = v;
            return arr;
        }

        private static string BuildUtf8Long(int approxBytes)
        {
            const string chunk = "Привіт, світе! 😀 ";
            var sb = new StringBuilder();
            while (Encoding.UTF8.GetByteCount(sb.ToString()) < approxBytes) sb.Append(chunk);
            return sb.ToString();
        }
    }
}
