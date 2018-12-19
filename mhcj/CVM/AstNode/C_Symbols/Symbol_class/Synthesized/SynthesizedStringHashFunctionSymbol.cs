using CVM.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated synthesized method symbol
    /// representing string switch hash function
    /// </summary>
    internal sealed partial class SynthesizedStringSwitchHashMethod : SynthesizedGlobalMethodSymbol
    {
        internal SynthesizedStringSwitchHashMethod(SourceModuleSymbol containingModule, PrivateImplementationDetails privateImplType, TypeSymbol returnType, TypeSymbol paramType)
            : base(containingModule, privateImplType, returnType, PrivateImplementationDetails.SynthesizedStringHashFunctionName)
        {
            this.SetParameters(ImmutableArray.Create<ParameterSymbol>(SynthesizedParameterSymbol.Create(this, TypeSymbolWithAnnotations.Create(paramType), 0, RefKind.None, "s")));
        }
    }
}
