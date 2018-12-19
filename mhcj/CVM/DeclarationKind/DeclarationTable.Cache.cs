// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using CVM.Collections.Immutable;
using CVM.Linq;
namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DeclarationTable
    {
        // The structure of the DeclarationTable provides us with a set of 'old' declarations that
        // stay relatively unchanged and a 'new' declaration that is repeatedly added and removed.
        // This mimics the expected usage pattern of a user repeatedly typing in a single file.
        // Because of this usage pattern, we can cache information about these 'old' declarations
        // and keep that around as long as they do not change.  For example, we keep a single 'merged
        // declaration' for all those root declarations as well as sets of interesting information
        // (like the type names in those decls). 
        private class Cache
        {
            // The merged root declaration for all the 'old' declarations.
            internal readonly Lazy1<MergedNamespaceDeclaration> MergedRoot;

            // All the simple type names for all the types in the 'old' declarations.
            internal readonly Lazy1<HashSet<string>> TypeNames;
            internal readonly Lazy1<HashSet<string>> NamespaceNames;
            internal readonly Lazy1<ImmutableArray<ReferenceDirective>> ReferenceDirectives;

            public Cache(DeclarationTable table)
            {

                var items = table._allOlderRootDeclarations.InInsertionOrder.ToImmutableArray();
                ImmutableArray<SingleNamespaceDeclaration> v2 =  ImmutableArray<SingleNamespaceDeclaration>.Empty;

                foreach(var item in items)
                {
                 v2=   v2.Add(item);
                }
                 this.MergedRoot = new Lazy1<MergedNamespaceDeclaration>(
                    () => MergedNamespaceDeclaration.Create(v2));

                this.TypeNames = new Lazy1<HashSet<string>>(
                    () => GetTypeNames(this.MergedRoot.Value));

                this.NamespaceNames = new Lazy1<HashSet<string>>(
                    () => GetNamespaceNames(this.MergedRoot.Value));

              //  ReferenceDirectives = MergedRoot.Value.Declarations.OfType<RootSingleNamespaceDeclaration>().SelectMany(r => r.ReferenceDirectives).AsImmutable();

            //    this.ReferenceDirectives = new Lazy1<ImmutableArray<ReferenceDirective>>(
            //      () => MergedRoot.Value.Declarations.OfType<RootSingleNamespaceDeclaration>().SelectMany(r => r.ReferenceDirectives).AsImmutable());
            }
        }
    }
}
