﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CVM.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class for all parameters that are emitted.
    /// </summary>
    internal abstract class SourceParameterSymbolBase : ParameterSymbol
    {
        private readonly Symbol _containingSymbol;
        private readonly ushort _ordinal;

        public SourceParameterSymbolBase(Symbol containingSymbol, int ordinal)
        {
            Debug.Assert((object)containingSymbol != null);
            _ordinal = (ushort)ordinal;
            _containingSymbol = containingSymbol;
        }

        public sealed override bool Equals(object obj)
        {
            if (obj == (object)this)
            {
                return true;
            }

            var symbol = obj as SourceParameterSymbolBase;
            return (object)symbol != null
                && symbol.Ordinal == this.Ordinal
                && Equals(symbol._containingSymbol, _containingSymbol);
        }

        public sealed override int GetHashCode()
        {
            return Hash.Combine(_containingSymbol.GetHashCode(), this.Ordinal);
        }

        public sealed override int Ordinal
        {
            get { return _ordinal; }
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        public sealed override AssemblySymbol ContainingAssembly
        {
            get { return _containingSymbol.ContainingAssembly; }
        }

        internal abstract ConstantValue DefaultValueFromAttributes { get; }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;

            if (this.IsParams)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_ParamArrayAttribute__ctor));
            }

            // Synthesize DecimalConstantAttribute if we don't have an explicit custom attribute already:
            var defaultValue = this.ExplicitDefaultConstantValue;
            if (defaultValue != ConstantValue.NotAvailable &&
                defaultValue.SpecialType == SpecialType.System_Decimal &&
                DefaultValueFromAttributes == ConstantValue.NotAvailable)
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDecimalConstantAttribute(defaultValue.DecimalValue));
            }

            var type = this.Type;

            if (type.TypeSymbol.ContainsDynamic())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(type.TypeSymbol, type.CustomModifiers.Length + this.RefCustomModifiers.Length, this.RefKind));
            }

            if (type.TypeSymbol.ContainsTupleNames())
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeTupleNamesAttribute(type.TypeSymbol));
            }

            if (this.RefKind == RefKind.RefReadOnly)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(this));
            }

            if (type.NeedsNullableAttribute())
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttribute(this, type));
            }
        }

        internal abstract ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ImmutableArray<CustomModifier> newRefCustomModifiers, bool newIsParams);
    }
}
