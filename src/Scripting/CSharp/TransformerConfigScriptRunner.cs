using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Scripting;

namespace RoslynEx
{
    class TransformerConfigScriptRunner : CSharpCompiler.ITransformerConfigScriptRunner
    {
        // TODO: consider running the script under InvariantCulture, possibly using EnsureInvariantCulture
        public ImmutableArray<object> RunConfigScript(string configFile, AnalyzerConfigOptions options)
        {
            using var configFileStream = File.OpenRead(configFile);

            var script = CSharpScript.Create<object[]>(configFileStream, ScriptOptions.Default.WithFilePath(configFile), typeof(TransformerConfigScriptGlobals));

            // TODO: doesn't Roslyn have some special way to do sync-over-async?
            return script.RunAsync(new TransformerConfigScriptGlobals(options)).Result.ReturnValue.ToImmutableArray();
        }
    }

    public class TransformerConfigScriptGlobals
    {
        private readonly OptionsDictionary _properties;

        internal TransformerConfigScriptGlobals(AnalyzerConfigOptions options) => _properties = new OptionsDictionary(options);

        // TODO: use custom interface instead of IReadOnlyDictionary (requires adding extra reference to RoslynEx.csx)
        public IReadOnlyDictionary<string, string> properties => _properties;

        class OptionsDictionary : IReadOnlyDictionary<string, string>
        {
            private readonly AnalyzerConfigOptions _options;

            public OptionsDictionary(AnalyzerConfigOptions options) => _options = options;

            static string FixKey(string key) => "build_property." + key;

            public string this[string key]
            {
                get
                {
                    _options.TryGetValue(FixKey(key), out var value);
                    return value;
                }
            }

            public IEnumerable<string> Keys => throw new NotSupportedException();

            public IEnumerable<string> Values => throw new NotSupportedException();

            public int Count => throw new NotSupportedException();

            public bool ContainsKey(string key) => _options.TryGetValue(FixKey(key), out _);

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => _options.TryGetValue(FixKey(key), out value);

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw new NotSupportedException();

            IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
        }
    }
}
