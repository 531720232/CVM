﻿using CVM.Collections.Immutable;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class TypeParameterSymbolExtensions
    {
        public static bool DependsOn(this TypeParameterSymbol typeParameter1, TypeParameterSymbol typeParameter2)
        {
            Debug.Assert((object)typeParameter1 != null);
            Debug.Assert((object)typeParameter2 != null);

            Func<TypeParameterSymbol, IEnumerable<TypeParameterSymbol>> dependencies = x => x.ConstraintTypesNoUseSiteDiagnostics.Select(c => c.TypeSymbol).OfType<TypeParameterSymbol>();
            return dependencies.TransitiveClosure(typeParameter1).Contains(typeParameter2);
        }
    }
}
