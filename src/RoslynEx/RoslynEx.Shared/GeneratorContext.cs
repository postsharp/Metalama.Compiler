using System;
using Microsoft.CodeAnalysis;

namespace RoslynEx
{
    /// <summary>
    /// Context passed to a source generator when <see cref="ISourceGenerator.Execute(GeneratorContext)"/> is called.
    /// </summary>
    public readonly struct GeneratorContext
    {
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Action<string, string> _addSource;

        internal GeneratorContext(Compilation compilation, Action<Diagnostic> reportDiagnostic, Action<string, string> addSource)
        {
            Compilation = compilation;
            _reportDiagnostic = reportDiagnostic;
            _addSource = addSource;
        }

        /// <summary>
        /// Get the current <see cref="Compilation"/> at the time of execution.
        /// </summary>
        public Compilation Compilation { get; }

        /// <summary>
        /// Adds source code in the form of a <see cref="string"/> to the compilation.
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="source">The source code to be add to the compilation</param>
        public void AddSource(string hintName, string source) => _addSource(hintName, source);

        /// <summary>
        /// Adds a <see cref="Diagnostic"/> to the user's compilation.
        /// </summary>
        /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
        /// <remarks>
        /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
        /// </remarks>
        public void ReportDiagnostic(Diagnostic diagnostic) => _reportDiagnostic(diagnostic);
    }
}
