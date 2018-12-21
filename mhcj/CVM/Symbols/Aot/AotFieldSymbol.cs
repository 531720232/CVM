// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CVM.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The class to represent all fields imported from a PE/module.
    /// </summary>
    internal sealed class AotFieldSymbol : FieldSymbol
    {
        private readonly FieldInfo _handle;
        private readonly string _name;
        private readonly FieldAttributes _flags;
        private readonly AotNamedTypeSymbol _containingType;
        private bool _lazyIsVolatile;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private ConstantValue _lazyConstantValue = Microsoft.CodeAnalysis.ConstantValue.Unset; // Indicates an uninitialized ConstantValue
        private CVM.Tuple2<CultureInfo, string> _lazyDocComment;
        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        private TypeSymbolWithAnnotations.Builder _lazyType;
        private int _lazyFixedSize;
        private NamedTypeSymbol _lazyFixedImplementationType;
        private AotEventSymbol _associatedEventOpt;

        internal AotFieldSymbol(
            AotModuleSymbol moduleSymbol,
            AotNamedTypeSymbol containingType,
            FieldInfo fieldDef)
        {


            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);


            _handle = fieldDef;
            _containingType = containingType;

            try
            {
                _name = fieldDef.Name;
                _flags = fieldDef.Attributes;

                //   moduleSymbol.GetFieldDefPropsOrThrow(fieldDef, out _name, out _flags);
            }
            catch
            {
                if ((object)_name == null)
                {
                    _name = String.Empty;
                }

                _lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        internal FieldAttributes Flags
        {
            get
            {
                return _flags;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return (_flags & FieldAttributes.SpecialName) != 0;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return (_flags & FieldAttributes.RTSpecialName) != 0;
            }
        }

        internal override bool IsNotSerialized
        {
            get
            {
                return (_flags & FieldAttributes.NotSerialized) != 0;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                // the compiler doesn't need full marshalling information, just the unmanaged type or descriptor
                return null;
            }
        }

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return ((_flags & FieldAttributes.HasFieldMarshal) != 0);
            }
        }

        internal override UnmanagedType MarshallingType
        {
            get
            {
                if ((_flags & FieldAttributes.HasFieldMarshal) == 0)
                {
                    return 0;
                }

             
                return UnmanagedType.LPStr;//_containingType.ContainingAotModule.GetMarshallingType(_handle);
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                if ((_flags & FieldAttributes.HasFieldMarshal) == 0)
                {
                    return default(ImmutableArray<byte>);
                }

                return default;
           //     return _containingType.ContainingAotModule.GetMarshallingDescriptor(_handle);
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return 0;
            }
        }

        internal FieldInfo Handle
        {
            get
            {
           
                return _handle;
            }
        }

        /// <summary>
        /// Mark this field as the backing field of a field-like event.
        /// The caller will also ensure that it is excluded from the member list of
        /// the containing type (as it would be in source).
        /// </summary>
        internal void SetAssociatedEvent(AotEventSymbol eventSymbol)
        {
            Debug.Assert((object)eventSymbol != null);
            Debug.Assert(eventSymbol.ContainingType == _containingType);

            // This should always be true in valid metadata - there should only
            // be one event with a given name in a given type.
            if ((object)_associatedEventOpt == null)
            {
                // No locking required since this method will only be called by the thread that created
                // the field symbol (and will be called before the field symbol is added to the containing 
                // type members and available to other threads).
                _associatedEventOpt = eventSymbol;
            }
        }


        //        _lazyType.InterlockedInitialize(type);
        //    }
        //}

        private AotModuleSymbol ContainingAotModule
        {
            get
            {
                return ((AotNamespaceSymbol)ContainingNamespace).ContainingAotModule;
            }
        }
        private bool IsFixedBuffer(out int fixedSize, out TypeSymbol fixedElementType)
        {
            fixedSize = 0;
            fixedElementType = null;

            //string elementTypeName;
            //int bufferSize;
            AotModuleSymbol containingAotModule = this.ContainingAotModule;
            try
            {
                var objs = _handle.GetCustomAttributes(typeof(System.Runtime.CompilerServices.FixedBufferAttribute), false);
                foreach (var obj in objs)
                {
                    if (obj is System.Runtime.CompilerServices.FixedBufferAttribute fix)
                    {

                        var item = containingAotModule.TypeHandleToTypeMap[fix.ElementType];
                        fixedSize = fix.Length;
                        fixedElementType = item;
                        return true;
                    }
                }

            }
            catch
            {

            }
            //if (containingAotModule.HasFixedBufferAttribute(_handle, out elementTypeName, out bufferSize))
            //{
            //    var decoder = new MetadataDecoder(containingPEModule);
            //    var elementType = decoder.GetTypeSymbolForSerializedType(elementTypeName);
            //    if (elementType.FixedBufferElementSizeInBytes() != 0)
            //    {
            //        fixedSize = bufferSize;
            //        fixedElementType = elementType;
            //        return true;
            //    }
            //}

            return false;
        }


        

        internal override TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            EnsureSignatureIsLoaded();
            return _lazyType.ToType();
        }

        public override bool IsFixedSizeBuffer
        {
            get
            {
                EnsureSignatureIsLoaded();
                return (object)_lazyFixedImplementationType != null;
            }
        }

        public override int FixedSize
        {
            get
            {
                EnsureSignatureIsLoaded();
                return _lazyFixedSize;
            }
        }

        private void EnsureSignatureIsLoaded()
        {
            throw new NotImplementedException();
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            EnsureSignatureIsLoaded();
            return _lazyFixedImplementationType;
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return _associatedEventOpt;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return (_flags & FieldAttributes.InitOnly) != 0;
            }
        }

        public override bool IsVolatile
        {
            get
            {
                EnsureSignatureIsLoaded();
                return _lazyIsVolatile;
            }
        }

        public override bool IsConst
        {
            get
            {
                return (_flags & FieldAttributes.Literal) != 0 || GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false) != null;
            }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            if (_lazyConstantValue == Microsoft.CodeAnalysis.ConstantValue.Unset)
            {
                ConstantValue value = null;

                if ((_flags & FieldAttributes.Literal) != 0)
                {
                    value = _containingType.ContainingAotModule.GetConstantFieldValue(_handle);
                }

                // If this is a Decimal, the constant value may come from DecimalConstantAttribute

                if (this.Type.SpecialType == SpecialType.System_Decimal)
                {
                    ConstantValue defaultValue;

                    if (_containingType.ContainingAotModule.HasDecimalConstantAttribute(Handle, out defaultValue))
                    {
                        value = defaultValue;
                    }
                }

                CVM.AHelper.CompareExchange(
                    ref _lazyConstantValue,
                    value,
                    Microsoft.CodeAnalysis.ConstantValue.Unset);
            }

            return _lazyConstantValue;
        }

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
                var access = Accessibility.Private;

                switch (_flags & FieldAttributes.FieldAccessMask)
                {
                    case FieldAttributes.Assembly:
                        access = Accessibility.Internal;
                        break;

                    case FieldAttributes.FamORAssem:
                        access = Accessibility.ProtectedOrInternal;
                        break;

                    case FieldAttributes.FamANDAssem:
                        access = Accessibility.ProtectedAndInternal;
                        break;

                    case FieldAttributes.Private:
                    case FieldAttributes.PrivateScope:
                        access = Accessibility.Private;
                        break;

                    case FieldAttributes.Public:
                        access = Accessibility.Public;
                        break;

                    case FieldAttributes.Family:
                        access = Accessibility.Protected;
                        break;

                    default:
                        access = Accessibility.Private;
                        break;
                }

                return access;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return (_flags & FieldAttributes.Static) != 0;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                var containingAotModuleSymbol = (AotModuleSymbol)this.ContainingModule;

                if (FilterOutDecimalConstantAttribute())
                {
                    // filter out DecimalConstantAttribute
                    //var attributes = containingAotModuleSymbol.GetCustomAttributesForToken(
                    //    _handle,
                    //    out _,
                    //    AttributeDescription.DecimalConstantAttribute);

                    //ImmutableInterlocked.InterlockedInitialize(ref _lazyCustomAttributes, attributes);
                }
                else
                {
              //  ..    containingAotModuleSymbol.LoadCustomAttributes(_handle, ref _lazyCustomAttributes);
                }
            }
            return _lazyCustomAttributes;
        }

        private bool FilterOutDecimalConstantAttribute()
        {
            ConstantValue value;
            return this.Type.SpecialType == SpecialType.System_Decimal &&
                   (object)(value = GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false)) != null &&
                   value.Discriminator == ConstantValueTypeDiscriminator.Decimal;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            foreach (CSharpAttributeData attribute in GetAttributes())
            {
                yield return attribute;
            }

            // Yield hidden attributes last, order might be important.
            if (FilterOutDecimalConstantAttribute())
            {

                var containingAotModuleSymbol = _containingType.ContainingAotModule;
                //yield return new AotAttributeData(containingAotModuleSymbol,
                //                          containingAotModuleSymbol.FindLastTargetAttribute(_handle, AttributeDescription.DecimalConstantAttribute).Handle);
            }
        }



        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                _lazyUseSiteDiagnostic = result;
            }

            return _lazyUseSiteDiagnostic;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref _lazyObsoleteAttributeData, _handle, (AotModuleSymbol)(this.ContainingModule), ignoreByRefLikeMarker: false);
                return _lazyObsoleteAttributeData;
            }
        }

        internal sealed override CVM_Zone DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        public override bool? NonNullTypes
        {
            get
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
