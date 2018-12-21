using CVM.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal struct ParamInfo<TypeSymbol>
        where TypeSymbol : class
    {
        internal bool IsByRef;
        internal TypeSymbol Type;
        internal System.Reflection.ParameterInfo Handle; // may be nil
        internal ImmutableArray<ModifierInfo<TypeSymbol>> RefCustomModifiers;
        internal ImmutableArray<ModifierInfo<TypeSymbol>> CustomModifiers;
    }
}