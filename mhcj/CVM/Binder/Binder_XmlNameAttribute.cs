﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CVM.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        // Diagnostics are generated in a separate pass when we emit.
        internal ImmutableArray<Symbol> BindXmlNameAttribute(XmlNameAttributeSyntax syntax, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var identifier = syntax.Identifier;

            if (identifier.IsMissing)
            {
                return ImmutableArray<Symbol>.Empty;
            }

            var name = identifier.Identifier.ValueText;

            var lookupResult = LookupResult.GetInstance();
            this.LookupSymbolsWithFallback(lookupResult, name, arity: 0, useSiteDiagnostics: ref useSiteDiagnostics);

            if (lookupResult.Kind == LookupResultKind.Empty)
            {
                lookupResult.Free();
                return ImmutableArray<Symbol>.Empty;
            }

            // If we found something, it must be viable, since only parameters or type parameters
            // of the current member are considered.
            Debug.Assert(lookupResult.IsMultiViable);

            ArrayBuilder<Symbol> lookupSymbols = lookupResult.Symbols;

            Debug.Assert(lookupSymbols[0].Kind == SymbolKind.TypeParameter || lookupSymbols[0].Kind == SymbolKind.Parameter);
            Debug.Assert(lookupSymbols.All(sym => sym.Kind == lookupSymbols[0].Kind));

            // We can sort later when we disambiguate.
            ImmutableArray<Symbol> result = lookupSymbols.ToImmutable();

            lookupResult.Free();

            return result;
        }
    }
}
