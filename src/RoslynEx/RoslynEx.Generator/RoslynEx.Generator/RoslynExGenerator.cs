using System.Reflection;
using Microsoft.CodeAnalysis;
using Roslyn = Microsoft.CodeAnalysis;

namespace RoslynEx
{
    [Generator]
    class RoslynExGenerator : Roslyn::ISourceGenerator
    {
        public void Initialize(InitializationContext context) { }

        public void Execute(SourceGeneratorContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RoslynEx_Analyzers", out var analyzersString);

            // TODO: this is probably not enough
            var analyzerPaths = analyzersString.Split(';');
            foreach (var analyzerPath in analyzerPaths)
            {
                // TODO: this is probably not enough
                var analyzerAssembly = Assembly.LoadFile(analyzerPath);
            }
        }
    }
}
