using System;
using System.Collections.Generic;
using System.Linq;
using GProtobuf.Generator.Analysis;
using GProtobuf.Generator.Utilities;
using GProtobuf.Generator.WireFormat;

namespace GProtobuf.Generator.V2.CodeGeneration.Core
{
    /// <summary>Shared deferred-init polymorphic dispatcher used by Span and Stream readers.</summary>
    internal static class DeferredInitDispatcher
    {
        /// <summary>Per-derived metadata: ProtoInclude, own members, and identifier-safe temp prefix.</summary>
        internal sealed class DerivedInfo
        {
            public ProtoIncludeInfo Include = null!;
            public IReadOnlyList<ProtoMemberInfo> OwnMembers = null!;
            public string TempPrefix = null!;
        }

        /// <summary>Per-reader specialization: wrapper region open/close, inner reader var, target-resolver scope.</summary>
        internal sealed class Strategy
        {
            public Action<ProtoMemberInfo, string /*wireTypeVar*/, string /*readerVar*/> GenerateFieldReadCase = null!;
            public Action EmitOpenWrapperRegion = null!;
            public Action EmitCloseWrapperRegion = null!;
            public string InnerReaderVar = null!;
            public Func<Func<string, string>, IDisposable> ScopedTargetResolver = null!;
        }

        /// <summary>Builds a stable, identifier-safe temp prefix from a derived index (e.g. "d0").</summary>
        public static string MakeTempPrefix(int derivedIndex) => "d" + derivedIndex;

        /// <summary>Builds the per-derived metadata list for the polymorphic root, in declaration order.</summary>
        public static List<DerivedInfo> CollectDerivedInfos(TypeRegistry registry, TypeDefinition rootType)
        {
            var result = new List<DerivedInfo>();
            if (rootType.ProtoIncludes == null)
                return result;

            int derivedIndex = 0;
            foreach (var include in rootType.ProtoIncludes)
            {
                var derivedType = registry.GetByFullName(include.Type);
                if (derivedType == null) { derivedIndex++; continue; }

                result.Add(new DerivedInfo
                {
                    Include = include,
                    OwnMembers = registry.GetOwnProtoMembers(include.Type),
                    TempPrefix = MakeTempPrefix(derivedIndex),
                });
                derivedIndex++;
            }
            return result;
        }

        /// <summary>Emits the entire deferred-init dispatcher body for a polymorphic root type.</summary>
        public static void EmitDispatcherBody(
            StringBuilderWithIndent sb,
            TypeDefinition rootType,
            IReadOnlyList<DerivedInfo> derivedInfos,
            Strategy strategy)
        {
            var baseMembers = rootType.ProtoMembers ?? new List<ProtoMemberInfo>();

            // Temp locals for base fields.
            foreach (var member in baseMembers)
            {
                var declaredTypeName = TypeMapping.GetGlobalGenericTypeName(member.Type);
                sb.AppendIndentedLine($"{declaredTypeName} tmp_{member.Name} = default;");
            }

            // Per-derived seen flag + temp locals for derived's own members.
            foreach (var d in derivedInfos)
            {
                sb.AppendIndentedLine($"bool seen_{d.TempPrefix} = false;");
                foreach (var member in d.OwnMembers)
                {
                    var declaredTypeName = TypeMapping.GetGlobalGenericTypeName(member.Type);
                    sb.AppendIndentedLine($"{declaredTypeName} tmp_{d.TempPrefix}_{member.Name} = default;");
                }
            }

            sb.AppendNewLine();

            // Outer read loop: dispatch base fields and ProtoInclude wrapper cases.
            sb.AppendIndentedLine("while (!reader.IsEnd)");
            sb.StartNewBlock();
            sb.AppendIndentedLine("reader.ReadWireTypeAndFieldId(out var wireType, out var fieldId);");
            sb.AppendNewLine();
            sb.AppendIndentedLine("switch (fieldId)");
            sb.StartNewBlock();

            foreach (var d in derivedInfos)
            {
                EmitWrapperCase(sb, d, strategy);
            }

            // Base field cases (read into base temp locals).
            if (baseMembers.Count > 0)
            {
                using (strategy.ScopedTargetResolver(name => $"tmp_{name}"))
                {
                    foreach (var member in GeneratorHelpers.GetSortedFieldsForDispatch(baseMembers))
                    {
                        strategy.GenerateFieldReadCase(member, "wireType", "reader");
                    }
                }
            }

            sb.AppendIndentedLine("default:");
            sb.IncreaseIndent();
            sb.AppendIndentedLine("reader.SkipField(wireType);");
            sb.AppendIndentedLine("break;");
            sb.DecreaseIndent();

            sb.EndBlock(); // outer switch
            sb.EndBlock(); // outer while

            sb.AppendNewLine();

            // Cascading construction: first matching seen flag wins; concrete base is the fallback.
            foreach (var d in derivedInfos)
            {
                sb.AppendIndentedLine($"if (seen_{d.TempPrefix})");
                sb.StartNewBlock();
                EmitObjectInitializerReturn(sb, d.Include.Type, baseMembers, d.OwnMembers, d.TempPrefix);
                sb.EndBlock();
            }

            if (!rootType.IsAbstract)
            {
                EmitObjectInitializerReturn(sb, rootType.FullName, baseMembers, null, null);
            }
            else
            {
                sb.AppendIndentedLine($"throw new global::System.InvalidOperationException(\"Cannot deserialize abstract type '{rootType.FullName}': no ProtoInclude wrapper found in wire data.\");");
            }
        }

        private static void EmitWrapperCase(StringBuilderWithIndent sb, DerivedInfo d, Strategy strategy)
        {
            sb.AppendIndentedLine($"case {d.Include.FieldId}: {{");
            sb.IncreaseIndent();
            sb.AppendIndentedLine($"seen_{d.TempPrefix} = true;");
            sb.AppendIndentedLine("var wrapperLength = reader.ReadVarInt32();");
            strategy.EmitOpenWrapperRegion();

            // Redirect derived field reads into this derived's temp pack.
            var prefix = d.TempPrefix;
            using (strategy.ScopedTargetResolver(name => $"tmp_{prefix}_{name}"))
            {
                sb.AppendIndentedLine($"while (!{strategy.InnerReaderVar}.IsEnd)");
                sb.StartNewBlock();
                sb.AppendIndentedLine($"{strategy.InnerReaderVar}.ReadWireTypeAndFieldId(out var wireType_w, out var fieldId_w);");
                sb.AppendNewLine();
                if (d.OwnMembers.Count > 0)
                {
                    sb.AppendIndentedLine("switch (fieldId_w)");
                    sb.StartNewBlock();
                    foreach (var member in GeneratorHelpers.GetSortedFieldsForDispatch(d.OwnMembers.ToList()))
                    {
                        strategy.GenerateFieldReadCase(member, "wireType_w", strategy.InnerReaderVar);
                    }
                    sb.AppendIndentedLine("default:");
                    sb.IncreaseIndent();
                    sb.AppendIndentedLine($"{strategy.InnerReaderVar}.SkipField(wireType_w);");
                    sb.AppendIndentedLine("break;");
                    sb.DecreaseIndent();
                    sb.EndBlock(); // inner switch
                }
                else
                {
                    sb.AppendIndentedLine($"{strategy.InnerReaderVar}.SkipField(wireType_w);");
                }
                sb.EndBlock(); // inner while
            }

            strategy.EmitCloseWrapperRegion();
            sb.AppendIndentedLine("break;");
            sb.DecreaseIndent();
            sb.AppendIndentedLine("}");
        }

        /// <summary>Emits `return new {targetType} { Field = tmp_..., ... };` combining base + derived temps.</summary>
        public static void EmitObjectInitializerReturn(
            StringBuilderWithIndent sb,
            string targetTypeFullName,
            IList<ProtoMemberInfo> baseMembers,
            IReadOnlyList<ProtoMemberInfo>? derivedOwnMembers,
            string? derivedTempPrefix)
        {
            int total = baseMembers.Count + (derivedOwnMembers?.Count ?? 0);
            if (total == 0)
            {
                sb.AppendIndentedLine($"return new global::{targetTypeFullName}();");
                return;
            }

            sb.AppendIndentedLine($"return new global::{targetTypeFullName}");
            sb.AppendIndentedLine("{");
            sb.IncreaseIndent();

            foreach (var member in baseMembers)
            {
                sb.AppendIndentedLine($"{member.Name} = tmp_{member.Name},");
            }
            if (derivedOwnMembers != null)
            {
                foreach (var member in derivedOwnMembers)
                {
                    sb.AppendIndentedLine($"{member.Name} = tmp_{derivedTempPrefix}_{member.Name},");
                }
            }

            sb.DecreaseIndent();
            sb.AppendIndentedLine("};");
        }
    }
}
