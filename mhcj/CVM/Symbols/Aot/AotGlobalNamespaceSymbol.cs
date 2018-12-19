// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class AotGlobalNamespaceSymbol
        : AotNamespaceSymbol
    {
        AotModuleSymbol _moduleSymbol;
        /// <summary>
        /// The module containing the namespace.
        /// </summary>
        /// <remarks></remarks>
        internal override AotModuleSymbol ContainingAotModule
        {
            get
            {
                return _moduleSymbol;
            }
        }

        internal AotGlobalNamespaceSymbol(AotModuleSymbol moduleSymbol)
        {
           
            Debug.Assert((object)moduleSymbol != null);
            _moduleSymbol = moduleSymbol;
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _moduleSymbol;
            }
        }

       

        public override string Name
        {
            get
            {
                return string.Empty;
            }
        }

        public override bool IsGlobalNamespace
        {
            get
            {
                return true;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _moduleSymbol.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _moduleSymbol;
            }
        }

        protected override void EnsureAllMembersLoaded()
        {
            if (lazyTypes == null || lazyNamespaces == null)
            {
                IEnumerable<IGrouping<string, Type>> groups;

                try
                {
                    groups = _moduleSymbol.GroupTypesByNamespaceOrThrow();
                }
                catch (BadImageFormatException)
                {
                    groups = SpecializedCollections.EmptyEnumerable<IGrouping<string, Type>>();
                }

                LoadAllMembers(groups);
            }
        }
        internal void AutoBind()
        {
            EnsureAllMembersLoaded();
        }


        internal sealed override CVM_Zone DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
