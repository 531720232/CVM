using CVM.Collections.Immutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
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



        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref _lazyObsoleteAttributeData, _handle, (AotModuleSymbol)(this.ContainingModule), ignoreByRefLikeMarker: false);
                return _lazyObsoleteAttributeData;
            }
        }

        private readonly Flags _flags;

        [Flags]
        private enum Flags : byte
        {
            IsSpecialName = 1,
            IsRuntimeSpecialName = 2,
            CallMethodsDirectly = 4
        }

        private ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        public override RefKind RefKind => _refKind;

        public override TypeSymbolWithAnnotations Type => _propertyType;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        public override bool IsIndexer
        {
            get
            {
                // NOTE: Dev10 appears to include static indexers in overload resolution 
                // for an array access expression, so it stands to reason that it considers
                // them indexers.
                if (this.ParameterCount > 0)
                {
                    string defaultMemberName = _containingType.DefaultMemberName;
                    return _name == defaultMemberName || //NB: not Name property (break mutual recursion)
                        ((object)this.GetMethod != null && this.GetMethod.Name == defaultMemberName) ||
                        ((object)this.SetMethod != null && this.SetMethod.Name == defaultMemberName);
                }
                return false;
            }
        }

        internal override bool HasSpecialName
        {
            get { return (_flags & Flags.IsSpecialName) != 0; }
        }


        public override MethodSymbol GetMethod
        {
            get { return _getMethod; }
        }

        public override MethodSymbol SetMethod
        {
            get { return _setMethod; }
        }

        internal override CallingConvention CallingConvention => CallingConvention.Standard;

        internal override bool MustCallMethodsDirectly => MustCallMethodsDirectlyCore();
        public override bool IsIndexedProperty
        {
            get
            {
                // Indexed property support is limited to types marked [ComImport],
                // to match the native compiler where the feature was scoped to
                // avoid supporting property groups.
                return (this.ParameterCount > 0) && _containingType.IsComImport;
            }
        }

        private bool MustCallMethodsDirectlyCore()
        {
            if (this.RefKind != RefKind.None && _setMethod != null)
            {
                return true;
            }
            else if (this.ParameterCount == 0)
            {
                return false;
            }
            else if (this.IsIndexedProperty)
            {
                return this.IsStatic;
            }
            else if (this.IsIndexer)
            {
                return this.HasRefOrOutParameter();
            }
            else
            {
                return true;
            }
        }
        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (((object)_getMethod == null || _getMethod.ExplicitInterfaceImplementations.Length == 0) &&
                    ((object)_setMethod == null || _setMethod.ExplicitInterfaceImplementations.Length == 0))
                {
                    return ImmutableArray<PropertySymbol>.Empty;
                }

                var propertiesWithImplementedGetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(_getMethod);
                var propertiesWithImplementedSetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(_setMethod);

                var builder = ArrayBuilder<PropertySymbol>.GetInstance();

                foreach (var prop in propertiesWithImplementedGetters)
                {
                    if ((object)prop.SetMethod == null || propertiesWithImplementedSetters.Contains(prop))
                    {
                        builder.Add(prop);
                    }
                }

                foreach (var prop in propertiesWithImplementedSetters)
                {
                    // No need to worry about duplicates.  If prop was added by the previous loop,
                    // then it must have a GetMethod.
                    if ((object)prop.GetMethod == null)
                    {
                        builder.Add(prop);
                    }
                }

                return builder.ToImmutableAndFree();
            }
        }
        public override Symbol ContainingSymbol => _containingType;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _containingType.ContainingAotModule.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }
        public override Accessibility DeclaredAccessibility
        {
            get
            {
                if (_declaredAccessibility == UnsetAccessibility)
                {
                    Accessibility accessibility;
                    if (this.IsOverride)
                    {
                        // Determining the accessibility of an overriding property is tricky.  It should be
                        // based on the accessibilities of the accessors, but the overriding property need
                        // not override both accessors.  As a result, we may need to look at the accessors
                        // of an overridden method.
                        //
                        // One might assume that we could just go straight to the least-derived 
                        // property (i.e. the original virtual property) and check its accessors, but
                        // that can yield incorrect results if the least-derived property is in a
                        // different assembly.  For any overridden and (directly) overriding members, M and M',
                        // in different assemblies, A1 and A2, if M is protected internal, then M' must be 
                        // protected internal if the internals of A1 are visible to A2 and protected otherwise.
                        //
                        // Therefore, if we cross an assembly boundary in the course of walking up the
                        // override chain, and if the overriding assembly cannot see the internals of the
                        // overridden assembly, then any protected internal accessors we find should be 
                        // treated as protected, for the purposes of determining property accessibility.
                        //
                        // NOTE: This process has no effect on accessor accessibility - a protected internal
                        // accessor in another assembly will still have declared accessibility protected internal.
                        // The difference between the accessibilities of the overriding and overridden accessors
                        // will be accommodated later, when we check for CS0507 (ERR_CantChangeAccessOnOverride).

                        bool crossedAssemblyBoundaryWithoutInternalsVisibleTo = false;
                        Accessibility getAccessibility = Accessibility.NotApplicable;
                        Accessibility setAccessibility = Accessibility.NotApplicable;
                        PropertySymbol curr = this;
                        while (true)
                        {
                            if (getAccessibility == Accessibility.NotApplicable)
                            {
                                MethodSymbol getMethod = curr.GetMethod;
                                if ((object)getMethod != null)
                                {
                                    Accessibility overriddenAccessibility = getMethod.DeclaredAccessibility;
                                    getAccessibility = overriddenAccessibility == Accessibility.ProtectedOrInternal && crossedAssemblyBoundaryWithoutInternalsVisibleTo
                                        ? Accessibility.Protected
                                        : overriddenAccessibility;
                                }
                            }

                            if (setAccessibility == Accessibility.NotApplicable)
                            {
                                MethodSymbol setMethod = curr.SetMethod;
                                if ((object)setMethod != null)
                                {
                                    Accessibility overriddenAccessibility = setMethod.DeclaredAccessibility;
                                    setAccessibility = overriddenAccessibility == Accessibility.ProtectedOrInternal && crossedAssemblyBoundaryWithoutInternalsVisibleTo
                                        ? Accessibility.Protected
                                        : overriddenAccessibility;
                                }
                            }

                            if (getAccessibility != Accessibility.NotApplicable && setAccessibility != Accessibility.NotApplicable)
                            {
                                break;
                            }

                            PropertySymbol next = curr.OverriddenProperty;

                            if ((object)next == null)
                            {
                                break;
                            }

                            if (!crossedAssemblyBoundaryWithoutInternalsVisibleTo && !curr.ContainingAssembly.HasInternalAccessTo(next.ContainingAssembly))
                            {
                                crossedAssemblyBoundaryWithoutInternalsVisibleTo = true;
                            }

                            curr = next;
                        }

                        accessibility = PEPropertyOrEventHelpers.GetDeclaredAccessibilityFromAccessors(getAccessibility, setAccessibility);
                    }
                    else
                    {
                        accessibility = PEPropertyOrEventHelpers.GetDeclaredAccessibilityFromAccessors(this.GetMethod, this.SetMethod);
                    }

                    CVM.AHelper.CompareExchange(ref _declaredAccessibility, (int)accessibility, UnsetAccessibility);
                }

                return (Accessibility)_declaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                // All accessors static.
                return
                    ((object)_getMethod == null || _getMethod.IsStatic) &&
                    ((object)_setMethod == null || _setMethod.IsStatic);
            }
        }


        public override bool IsVirtual
        {
            get
            {
                // Some accessor virtual (as long as another isn't override or abstract).
                return !IsOverride && !IsAbstract &&
                       (((object)_getMethod != null && _getMethod.IsVirtual) ||
                        ((object)_setMethod != null && _setMethod.IsVirtual));
            }
        }

        public override bool IsOverride
        {
            get
            {
                // Some accessor override.
                return
                    ((object)_getMethod != null && _getMethod.IsOverride) ||
                    ((object)_setMethod != null && _setMethod.IsOverride);
            }
        }

        public override bool IsAbstract
        {
            get
            {
                // Some accessor abstract.
                return
                    ((object)_getMethod != null && _getMethod.IsAbstract) ||
                    ((object)_setMethod != null && _setMethod.IsAbstract);
            }
        }

        public override bool IsSealed
        {
            get
            {
                // All accessors sealed.
                return
                    ((object)_getMethod == null || _getMethod.IsSealed) &&
                    ((object)_setMethod == null || _setMethod.IsSealed);
            }
        }
        public override bool IsExtern
        {
            get
            {
                // Some accessor extern.
                return
                    ((object)_getMethod != null && _getMethod.IsExtern) ||
                    ((object)_setMethod != null && _setMethod.IsExtern);
            }
        }



        public override string Name
        {
            get { return this.IsIndexer ? WellKnownMemberNames.Indexer : _name; }
        }

        internal static AotPropertySymbol Create(AotModuleSymbol module, AotNamedTypeSymbol aotNamedTypeSymbol, PropertyInfo propertyDef, AotMethodSymbol getMethod, AotMethodSymbol setMethod)
        {
            
            Debug.Assert((object)module != null);
            Debug.Assert((object)aotNamedTypeSymbol != null);
            Debug.Assert(propertyDef!=null);
      //          .          ..  var propertyParams = MetadataDecoder.GetSignatureForProperty(propertyDef);

            AotPropertySymbol result = new AotPropertySymbol(module, aotNamedTypeSymbol, propertyDef, getMethod, setMethod, 0, default,new MetadataDecoder(module,aotNamedTypeSymbol));
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

        
            //var getMethodParams = (object)getMethod == null ? null : MetadataDecoder.GetSignatureForMethod(getMethod.Handle1);
            //var setMethodParams = (object)setMethod == null ? null : MetadataDecoder.GetSignatureForMethod(setMethod.Handle1);

           
            // NOTE: property parameter names are not recorded in metadata, so we have to
            // use the parameter names from one of the indexers
            // NB: prefer setter names to getter names if both are present.
            bool isBad;

            if (setMethod == null)
            {

            }
            else
            {
                
            }

            _parameters = setMethod == null
                ? GetParameters(moduleSymbol, this, handle, getMethod, getMethod.IsMetadataVirtual(), out isBad)
                : GetParameters(moduleSymbol, this, handle, setMethod, setMethod.IsMetadataVirtual(), out isBad);

            if ( mrEx != null || isBad)
            {
                _lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }

            var returnInfo = new MetadataDecoder(moduleSymbol);//  propertyParams[0];
                                                               // var typeCustomModifiers = CSharpCustomModifier.Convert(returnInfo.CustomModifiers);

            var gt = returnInfo.GetTypeOfToken(handle.PropertyType);


            if ( handle.PropertyType.IsByRef)
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
            TypeSymbol originalPropertyType = gt;// returnInfo.Type;

        //..    originalPropertyType = DynamicTypeDecoder.TransformType(originalPropertyType, typeCustomModifiers.Length, handle, moduleSymbol, _refKind);

            // Dynamify object type if necessary
            //      originalPropertyType = originalPropertyType.AsDynamicIfNoPia(_containingType);

            // We start without annotation (they will be decoded below)
            var propertyType = TypeSymbolWithAnnotations.Create(originalPropertyType, customModifiers: default);

            // Decode nullable before tuple types to avoid converting between
            // NamedTypeSymbol and TupleTypeSymbol unnecessarily.
     //       propertyType = NullableTypeDecoder.TransformType(propertyType, handle, moduleSymbol);
      //      propertyType = TupleTypeDecoder.DecodeTupleTypesIfApplicable(propertyType, handle, moduleSymbol);

            _propertyType = propertyType;

            // A property is bogus and must be accessed by calling its accessors directly if the
            // accessor signatures do not agree, both with each other and with the property,
            // or if it has parameters and is not an indexer or indexed property.
            bool callMethodsDirectly = MustCallMethodsDirectlyCore();

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

        private static ImmutableArray<ParameterSymbol> GetParameters(AotModuleSymbol moduleSymbol,
            AotPropertySymbol property,PropertyInfo handle,AotMethodSymbol acc,bool is_virtual,
            out bool anyParameterIsBad
        )
        {
            anyParameterIsBad = false;
            var ars = handle.GetIndexParameters();
            if (ars.Length == 0)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }
            var parameters = new ParameterSymbol[ars.Length];
            var AccessorParams = acc.Handle.GetParameters();
            var numAccessorParams = AccessorParams.Length;
          
            int i = 1;

            foreach (var ar in ars)
            {
                var paramHandle = i < numAccessorParams ? AccessorParams[i] : ar ;
                var ordinal = i - 1;
                bool isBad;
                var t1 = new ParamInfo<TypeSymbol>();
              var dc= new MetadataDecoder(moduleSymbol);
              t1.Type=  dc.GetTypeOfToken(paramHandle.ParameterType);
                parameters[ordinal] = AotParameterSymbol.Create(moduleSymbol, property, is_virtual, ordinal, paramHandle, t1, extraAnnotations: default, out isBad);

                if (isBad)
                {
                    anyParameterIsBad = true;
                }

                i ++;
            }
            return parameters.AsImmutableOrNull();
        }
        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = (AotModuleSymbol)this.ContainingModule;

                ImmutableArray<CSharpAttributeData> attributes = containingPEModuleSymbol.GetCustomAttributesForToken(
                      _handle,
                      out _,
                      this.RefKind == RefKind.RefReadOnly ? AttributeDescription.IsReadOnlyAttribute : default);

                ImmutableInterlocked.InterlockedInitialize(ref _lazyCustomAttributes, attributes);
            }
            return _lazyCustomAttributes;
        }
        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return GetAttributes();
        }


    }
}