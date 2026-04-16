using System.Collections.Immutable;
using System.Reflection;
using GProtobuf.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GProtobuf.CrossTests.Refactored;

/// <summary>
/// Diagnostic tests for the init-only property and get-only property analysis paths.
///
/// GPROTO004: A type with [ProtoMember] init-only properties must have either a parameterless
/// constructor (so the deferred object-initializer path can run) or a public constructor whose
/// parameters match all ProtoMembers (so constructor injection works). Otherwise: error.
///
/// GPROTO005: A get-only property (no setter at all) marked with [ProtoMember] is silently
/// dropped today; we want a warning so the user knows the field will not be deserialized.
/// </summary>
public sealed class InitPropertyDiagnosticTests
{
    // ─── GPROTO004 ──────────────────────────────────────────────────────────

    [Fact]
    public void InitOnly_WithParameterlessConstructor_ShouldNotProduceGPROTO004()
    {
        // language=C#
        var code = """
            using GProtobuf;

            namespace TestNamespace;

            [ProtoContract]
            public class GoodInitWithParameterless
            {
                [ProtoMember(1)]
                public int Value { get; init; }

                [ProtoMember(2)]
                public string Name { get; init; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "GPROTO004");
    }

    [Fact]
    public void InitOnly_WithMatchingConstructor_ShouldNotProduceGPROTO004()
    {
        // Init-only properties + a constructor matching all ProtoMembers by name & type:
        // generator should pick the constructor-injection path; no diagnostic expected.
        // language=C#
        var code = """
            using GProtobuf;

            namespace TestNamespace;

            [ProtoContract]
            public class GoodInitWithMatchingCtor
            {
                public GoodInitWithMatchingCtor(int value, string name)
                {
                    Value = value;
                    Name = name;
                }

                [ProtoMember(1)]
                public int Value { get; init; }

                [ProtoMember(2)]
                public string Name { get; init; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "GPROTO004");
    }

    [Fact]
    public void InitOnly_WithUnrelatedConstructor_ShouldProduceGPROTO004()
    {
        // No parameterless constructor and the only constructor takes parameters that
        // do NOT match ProtoMembers (different name): both deserialization paths fail.
        // language=C#
        var code = """
            using GProtobuf;

            namespace TestNamespace;

            [ProtoContract]
            public class BadInitNoMatchingCtor
            {
                public BadInitNoMatchingCtor(int unrelated)
                {
                    Value = unrelated;
                }

                [ProtoMember(1)]
                public int Value { get; init; }

                [ProtoMember(2)]
                public string Name { get; init; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);

        var errors = diagnostics.Where(d => d.Id == "GPROTO004").ToList();
        Assert.NotEmpty(errors);
        Assert.All(errors, e =>
        {
            Assert.Equal(DiagnosticSeverity.Error, e.Severity);
            Assert.Contains("BadInitNoMatchingCtor", e.GetMessage());
        });
    }

    [Fact]
    public void InitOnly_StructWithDefaultCtor_ShouldNotProduceGPROTO004()
    {
        // Structs always have an implicit parameterless constructor, so the deferred
        // object-initializer path is always available — no diagnostic expected.
        // language=C#
        var code = """
            using GProtobuf;

            namespace TestNamespace;

            [ProtoContract]
            public struct InitStruct
            {
                [ProtoMember(1)]
                public int Value { get; init; }

                [ProtoMember(2)]
                public string Name { get; init; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "GPROTO004");
    }

    // ─── GPROTO005 ──────────────────────────────────────────────────────────

    [Fact]
    public void GetOnlyProperty_WithProtoMember_ShouldProduceGPROTO005()
    {
        // language=C#
        var code = """
            using GProtobuf;

            namespace TestNamespace;

            [ProtoContract]
            public class GetOnlyModel
            {
                [ProtoMember(1)]
                public int Value { get; }

                [ProtoMember(2)]
                public string Name { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);

        var warnings = diagnostics.Where(d => d.Id == "GPROTO005").ToList();
        Assert.NotEmpty(warnings);
        Assert.All(warnings, w =>
        {
            Assert.Equal(DiagnosticSeverity.Warning, w.Severity);
            Assert.Contains("Value", w.GetMessage());
            Assert.Contains("GetOnlyModel", w.GetMessage());
        });
    }

    [Fact]
    public void NoGetOnlyProperty_ShouldNotProduceGPROTO005()
    {
        // language=C#
        var code = """
            using GProtobuf;

            namespace TestNamespace;

            [ProtoContract]
            public class CleanModel
            {
                [ProtoMember(1)]
                public int Value { get; set; }

                [ProtoMember(2)]
                public string Name { get; init; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "GPROTO005");
    }

    // ─── Helper ─────────────────────────────────────────────────────────────

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
