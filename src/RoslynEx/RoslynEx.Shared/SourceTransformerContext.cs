using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace RoslynEx
{
    /// <summary>
    /// Context passed to a source transformer when <see cref="ISourceTransformer.Execute(SourceTransformerContext)"/> is called.
    /// </summary>
    public readonly struct SourceTransformerContext
    {
#if !ROSLYNEX_INTERFACE
        private readonly DiagnosticBag _diagnostics;
        private readonly Dictionary<Type, object> _options;

        internal SourceTransformerContext(Compilation compilation, DiagnosticBag diagnostics, ImmutableArray<object> configOptions)
        {
            Compilation = compilation;
            _diagnostics = diagnostics;
            _options = configOptions.ToDictionary(o => o.GetType(), o => o);
        }
#endif

        /// <summary>
        /// Get the current <see cref="Compilation"/> at the time of execution.
        /// </summary>
        public Compilation Compilation { get; }

        /// <summary>
        /// Adds a <see cref="Diagnostic"/> to the user's compilation.
        /// </summary>
        /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
        /// <remarks>
        /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
        /// </remarks>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
#if !ROSLYNEX_INTERFACE
            _diagnostics.Add(diagnostic);
#endif
        }

        public T GetOptions<T>()
        {
#if ROSLYNEX_INTERFACE
            return default;
#else
            _options.TryGetValue(typeof(T), out var value);
            return (T)value;
#endif
        }
    }
}
