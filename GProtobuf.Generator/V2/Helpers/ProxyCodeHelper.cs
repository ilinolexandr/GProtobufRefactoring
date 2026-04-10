using GProtobuf.Generator.Analysis;
using GProtobuf.Generator.V2.CodeGeneration.Core;
using GProtobuf.Generator.V2.Helpers;
using GProtobuf.Generator.WireFormat;

namespace GProtobuf.Generator.V2.Helpers
{
    /// <summary>
    /// Centralized helper for proxy-related code generation.
    /// Eliminates duplication of proxy lookup, namespace prefix, read/write/size patterns
    /// that were previously scattered across 6+ files.
    /// </summary>
    internal class ProxyCodeHelper
    {
        private readonly StringBuilderWithIndent _sb;
        private readonly ProxyRegistry _proxyRegistry;
        private readonly TypeRegistry _typeRegistry;

        public ProxyCodeHelper(StringBuilderWithIndent sb, ProxyRegistry proxyRegistry, TypeRegistry typeRegistry = null)
        {
            _sb = sb;
            _proxyRegistry = proxyRegistry;
            _typeRegistry = typeRegistry;
        }

        #region Lookup

        /// <summary>
        /// Single source of truth for proxy lookup with nullable type stripping.
        /// </summary>
        public ProxyDefinition GetProxy(string typeName)
        {
            if (_proxyRegistry == null) return null;
            var result = _proxyRegistry.GetProxy(typeName);
            if (result != null) return result;
            if (typeName != null && typeName.EndsWith("?"))
                result = _proxyRegistry.GetProxy(typeName.TrimEnd('?'));
            return result;
        }

        #endregion

        #region Namespace

        /// <summary>
        /// Returns the fully qualified serialization namespace prefix for the proxy type.
        /// e.g. "global::MyNamespace.Serialization." or "" if proxy has no namespace.
        /// </summary>
        public static string GetQualifiedPrefix(ProxyDefinition proxy)
        {
            return string.IsNullOrEmpty(proxy.ProxyNamespace)
                ? ""
                : $"global::{proxy.ProxyNamespace}.Serialization.";
        }

        #endregion

        #region Emit helpers

        /// <summary>
        /// Emits proxy.Return() call if the proxy defines a [ProxyReturn] method.
        /// </summary>
        public void EmitReturn(ProxyDefinition proxy, string proxyVarName)
        {
            if (proxy?.ReturnMethodName != null)
                _sb.AppendIndentedLine($"{proxyVarName}.{proxy.ReturnMethodName}();");
        }

        /// <summary>
        /// Emits: var {proxyVar} = global::{ProxyType}.{CreateMethod}({sourceVar}{extraArgs});
        /// </summary>
        public void EmitCreate(ProxyDefinition proxy, string sourceVar, string proxyVar)
        {
            _sb.AppendIndentedLine($"var {proxyVar} = global::{proxy.ProxyTypeFullName}.{proxy.WrapMethodName}({sourceVar}{proxy.WrapExtraArgs});");
        }

        /// <summary>
        /// Emits: var {resultVar} = {proxyVar}.{ConvertMethod}();
        /// </summary>
        public void EmitConvert(ProxyDefinition proxy, string proxyVar, string resultVar)
        {
            _sb.AppendIndentedLine($"var {resultVar} = {proxyVar}.{proxy.ConvertMethodName}();");
        }

        /// <summary>
        /// Emits: var {resultVar} = {prefix}SpanReaders.Read{ProxyClassName}Content(ref {readerVar});
        /// </summary>
        public void EmitSpanRead(ProxyDefinition proxy, string readerVar, string resultVar)
        {
            var prefix = GetQualifiedPrefix(proxy);
            _sb.AppendIndentedLine($"var {resultVar} = {prefix}SpanReaders.Read{proxy.ProxyClassName}Content(ref {readerVar});");
        }

        /// <summary>
        /// Emits: var {resultVar} = {prefix}StreamReaders.Read{ProxyClassName}Content(ref {readerVar});
        /// </summary>
        public void EmitStreamRead(ProxyDefinition proxy, string readerVar, string resultVar)
        {
            var prefix = GetQualifiedPrefix(proxy);
            _sb.AppendIndentedLine($"var {resultVar} = {prefix}StreamReaders.Read{proxy.ProxyClassName}Content(ref {readerVar});");
        }

        /// <summary>
        /// Emits: {prefix}{writerClassName}.Write{ProxyClassName}{suffix}(ref {writerVar}, {proxyVar});
        /// </summary>
        public void EmitWrite(ProxyDefinition proxy, string writerVar, string proxyVar, string writerClassName)
        {
            var prefix = GetQualifiedPrefix(proxy);
            var suffix = GetWriteMethodSuffix(proxy);
            _sb.AppendIndentedLine($"{prefix}{writerClassName}.Write{proxy.ProxyClassName}{suffix}(ref {writerVar}, {proxyVar});");
        }

        /// <summary>
        /// Emits: {prefix}SizeCalculators.Calculate{ProxyClassName}ContentSize(ref {calcVar}, {proxyVar});
        /// </summary>
        public void EmitSizeCalc(ProxyDefinition proxy, string calcVar, string proxyVar)
        {
            var prefix = GetQualifiedPrefix(proxy);
            _sb.AppendIndentedLine($"{prefix}SizeCalculators.Calculate{proxy.ProxyClassName}ContentSize(ref {calcVar}, {proxyVar});");
        }

        #endregion

        #region Write method suffix

        /// <summary>
        /// Determines whether to use Write{Name} or Write{Name}Content for the proxy type.
        /// Returns "" for simple types or "Content" for types with ProtoIncludes/callbacks.
        /// </summary>
        public string GetWriteMethodSuffix(ProxyDefinition proxy)
        {
            if (_typeRegistry == null) return "";
            return GetWriteMethodSuffix(proxy.ProxyTypeFullName);
        }

        /// <summary>
        /// Determines whether to use Write{Name} or Write{Name}Content for a given type.
        /// </summary>
        public string GetWriteMethodSuffix(string typeName)
        {
            if (_typeRegistry == null) return "Content";
            bool isDerivedType = _typeRegistry.IsDerivedType(typeName);
            if (isDerivedType) return "";
            var typeDef = _typeRegistry.GetByFullName(TypeMapping.NormalizeTypeName(typeName));
            if (typeDef == null) return "Content";
            bool hasProtoIncludes = typeDef.ProtoIncludes != null && typeDef.ProtoIncludes.Count > 0;
            bool hasCallbacks = (typeDef.BeforeSerializationCallbacks != null && typeDef.BeforeSerializationCallbacks.Count > 0)
                || (typeDef.AfterSerializationCallbacks != null && typeDef.AfterSerializationCallbacks.Count > 0);
            return (!hasProtoIncludes && !hasCallbacks) ? "" : "Content";
        }

        #endregion
    }
}
