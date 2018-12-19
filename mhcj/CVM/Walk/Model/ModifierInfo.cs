using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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