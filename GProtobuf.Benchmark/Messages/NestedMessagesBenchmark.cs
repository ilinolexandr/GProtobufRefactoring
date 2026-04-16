using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using GProtobuf.Benchmark.Infrastructure;
using GProtobuf.Benchmark.Models;
using GProtobuf.Core;

namespace GProtobuf.Benchmark.Messages
{
    [BenchmarkCategory("CiFast")]
    public class NestedMessagesBenchmark : SerdeBenchmarkBase<NestedMessagesModel>
    {
        protected override NestedMessagesModel BuildModel()
        {
            var addresses = Enumerable.Range(1, 5).Select(i => new AddressModel
            {
                Street  = $"{100 + i} Main Street",
                City    = $"City {i}",
                State   = $"State {i}",
                ZipCode = $"1234{i}",
                Country = "USA"
            }).ToList();

            var people = Enumerable.Range(1, 10).Select(i => new PersonModel
            {
                FirstName    = $"FirstName{i}",
                LastName     = $"LastName{i}",
                Age          = 20 + i,
                Email        = $"person{i}@example.com",
                Address      = addresses[i % addresses.Count],
                PhoneNumbers = new List<string> { $"555-000{i}", $"555-111{i}" }
            }).ToList();

            return new NestedMessagesModel
            {
                StringField1 = "1",
                Person       = people[0],
                People       = people,
                Address      = addresses[0],
                Addresses    = addresses,
                Company      = new CompanyModel
                {
                    Name                = "Tech Corp Inc.",
                    HeadquartersAddress = addresses[0],
                    Employees           = people,
                    Offices             = addresses,
                    FoundedYear         = 2020
                }
            };
        }

        protected override byte[] PreSerialize(NestedMessagesModel model)
        {
            using var ms = new MemoryStream();
            Models.Serialization.Serializers.SerializeNestedMessagesModel(ms, model);
            return ms.ToArray();
        }

        // ── Serialize ─────────────────────────────────────────────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Serialize")]
        public long ProtobufNet_Ser()
        {
            global::ProtoBuf.Serializer.Serialize(Stream, Model);
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_OnePass_Ser()
        {
            using var scope = new OnePassScope(OnePassHarness.Pool);
            var writer = new OnePassStreamWriter(Stream, OnePassHarness.TempBuffer, scope.Pool);
            Models.Serialization.OnePassStreamWriters.WriteNestedMessagesModel(ref writer, Model);
            writer.Flush();
            return Stream.Length;
        }

        [Benchmark, BenchmarkCategory("Serialize")]
        public long GProtobuf_TwoPass_Stream_Ser()
        {
            Models.Serialization.Serializers.SerializeNestedMessagesModel(Stream, Model);
            return Stream.Length;
        }

        // ── Deserialize ───────────────────────────────────────────────────────

        [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
        public NestedMessagesModel ProtobufNet_De()
            => global::ProtoBuf.Serializer.Deserialize<NestedMessagesModel>((System.ReadOnlySpan<byte>)Serialized);

        [Benchmark, BenchmarkCategory("Deserialize")]
        public NestedMessagesModel GProtobuf_De()
            => Models.Serialization.Deserializers.DeserializeNestedMessagesModel(Serialized);

        // ── SizeCalc ──────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("SizeCalc")]
        public int GProtobuf_SizeCalc()
        {
            var calc = new WriteSizeCalculator();
            Models.Serialization.SizeCalculators.CalculateNestedMessagesModelContentSize(ref calc, Model);
            return calc.Length;
        }
    }
}
