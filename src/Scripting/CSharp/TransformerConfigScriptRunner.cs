using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Scripting;

namespace RoslynEx
{
    class TransformerConfigScriptRunner : CSharpCompiler.ITransformerConfigScriptRunner
    {
        public ImmutableArray<object> RunConfigScript(string configFile, AnalyzerConfigOptions options)
        {
            using var configFileStream = File.OpenRead(configFile);

            var script = CSharpScript.Create<object[]>(configFileStream, ScriptOptions.Default.WithFilePath(configFile), typeof(TransformerConfigScriptGlobals));

            return script.RunAsync(new TransformerConfigScriptGlobals(options)).Result.ReturnValue.ToImmutableArray();
        }
    }

    public class TransformerConfigScriptGlobals
    {
        internal TransformerConfigScriptGlobals(AnalyzerConfigOptions options) => properties = new TransformerProperties(options);

        public ITransformerProperties properties { get; }

        class TransformerProperties : ITransformerProperties
        {
            private readonly AnalyzerConfigOptions _options;

            public TransformerProperties(AnalyzerConfigOptions options) => _options = options;

            static string FixKey(string key) => "build_property." + key;

            public T Get<T>(string name)
            {
                _options.TryGetValue(FixKey(name), out var stringValue);

                if (string.IsNullOrEmpty(stringValue))
                    return default;

                return (T)Convert.ChangeType(stringValue, typeof(T), CultureInfo.InvariantCulture);
            }
        }
    }
}
