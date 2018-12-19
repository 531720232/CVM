// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using CVM.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class PEAssemblyBuilderBase : PEModuleBuilder, Cci.IAssemblyReference
    {
        private readonly SourceAssemblySymbol _sourceAssembly;
        private readonly ImmutableArray<NamedTypeSymbol> _additionalTypes;
        private ImmutableArray<Cci.IFileReference> _lazyFiles;

        private SynthesizedEmbeddedAttributeSymbol _lazyEmbeddedAttribute;
        private SynthesizedEmbeddedAttributeSymbol _lazyIsReadOnlyAttribute;
        private SynthesizedEmbeddedAttributeSymbol _lazyIsByRefLikeAttribute;
        private SynthesizedEmbeddedAttributeSymbol _lazyIsUnmanagedAttribute;
        private SynthesizedEmbeddedAttributeSymbol _lazyNullableAttribute;

        /// <summary>
        /// The behavior of the C# command-line compiler is as follows:
        ///   1) If the /out switch is specified, then the explicit assembly name is used.
        ///   2) Otherwise,
        ///      a) if the assembly is executable, then the assembly name is derived from
        ///         the name of the file containing the entrypoint;
        ///      b) otherwise, the assembly name is derived from the name of the first input
        ///         file.
        /// 
        /// Since we don't know which method is the entrypoint until well after the
        /// SourceAssemblySymbol is created, in case 2a, its name will not reflect the
        /// name of the file containing the entrypoint.  We leave it to our caller to
        /// provide that name explicitly.
        /// </summary>
        /// <remarks>
        /// In cases 1 and 2b, we expect (metadataName == sourceAssembly.MetadataName).
        /// </remarks>
        private readonly string _metadataName;

        public PEAssemblyBuilderBase(
            SourceAssemblySymbol sourceAssembly,
            EmitOptions emitOptions,
            OutputKind outputKind,
            ImmutableArray<NamedTypeSymbol> additionalTypes)
            : base((SourceModuleSymbol)sourceAssembly.Modules[0], emitOptions, outputKind)
        {
            Debug.Assert((object)sourceAssembly != null);

            _sourceAssembly = sourceAssembly;
            _additionalTypes = additionalTypes.NullToEmpty();
            _metadataName =sourceAssembly.MetadataName ;

            AssemblyOrModuleSymbolToModuleRefMap.TryAdd(sourceAssembly, this);
        }

        public override ISourceAssemblySymbolInternal SourceAssemblyOpt => _sourceAssembly;

        internal override ImmutableArray<NamedTypeSymbol> GetAdditionalTopLevelTypes(DiagnosticBag diagnostics)
        {
            return _additionalTypes;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetEmbeddedTypes(DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            CreateEmbeddedAttributesIfNeeded(diagnostics);
            if ((object)_lazyEmbeddedAttribute != null)
            {
                builder.Add(_lazyEmbeddedAttribute);
            }

            if ((object)_lazyIsReadOnlyAttribute != null)
            {
                builder.Add(_lazyIsReadOnlyAttribute);
            }

            if ((object)_lazyIsUnmanagedAttribute != null)
            {
                builder.Add(_lazyIsUnmanagedAttribute);
            }

            if ((object)_lazyIsByRefLikeAttribute != null)
            {
                builder.Add(_lazyIsByRefLikeAttribute);
            }

            if ((object)_lazyNullableAttribute != null)
            {
                builder.Add(_lazyNullableAttribute);
            }

            return builder.ToImmutableAndFree();
        }


  

        public override string Name => _metadataName;
        public AssemblyIdentity Identity => _sourceAssembly.Identity;
        public Version AssemblyVersionPattern => _sourceAssembly.AssemblyVersionPattern;

        internal override SynthesizedAttributeData SynthesizeEmbeddedAttribute()
        {
            // _lazyEmbeddedAttribute should have been created before calling this method.
            return new SynthesizedAttributeData(
                _lazyEmbeddedAttribute.Constructors[0],
                ImmutableArray<TypedConstant>.Empty,
                ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }

        internal override SynthesizedAttributeData SynthesizeNullableAttribute(WellKnownMember member, ImmutableArray<TypedConstant> arguments)
        {
            if ((object)_lazyNullableAttribute != null)
            {
                var constructorIndex = (member == WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags) ? 1 : 0;
                return new SynthesizedAttributeData(
                    _lazyNullableAttribute.Constructors[constructorIndex],
                    arguments,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.SynthesizeNullableAttribute(member, arguments);
        }

        protected override SynthesizedAttributeData TrySynthesizeIsReadOnlyAttribute()
        {
            if ((object)_lazyIsReadOnlyAttribute != null)
            {
                return new SynthesizedAttributeData(
                    _lazyIsReadOnlyAttribute.Constructors[0],
                    ImmutableArray<TypedConstant>.Empty,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.TrySynthesizeIsReadOnlyAttribute();
        }

        protected override SynthesizedAttributeData TrySynthesizeIsUnmanagedAttribute()
        {
            if ((object)_lazyIsUnmanagedAttribute != null)
            {
                return new SynthesizedAttributeData(
                    _lazyIsUnmanagedAttribute.Constructors[0],
                    ImmutableArray<TypedConstant>.Empty,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.TrySynthesizeIsUnmanagedAttribute();
        }

        protected override SynthesizedAttributeData TrySynthesizeIsByRefLikeAttribute()
        {
            if ((object)_lazyIsByRefLikeAttribute != null)
            {
                return new SynthesizedAttributeData(
                    _lazyIsByRefLikeAttribute.Constructors[0],
                    ImmutableArray<TypedConstant>.Empty,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.TrySynthesizeIsByRefLikeAttribute();
        }

        private void CreateEmbeddedAttributesIfNeeded(DiagnosticBag diagnostics)
        {
            if (this.NeedsGeneratedIsReadOnlyAttribute)
            {
                CreateEmbeddedAttributeItselfIfNeeded(diagnostics);

                CreateEmbeddedAttributeIfNeeded(
                    ref _lazyIsReadOnlyAttribute,
                    diagnostics,
                    AttributeDescription.IsReadOnlyAttribute);
            }

            if (this.NeedsGeneratedIsByRefLikeAttribute)
            {
                CreateEmbeddedAttributeItselfIfNeeded(diagnostics);

                CreateEmbeddedAttributeIfNeeded(
                    ref _lazyIsByRefLikeAttribute,
                    diagnostics,
                    AttributeDescription.IsByRefLikeAttribute);
            }

            if (this.NeedsGeneratedIsUnmanagedAttribute)
            {
                CreateEmbeddedAttributeItselfIfNeeded(diagnostics);

                CreateEmbeddedAttributeIfNeeded(
                    ref _lazyIsUnmanagedAttribute,
                    diagnostics,
                    AttributeDescription.IsUnmanagedAttribute);
            }

            if (this.NeedsGeneratedNullableAttribute)
            {
                CreateEmbeddedAttributeItselfIfNeeded(diagnostics);

                CreateEmbeddedAttributeIfNeeded(
                    ref _lazyNullableAttribute,
                    diagnostics,
                    AttributeDescription.NullableAttribute,
                    GetNullableAttributeConstructors);
            }
        }

        private void CreateEmbeddedAttributeItselfIfNeeded(DiagnosticBag diagnostics)
        {
            CreateEmbeddedAttributeIfNeeded(
                ref _lazyEmbeddedAttribute,
                diagnostics,
                AttributeDescription.CodeAnalysisEmbeddedAttribute);
        }

        private void CreateEmbeddedAttributeIfNeeded(
            ref SynthesizedEmbeddedAttributeSymbol symbol,
            DiagnosticBag diagnostics,
            AttributeDescription description,
            Func<CSharp.CVM_Zone, NamedTypeSymbol, DiagnosticBag, ImmutableArray<MethodSymbol>> getConstructors = null)
        {
            if ((object)symbol == null)
            {
                var attributeMetadataName = MetadataTypeName.FromFullName(description.FullName);
                var userDefinedAttribute = _sourceAssembly.SourceModule.LookupTopLevelMetadataType(ref attributeMetadataName);
                Debug.Assert((object)userDefinedAttribute.ContainingModule == _sourceAssembly.SourceModule);

                if (!(userDefinedAttribute is MissingMetadataTypeSymbol))
                {
                    diagnostics.Add(ErrorCode.ERR_TypeReserved, userDefinedAttribute.Locations[0], description.FullName);
                }

                symbol = new SynthesizedEmbeddedAttributeSymbol(description, _sourceAssembly.DeclaringCompilation, getConstructors, diagnostics);
            }
        }

        private static ImmutableArray<MethodSymbol> GetNullableAttributeConstructors(
            CVM_Zone compilation,
            NamedTypeSymbol containingType,
            DiagnosticBag diagnostics)
        {
            var byteType = TypeSymbolWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Byte));
            Binder.ReportUseSiteDiagnostics(byteType.TypeSymbol, diagnostics, Location.None);
            var byteArray = TypeSymbolWithAnnotations.Create(
                ArrayTypeSymbol.CreateSZArray(
                    byteType.TypeSymbol.ContainingAssembly,
                    byteType));
            return ImmutableArray.Create<MethodSymbol>(
                new SynthesizedEmbeddedAttributeConstructorSymbol(
                    containingType,
                    m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, byteType, 0, RefKind.None))),
                new SynthesizedEmbeddedAttributeConstructorSymbol(
                    containingType,
                    m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, byteArray, 0, RefKind.None))));
        }
    }

    internal sealed class PEAssemblyBuilder : PEAssemblyBuilderBase
    {
        public PEAssemblyBuilder(
            SourceAssemblySymbol sourceAssembly,
            EmitOptions emitOptions,
            OutputKind outputKind
         )
            : base(sourceAssembly, emitOptions, outputKind, ImmutableArray<NamedTypeSymbol>.Empty)
        {
        }

        public override int CurrentGenerationOrdinal => 0;
    }
}
