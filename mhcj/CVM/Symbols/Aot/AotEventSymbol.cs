// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CVM.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class AotEventSymbol:EventSymbol
    {
        private readonly string _name;
        private readonly AotNamedTypeSymbol _containingType;
        private readonly System.Reflection.EventInfo _handle;
        private readonly TypeSymbolWithAnnotations _eventType;
        private readonly AotMethodSymbol _addMethod;
        private readonly AotMethodSymbol _removeMethod;
        private readonly AotFieldSymbol _associatedFieldOpt;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo;


        private ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        private const int UnsetAccessibility = -1;
        private int _lazyDeclaredAccessibility = UnsetAccessibility;

        private readonly System.Reflection.EventAttributes _flags;

        public override TypeSymbolWithAnnotations Type
        {
            get { return _eventType; }
        }


        public override MethodSymbol AddMethod
        {
            get { return _addMethod; }
        }

        public override MethodSymbol RemoveMethod
        {
            get { return _removeMethod; }
        }


        public override bool IsWindowsRuntimeEvent => false;//_flags&System.Reflection.EventAttributes.RTSpecialName;

        internal override bool HasSpecialName => (_flags&System.Reflection.EventAttributes.SpecialName)!=0;

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (_addMethod.ExplicitInterfaceImplementations.Length == 0 &&
                    _removeMethod.ExplicitInterfaceImplementations.Length == 0)
                {
                    return ImmutableArray<EventSymbol>.Empty;
                }

                var implementedEvents = PEPropertyOrEventHelpers.GetEventsForExplicitlyImplementedAccessor(_addMethod);
                implementedEvents.IntersectWith(PEPropertyOrEventHelpers.GetEventsForExplicitlyImplementedAccessor(_removeMethod));

                var builder = ArrayBuilder<EventSymbol>.GetInstance();

                foreach (var @event in implementedEvents)
                {
                    builder.Add(@event);
                }

                return builder.ToImmutableAndFree();
            }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return (_flags & System.Reflection.EventAttributes.ReservedMask) != 0; }
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

        public override Accessibility DeclaredAccessibility =>Accessibility.Public;

        public override bool IsStatic
        {
            get
            {
                // All accessors static.
                return _addMethod.IsStatic && _removeMethod.IsStatic;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                // Some accessor virtual (as long as another isn't override or abstract).
                return !IsOverride && !IsAbstract && (_addMethod.IsVirtual || _removeMethod.IsVirtual);
            }
        }

        public override bool IsOverride
        {
            get
            {
                // Some accessor override.
                return _addMethod.IsOverride || _removeMethod.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                // Some accessor abstract.
                return _addMethod.IsAbstract || _removeMethod.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                // Some accessor sealed. (differs from properties)
                return _addMethod.IsSealed || _removeMethod.IsSealed;
            }
        }


        public override bool IsExtern
        {
            get
            {
                // Some accessor extern.
                return _addMethod.IsExtern || _removeMethod.IsExtern;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {

            get
            {

                if(_lazyObsoleteAttributeData==null)
                {
                   
                    _lazyObsoleteAttributeData =MetadataDecoder.GetOb(_handle);
                }
                return _lazyObsoleteAttributeData;
            }
        }

        public AotEventSymbol(AotModuleSymbol moduleSymbol,
            AotNamedTypeSymbol containingType,
            System.Reflection.EventInfo handle,
            AotMethodSymbol addMethod,
            AotMethodSymbol removeMethod,
            MultiDictionary<string, AotFieldSymbol> privateFieldNameToSymbols)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(handle!=null);
            Debug.Assert((object)addMethod != null);
            Debug.Assert((object)removeMethod != null);
            _addMethod = addMethod;
            _removeMethod = removeMethod;
            _handle = handle;
            _containingType = containingType;

            _flags = handle.Attributes;
            _name = handle.Name;

            try
            {

                _eventType =TypeSymbolWithAnnotations.Create(moduleSymbol.TypeHandleToTypeMap[handle.EventHandlerType]);
            }
            catch
            {

            }
        }
    }
}