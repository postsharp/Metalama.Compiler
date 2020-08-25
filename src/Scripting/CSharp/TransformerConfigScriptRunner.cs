using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RoslynEx
{
    class TransformerConfigScriptRunner : CSharpCompiler.ITransformerConfigScriptRunner
    {
        public ImmutableArray<object> RunConfigScript(string configFile, AnalyzerConfigOptions options)
        {
            return ImmutableArray.Create<object>();
        }
    }
}
