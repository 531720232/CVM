using CVM.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

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

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override MarshalPseudoCustomAttributeData MarshallingInformation => null;

        public override int Ordinal =>0;

        public override bool IsParams => _lazyIsParams.Value();

        internal override bool IsMetadataOptional => (_flags&ParameterAttributes.Optional)!=0;

        internal override bool IsMetadataIn => (_flags & ParameterAttributes.In) != 0;

        internal override bool IsMetadataOut => (_flags & ParameterAttributes.Out) != 0;

        internal override ConstantValue ExplicitDefaultConstantValue => throw new System.NotImplementedException();

        internal override bool IsIDispatchConstant =>false;

        internal override bool IsIUnknownConstant => false;

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

                if (isByRef)
                {
                    ParameterAttributes inOutFlags = _flags & (ParameterAttributes.Out | ParameterAttributes.In);

                    if (inOutFlags == ParameterAttributes.Out)
                    {
                        refKind = RefKind.Out;
                    }
                    else if (handle.ParameterType.BaseType == typeof(System.Collections.ReadOnlyCollectionBase))
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
    }
}