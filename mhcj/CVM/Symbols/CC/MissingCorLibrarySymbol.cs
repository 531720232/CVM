﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// AssemblySymbol to represent missing, for whatever reason, CorLibrary.
    /// The symbol is created by ReferenceManager on as needed basis and is shared by all compilations
    /// with missing CorLibraries.
    /// </summary>
    internal sealed class MissingCorLibrarySymbol : MissingAssemblySymbol
    {
        internal static readonly MissingCorLibrarySymbol Instance = new MissingCorLibrarySymbol();

        /// <summary>
        /// An array of cached Cor types defined in this assembly.
        /// Lazily filled by GetDeclaredSpecialType method.
        /// </summary>
        /// <remarks></remarks>
        private NamedTypeSymbol[] _lazySpecialTypes;

        private MissingCorLibrarySymbol()
            : base(new AssemblyIdentity("<Missing Core Assembly>"))
        {
            this.SetCorLibrary(this);
        }

        /// <summary>
        /// Lookup declaration for predefined CorLib type in this Assembly. Only should be
        /// called if it is know that this is the Cor Library (mscorlib).
        /// </summary>
        /// <param name="type"></param>
        internal override NamedTypeSymbol GetDeclaredSpecialType(SpecialType type)
        {
//#if DEBUG
//            foreach (var module in this.Modules)
//            {
//                Debug.Assert(module.GetReferencedAssemblies().Length == 0);
//            }
//#endif

            if (_lazySpecialTypes == null)
            {
                CVM.AHelper.CompareExchange(ref _lazySpecialTypes,
                    new NamedTypeSymbol[(int)SpecialType.Count + 1], null);
            }

            if ((object)_lazySpecialTypes[(int)type] == null)
            {
                MetadataTypeName emittedFullName = MetadataTypeName.FromFullName(SpecialTypes.GetMetadataName(type), useCLSCompliantNameArityEncoding: true);
                NamedTypeSymbol corType = new MissingMetadataTypeSymbol.TopLevel(this.moduleSymbol, ref emittedFullName, type);
                CVM.AHelper.CompareExchange(ref _lazySpecialTypes[(int)type], corType, null);
            }

            return _lazySpecialTypes[(int)type];
        }
    }
}
