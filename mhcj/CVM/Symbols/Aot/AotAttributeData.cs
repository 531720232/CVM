// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CVM.Collections.Immutable;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class AotAttributeData : CSharpAttributeData
    {
        private readonly System.Attribute _handle;
        private readonly System.Reflection.ConstructorInfo _ci;
        private NamedTypeSymbol _lazyAttributeClass = ErrorTypeSymbol.UnknownResultType; // Indicates uninitialized.
        private MethodSymbol _lazyAttributeConstructor;
        private ImmutableArray<TypedConstant> _lazyConstructorArguments;
        private ImmutableArray<KeyValuePair<string, TypedConstant>> _lazyNamedArguments;
        private ThreeState _lazyHasErrors = ThreeState.Unknown;
        private AotModuleSymbol containingAotModuleSymbol;
        private object handle;


        public AotAttributeData(AotModuleSymbol containingAotModuleSymbol, System.Attribute handle,System.Reflection.ConstructorInfo ci)
        {
            this.containingAotModuleSymbol = containingAotModuleSymbol;
            this.handle = handle;
            this._ci = ci;

        }
        public override NamedTypeSymbol AttributeClass
        {
            get
            {

             

                EnsureClassAndConstructorSymbolsAreLoaded();
                return _lazyAttributeClass;
            }
        }

        public override MethodSymbol AttributeConstructor
        {
            get
            {
                EnsureClassAndConstructorSymbolsAreLoaded();
                return _lazyAttributeConstructor;
            }
        }

        public override SyntaxReference ApplicationSyntaxReference
        {
            get { return null; }
        }

        internal protected override ImmutableArray<TypedConstant> CommonConstructorArguments
        {
            get
            {
                EnsureAttributeArgumentsAreLoaded();
                return _lazyConstructorArguments;
            }
        }

        private void EnsureAttributeArgumentsAreLoaded()
        {
            if (_lazyConstructorArguments.IsDefault || _lazyNamedArguments.IsDefault)
            {
           //     TypedConstant[] lazyConstructorArguments = null;
             //   KeyValuePair<string, TypedConstant>[] lazyNamedArguments = null;

             var   lazyConstructorArguments = new List<TypedConstant>();
            var    lazyNamedArguments = new List<KeyValuePair<string, TypedConstant>>();
             var ts=   _ci.GetParameters();
                var mo = containingAotModuleSymbol;
                foreach(var p in ts)
                {
                    TypedConstant t1;
                    var t = p.GetType();
                    var n = p.Name;
                //    var typ=new TypedConstant(mo.TypeHandleToTypeMap)
               
                  if ( p.GetType().IsArray)
                    {

                        
                    }
                    else
                    {
                      //  t1 = new TypedConstant(mo.TypeHandleToTypeMap[t]);
                    }
                    
                }
                //if (!_decoder.GetCustomAttribute(_handle, out lazyConstructorArguments, out lazyNamedArguments))
                //{
                //    _lazyHasErrors = ThreeState.True;
                //}

                Debug.Assert(lazyConstructorArguments != null && lazyNamedArguments != null);

                ImmutableInterlocked.InterlockedInitialize(ref _lazyConstructorArguments,
                    ImmutableArray.Create<TypedConstant>(lazyConstructorArguments.ToArray()));

                ImmutableInterlocked.InterlockedInitialize(ref _lazyNamedArguments,
                    ImmutableArray.Create<KeyValuePair<string, TypedConstant>>(lazyNamedArguments.ToArray()));
            }
        }

        internal protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments
        {
            get
            {
                EnsureAttributeArgumentsAreLoaded();
                return _lazyNamedArguments;
            }
        }
        internal override int GetTargetAttributeSignatureIndex(Symbol targetSymbol, AttributeDescription description)
        {
            throw new System.NotImplementedException();
        }

        private void EnsureClassAndConstructorSymbolsAreLoaded()
        {
#pragma warning disable 0252
            if ((object)_lazyAttributeClass == ErrorTypeSymbol.UnknownResultType)
            {
                TypeSymbol attributeClass;
                MethodSymbol attributeConstructor=default;

                
            if(containingAotModuleSymbol.TypeHandleToTypeMap.TryGetValue(_handle.GetType(),out attributeClass))
                    {
                    var mod = new AotMethodSymbol(_ci);
                    attributeConstructor = mod;
                }
                else
                {

                }

                CVM.AHelper.CompareExchange(ref _lazyAttributeConstructor, attributeConstructor, null);
                CVM.AHelper.CompareExchange(ref _lazyAttributeClass, (NamedTypeSymbol)attributeClass, ErrorTypeSymbol.UnknownResultType); // Serves as a flag, so do it last.
            }
#pragma warning restore 0252
        }
    }
}