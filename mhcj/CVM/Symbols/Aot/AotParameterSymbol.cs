using System;
using CVM.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class AotParameterSymbol : ParameterSymbol
    {
        private readonly Symbol _containingSymbol;
        private readonly string _name;
        private readonly TypeSymbolWithAnnotations _type;
        private readonly ParameterInfo _handle;
        private readonly ParameterAttributes _flags;
        private readonly AotModuleSymbol _moduleSymbol;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private ConstantValue _lazyDefaultValue = ConstantValue.Unset;
        private ThreeState _lazyIsParams;
        private readonly ushort _ordinal;

        public override TypeSymbolWithAnnotations Type => _type;
          RefKind _refKind ;
        public override RefKind RefKind => _refKind;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override MarshalPseudoCustomAttributeData MarshallingInformation => null;

        public override int Ordinal => _ordinal;


        /// <summary>
        /// Attributes filtered out from m_lazyCustomAttributes, ParamArray, etc.
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyHiddenAttributes;
        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                Debug.Assert(_handle!=null);
                var containingPEModuleSymbol = (AotModuleSymbol)this.ContainingModule;

                // Filter out ParamArrayAttributes if necessary and cache
                // the attribute handle for GetCustomAttributesToEmit
                bool filterOutParamArrayAttribute = (!_lazyIsParams.HasValue() || _lazyIsParams.Value());

                ConstantValue defaultValue = this.ExplicitDefaultConstantValue;
                AttributeDescription filterOutConstantAttributeDescription = default(AttributeDescription);

                if ((object)defaultValue != null)
                {
                    if (defaultValue.Discriminator == ConstantValueTypeDiscriminator.DateTime)
                    {
                        filterOutConstantAttributeDescription = AttributeDescription.DateTimeConstantAttribute;
                    }
                    else if (defaultValue.Discriminator == ConstantValueTypeDiscriminator.Decimal)
                    {
                        filterOutConstantAttributeDescription = AttributeDescription.DecimalConstantAttribute;
                    }
                }

                bool filterIsReadOnlyAttribute = this.RefKind == RefKind.In;

                if (filterOutParamArrayAttribute || filterOutConstantAttributeDescription.Signatures != null || filterIsReadOnlyAttribute)
                {
                    Attribute paramArrayAttribute;
                    Attribute constantAttribute;
                    Attribute isReadOnlyAttribute;
                  
                    ImmutableArray<CSharpAttributeData> attributes =
                        containingPEModuleSymbol.GetCustomAttributesForToken(
                            _handle,
                            out paramArrayAttribute,
                            filterOutParamArrayAttribute ? AttributeDescription.ParamArrayAttribute : default,
                            out constantAttribute,
                            filterOutConstantAttributeDescription,
                            out isReadOnlyAttribute,
                      filterIsReadOnlyAttribute ? AttributeDescription.IsReadOnlyAttribute : default,
                            out _,
                            default);

                    if (paramArrayAttribute!=null || constantAttribute!=null)
                    {
                        var builder = ArrayBuilder<CSharpAttributeData>.GetInstance();

                        if (paramArrayAttribute != null)
                        {
                            builder.Add(new AotAttributeData(containingPEModuleSymbol, paramArrayAttribute));
                        }

                        if (constantAttribute!= null)
                        {
                            builder.Add(new AotAttributeData(containingPEModuleSymbol, constantAttribute));
                        }

                        ImmutableInterlocked.InterlockedInitialize(ref _lazyHiddenAttributes, builder.ToImmutableAndFree());
                    }
                    else
                    {
                        ImmutableInterlocked.InterlockedInitialize(ref _lazyHiddenAttributes, ImmutableArray<CSharpAttributeData>.Empty);
                    }

                    if (!_lazyIsParams.HasValue())
                    {
                        Debug.Assert(filterOutParamArrayAttribute);
                        _lazyIsParams = (paramArrayAttribute != null).ToThreeState();
                    }

                    ImmutableInterlocked.InterlockedInitialize(
                        ref _lazyCustomAttributes,
                        attributes);
                }
                else
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyHiddenAttributes, ImmutableArray<CSharpAttributeData>.Empty);
                    containingPEModuleSymbol.LoadCustomAttributes(_handle, ref _lazyCustomAttributes);
                }
            }

            Debug.Assert(!_lazyHiddenAttributes.IsDefault);
            return _lazyCustomAttributes;
        }
        public override bool IsParams    {
            get
            {
                // This is also populated by loading attributes, but loading
                // attributes is more expensive, so we should only do it if
                // attributes are requested.
                if (!_lazyIsParams.HasValue())
                {

                    _lazyIsParams = _moduleSymbol.HasParamsAttribute(_handle).ToThreeState();
                }
                return _lazyIsParams.Value();
            }
        }

        internal override bool IsMetadataOptional => (_flags&ParameterAttributes.Optional)!=0;

        internal override bool IsMetadataIn => (_flags & ParameterAttributes.In) != 0;

        internal override bool IsMetadataOut => (_flags & ParameterAttributes.Out) != 0;

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get
            {

               
                // The HasDefault flag has to be set, it doesn't suffice to mark the parameter with DefaultParameterValueAttribute.
                if (_lazyDefaultValue == ConstantValue.Unset)
                {
                    // From the C# point of view, there is no need to import a parameter's default value
                    // if the language isn't going to treat it as optional. However, we might need metadata constant value for NoPia.
                    // NOTE: Ignoring attributes for non-Optional parameters disrupts round-tripping, but the trade-off seems acceptable.
                    ConstantValue value = ImportConstantValue(ignoreAttributes: !IsMetadataOptional);
                    CVM.AHelper.CompareExchange(ref _lazyDefaultValue, value, ConstantValue.Unset);
                }

                return _lazyDefaultValue;
            }
        }
internal override bool IsIDispatchConstant =>false;



        internal override bool IsIUnknownConstant
        {
            get
            {
             


                return this._moduleSymbol.HasAttribute(_handle,typeof(System.Runtime.CompilerServices.IUnknownConstantAttribute));
            }
        }

        internal override bool IsCallerFilePath => false;

        internal override bool IsCallerLineNumber =>false;

        internal override bool IsCallerMemberName => false;

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override Symbol ContainingSymbol => _containingSymbol;

        public override ImmutableArray<Location> Locations => new ImmutableArray<Location>(new Location[] { new AotLocation()});

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        internal static AotParameterSymbol Create(
                  AotModuleSymbol moduleSymbol,
                  AotMethodSymbol containingSymbol,
                  bool isContainingSymbolVirtual,
                  int ordinal,
                  ParamInfo<TypeSymbol> parameterInfo,
                  ImmutableArray<byte> extraAnnotations,
                  bool isReturn,
                  out bool isBad)
        {
            
            return Create(
                moduleSymbol, containingSymbol, isContainingSymbolVirtual, ordinal,
                parameterInfo.IsByRef, parameterInfo.RefCustomModifiers, parameterInfo.Type, extraAnnotations,
                parameterInfo.Handle, parameterInfo.CustomModifiers, isReturn, out isBad);
        }

        internal static AotParameterSymbol Create(
          AotModuleSymbol moduleSymbol,
          AotPropertySymbol containingSymbol,
          bool isContainingSymbolVirtual,
          int ordinal,
          ParameterInfo handle,
          ParamInfo<TypeSymbol> parameterInfo,
          ImmutableArray<byte> extraAnnotations,
          out bool isBad)
        {
            return Create(
                moduleSymbol, containingSymbol, isContainingSymbolVirtual, ordinal,
                parameterInfo.IsByRef, parameterInfo.RefCustomModifiers, parameterInfo.Type, extraAnnotations,
                handle, parameterInfo.CustomModifiers, isReturn: false, out isBad);
        }
        private static AotParameterSymbol Create(
           AotModuleSymbol moduleSymbol,
           Symbol containingSymbol,
           bool isContainingSymbolVirtual,
           int ordinal,
           bool isByRef,
           ImmutableArray<ModifierInfo<TypeSymbol>> refCustomModifiers,
           TypeSymbol type,
           ImmutableArray<byte> extraAnnotations,
           ParameterInfo handle,
           ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers,
           bool isReturn,
           out bool isBad)
        {
            // We start without annotation (they will be decoded below)
            var typeWithModifiers = TypeSymbolWithAnnotations.Create(type, customModifiers: CSharpCustomModifier.Convert(customModifiers));

            var parameter = new AotParameterSymbol(moduleSymbol, containingSymbol, ordinal, isByRef, typeWithModifiers, extraAnnotations, handle, 0, out isBad);
        //    AotParameterSymbol parameter = customModifiers.IsDefaultOrEmpty && refCustomModifiers.IsDefaultOrEmpty
        //      ? new AotParameterSymbol(moduleSymbol, containingSymbol, ordinal, isByRef, typeWithModifiers, extraAnnotations, handle, 0, out isBad)
        //    : new AotParameterSymbolWithCustomModifiers(moduleSymbol, containingSymbol, ordinal, isByRef, refCustomModifiers, typeWithModifiers, extraAnnotations, handle, out isBad);

            bool hasInAttributeModifier = parameter.RefCustomModifiers.HasInAttributeModifier();

            if (isReturn)
            {
                // A RefReadOnly return parameter should always have this modreq, and vice versa.
                isBad |= (parameter.RefKind == RefKind.RefReadOnly) != hasInAttributeModifier;
            }
            else if (parameter.RefKind == RefKind.In)
            {
                // An in parameter should not have this modreq, unless the containing symbol was virtual or abstract.
                isBad |= isContainingSymbolVirtual != hasInAttributeModifier;
            }
            else if (hasInAttributeModifier)
            {
                // This modreq should not exist on non-in parameters.
                isBad = true;
            }

            return parameter;
        }
        private AotParameterSymbol(
           AotModuleSymbol moduleSymbol,
           Symbol containingSymbol,
           int ordinal,
           bool isByRef,
           TypeSymbolWithAnnotations type,
           ImmutableArray<byte> extraAnnotations,
           ParameterInfo handle,
           int countOfCustomModifiers,
           out bool isBad)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingSymbol != null);
            Debug.Assert(ordinal >= 0);
            Debug.Assert(!type.IsNull);

            isBad = false;
            _moduleSymbol = moduleSymbol;
            _containingSymbol = containingSymbol;
            _ordinal = (ushort)ordinal;

            _handle = handle;


            RefKind refKind = RefKind.None;

            if (handle==null)
            {
                refKind = isByRef ? RefKind.Ref : RefKind.None;

                type = TupleTypeSymbol.TryTransformToTuple(type.TypeSymbol, out TupleTypeSymbol tuple) ?
                    TypeSymbolWithAnnotations.Create(tuple) :
                    type;
               

                _lazyCustomAttributes = ImmutableArray<CSharpAttributeData>.Empty;
             //.   _lazyHiddenAttributes = ImmutableArray<CSharpAttributeData>.Empty;
                _lazyDefaultValue = ConstantValue.NotAvailable;
                _lazyIsParams = ThreeState.False;
            }
            else
            {
                try
                {
                    _name = handle.Name;
                    _flags = handle.Attributes;
                  //  moduleSymbol.Module.GetParamPropsOrThrow(handle, out _name, out _flags);
                }
                catch 
                {
                    isBad = true;
                }
                isByRef = _handle.ParameterType.IsByRef;
                if (isByRef)
                {
                    ParameterAttributes inOutFlags = _flags & (ParameterAttributes.Out | ParameterAttributes.In);

                    if (inOutFlags == ParameterAttributes.Out)
                    {
                        refKind = RefKind.Out;
                    }
                    else if (handle.ParameterType.BaseType == typeof(System.Collections.ReadOnlyCollectionBase)||handle.IsIn)
                    {
                        refKind = RefKind.In;
                    }
                    else
                    {
                        refKind = RefKind.Ref;
                    }
                }

                // CONSIDER: Can we make parameter type computation lazy?
                var typeSymbol = type.TypeSymbol;
                type = type.WithTypeAndModifiers(typeSymbol, type.CustomModifiers);
                // Decode nullable before tuple types to avoid converting between
                // NamedTypeSymbol and TupleTypeSymbol unnecessarily.
                //type = NullableTypeDecoder.TransformType(type, handle, moduleSymbol, extraAnnotations);
                //type = TupleTypeDecoder.DecodeTupleTypesIfApplicable(type, handle, moduleSymbol);
            }

            _type = type;

            bool hasNameInMetadata = !string.IsNullOrEmpty(_name);
            if (!hasNameInMetadata)
            {
                // As was done historically, if the parameter doesn't have a name, we give it the name "value".
                _name = "value";
            }
       
       //     _packedFlags = new PackedFlags(refKind, attributesAreComplete: handle.IsNil, hasNameInMetadata: hasNameInMetadata);

            Debug.Assert(refKind == this.RefKind);
        //    Debug.Assert(hasNameInMetadata == this.HasNameInMetadata);
        }
        internal ConstantValue ImportConstantValue( bool ignoreAttributes = false)
        {
            Debug.Assert(_handle!=null);

            // Metadata Spec 22.33: 
            //   6. If Flags.HasDefault = 1 then this row [of Param table] shall own exactly one row in the Constant table [ERROR]
            //   7. If Flags.HasDefault = 0, then there shall be no rows in the Constant table owned by this row [ERROR]
            ConstantValue value = null;

            if ((_flags & ParameterAttributes.HasDefault) != 0)
            {
                value =AotAssemblySymbol.Inst.Aot.GetConstantValue(_handle.DefaultValue);
            }

            if (value == null && !ignoreAttributes)
            {
                value = GetDefaultDecimalOrDateTimeValue();
            }

            return value;
        }
        private ConstantValue GetDefaultDecimalOrDateTimeValue()
        {
            Debug.Assert(_handle!=null);
            ConstantValue value = null;

            // It is possible in Visual Basic for a parameter of object type to have a default value of DateTime type.
            // If it's present, use it.  We'll let the call-site figure out whether it can actually be used.
            if (_moduleSymbol.HasDateTimeConstantAttribute(_handle, out value))
           {
                return value;
            }

            // It is possible in Visual Basic for a parameter of object type to have a default value of decimal type.
            // If it's present, use it.  We'll let the call-site figure out whether it can actually be used.
            if (_moduleSymbol.HasDecimalConstantAttribute(_handle, out value))
            {
                return value;
            }

            return value;
        }

    }
}