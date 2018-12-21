using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using CVM.Collections.Immutable;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The class to represent all methods imported from a type .
    /// </summary>
    internal sealed class AotMethodSymbol : MethodSymbol
    {
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
           
        }

        public AotMethodSymbol(AotModuleSymbol module, AotNamedTypeSymbol aotNamedTypeSymbol, MethodInfo methodHandle)
        {
            this.module = module;
            this.aotNamedTypeSymbol = aotNamedTypeSymbol;
            //     this.methodHandle = methodHandle;
            me = methodHandle;
        }
       

        void SetMethodKind()
        {
        
            MethodKind k=MethodKind.Ordinary;
          if(me is ConstructorInfo)
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
            _flag = k;
        }

        MethodKind _flag;
        public override MethodKind MethodKind => _flag;

        public override int Arity => me.GetParameters().Length;
        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName =>me.Attributes!=MethodAttributes.HideBySig;

        public override bool IsVararg => false;

        public override bool ReturnsVoid =>((System.Reflection.MethodInfo)me).ReturnType==typeof(void);

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        public override TypeSymbolWithAnnotations ReturnType =>throw new Exception();

        public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments => throw new NotImplementedException();

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => throw new NotImplementedException();

        public override ImmutableArray<ParameterSymbol> Parameters => throw new NotImplementedException();

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => throw new NotImplementedException();

        public override ImmutableArray<CustomModifier> RefCustomModifiers => throw new NotImplementedException();

        public override Symbol AssociatedSymbol => throw new NotImplementedException();

        public override Symbol ContainingSymbol => throw new NotImplementedException();

        public override ImmutableArray<Location> Locations => throw new NotImplementedException();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new NotImplementedException();

        public override Accessibility DeclaredAccessibility =>Accessibility.NotApplicable;

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
            throw new NotImplementedException();
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

    }
}
