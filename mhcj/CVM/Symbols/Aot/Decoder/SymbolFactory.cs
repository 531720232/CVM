﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using CVM.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SymbolFactory : SymbolFactory<AotModuleSymbol, TypeSymbol>
    {
        internal static readonly SymbolFactory Instance = new SymbolFactory();

        internal override TypeSymbol GetMDArrayTypeSymbol(AotModuleSymbol moduleSymbol, int rank, TypeSymbol elementType, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers,
                                                          ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
        {
            if (elementType is UnsupportedMetadataTypeSymbol)
            {
                return elementType;
            }

            return ArrayTypeSymbol.CreateMDArray(moduleSymbol.ContainingAssembly, CreateType(elementType, customModifiers), rank, sizes, lowerBounds);
        }

        internal override TypeSymbol GetSpecialType(AotModuleSymbol moduleSymbol, SpecialType specialType)
        {
            return moduleSymbol.ContainingAssembly.GetSpecialType(specialType);
        }

        internal override TypeSymbol GetSystemTypeSymbol(AotModuleSymbol moduleSymbol)
        {
            return moduleSymbol.SystemTypeSymbol;
        }

        internal override TypeSymbol GetCSArrayTypeSymbol(AotModuleSymbol moduleSymbol, int rank,
            TypeSymbol elementType, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers)
        {
            return ArrayTypeSymbol.CreateCSharpArray(AotAssemblySymbol.Inst, CreateType(elementType, customModifiers), rank);
        }

        internal override TypeSymbol MakePointerTypeSymbol(AotModuleSymbol moduleSymbol, TypeSymbol type, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers)
        {
            if (type is UnsupportedMetadataTypeSymbol)
            {
                return type;
            }

            return new PointerTypeSymbol(CreateType(type, customModifiers));
        }

        internal override TypeSymbol GetEnumUnderlyingType(AotModuleSymbol moduleSymbol, TypeSymbol type)
        {
            return type.GetEnumUnderlyingType();
        }

        internal override Cci.PrimitiveTypeCode GetPrimitiveTypeCode(AotModuleSymbol moduleSymbol, TypeSymbol type)
        {
            return type.PrimitiveTypeCode;
        }

        internal override bool IsAcceptedVolatileModifierType(AotModuleSymbol moduleSymbol, TypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_Runtime_CompilerServices_IsVolatile;
        }

        internal override bool IsAcceptedInAttributeModifierType(TypeSymbol type)
        {
            return type.IsWellKnownTypeInAttribute();
        }

        internal override bool IsAcceptedUnmanagedTypeModifierType(TypeSymbol type)
        {
            return type.IsWellKnownTypeUnmanagedType();
        }

        internal override TypeSymbol GetSZArrayTypeSymbol(AotModuleSymbol moduleSymbol, TypeSymbol elementType, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers)
        {
            if (elementType is UnsupportedMetadataTypeSymbol)
            {
                return elementType;
            }

            return ArrayTypeSymbol.CreateSZArray(moduleSymbol.ContainingAssembly, CreateType(elementType, customModifiers));
        }

        internal override TypeSymbol GetUnsupportedMetadataTypeSymbol(AotModuleSymbol moduleSymbol, BadImageFormatException exception)
        {
            return new UnsupportedMetadataTypeSymbol(exception);
        }

        internal override TypeSymbol SubstituteTypeParameters(
            AotModuleSymbol moduleSymbol,
            TypeSymbol genericTypeDef,
            ImmutableArray<KeyValuePair<TypeSymbol, ImmutableArray<ModifierInfo<TypeSymbol>>>> arguments,
            ImmutableArray<bool> refersToNoPiaLocalType)
        {
            if (genericTypeDef is UnsupportedMetadataTypeSymbol)
            {
                return genericTypeDef;
            }

            // Let's return unsupported metadata type if any argument is unsupported metadata type 
            foreach (var arg in arguments)
            {
                if (arg.Key.Kind == SymbolKind.ErrorType &&
                    arg.Key is UnsupportedMetadataTypeSymbol)
                {
                    return new UnsupportedMetadataTypeSymbol();
                }
            }

            NamedTypeSymbol genericType = (NamedTypeSymbol)genericTypeDef;

            // See if it is or its enclosing type is a non-interface closed over NoPia local types. 
            ImmutableArray<AssemblySymbol> linkedAssemblies = moduleSymbol.ContainingAssembly.GetLinkedReferencedAssemblies();

            bool noPiaIllegalGenericInstantiation = false;

            //if (!linkedAssemblies.IsDefaultOrEmpty || moduleSymbol.Module.ContainsNoPiaLocalTypes())
            //{
            //    NamedTypeSymbol typeToCheck = genericType;
            //    int argumentIndex = refersToNoPiaLocalType.Length - 1;

            //    do
            //    {
            //        if (!typeToCheck.IsInterface)
            //        {
            //            break;
            //        }
            //        else
            //        {
            //            argumentIndex -= typeToCheck.Arity;
            //        }

            //        typeToCheck = typeToCheck.ContainingType;
            //    }
            //    while ((object)typeToCheck != null);

            //    for (int i = argumentIndex; i >= 0; i--)
            //    {
            //        if (refersToNoPiaLocalType[i] ||
            //            (!linkedAssemblies.IsDefaultOrEmpty &&
            //            MetadataDecoder.IsOrClosedOverATypeFromAssemblies(arguments[i].Key, linkedAssemblies)))
            //        {
            //            noPiaIllegalGenericInstantiation = true;
            //            break;
            //        }
            //    }
            //}

            // Collect generic parameters for the type and its containers in the order
            // that matches passed in arguments, i.e. sorted by the nesting.
            ImmutableArray<TypeParameterSymbol> typeParameters = genericType.GetAllTypeParameters();
            Debug.Assert(typeParameters.Length > 0);

            if (typeParameters.Length != arguments.Length)
            {
                return new UnsupportedMetadataTypeSymbol();
            }

            TypeMap substitution = new TypeMap(typeParameters, arguments.SelectAsArray(arg => CreateType(arg.Key, arg.Value)));

            NamedTypeSymbol constructedType = substitution.SubstituteNamedType(genericType);

            if (noPiaIllegalGenericInstantiation)
            {
                constructedType = new NoPiaIllegalGenericInstantiationSymbol(moduleSymbol, constructedType);
            }

            return constructedType;
        }

        internal override TypeSymbol MakeUnboundIfGeneric(AotModuleSymbol moduleSymbol, TypeSymbol type)
        {
            var namedType = type as NamedTypeSymbol;
            return ((object)namedType != null && namedType.IsGenericType) ? namedType.AsUnboundGenericType() : type;
        }

        private static TypeSymbolWithAnnotations CreateType(TypeSymbol type, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers)
        {
            // The actual annotation will be set when these types are transformed by the caller.
            return TypeSymbolWithAnnotations.Create(type, NullableAnnotation.Unknown, CSharpCustomModifier.Convert(customModifiers));
        }
    }
}
