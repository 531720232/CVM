using CVM.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal interface ISymbol1
    {
        ImmutableArray<Location> Locations { get; set; }
    }
}