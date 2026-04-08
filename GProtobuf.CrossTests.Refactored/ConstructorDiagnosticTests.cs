using System.Collections.Immutable;
using System.Reflection;
using GProtobuf.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GProtobuf.CrossTests.Refactored;

public sealed class ConstructorDiagnosticTests
{
    [Fact(Skip = "GPROTO003 diagnostic not yet implemented in the generator")]
    public void ReadonlyStruct_WithNoMatchingConstructor_ShouldProduceGPROTO003()
    {
        // language=C#
        var code = """
            using ProtoBuf;

            namespace TestNamespace;

            [ProtoContract]
            public readonly struct BadStruct
            {
                // Constructor has wrong number of parameters (2 fields, 1 param)
                public BadStruct(int value)
                {
                    Value = value;
                    Name = string.Empty;
                }

                [ProtoMember(1)]
                public readonly int Value;

                [ProtoMember(2)]
                public readonly string Name;
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);

        var errors = diagnostics.Where(d => d.Id == "GPROTO003").ToList();
        Assert.NotEmpty(errors);
        Assert.All(errors, e =>
        {
            Assert.Equal(DiagnosticSeverity.Error, e.Severity);
            Assert.Contains("BadStruct", e.GetMessage());
        });
    }

    [Fact]
    public void ReadonlyStruct_WithMatchingConstructor_ShouldNotProduceGPROTO003()
    {
        // language=C#
        var code = """
            using ProtoBuf;

            namespace TestNamespace;

            [ProtoContract]
            public readonly struct GoodStruct
            {
                public GoodStruct(int value)
                {
                    Value = value;
                }

                [ProtoMember(1)]
                public readonly int Value;
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "GPROTO003");
    }

    private static ImmutableArray<Diagnostic> RunGeneratorAndGetDiagnostics(string code)
    {
        var generator = new SerializerGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var protobufAssembly = Assembly.GetAssembly(typeof(ProtoBuf.ProtoContractAttribute));
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .Append(MetadataReference.CreateFromFile(protobufAssembly!.Location))
            .ToArray();

        var compilation = CSharpCompilation.Create("TestCompilation",
            [CSharpSyntaxTree.ParseText(code)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        return diagnostics;
    }
}
