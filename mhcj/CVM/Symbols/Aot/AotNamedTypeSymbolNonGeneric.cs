using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CVM.Collections.Immutable;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class AotNamedTypeSymbolNonGeneric : AotNamedTypeSymbol
    {
        private ModuleSymbol aot;
        private NamespaceOrTypeSymbol ab;
        private Type type;
        private string em;

        internal AotNamedTypeSymbolNonGeneric(AotModuleSymbol aot, NamespaceOrTypeSymbol ab, Type type, string em):base(aot,ab,type,em,0)
        {
            this.aot = aot;
            this.ab = ab;
            this.type = type;
            this.em = em;
        }
        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => throw new NotImplementedException();


        public override bool MightContainExtensionMethods => throw new NotImplementedException();


        public override IEnumerable<string> MemberNames => throw new NotImplementedException();

        public override Accessibility DeclaredAccessibility =>Accessibility.Public;

        public override bool IsSerializable => throw new NotImplementedException();



        public override ImmutableArray<Location> Locations => throw new NotImplementedException();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new NotImplementedException();

  

        internal override bool MangleName
        {
            get
            {
                return false;
            }
        }

        internal override int MetadataArity => throw new NotImplementedException();

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                return ImmutableArray<TypeSymbolWithAnnotations>.Empty;
            }
        }





    

     

        public override ImmutableArray<Symbol> GetMembers()
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