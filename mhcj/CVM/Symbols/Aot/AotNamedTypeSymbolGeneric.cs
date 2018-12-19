using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CVM.Collections.Immutable;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class AotNamedTypeSymbolGeneric : AotNamedTypeSymbol
    {
        private ModuleSymbol aot;
        private NamespaceOrTypeSymbol ab;
        private Type type;
        private string em;
        private Type[] gen;
        private int arity;

        public AotNamedTypeSymbolGeneric(AotModuleSymbol aot, NamespaceOrTypeSymbol ab, Type type, string em, Type[] gen, int arity):base(aot,ab,type,em,arity)
        {
            this.aot = aot;
            this.ab = ab;
            this.type = type;
            this.em = em;
            this.gen = gen;
            this.arity = arity;
        }

        public override int Arity => throw new NotImplementedException();

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => throw new NotImplementedException();

        public override NamedTypeSymbol ConstructedFrom => throw new NotImplementedException();

        public override bool MightContainExtensionMethods => throw new NotImplementedException();


        public override IEnumerable<string> MemberNames => throw new NotImplementedException();

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

      



        public override ImmutableArray<Location> Locations => throw new NotImplementedException();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new NotImplementedException();

  
   

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics => throw new NotImplementedException();

        internal override bool MangleName => throw new NotImplementedException();

        internal override int MetadataArity => throw new NotImplementedException();

        public override ImmutableArray<Symbol> GetMembers()
        {
            throw new NotImplementedException();
        }

    
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            throw new NotImplementedException();
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
        {
            throw new NotImplementedException();
        }
    }
}