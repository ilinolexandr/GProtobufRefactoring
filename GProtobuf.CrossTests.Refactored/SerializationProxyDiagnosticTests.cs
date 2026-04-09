using ProtoBuf;

namespace GProtobuf.CrossTests.Refactored;

public sealed class SerializationProxyDiagnosticTests
{
    // GPROTO010: Proxy type без [ProtoContract]
    [Fact]
    public void Proxy_WithoutProtoContract_ShouldProduceGPROTO010()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            // Немає [ProtoContract]!
            public struct BadProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                [ProxyCreate]
                public static BadProxy Create(ExtType src) => new() { X = src.X };

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto010 = diagnostics.Where(d => d.Id == "GPROTO010").ToList();
        Assert.NotEmpty(gproto010);
        Assert.Contains("BadProxy", gproto010[0].GetMessage());
    }

    // GPROTO011: Немає [ProxyCreate] методу
    [Fact]
    public void Proxy_WithoutProxyCreate_ShouldProduceGPROTO011()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public struct BadProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                // Немає [ProxyCreate]!

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto011 = diagnostics.Where(d => d.Id == "GPROTO011").ToList();
        Assert.NotEmpty(gproto011);
        Assert.Contains("BadProxy", gproto011[0].GetMessage());
    }

    // GPROTO013: Немає [ProxyConvert] методу
    [Fact]
    public void Proxy_WithoutProxyConvert_ShouldProduceGPROTO013()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public struct BadProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                [ProxyCreate]
                public static BadProxy Create(ExtType src) => new() { X = src.X };

                // Немає [ProxyConvert]!
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto013 = diagnostics.Where(d => d.Id == "GPROTO013").ToList();
        Assert.NotEmpty(gproto013);
        Assert.Contains("BadProxy", gproto013[0].GetMessage());
    }

    // GPROTO012: [ProxyCreate] не static
    [Fact]
    public void Proxy_WithNonStaticProxyCreate_ShouldProduceGPROTO012()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public struct BadProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                [ProxyCreate]
                public BadProxy Create(ExtType src) => new() { X = src.X }; // Не static!

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto012 = diagnostics.Where(d => d.Id == "GPROTO012").ToList();
        Assert.NotEmpty(gproto012);
        Assert.Contains("must be static", gproto012[0].GetMessage());
    }

    // GPROTO014: [ProxyConvert] static замість instance
    [Fact]
    public void Proxy_WithStaticProxyConvert_ShouldProduceGPROTO014()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public struct BadProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                [ProxyCreate]
                public static BadProxy Create(ExtType src) => new() { X = src.X };

                [ProxyConvert]
                public static ExtType Convert(BadProxy p) => new() { X = p.X }; // Static! Має бути instance
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto014 = diagnostics.Where(d => d.Id == "GPROTO014").ToList();
        Assert.NotEmpty(gproto014);
        Assert.Contains("instance method", gproto014[0].GetMessage());
    }

    private static System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> RunGeneratorAndGetDiagnostics(string code)
    {
        var generator = new global::GProtobuf.Generator.SerializerGenerator();
        var driver = Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver.Create(generator);

        var protobufAssembly = System.Reflection.Assembly.GetAssembly(typeof(ProtoBuf.ProtoContractAttribute));
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(assembly.Location))
            .Cast<Microsoft.CodeAnalysis.MetadataReference>()
            .Append(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(protobufAssembly!.Location))
            .ToArray();

        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("TestCompilation",
            [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code)],
            references,
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        return diagnostics;
    }
}
