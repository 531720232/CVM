using CVM.Collections.Immutable;
using Microsoft.Cci;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class AotPropertySymbol : PropertySymbol
    {
        private readonly string _name;
        private readonly AotNamedTypeSymbol _containingType;
        private readonly PropertyInfo _handle;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private readonly RefKind _refKind;
        private readonly TypeSymbolWithAnnotations _propertyType;
        private readonly AotMethodSymbol _getMethod;
        private readonly AotMethodSymbol _setMethod;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private const int UnsetAccessibility = -1;
        private int _declaredAccessibility = UnsetAccessibility;

        private readonly Flags _flags;

        [Flags]
        private enum Flags : byte
        {
            IsSpecialName = 1,
            IsRuntimeSpecialName = 2,
            CallMethodsDirectly = 4
        }

        private ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        public override RefKind RefKind => RefKind.None;

        public override TypeSymbolWithAnnotations Type => throw new NotImplementedException();

        public override ImmutableArray<CustomModifier> RefCustomModifiers => throw new NotImplementedException();

        public override ImmutableArray<ParameterSymbol> Parameters => throw new NotImplementedException();

        public override bool IsIndexer => throw new NotImplementedException();

        internal override bool HasSpecialName => throw new NotImplementedException();

        public override MethodSymbol GetMethod => throw new NotImplementedException();

        public override MethodSymbol SetMethod => throw new NotImplementedException();

        internal override CallingConvention CallingConvention => throw new NotImplementedException();

        internal override bool MustCallMethodsDirectly => throw new NotImplementedException();

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => throw new NotImplementedException();

        public override Symbol ContainingSymbol => throw new NotImplementedException();

        public override ImmutableArray<Location> Locations => throw new NotImplementedException();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new NotImplementedException();

        public override Accessibility DeclaredAccessibility => throw new NotImplementedException();

        public override bool IsStatic => throw new NotImplementedException();

        public override bool IsVirtual => throw new NotImplementedException();

        public override bool IsOverride => throw new NotImplementedException();

        public override bool IsAbstract => throw new NotImplementedException();

        public override bool IsSealed => throw new NotImplementedException();

        public override bool IsExtern => throw new NotImplementedException();

        internal override ObsoleteAttributeData ObsoleteAttributeData => throw new NotImplementedException();

        internal static AotPropertySymbol Create(AotModuleSymbol module, AotNamedTypeSymbol aotNamedTypeSymbol, PropertyInfo propertyDef, AotMethodSymbol getMethod, AotMethodSymbol setMethod)
        {
            Debug.Assert((object)module != null);
            Debug.Assert((object)aotNamedTypeSymbol != null);
            Debug.Assert(propertyDef!=null);
            var propertyParams = MetadataDecoder.GetSignatureForProperty(propertyDef);

            AotPropertySymbol result = new AotPropertySymbol(module, aotNamedTypeSymbol, propertyDef, getMethod, setMethod, 0, propertyParams,new MetadataDecoder());
            return result;
        }
        private AotPropertySymbol(
           AotModuleSymbol moduleSymbol,
           AotNamedTypeSymbol containingType,
           PropertyInfo handle,
           AotMethodSymbol getMethod,
           AotMethodSymbol setMethod,
           int countOfCustomModifiers,
           ParamInfo<TypeSymbol>[] propertyParams,
           MetadataDecoder metadataDecoder)
        {
            _containingType = containingType;
            var module = moduleSymbol;
            PropertyAttributes mdFlags = 0;
            BadImageFormatException mrEx = null;

            try
            {
                _name = handle.Name;
                mdFlags = handle.Attributes;
            }
            catch (BadImageFormatException e)
            {
                mrEx = e;

                if ((object)_name == null)
                {
                    _name = string.Empty;
                }
            }

            _getMethod = getMethod;
            _setMethod = setMethod;
            _handle = handle;

        
            var getMethodParams = (object)getMethod == null ? null : MetadataDecoder.GetSignatureForMethod(getMethod.Handle1);
            var setMethodParams = (object)setMethod == null ? null : MetadataDecoder.GetSignatureForMethod(setMethod.Handle1);

           
            // NOTE: property parameter names are not recorded in metadata, so we have to
            // use the parameter names from one of the indexers
            // NB: prefer setter names to getter names if both are present.
            bool isBad;

            _parameters = setMethodParams is null
                ? GetParameters(moduleSymbol, this, propertyParams, getMethodParams, getMethod.IsMetadataVirtual(), out isBad)
                : GetParameters(moduleSymbol, this, propertyParams, setMethodParams, setMethod.IsMetadataVirtual(), out isBad);

            if ( mrEx != null || isBad)
            {
                _lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }

            var returnInfo = propertyParams[0];
            var typeCustomModifiers = CSharpCustomModifier.Convert(returnInfo.CustomModifiers);

            if (returnInfo.IsByRef)
            {
                //if (moduleSymbol.Module.HasIsReadOnlyAttribute(handle))
                //{
                //    _refKind = RefKind.RefReadOnly;
                //}
                //else
                {
                    _refKind = RefKind.Ref;
                }
            }
            else
            {
                _refKind = RefKind.None;
            }

            // CONSIDER: Can we make parameter type computation lazy?
            TypeSymbol originalPropertyType = returnInfo.Type;

        //..    originalPropertyType = DynamicTypeDecoder.TransformType(originalPropertyType, typeCustomModifiers.Length, handle, moduleSymbol, _refKind);

            // Dynamify object type if necessary
      //      originalPropertyType = originalPropertyType.AsDynamicIfNoPia(_containingType);

            // We start without annotation (they will be decoded below)
            var propertyType = TypeSymbolWithAnnotations.Create(originalPropertyType, customModifiers: typeCustomModifiers);

            // Decode nullable before tuple types to avoid converting between
            // NamedTypeSymbol and TupleTypeSymbol unnecessarily.
     //       propertyType = NullableTypeDecoder.TransformType(propertyType, handle, moduleSymbol);
      //      propertyType = TupleTypeDecoder.DecodeTupleTypesIfApplicable(propertyType, handle, moduleSymbol);

            _propertyType = propertyType;

            // A property is bogus and must be accessed by calling its accessors directly if the
            // accessor signatures do not agree, both with each other and with the property,
            // or if it has parameters and is not an indexer or indexed property.
            bool callMethodsDirectly = false;

            if (!callMethodsDirectly)
            {
                if ((object)_getMethod != null)
                {
                    _getMethod.SetAssociatedProperty(this, MethodKind.PropertyGet);
                }

                if ((object)_setMethod != null)
                {
                    _setMethod.SetAssociatedProperty(this, MethodKind.PropertySet);
                }
            }

            if (callMethodsDirectly)
            {
                _flags |= Flags.CallMethodsDirectly;
            }

            if ((mdFlags & PropertyAttributes.SpecialName) != 0)
            {
                _flags |= Flags.IsSpecialName;
            }

            if ((mdFlags & PropertyAttributes.RTSpecialName) != 0)
            {
                _flags |= Flags.IsRuntimeSpecialName;
            }
        }
        private static ImmutableArray<ParameterSymbol> GetParameters(
        AotModuleSymbol moduleSymbol,
        AotPropertySymbol property,
        ParamInfo<TypeSymbol>[] propertyParams,
        ParamInfo<TypeSymbol>[] accessorParams,
        bool isPropertyVirtual,
        out bool anyParameterIsBad)
        {
            anyParameterIsBad = false;

            // First parameter is the property type.
            if (propertyParams.Length < 2)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }

            var numAccessorParams = accessorParams.Length;

            var parameters = new ParameterSymbol[propertyParams.Length - 1];
            for (int i = 1; i < propertyParams.Length; i++) // from 1 to skip property/return type
            {
                // NOTE: this is a best guess at the Dev10 behavior.  The actual behavior is
                // in the unmanaged helper code that Dev10 uses to load the metadata.
                var propertyParam = propertyParams[i];
                var paramHandle = i < numAccessorParams ? accessorParams[i].Handle : propertyParam.Handle;
                var ordinal = i - 1;
                bool isBad;

                // https://github.com/dotnet/roslyn/issues/29821: handle extra annotations
                parameters[ordinal] = AotParameterSymbol.Create(moduleSymbol, property, isPropertyVirtual, ordinal, paramHandle, propertyParam, extraAnnotations: default, out isBad);

                if (isBad)
                {
                    anyParameterIsBad = true;
                }
            }

            return parameters.AsImmutableOrNull();
        }

    }
}