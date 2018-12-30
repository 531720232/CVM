using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    [StructLayout(LayoutKind.Auto)]
    internal struct ModifierInfo<TypeSymbol>
        where TypeSymbol : class
    {
        internal readonly bool IsOptional;
        internal readonly TypeSymbol Modifier;

        public ModifierInfo(bool isOptional, TypeSymbol modifier)
        {
            IsOptional = isOptional;
            Modifier = modifier;
        }

    }

}