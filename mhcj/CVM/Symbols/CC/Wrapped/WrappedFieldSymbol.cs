﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CVM.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a field that is based on another field.
    /// When inheriting from this class, one shouldn't assume that 
    /// the default behavior it has is appropriate for every case.
    /// That behavior should be carefully reviewed and derived type
    /// should override behavior as appropriate.
    /// </summary>
    internal abstract class WrappedFieldSymbol : FieldSymbol
    {
        /// <summary>
        /// The underlying FieldSymbol.
        /// </summary>
        protected readonly FieldSymbol _underlyingField;

        public WrappedFieldSymbol(FieldSymbol underlyingField)
        {
            Debug.Assert((object)underlyingField != null);
            _underlyingField = underlyingField;
        }

        public FieldSymbol UnderlyingField
        {
            get
            {
                return _underlyingField;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingField.IsImplicitlyDeclared; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingField.DeclaredAccessibility;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingField.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingField.HasSpecialName;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _underlyingField.HasRuntimeSpecialName;
            }
        }

      
        internal override bool IsNotSerialized
        {
            get
            {
                return _underlyingField.IsNotSerialized;
            }
        }

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return _underlyingField.IsMarshalledExplicitly;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return _underlyingField.MarshallingInformation;
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                return _underlyingField.MarshallingDescriptor;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return _underlyingField.TypeLayoutOffset;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return _underlyingField.IsReadOnly;
            }
        }

        public override bool IsVolatile
        {
            get
            {
                return _underlyingField.IsVolatile;
            }
        }

        public override bool IsConst
        {
            get
            {
                return _underlyingField.IsConst;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return _underlyingField.ObsoleteAttributeData;
            }
        }

        public override object ConstantValue
        {
            get
            {
                return _underlyingField.ConstantValue;
            }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            return _underlyingField.GetConstantValue(inProgress, earlyDecodingWellKnownAttributes);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingField.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingField.DeclaringSyntaxReferences;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingField.IsStatic;
            }
        }
    }
}
