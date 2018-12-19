using CVM.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class TypeSymbol
    {
        /// <summary>
        /// Represents the method by which this type implements a given interface type
        /// and/or the corresponding diagnostics.
        /// </summary>
        protected class SymbolAndDiagnostics
        {
            public static readonly SymbolAndDiagnostics Empty = new SymbolAndDiagnostics(null, ImmutableArray<Diagnostic>.Empty);

            public readonly Symbol Symbol;
            public readonly ImmutableArray<Diagnostic> Diagnostics;

            public SymbolAndDiagnostics(Symbol symbol, ImmutableArray<Diagnostic> diagnostics)
            {
                this.Symbol = symbol;
                this.Diagnostics = diagnostics;
            }
        }
    }
}
