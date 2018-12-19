﻿namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a using alias (Imports alias in Visual Basic).
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAliasSymbol : ISymbol
    {
        /// <summary>
        /// Gets the <see cref="INamespaceOrTypeSymbol"/> for the
        /// namespace or type referenced by the alias.
        /// </summary>
        INamespaceOrTypeSymbol Target { get; }
    }
}
