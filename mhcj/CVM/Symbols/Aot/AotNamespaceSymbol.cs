using CVM.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class AotNamespaceSymbol : NamespaceSymbol
    {
        /// <summary>
        /// A map of namespaces immediately contained within this namespace 
        /// mapped by their name (case-sensitively).
        /// </summary>
        protected Dictionary<string, AotNestedNamespaceSymbol> lazyNamespaces;

        /// <summary>
        /// A map of types immediately contained within this namespace 
        /// grouped by their name (case-sensitively).
        /// </summary>
        protected Dictionary<string, ImmutableArray<AotNamedTypeSymbol>> lazyTypes;

        private ImmutableArray<AotNamedTypeSymbol> _lazyFlattenedTypes;

        internal sealed override NamespaceExtent Extent
        {
            get
            {
                return new NamespaceExtent(this.ContainingModule);
            }
        }
        protected abstract void EnsureAllMembersLoaded();

        private ImmutableArray<NamedTypeSymbol> GetMemberTypesPrivate()
        {
            //assume that EnsureAllMembersLoaded() has initialize lazyTypes
            if (_lazyFlattenedTypes.IsDefault)
            {
                var flattened = lazyTypes.Flatten();
                ImmutableInterlocked.InterlockedExchange(ref _lazyFlattenedTypes, flattened);
            }

            return StaticCast<NamedTypeSymbol>.From(_lazyFlattenedTypes);
        }

        public sealed override ImmutableArray<Symbol> GetMembers()
        {
            EnsureAllMembersLoaded();

            var memberTypes = GetMemberTypesPrivate();
            var builder = ArrayBuilder<Symbol>.GetInstance(memberTypes.Length + lazyNamespaces.Count);

            builder.AddRange(memberTypes);
            foreach (var pair in lazyNamespaces)
            {
                builder.Add(pair.Value);
            }

            return builder.ToImmutableAndFree();
        }
        public sealed override ImmutableArray<Symbol> GetMembers(string name)
        {
            EnsureAllMembersLoaded();

            AotNestedNamespaceSymbol ns = null;
            ImmutableArray<AotNamedTypeSymbol> t;

            if (lazyNamespaces.TryGetValue(name, out ns))
            {
                if (lazyTypes.TryGetValue(name, out t))
                {
                    // TODO - Eliminate the copy by storing all members and type members instead of non-type and type members?
                    return StaticCast<Symbol>.From(t).Add(ns);
                }
                else
                {
                    return ImmutableArray.Create<Symbol>(ns);
                }
            }
            else if (lazyTypes.TryGetValue(name, out t))
            {
                return StaticCast<Symbol>.From(t);
            }

            return ImmutableArray<Symbol>.Empty;
        }
        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            EnsureAllMembersLoaded();

            return GetMemberTypesPrivate();
        }
        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            EnsureAllMembersLoaded();

            ImmutableArray<AotNamedTypeSymbol> t;

            return lazyTypes.TryGetValue(name, out t)
                ? StaticCast<NamedTypeSymbol>.From(t)
                : ImmutableArray<NamedTypeSymbol>.Empty;
        }
        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return GetTypeMembers(name).WhereAsArray(type => type.Arity == arity);
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get
            {
                return ContainingModule.Locations;
            }
        }
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }
        protected void LoadAllMembers(IEnumerable<IGrouping<string, Type>> typesByNS)
        {

        
            // A sequence of groups of TypeDef row ids for types immediately contained within this namespace.
            IEnumerable<IGrouping<string, Type>> nestedTypes = null;

            // A sequence with information about namespaces immediately contained within this namespace.
            // For each pair:
            //    Key - contains simple name of a child namespace.
            //    Value - contains a sequence similar to the one passed to this function, but
            //            calculated for the child namespace. 
            IEnumerable<KeyValuePair<string, IEnumerable<IGrouping<string, Type>>>> nestedNamespaces = null;
            bool isGlobalNamespace = this.IsGlobalNamespace;

            MetadataHelpers.GetInfoForImmediateNamespaceMembers(
                isGlobalNamespace,
                isGlobalNamespace ? 0 : GetQualifiedNameLength(),
                typesByNS,
                StringComparer.Ordinal,
                out nestedTypes, out nestedNamespaces);
        

            lazyNamespaces =     LazyInitializeNamespaces(nestedNamespaces);

            lazyTypes= LazyInitializeTypes(nestedTypes);
        }
        /// <summary>
        /// Create symbols for nested namespaces and initialize namespaces map.
        /// </summary>
        private Dictionary<string, AotNestedNamespaceSymbol> LazyInitializeNamespaces(
            IEnumerable<KeyValuePair<string, IEnumerable<IGrouping<string, Type>>>> childNamespaces)
        {
            if (this.lazyNamespaces == null)
            {
                var namespaces = new Dictionary<string, AotNestedNamespaceSymbol>();

                foreach (var child in childNamespaces)
                {
                    var c = new AotNestedNamespaceSymbol(child.Key, this, child.Value);
                    namespaces.Add(c.Name, c);
                }

               return  namespaces;
            }
            return lazyNamespaces;
        }

        internal abstract AotModuleSymbol ContainingAotModule { get; }

        /// <summary>
        /// Create symbols for nested types and initialize types map.
        /// </summary>
        private Dictionary<string, ImmutableArray<AotNamedTypeSymbol>> LazyInitializeTypes(IEnumerable<IGrouping<string, Type>> typeGroups)
        {
            if (this.lazyTypes == null)
            {
                var moduleSymbol = ContainingAotModule;

                var children = ArrayBuilder<AotNamedTypeSymbol>.GetInstance();
           

                foreach (var g in typeGroups)
                {
                    foreach (var t in g)
                    {
                     
                            children.Add(AotNamedTypeSymbol.Create(moduleSymbol, this, t, g.Key));
                      
                    }
                }

                var typesDict = children.ToDictionary(c => c.Name, StringOrdinalComparer.Instance);
                children.Free();


                var original = CVM.AHelper.CompareExchange(ref this.lazyTypes, typesDict, null);

                // Build cache of TypeDef Tokens
                // Potentially this can be done in the background.
                if (original == null)
                {
                    moduleSymbol.OnNewTypeDeclarationsLoaded(typesDict);
                }
                return typesDict;
            }
            return lazyTypes;
        }

        private int GetQualifiedNameLength()
        {
            int length = this.Name.Length;

            var parent = ContainingNamespace;
            while (parent?.IsGlobalNamespace == false)
            {
                // add name of the parent + "."
                length += parent.Name.Length + 1;
                parent = parent.ContainingNamespace;
            }

            return length;
        }
        internal NamedTypeSymbol LookupMetadataType(ref MetadataTypeName emittedTypeName, out bool isNoPiaLocalType)
        {
            NamedTypeSymbol result = LookupMetadataType(ref emittedTypeName);
            isNoPiaLocalType = false;


            return result;
        }
    }
}
