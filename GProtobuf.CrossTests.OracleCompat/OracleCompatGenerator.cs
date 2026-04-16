using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GProtobuf.CrossTests.OracleCompat;

/// <summary>
/// Emits protobuf-net mirror attributes ([ProtoBuf.ProtoContract], [ProtoBuf.ProtoPartialMember], [ProtoBuf.ProtoInclude],
/// callback forwarders) as a partial declaration for every type that carries [GProtobuf.ProtoContract].
/// This lets protobuf-net act as an oracle for wire-format tests while consumer models stay free of any ProtoBuf.* reference.
/// </summary>
[Generator]
public sealed class OracleCompatGenerator : IIncrementalGenerator
{
    private const string ProtoContractFqn = "GProtobuf.ProtoContractAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ProtoContractFqn,
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => Capture(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(typeProvider, static (spc, model) =>
        {
            var source = Emit(model);
            spc.AddSource($"{model.HintName}.OracleCompat.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static TypeModel? Capture(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol symbol) return null;
        if (symbol.TypeKind != TypeKind.Class && symbol.TypeKind != TypeKind.Struct) return null;

        var members = ImmutableArray.CreateBuilder<ProtoMemberModel>();
        var includes = ImmutableArray.CreateBuilder<ProtoIncludeModel>();
        var callbacks = ImmutableArray.CreateBuilder<CallbackModel>();

        foreach (var member in symbol.GetMembers())
        {
            switch (member)
            {
                case IPropertySymbol prop:
                    var propAttr = FindProtoMember(prop);
                    if (propAttr is { Tag: > 0 }) members.Add(propAttr);
                    break;

                case IFieldSymbol field when !field.IsImplicitlyDeclared:
                    var fieldAttr = FindProtoMember(field);
                    if (fieldAttr is { Tag: > 0 }) members.Add(fieldAttr);
                    break;

                case IMethodSymbol method when !method.IsStatic && method.MethodKind == MethodKind.Ordinary:
                    var cb = FindCallback(method);
                    if (cb is not null) callbacks.Add(cb.Value);
                    break;
            }
        }

        bool enumPassthru = false;
        foreach (var attr in symbol.GetAttributes())
        {
            var name = attr.AttributeClass?.Name;
            if (name == "ProtoIncludeAttribute" && attr.ConstructorArguments.Length >= 2)
            {
                var tag = (int)(attr.ConstructorArguments[0].Value ?? 0);
                if (attr.ConstructorArguments[1].Value is INamedTypeSymbol knownType)
                {
                    includes.Add(new ProtoIncludeModel(tag, knownType.ToDisplayString()));
                }
            }
            else if (name == "ProtoContractAttribute")
            {
                foreach (var named in attr.NamedArguments)
                {
                    if (named.Key == "EnumPassthru" && named.Value.Value is bool ep) enumPassthru = ep;
                }
            }
        }

        var ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
        var hint = symbol.ToDisplayString().Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_');

        var containing = ImmutableArray.CreateBuilder<string>();
        for (var outer = symbol.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            containing.Insert(0, outer.Name);
        }

        return new TypeModel(
            ns,
            symbol.Name,
            symbol.TypeKind == TypeKind.Struct ? "struct" : "class",
            hint,
            symbol.ToDisplayString(),
            enumPassthru,
            containing.ToImmutable(),
            members.ToImmutable(),
            includes.ToImmutable(),
            callbacks.ToImmutable());
    }

    private static ProtoMemberModel? FindProtoMember(ISymbol memberSymbol)
    {
        foreach (var attr in memberSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "ProtoMemberAttribute") continue;
            if (attr.ConstructorArguments.Length == 0) continue;

            var tag = attr.ConstructorArguments[0].Value is int t ? t : 0;
            if (tag <= 0) return null;

            string? name = null;
            bool isPacked = false;
            bool isRequired = false;
            string? dataFormat = null;

            foreach (var named in attr.NamedArguments)
            {
                switch (named.Key)
                {
                    case "Name" when named.Value.Value is string s: name = s; break;
                    case "IsPacked" when named.Value.Value is bool ip: isPacked = ip; break;
                    case "IsRequired" when named.Value.Value is bool ir: isRequired = ir; break;
                    case "DataFormat" when named.Value.Value is int df: dataFormat = MapDataFormat(df); break;
                }
            }

            return new ProtoMemberModel(tag, memberSymbol.Name, name, isPacked, isRequired, dataFormat);
        }
        return null;
    }

    private static string MapDataFormat(int value) => value switch
    {
        0 => "Default",
        1 => "ZigZag",
        2 => "TwosComplement",
        3 => "FixedSize",
        4 => "Group",
        5 => "WellKnown",
        _ => "Default",
    };

    private static CallbackModel? FindCallback(IMethodSymbol method)
    {
        foreach (var attr in method.GetAttributes())
        {
            var kind = attr.AttributeClass?.Name switch
            {
                "ProtoBeforeSerializationAttribute" => "ProtoBeforeSerialization",
                "ProtoAfterSerializationAttribute" => "ProtoAfterSerialization",
                "ProtoBeforeDeserializationAttribute" => "ProtoBeforeDeserialization",
                "ProtoAfterDeserializationAttribute" => "ProtoAfterDeserialization",
                _ => null,
            };
            if (kind is not null)
            {
                return new CallbackModel(kind, method.Name, method.Parameters.Length > 0);
            }
        }
        return null;
    }

    private static string Emit(TypeModel t)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable disable");

        if (t.Namespace is not null)
        {
            sb.Append("namespace ").Append(t.Namespace).AppendLine(";");
            sb.AppendLine();
        }

        if (t.Kind == "class")
        {
            EmitClassDecoration(sb, t);
        }
        else
        {
            EmitStructRuntimeRegistration(sb, t);
        }

        return sb.ToString();
    }

    private static void EmitClassDecoration(StringBuilder sb, TypeModel t)
    {
        foreach (var outer in t.ContainingTypes)
        {
            sb.Append("partial class ").AppendLine(outer);
            sb.AppendLine("{");
        }

        foreach (var inc in t.Includes)
        {
            sb.Append("[global::ProtoBuf.ProtoInclude(")
              .Append(inc.Tag).Append(", typeof(global::").Append(inc.KnownTypeFqn).AppendLine("))]");
        }
        sb.Append("[global::ProtoBuf.ProtoContract(ImplicitFields = global::ProtoBuf.ImplicitFields.None");
        if (t.EnumPassthru) sb.Append(", EnumPassthru = true");
        sb.AppendLine(")]");

        foreach (var m in t.Members)
        {
            sb.Append("[global::ProtoBuf.ProtoPartialMember(")
              .Append(m.Tag).Append(", nameof(").Append(m.MemberName).Append(')');
            if (m.Name is not null) sb.Append(", Name = \"").Append(m.Name).Append('"');
            if (m.IsPacked) sb.Append(", IsPacked = true");
            if (m.IsRequired) sb.Append(", IsRequired = true");
            if (m.DataFormat is not null && m.DataFormat != "Default")
                sb.Append(", DataFormat = global::ProtoBuf.DataFormat.").Append(m.DataFormat);
            sb.AppendLine(")]");
        }

        sb.Append("partial class ").AppendLine(t.Name);
        sb.AppendLine("{");

        int forwarderIndex = 0;
        foreach (var cb in t.Callbacks)
        {
            sb.Append("    [global::ProtoBuf.").Append(cb.AttributeName).AppendLine("]");
            sb.Append("    private void __OracleCompat_").Append(cb.AttributeName).Append('_').Append(forwarderIndex++).AppendLine("()");
            sb.AppendLine("    {");
            if (cb.HasParameters)
            {
                sb.Append("        // Source method '").Append(cb.SourceMethodName).AppendLine("' has parameters; oracle invokes parameterless forwarder only.");
            }
            else
            {
                sb.Append("        ").Append(cb.SourceMethodName).AppendLine("();");
            }
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        foreach (var _ in t.ContainingTypes) sb.AppendLine("}");
    }

    private static void EmitStructRuntimeRegistration(StringBuilder sb, TypeModel t)
    {
        // ProtoPartialMember is class-only, so structs are registered at runtime through RuntimeTypeModel.
        var safeName = t.FullName.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_');
        sb.Append("internal static class __OracleCompat_").Append(safeName).AppendLine("_Init");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    internal static void Register()");
        sb.AppendLine("    {");
        sb.Append("        var __mt = global::ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(global::")
          .Append(t.FullName).AppendLine("), false);");
        sb.AppendLine("        __mt.UseConstructor = false;");
        foreach (var m in t.Members)
        {
            sb.Append("        __mt.Add(").Append(m.Tag).Append(", \"").Append(m.MemberName).AppendLine("\");");
        }
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    private sealed record TypeModel(
        string? Namespace,
        string Name,
        string Kind,
        string HintName,
        string FullName,
        bool EnumPassthru,
        ImmutableArray<string> ContainingTypes,
        ImmutableArray<ProtoMemberModel> Members,
        ImmutableArray<ProtoIncludeModel> Includes,
        ImmutableArray<CallbackModel> Callbacks);

    private sealed record ProtoMemberModel(int Tag, string MemberName, string? Name, bool IsPacked, bool IsRequired, string? DataFormat);

    private sealed record ProtoIncludeModel(int Tag, string KnownTypeFqn);

    private readonly record struct CallbackModel(string AttributeName, string SourceMethodName, bool HasParameters);
}
