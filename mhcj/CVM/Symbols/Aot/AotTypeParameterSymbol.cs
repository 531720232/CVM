using CVM.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class AotTypeParameterSymbol : TypeParameterSymbol
    {
        private readonly Symbol _containingSymbol; // Could be AotNamedType or a AotMethod
        private readonly Type _handle;

        #region Metadata
        private readonly string _name;
        private readonly ushort _ordinal; // 0 for first, 1 for second, ...
        #endregion

        /// <summary>
        /// First error calculating bounds.
        /// </summary>
        //private DiagnosticInfo _lazyConstraintsUseSiteErrorInfo = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state.

        private readonly GenericParameterAttributes _flags;
        private ThreeState _lazyHasIsUnmanagedConstraint;
        private TypeParameterBounds _lazyBounds = TypeParameterBounds.Unset;
        private ImmutableArray<TypeSymbolWithAnnotations> _lazyDeclaredConstraintTypes;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        public override bool HasUnmanagedTypeConstraint => false;

        internal AotTypeParameterSymbol(
             AotModuleSymbol moduleSymbol,
             AotNamedTypeSymbol definingNamedType,
             ushort ordinal,
             Type handle)
             : this(moduleSymbol, (Symbol)definingNamedType, ordinal, handle)
        {
        }

        internal AotTypeParameterSymbol(
            AotModuleSymbol moduleSymbol,
            AotMethodSymbol definingMethod,
            ushort ordinal,
            Type handle)
            : this(moduleSymbol, (Symbol)definingMethod, ordinal, handle)
        {
        }

        private AotTypeParameterSymbol(
            AotModuleSymbol moduleSymbol,
            Symbol definingSymbol,
            ushort ordinal,
            Type handle)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)definingSymbol != null);
            Debug.Assert(ordinal >= 0);
            Debug.Assert(handle!=null);

            _containingSymbol = definingSymbol;

            GenericParameterAttributes flags = 0;

            try
            {
                _name = handle.Name;
                _flags = handle.GenericParameterAttributes;
           //     moduleSymbol.Module.GetGenericParamPropsOrThrow(handle, out _name, out flags);
            }
            catch 
            {
                if ((object)_name == null)
                {
                    _name = string.Empty;
                }

            // ..   _lazyConstraintsUseSiteErrorInfo = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }

            // Clear the '.ctor' flag if both '.ctor' and 'valuetype' are
            // set since '.ctor' is redundant in that case.
            _flags = ((flags & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0) ? flags : (flags & ~GenericParameterAttributes.DefaultConstructorConstraint);
          
         
            _ordinal = ordinal;
            _handle = handle;
        }
        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return this.ContainingSymbol.Kind == SymbolKind.Method
                    ? TypeParameterKind.Method
                    : TypeParameterKind.Type;
            }
        }

        public override int Ordinal
        {
            get { return _ordinal; }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }
        internal Type Handle
        {
            get
            {
                return _handle;
            }
        }
        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _containingSymbol.ContainingAssembly;
            }
        }
        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _containingSymbol.Locations;
            }
        }
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }
        public override bool HasConstructorConstraint
        {
            get
            {
                return (_flags & GenericParameterAttributes.DefaultConstructorConstraint) != 0;
            }
        }
        public override bool HasReferenceTypeConstraint
        {
            get
            {
                return (_flags & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
            }
        }
        internal override bool? ReferenceTypeConstraintIsNullable
        {
            get
            {
                // https://github.com/dotnet/roslyn/issues/29821 Support external annotations.
                if (!HasReferenceTypeConstraint)
                {
                    return false;
                }


               if((Handle.GenericParameterAttributes&GenericParameterAttributes.NotNullableValueTypeConstraint)!=0)
                {
                    return false;

                }
                //if ((Handle.GenericParameterAttributes & GenericParameterAttributes.VarianceMask) != 0)
                //{
                //    return false;

                //}
                //_handle.GenericParameterAttributes==GenericParameterAttributes.NotNullableValueTypeConstraint
                //if (((AotModuleSymbol)this.ContainingModule).Module.HasNullableAttribute(_handle, out byte transformFlag, out _))
                //{
                //    switch ((NullableAnnotation)transformFlag)
                //    {
                //        case NullableAnnotation.Nullable:
                //            return true;
                //        case NullableAnnotation.NotNullable:
                //            return false;
                //    }
                //}

                return null;
            }
        }
        public override bool HasValueTypeConstraint
        {
            get
            {
                return (_flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            }
        }
        public override VarianceKind Variance
        {
            get
            {
                return (VarianceKind)(_flags & GenericParameterAttributes.VarianceMask);
            }
        }
        internal override void EnsureAllConstraintsAreResolved(bool early)
        {
            if (!_lazyBounds.IsSet(early))
            {
                var typeParameters = (_containingSymbol.Kind == SymbolKind.Method) ?
                    ((AotMethodSymbol)_containingSymbol).TypeParameters :
                    ((AotNamedTypeSymbol)_containingSymbol).TypeParameters;
                EnsureAllConstraintsAreResolved(typeParameters, early);
            }
        }
        internal override ImmutableArray<TypeSymbolWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress, bool early)
        {
            var bounds = this.GetBounds(inProgress, early);
            return (bounds != null) ? bounds.ConstraintTypes : ImmutableArray<TypeSymbolWithAnnotations>.Empty;
        }
        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress, early: false);
            return (bounds != null) ? bounds.Interfaces : ImmutableArray<NamedTypeSymbol>.Empty;
        }
        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress, early: false);
            return (bounds != null) ? bounds.EffectiveBaseClass : this.GetDefaultBaseType();
        }
        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress, early: false);
            return (bounds != null) ? bounds.DeducedBaseType : this.GetDefaultBaseType();
        }
        private TypeParameterBounds GetBounds(ConsList<TypeParameterSymbol> inProgress, bool early)
        {
            // https://github.com/dotnet/roslyn/issues/30081: Re-enable asserts.
            //Debug.Assert(!inProgress.ContainsReference(this));
            //Debug.Assert(!inProgress.Any() || ReferenceEquals(inProgress.Head.ContainingSymbol, this.ContainingSymbol));

            var currentBounds = _lazyBounds;
            if (currentBounds == TypeParameterBounds.Unset)
            {
                var constraintTypes = GetDeclaredConstraintTypes();
                Debug.Assert(!constraintTypes.IsDefault);
                var bounds = new TypeParameterBounds(constraintTypes);
                CVM.AHelper.CompareExchange(ref _lazyBounds, bounds, currentBounds);
                currentBounds = _lazyBounds;
            }

            if (!currentBounds.IsSet(early))
            {
                var constraintTypes = currentBounds.ConstraintTypes;
                var diagnostics = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
                ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
                bool inherited = (_containingSymbol.Kind == SymbolKind.Method) && ((MethodSymbol)_containingSymbol).IsOverride;
                var bounds = this.ResolveBounds(this.ContainingAssembly.CorLibrary, inProgress.Prepend(this), constraintTypes, inherited, currentCompilation: null,
                                                diagnosticsBuilder: diagnostics, useSiteDiagnosticsBuilder: ref useSiteDiagnosticsBuilder);
                DiagnosticInfo errorInfo = null;

                if (diagnostics.Count > 0)
                {
                    errorInfo = diagnostics[0].DiagnosticInfo;
                }
                else if (useSiteDiagnosticsBuilder != null && useSiteDiagnosticsBuilder.Count > 0)
                {
                    foreach (var diag in useSiteDiagnosticsBuilder)
                    {
                        if (diag.DiagnosticInfo.Severity == DiagnosticSeverity.Error)
                        {
                            errorInfo = diag.DiagnosticInfo;
                            break;
                        }
                        else if ((object)errorInfo == null)
                        {
                            errorInfo = diag.DiagnosticInfo;
                        }
                    }
                }

                diagnostics.Free();

          //      CVM.AHelper.CompareExchange(ref _lazyConstraintsUseSiteErrorInfo, errorInfo, CSDiagnosticInfo.EmptyErrorInfo);
                CVM.AHelper.CompareExchange(ref _lazyBounds, bounds, currentBounds);
            }

            return _lazyBounds;
        }
        private NamedTypeSymbol GetDefaultBaseType()
        {
            return this.ContainingAssembly.GetSpecialType(SpecialType.System_Object);
        }
        private ImmutableArray<TypeSymbolWithAnnotations> GetDeclaredConstraintTypes()
        {
            if (_lazyDeclaredConstraintTypes.IsDefault)
            {
                ImmutableArray<TypeSymbolWithAnnotations> declaredConstraintTypes;

                AotMethodSymbol containingMethod = null;
                AotNamedTypeSymbol containingType;

                if (_containingSymbol.Kind == SymbolKind.Method)
                {
                    containingMethod = (AotMethodSymbol)_containingSymbol;
                    containingType = (AotNamedTypeSymbol)containingMethod.ContainingSymbol;
                }
                else
                {
                    containingType = (AotNamedTypeSymbol)_containingSymbol;
                }

                var moduleSymbol = containingType.ContainingAotModule;
                Type[] constraints;

                try
                {
                    constraints = _handle.GetGenericParameterConstraints();
                }
                catch 
                {
                    constraints = default(Type[]);
                //    CVM.AHelper.CompareExchange(ref _lazyConstraintsUseSiteErrorInfo, new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this), CSDiagnosticInfo.EmptyErrorInfo);
                }

                bool hasUnmanagedModreqPattern = false;

                if (constraints.Length > 0)
                {
                    var symbolsBuilder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance();
                    MetadataDecoder tokenDecoder;

                    //if ((object)containingMethod != null)
                    //{
                    //    tokenDecoder = new MetadataDecoder(moduleSymbol, containingMethod);
                    //}
                    //else
                    //{
                    //    tokenDecoder = new MetadataDecoder(moduleSymbol, containingType);
                    //}

                    foreach (var constraintHandle in constraints)
                    {
                        var nw = MetadataTypeName.FromFullName(constraintHandle.FullName);
                        var constraint = AotAssemblySymbol.Inst.Aot.LookupTopLevelMetadataType(ref nw);// metadataReader.GetGenericParameterConstraint(constraintHandle);
                        //var typeSymbol = tokenDecoder.DecodeGenericParameterConstraint(constraint.Type, out bool hasUnmanagedModreq);
                        var typeSymbol = constraint;
                        if (typeSymbol.SpecialType == SpecialType.System_ValueType)
                        {
                            // recognize "(class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType" pattern as "unmanaged"
                            //if (hasUnmanagedModreq)
                            //{
                            //    hasUnmanagedModreqPattern = true;
                            //}

                            // Drop 'System.ValueType' constraint type if the 'valuetype' constraint was also specified.
                            if (((_flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0))
                            {
                                continue;
                            }
                        }

                        var type = TypeSymbolWithAnnotations.Create(typeSymbol);
//                        type = NullableTypeDecoder.TransformType(type, constraintHandle, moduleSymbol);

                        // Drop 'System.Object?' constraint type.
                        if (type.SpecialType == SpecialType.System_Object && type.NullableAnnotation.IsAnyNullable())
                        {
                            continue;
                        }

                      //  type = TupleTypeDecoder.DecodeTupleTypesIfApplicable(type, constraintHandle, moduleSymbol);

                        symbolsBuilder.Add(type);
                    }

                    declaredConstraintTypes = symbolsBuilder.ToImmutableAndFree();
                }
                else
                {
                    declaredConstraintTypes = ImmutableArray<TypeSymbolWithAnnotations>.Empty;
                }

                // - presence of unmanaged pattern has to be matched with `valuetype`
                // - IsUnmanagedAttribute is allowed iif there is an unmanaged pattern
                //if (hasUnmanagedModreqPattern && (_flags & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0 ||
                //    hasUnmanagedModreqPattern != moduleSymbol.Module.HasIsUnmanagedAttribute(_handle))
                //{
                //    // we do not recognize these combinations as "unmanaged"
                //    hasUnmanagedModreqPattern = false;
                //    Interlocked.CompareExchange(ref _lazyConstraintsUseSiteErrorInfo, new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this), CSDiagnosticInfo.EmptyErrorInfo);
                //}

                _lazyHasIsUnmanagedConstraint = hasUnmanagedModreqPattern.ToThreeState();
                ImmutableInterlocked.InterlockedInitialize(ref _lazyDeclaredConstraintTypes, declaredConstraintTypes);
            }

            return _lazyDeclaredConstraintTypes;
        }

    }
}