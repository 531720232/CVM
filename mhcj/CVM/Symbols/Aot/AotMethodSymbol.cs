using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using CVM.Collections.Immutable;
using CVM.Linq;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The class to represent all methods imported from a type .
    /// </summary>
    internal sealed class AotMethodSymbol : MethodSymbol
    {
        /// <summary>
        /// Holds infrequently accessed fields. See <seealso cref="_uncommonFields"/> for an explanation.
        /// </summary>
        private sealed class UncommonFields
        {
            public ParameterSymbol _lazyThisParameter;
            public OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembersResult;
            public ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
            public ImmutableArray<string> _lazyConditionalAttributeSymbols;
            public ObsoleteAttributeData _lazyObsoleteAttributeData;
            public DiagnosticInfo _lazyUseSiteDiagnostic;
        }
        MethodBase me;
        MethodKind _kind;
        private AotModuleSymbol module;
        private AotNamedTypeSymbol aotNamedTypeSymbol;
        private MethodInfo methodHandle;
        private readonly AotNamedTypeSymbol _containingType;

        internal AotMethodSymbol(MethodBase m
     )
        {
            me = m;

            _name = m.Name;
            SetMethodKind();
        }


        public AotMethodSymbol(AotModuleSymbol module, AotNamedTypeSymbol aotNamedTypeSymbol, MethodInfo methodHandle)
        {
            this.module = module;
            this._containingType = aotNamedTypeSymbol;
            //     this.methodHandle = methodHandle;
            me = methodHandle;

            _name = me.Name;
            SetMethodKind();
        }

        public AotMethodSymbol(AotModuleSymbol module, AotNamedTypeSymbol aotNamedTypeSymbol, ConstructorInfo methodHandle)
        {
            this.module = module;
            this._containingType = aotNamedTypeSymbol;
            //     this.methodHandle = methodHandle;
            me = methodHandle;

            _name = me.Name;
            _flag = MethodKind.Constructor;
        }
        void SetMethodKind()
        {

            MethodKind k = MethodKind.Ordinary;
            if (me is ConstructorInfo)
            {
                if (me.IsStatic)
                {

                    k = MethodKind.StaticConstructor;
                }
                else
                {
                    k = MethodKind.Constructor;
                }
            }
            //else if(me.Name.Contains("op_"))
            //  {
            //      k=MethodKind.
            //  }
            else
            {
                k = MethodKind.Ordinary;
            }
            _flag = k;
        }

        MethodKind _flag;
        private ImmutableArray<MethodSymbol> _lazyExplicitMethodImplementations;

        public override MethodKind MethodKind => _flag;

        public override int Arity {
            get
            {
                if (!tps.IsDefault)
                {
                    return tps.Length;
                }

                try
                {
                    int parameterCount;
                    int typeParameterCount;
                    parameterCount = Handle.GetParameters().Length;
                    typeParameterCount = Handle.GetGenericArguments().Length;
                    return typeParameterCount;

                }
                catch
                {
                    return TypeParameters.Length;
                }
            }
        }
        public override bool IsExtensionMethod
        {
            get
            {
                var m1 = AotAssemblySymbol.Inst.Aot;

                return m1.HasAttribute(Handle, typeof(System.Runtime.CompilerServices.ExtensionAttribute)); ;
            }
        }

        public override bool HidesBaseMethodsByName => me.Attributes != MethodAttributes.HideBySig;

        public override bool IsVararg => false;

        public override bool ReturnsVoid {
        
        
        get {
                try
                {
                    return ((System.Reflection.MethodInfo)me).ReturnType == typeof(void);
                }
                catch
                {

                    return true;
                }
            }

}
        public override bool IsAsync => false;

        public override RefKind RefKind => ReturnTypeParameter.RefKind;

        public override TypeSymbolWithAnnotations ReturnType => ReturnTypeParameter.Type;
        AotParameterSymbol _returnTypeParameter ;



        internal AotParameterSymbol ReturnTypeParameter => L1();

        public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments =>IsGenericMethod? GetTypeParametersAsTypeArguments() : ImmutableArray<TypeSymbolWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters { get {
                if(tps==null)
                {
                    tps = GetTs();
                }
                return tps;

            } }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (ps == null)
                {
                    ps = GetPs();
                }
                return ps;

            }
        }


        ImmutableArray<TypeParameterSymbol> tps;
        ImmutableArray<ParameterSymbol> ps;

        private ImmutableArray<TypeParameterSymbol> GetTs()
        {
          
            var ls = new List<TypeParameterSymbol>();
            if(me is ConstructorInfo)
            {
                return ls.ToImmutableArrayOrEmpty();
            }
        var ps=    me.GetGenericArguments();
            int i = 0;
            foreach(var p in ps)
            {
                AotTypeParameterSymbol a1 = new AotTypeParameterSymbol(AotAssemblySymbol.Inst.Aot, this.aotNamedTypeSymbol, (ushort)i, p);
                ls.Add(a1);
                i++;
            }
          return ls.AsImmutableOrEmpty();

        }
        private AotParameterSymbol L1()
        {
            if (_returnTypeParameter == null)
            {
                if (this.Handle is MethodInfo m)
                {
                    var rt = m.ReturnParameter;
                    var pi = new ParamInfo<TypeSymbol>();
                    pi.CustomModifiers = ImmutableArray<ModifierInfo<TypeSymbol>>.Empty;
                    pi.RefCustomModifiers = ImmutableArray<ModifierInfo<TypeSymbol>>.Empty;
                    pi.IsByRef = false;
                    pi.Type = new MetadataDecoder(AotAssemblySymbol.Inst.Aot).GetTypeOfToken(rt.ParameterType);
                    _returnTypeParameter = AotParameterSymbol.Create(AotAssemblySymbol.Inst.Aot, this,
                        IsMetadataVirtual(), 0, pi, default,
                        true, out bool f);
                }
                if (this.Handle is ConstructorInfo c)
                {
                    var dc = AotAssemblySymbol.Inst.GetSpecialType(SpecialType.System_Void);
                    var pi = new ParamInfo<TypeSymbol>();
                    pi.CustomModifiers = ImmutableArray<ModifierInfo<TypeSymbol>>.Empty;
                    pi.RefCustomModifiers = ImmutableArray<ModifierInfo<TypeSymbol>>.Empty;
                    pi.IsByRef = false;
                    pi.Type = dc;
                    _returnTypeParameter = AotParameterSymbol.Create(AotAssemblySymbol.Inst.Aot,this,IsMetadataVirtual(),0, pi, default,false,out bool a1);
                }
            }

            return _returnTypeParameter;

        }
        private ImmutableArray<ParameterSymbol> GetPs()
        {
         
          
            var ls = new List<ParameterSymbol>();
            var ps = me.GetParameters();

            int i = 0;
            foreach (var p in ps)
            {
                var mt = new MetadataDecoder(AotAssemblySymbol.Inst.Aot);
                var typ = mt.GetTypeOfToken(p.ParameterType);
                var a1 = AotParameterSymbol.Create(AotAssemblySymbol.Inst.Aot, this,
                    IsMetadataVirtual(), i, new ParamInfo<TypeSymbol>() {Type=typ,CustomModifiers=ImmutableArray<ModifierInfo<TypeSymbol>>.Empty,IsByRef=p.ParameterType.IsByRef,Handle=p,RefCustomModifiers=ImmutableArray<ModifierInfo<TypeSymbol>>.Empty }, default,
                    false, out bool f);
                ls.Add(a1);
                i++;
            }

            return ls.ToImmutableArrayOrEmpty();// ls.AsImmutableOrEmpty();

        }
        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {

            get
            {
               
                return ImmutableArray<MethodSymbol>.Empty;
                var explicitInterfaceImplementations = _lazyExplicitMethodImplementations;
                if (!explicitInterfaceImplementations.IsDefault)
                {
                    return explicitInterfaceImplementations;
                }

                var moduleSymbol = _containingType.ContainingAotModule;


                List<MethodBase> mbs = new List<MethodBase>();

                var top = Handle.DeclaringType;
                var inters = top.GetInterfaces();
              foreach (var ie1 in inters)
                {
                    var map = top.GetInterfaceMap(ie1);
                    foreach(var m in map.TargetMethods)
                    {
                        if(m == me)
                        {
                            mbs.Add(m);
                        }
                    }
                }
                var mt = new MetadataDecoder(AotAssemblySymbol.Inst.Aot);
              foreach(var m in mbs)
                {
                   
                }
                //// Context: we need the containing type of this method as context so that we can substitute appropriately into
                //// any generic interfaces that we might be explicitly implementing.  There is no reason to pass in the method
                //// context, however, because any method type parameters will belong to the implemented (i.e. interface) method,
                //// which we do not yet know.
                //var explicitlyOverriddenMethods = new MetadataDecoder(moduleSymbol, _containingType).GetExplicitlyOverriddenMethods(_containingType.Handle, me, this.ContainingType);

                ////avoid allocating a builder in the common case
                //var anyToRemove = false;
                //var sawObjectFinalize = false;
                //foreach (var method in explicitlyOverriddenMethods)
                //{
                //    if (!method.ContainingType.IsInterface)
                //    {
                //        anyToRemove = true;
                //        sawObjectFinalize =
                //            (method.ContainingType.SpecialType == SpecialType.System_Object &&
                //             method.Name == WellKnownMemberNames.DestructorName && // Cheaper than MethodKind.
                //             method.MethodKind == MethodKind.Destructor);
                //    }

                //    if (anyToRemove && sawObjectFinalize)
                //    {
                //        break;
                //    }
                //}

                //// CONSIDER: could assert that we're writing the existing value if it's already there
                //// CONSIDER: what we'd really like to do is set this bit only in cases where the explicitly
                //// overridden method matches the method that will be returned by MethodSymbol.OverriddenMethod.
                //// Unfortunately, this MethodSymbol will not be sufficiently constructed (need IsOverride and MethodKind,
                //// which depend on this property) to determine which method OverriddenMethod will return.
                //_packedFlags.InitializeIsExplicitOverride(isExplicitFinalizerOverride: sawObjectFinalize, isExplicitClassOverride: anyToRemove);

                //explicitInterfaceImplementations = explicitlyOverriddenMethods;

                //if (anyToRemove)
                //{
                //    var explicitInterfaceImplementationsBuilder = ArrayBuilder<MethodSymbol>.GetInstance();
                //    foreach (var method in explicitlyOverriddenMethods)
                //    {
                //        if (method.ContainingType.IsInterface)
                //        {
                //            explicitInterfaceImplementationsBuilder.Add(method);
                //        }
                //    }

                //    explicitInterfaceImplementations = explicitInterfaceImplementationsBuilder.ToImmutableAndFree();
                //}

                //return InterlockedOperations.Initialize(ref _lazyExplicitMethodImplementations, explicitInterfaceImplementations);
            }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers =>ImmutableArray<CustomModifier>.Empty;

        public override Symbol AssociatedSymbol => _associatedPropertyOrEventOpt;

        public override Symbol ContainingSymbol => _containingType;

        public override ImmutableArray<Location> Locations => throw new NotImplementedException();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new NotImplementedException();

        public override Accessibility DeclaredAccessibility =>Accessibility.Public;

        public override bool IsStatic => me.IsStatic;

        public override bool IsVirtual => me.IsVirtual;

        public override bool IsOverride => HasFlag(MethodAttributes.Final);// throw new NotImplementedException();

        public override bool IsAbstract => me.IsAbstract;

        public override bool IsSealed => HasFlag(MethodAttributes.Final)&&!HasFlag(MethodAttributes.Virtual)&&!HasFlag(MethodAttributes.Abstract);

        public override bool IsExtern => HasFlag(MethodAttributes.PinvokeImpl);

        internal override bool HasSpecialName =>me.IsSpecialName
            ;

        internal override MethodImplAttributes ImplementationAttributes => me.GetMethodImplementationFlags();

        internal override bool HasDeclarativeSecurity => me.Attributes == MethodAttributes.HasSecurity;// throw new NotImplementedException();

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => throw new NotImplementedException();

        internal override bool RequiresSecurityObject => me.Attributes == MethodAttributes.RequireSecObject;// throw new NotImplementedException();

        internal override CallingConvention CallingConvention => CallingConvention.Standard;

        internal override bool GenerateDebugInfo => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData => GetOb();

        ObsoleteAttributeData GetOb()
        {
            try
            {
                var we = me.GetCustomAttributes(typeof(ObsoleteAttribute), false);
                var dll = (System.ObsoleteAttribute)we[0];
                var data = new ObsoleteAttributeData(dll.IsError?ObsoleteAttributeKind.Deprecated:ObsoleteAttributeKind.Obsolete,dll.Message,dll.IsError);

                return data;
            }
            catch
            {

            }
        

                return null;
        }

        public override DllImportData GetDllImportData()
        {
            if (HasFlag(MethodAttributes.PinvokeImpl))
            {
                try
                {
                    var we = me.GetCustomAttributes(typeof(System.Runtime.InteropServices.DllImportAttribute), false);
                    var dll = (System.Runtime.InteropServices.DllImportAttribute)we[0];
                    var data = new DllImportData(dll.Value, dll.EntryPoint, dll.CharSet);
                    return data;
                }
                catch
                {

                }
                }

                return null;

        }
        private Symbol _associatedPropertyOrEventOpt;

        internal bool SetAssociatedProperty(AotPropertySymbol propertySymbol, MethodKind methodKind)
        {
            Debug.Assert((methodKind == MethodKind.PropertyGet) || (methodKind == MethodKind.PropertySet));
            return this.SetAssociatedPropertyOrEvent(propertySymbol, methodKind);
        }
        private bool SetAssociatedPropertyOrEvent(Symbol propertyOrEventSymbol, MethodKind methodKind)
        {
            if ((object)_associatedPropertyOrEventOpt == null)
            {
         //       Debug.Assert(TypeSymbol.Equals(propertyOrEventSymbol.ContainingType, _containingType, TypeCompareKind.ConsiderEverything2));

                // No locking required since SetAssociatedProperty/SetAssociatedEvent will only be called
                // by the thread that created the method symbol (and will be called before the method
                // symbol is added to the containing type members and available to other threads).
                _associatedPropertyOrEventOpt = propertyOrEventSymbol;

                // NOTE: may be overwriting an existing value.


                _flag = methodKind;
                return true;
            }

            return false;
        }
        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
      
            var uncommonFields = _uncommonFields;
            if (uncommonFields == null)
            {
                return ImmutableArray<string>.Empty;
            }
            else
            {
                var result = uncommonFields._lazyConditionalAttributeSymbols;
              
                if(result.IsDefault)
                {
                    return uncommonFields._lazyConditionalAttributeSymbols = ImmutableArray<string>.Empty;

                }
                else
                {
                    return result;
                }
            }
        }

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
        {
            throw new NotImplementedException();
        }
        private bool HasFlag(MethodAttributes flag)
        {
          
            return (flag & me.Attributes) != 0;
        }
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return HasFlag( MethodAttributes.NewSlot);
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return HasFlag(MethodAttributes.Virtual);
        }
        private void LoadSignature()
        {
            int pc;
         var ps=  me.GetParameters();
            pc = ps.Length;

      
        }
        internal MethodBase Handle => me;
        internal MethodInfo Handle1 =>(MethodInfo) me;

        private UncommonFields _uncommonFields;

        public static T Initialize<T>(ref T target, T value) where T : class
        {
            Debug.Assert((object)value != null);
            return CVM.AHelper.CompareExchange(ref target, value, null) ?? value;
        }
        private UncommonFields AccessUncommonFields()
        {
            var retVal = _uncommonFields;
            return retVal ??Initialize(ref _uncommonFields, CreateUncommonFields());
        }

        private UncommonFields CreateUncommonFields()
        {
           
            var retVal = new UncommonFields();
            if ((Handle.GetCustomAttributes(typeof(ObsoleteAttribute),false).Length)<1)
            {
                retVal._lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
            }

            //
            // Do not set _lazyUseSiteDiagnostic !!!!
            //
            // "null" Indicates "no errors" or "unknown state",
            // and we know which one of the states we have from IsUseSiteDiagnosticPopulated
            //
            // Setting _lazyUseSiteDiagnostic to a sentinel value here would introduce
            // a number of extra states for various permutations of IsUseSiteDiagnosticPopulated, UncommonFields and _lazyUseSiteDiagnostic
            // Some of them, in tight races, may lead to returning the sentinel as the diagnostics.
            //
           
            if (Handle.GetCustomAttributes(false).Length>0)
            {
                retVal._lazyCustomAttributes = ImmutableArray<CSharpAttributeData>.Empty;
            }
            
            //if (_packedFlags.IsConditionalPopulated)
            //{
            //    retVal._lazyConditionalAttributeSymbols = ImmutableArray<string>.Empty;
            //}

            //if (Handle1.Attributes==Me _packedFlags.IsOverriddenOrHiddenMembersPopulated)
            //{
            //    retVal._lazyOverriddenOrHiddenMembersResult = OverriddenOrHiddenMembersResult.Empty;
            //}

            return retVal;
        }
        public override string Name => _name;
        private readonly string _name;

    }
}
