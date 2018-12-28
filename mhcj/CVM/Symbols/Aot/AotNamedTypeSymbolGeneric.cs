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
        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        bool m_name;
        Type[] _genericParameterHandles;
        public AotNamedTypeSymbolGeneric(AotModuleSymbol aot, NamespaceOrTypeSymbol ab, Type type, string em, Type[] gen, int arity, out bool mangleName) :base(aot,ab,type,em,arity, out  mangleName)
        {
            this.aot = aot;
            this.ab = ab;
            this.type = type;
            this.em = em;
            this.gen = gen;
            this.arity = arity;
            _genericParameterHandles= type.GetGenericArguments();
            m_name = mangleName;
        }

        public override int Arity => arity;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                EnsureTypeParametersAreLoaded();
                return _lazyTypeParameters;
            }
        }
        private void EnsureTypeParametersAreLoaded()
        {
            if (_lazyTypeParameters.IsDefault)
            {
                var moduleSymbol = ContainingAotModule;

                // If this is a nested type generic parameters in metadata include generic parameters of the outer types.
                int firstIndex = _genericParameterHandles.Length - arity;

                TypeParameterSymbol[] ownedParams = new TypeParameterSymbol[arity];
                for (int i = 0; i < ownedParams.Length; i++)
                {
                    ownedParams[i] = new AotTypeParameterSymbol(moduleSymbol, this, (ushort)i, _genericParameterHandles[firstIndex + i]);
                }

                ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameters,
                    ImmutableArray.Create<TypeParameterSymbol>(ownedParams));
            }
        }






      





  
   

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics => GetTypeParametersAsTypeArguments();

        internal override bool MangleName => m_name;

        internal override int MetadataArity => arity;

        public override ImmutableArray<Symbol> GetMembers()
        {
            throw new NotImplementedException();
        }

    
        //public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        //{
        //    throw new NotImplementedException();
        //}

        //public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        //{
        //    throw new NotImplementedException();
        //}

        //public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        //{
        //    throw new NotImplementedException();
        //}

        //internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        //{
        //    throw new NotImplementedException();
        //}

        //internal override AttributeUsageInfo GetAttributeUsageInfo()
        //{
        //    throw new NotImplementedException();
        //}

        //internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        //{
        //    throw new NotImplementedException();
        //}

        //internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        //{
        //    throw new NotImplementedException();
        //}

        //internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        //{
        //    throw new NotImplementedException();
        //}

        //internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        //{
        //    throw new NotImplementedException();
        //}

        //internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
        //{
        //    throw new NotImplementedException();
        //}
    }
}