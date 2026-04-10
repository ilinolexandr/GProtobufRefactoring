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

                [ProxyWrap]
                public static BadProxy Wrap(ExtType src) => new() { X = src.X };

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto010 = diagnostics.Where(d => d.Id == "GPROTO010").ToList();
        Assert.NotEmpty(gproto010);
        Assert.Contains("BadProxy", gproto010[0].GetMessage());
    }

    // GPROTO011: Немає [ProxyWrap] методу
    [Fact]
    public void Proxy_WithoutProxyWrap_ShouldProduceGPROTO011()
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

                // Немає [ProxyWrap]!

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

                [ProxyWrap]
                public static BadProxy Wrap(ExtType src) => new() { X = src.X };

                // Немає [ProxyConvert]!
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto013 = diagnostics.Where(d => d.Id == "GPROTO013").ToList();
        Assert.NotEmpty(gproto013);
        Assert.Contains("BadProxy", gproto013[0].GetMessage());
    }

    // GPROTO012: [ProxyWrap] не static
    [Fact]
    public void Proxy_WithNonStaticProxyWrap_ShouldProduceGPROTO012()
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

                [ProxyWrap]
                public BadProxy Wrap(ExtType src) => new() { X = src.X }; // Не static!

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

                [ProxyWrap]
                public static BadProxy Wrap(ExtType src) => new() { X = src.X };

                [ProxyConvert]
                public static ExtType Convert(BadProxy p) => new() { X = p.X }; // Static! Має бути instance
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto014 = diagnostics.Where(d => d.Id == "GPROTO014").ToList();
        Assert.NotEmpty(gproto014);
        Assert.Contains("instance method", gproto014[0].GetMessage());
    }

    // GPROTO019: [ProxyAcquire] не static
    [Fact]
    public void Proxy_WithNonStaticProxyAcquire_ShouldProduceGPROTO019()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public sealed class BadProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                [ProxyWrap]
                public static BadProxy Wrap(ExtType src) => new() { X = src.X };

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };

                [ProxyAcquire]
                public BadProxy Acquire() => new(); // Не static!
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto019 = diagnostics.Where(d => d.Id == "GPROTO019").ToList();
        Assert.NotEmpty(gproto019);
        Assert.Contains("must be static", gproto019[0].GetMessage());
    }

    // GPROTO019: [ProxyAcquire] має параметри
    [Fact]
    public void Proxy_WithParameteredProxyAcquire_ShouldProduceGPROTO019()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public sealed class BadProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                [ProxyWrap]
                public static BadProxy Wrap(ExtType src) => new() { X = src.X };

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };

                [ProxyAcquire]
                public static BadProxy Acquire(int unused) => new(); // Має параметри!
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto019 = diagnostics.Where(d => d.Id == "GPROTO019").ToList();
        Assert.NotEmpty(gproto019);
        Assert.Contains("take no parameters", gproto019[0].GetMessage());
    }

    // GPROTO019: [ProxyAcquire] неправильний return type
    [Fact]
    public void Proxy_WithWrongReturnTypeProxyAcquire_ShouldProduceGPROTO019()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public sealed class BadProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                [ProxyWrap]
                public static BadProxy Wrap(ExtType src) => new() { X = src.X };

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };

                [ProxyAcquire]
                public static object Acquire() => new BadProxy(); // Wrong return type!
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto019 = diagnostics.Where(d => d.Id == "GPROTO019").ToList();
        Assert.NotEmpty(gproto019);
        Assert.Contains("BadProxy", gproto019[0].GetMessage());
    }

    // GPROTO020: Більше одного [ProxyAcquire] методу
    [Fact]
    public void Proxy_WithMultipleProxyAcquire_ShouldProduceGPROTO020()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public sealed class BadProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                [ProxyWrap]
                public static BadProxy Wrap(ExtType src) => new() { X = src.X };

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };

                [ProxyAcquire]
                public static BadProxy AcquireOne() => new();

                [ProxyAcquire]
                public static BadProxy AcquireTwo() => new();
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto020 = diagnostics.Where(d => d.Id == "GPROTO020").ToList();
        Assert.NotEmpty(gproto020);
        Assert.Contains("BadProxy", gproto020[0].GetMessage());
    }

    // GPROTO018: [ProxyAcquire] + matching constructor конфлікт
    [Fact]
    public void Proxy_AcquireWithMatchingConstructor_ShouldProduceGPROTO018()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.BadProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public sealed class BadProxy
            {
                [ProtoMember(1)] public int X { get; }

                // Matching constructor — буде використано як ctor-based deserialization
                public BadProxy(int x) { X = x; }

                [ProxyWrap]
                public static BadProxy Wrap(ExtType src) => new(src.X);

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };

                [ProxyAcquire]
                public static BadProxy Acquire() => new(0);
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        var gproto018 = diagnostics.Where(d => d.Id == "GPROTO018").ToList();
        Assert.NotEmpty(gproto018);
        Assert.Contains("BadProxy", gproto018[0].GetMessage());
    }

    // Sanity: відсутність [ProxyAcquire] — це валідно (fallback на new())
    [Fact]
    public void Proxy_WithoutProxyAcquire_ShouldNotProduceProxyAcquireDiagnostics()
    {
        // language=C#
        var code = """
            using ProtoBuf;
            [assembly: SerializationProxy(typeof(TestNs.ExtType), typeof(TestNs.GoodProxy))]
            namespace TestNs;

            public struct ExtType { public int X; }

            [ProtoContract]
            public sealed class GoodProxy
            {
                [ProtoMember(1)] public int X { get; set; }

                [ProxyWrap]
                public static GoodProxy Wrap(ExtType src) => new() { X = src.X };

                [ProxyConvert]
                public ExtType Convert() => new() { X = X };
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(code);
        Assert.Empty(diagnostics.Where(d => d.Id == "GPROTO018" || d.Id == "GPROTO019" || d.Id == "GPROTO020"));
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
