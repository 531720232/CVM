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

    //    private ModuleSymbol aot;
   //     private NamespaceOrTypeSymbol _container;
   //     private Type type;
   //     private string em;

        internal AotNamedTypeSymbolNonGeneric(AotModuleSymbol aot, NamespaceOrTypeSymbol ab, Type type, string em, out bool m) :base(aot,ab,type,em,0,out m )
        {
     //       this.aot = aot;
     //       this._container = ab;
    //        this.type = type;
    //        this.em = em;
        }
     
        public override int Arity
        {
            get
            {
                return 0;
            }
        }












  

        internal override bool MangleName
        {
            get
            {
                return false;
            }
        }

        internal override int MetadataArity
        {
            get
            {
                var containingType = _container as AotNamedTypeSymbol;
                return (object)containingType == null ? 0 : containingType.MetadataArity;
            }
        }

  




    

     

     

      
    }
}