﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using CVM.Collections.Concurrent;
using System.Collections.Generic;
using CVM.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Helper class to resolve metadata tokens and signatures.
    /// </summary>
    internal class MetadataDecoder : MetadataDecoder<AotModuleSymbol, TypeSymbol, MethodSymbol, FieldSymbol, Symbol>
    {
        public static ObsoleteAttributeData GetOb(System.Reflection.ICustomAttributeProvider member)
        {
            ObsoleteAttributeData data = ObsoleteAttributeData.Uninitialized;
            var objs = member.GetCustomAttributes(typeof(System.ObsoleteAttribute), false);
            {
                foreach (var obj in objs)
                {
                    if (obj is ObsoleteAttribute attr)
                    {
                        data = new ObsoleteAttributeData(ObsoleteAttributeKind.Obsolete, attr.Message, attr.IsError);
                        break;
                    }
                }
            }
            return data;
        }
        protected override ConcurrentDictionary<Type, TypeSymbol> GetTypeRefHandleToTypeMap()
        {
            throw new NotImplementedException();
        }

        protected override MethodSymbol FindMethodSymbolInType(TypeSymbol typeSymbol, MethodBase targetMethodDef)
        {
            Debug.Assert(typeSymbol is AotNamedTypeSymbol || typeSymbol is ErrorTypeSymbol);

            foreach (Symbol member in typeSymbol.GetMembersUnordered())
            {
                var method = member as AotMethodSymbol;
                if ((object)method != null && method.Handle == targetMethodDef)
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// Type context for resolving generic type arguments.
        /// </summary>
        private readonly AotNamedTypeSymbol _typeContextOpt;

        /// <summary>
        /// Method context for resolving generic method type arguments.
        /// </summary>
        private readonly AotMethodSymbol _methodContextOpt;

        public MetadataDecoder(
            AotModuleSymbol moduleSymbol,
            AotNamedTypeSymbol context) :
            this(moduleSymbol, context, null)
        {
        }

        public MetadataDecoder(
            AotModuleSymbol moduleSymbol,
            AotMethodSymbol context) :
            this(moduleSymbol, (AotNamedTypeSymbol)context.ContainingType, context)
        {
        }

        public MetadataDecoder(
            AotModuleSymbol moduleSymbol) :
            this(moduleSymbol, null, null)
        {
        }

        private MetadataDecoder(AotModuleSymbol moduleSymbol, AotNamedTypeSymbol typeContextOpt, AotMethodSymbol methodContextOpt)
            // TODO (tomat): if the containing assembly is a source assembly and we are about to decode assembly level attributes, we run into a cycle,
            // so for now ignore the assembly identity.
            : base((moduleSymbol.ContainingAssembly is AotAssemblySymbol) ? moduleSymbol.ContainingAssembly.Identity : null, SymbolFactory.Instance, moduleSymbol)
        {
            Debug.Assert((object)moduleSymbol != null);

            _typeContextOpt = typeContextOpt;
            _methodContextOpt = methodContextOpt;
        }

        internal AotModuleSymbol ModuleSymbol
        {
            get { return moduleSymbol; }
        }

        protected override TypeSymbol GetGenericMethodTypeParamSymbol(int position)
        {
            if ((object)_methodContextOpt == null)
            {
                return new UnsupportedMetadataTypeSymbol(); // type parameter not associated with a method
            }

            var typeParameters = _methodContextOpt.TypeParameters;

            if (typeParameters.Length <= position)
            {
                return new UnsupportedMetadataTypeSymbol(); // type parameter position too large
            }

            return typeParameters[position];
        }

        protected override TypeSymbol GetGenericTypeParamSymbol(int position)
        {
            AotNamedTypeSymbol type = _typeContextOpt;

            while ((object)type != null && (type.MetadataArity - type.Arity) > position)
            {
                type = type.ContainingSymbol as AotNamedTypeSymbol;
            }

            if ((object)type == null || type.MetadataArity <= position)
            {
                return new UnsupportedMetadataTypeSymbol(); // position of type parameter too large
            }

            position -= type.MetadataArity - type.Arity;
            Debug.Assert(position >= 0 && position < type.Arity);

            return type.TypeParameters[position];
        }

        protected override ConcurrentDictionary<Type, TypeSymbol> GetTypeHandleToTypeMap()
        {
            return moduleSymbol.TypeHandleToTypeMap;
        }

      
        protected override TypeSymbol LookupNestedTypeDefSymbol(TypeSymbol container, ref MetadataTypeName emittedName)
        {
            var result = container.LookupMetadataType(ref emittedName);
            Debug.Assert((object)result != null);

            return result;
        }

        /// <summary>
        /// Lookup a type defined in referenced assembly.
        /// </summary>
        /// <param name="referencedAssemblyIndex"></param>
        /// <param name="emittedName"></param>
        protected override TypeSymbol LookupTopLevelTypeDefSymbol(
            int referencedAssemblyIndex,
            ref MetadataTypeName emittedName)
        {
            var assembly = moduleSymbol.GetReferencedAssemblySymbol(referencedAssemblyIndex);
            if ((object)assembly == null)
            {
                return new UnsupportedMetadataTypeSymbol();
            }

            try
            {
                return assembly.LookupTopLevelMetadataType(ref emittedName, digThroughForwardedTypes: true);
            }
            catch (Exception e)  // Trying to get more useful Watson dumps.
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Lookup a type defined in a module of a multi-module assembly.
        /// </summary>
        protected override TypeSymbol LookupTopLevelTypeDefSymbol(string moduleName, ref MetadataTypeName emittedName, out bool isNoPiaLocalType)
        {
            isNoPiaLocalType = false;
            foreach (ModuleSymbol m in moduleSymbol.ContainingAssembly.Modules)
            {
                if (string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    if ((object)m == (object)moduleSymbol)
                    {
                        return moduleSymbol.LookupTopLevelMetadataType(ref emittedName);//, out isNoPiaLocalType);
                    }
                    else
                    {
                        isNoPiaLocalType = false;
                        return m.LookupTopLevelMetadataType(ref emittedName);
                    }
                }
            }

            isNoPiaLocalType = false;
            return new MissingMetadataTypeSymbol.TopLevel(new MissingModuleSymbolWithName(moduleSymbol.ContainingAssembly, moduleName), ref emittedName, SpecialType.None);
        }

        /// <summary>
        /// Lookup a type defined in this module.
        /// This method will be called only if the type we are
        /// looking for hasn't been loaded yet. Otherwise, MetadataDecoder
        /// would have found the type in TypeDefRowIdToTypeMap based on its 
        /// TypeDef row id. 
        /// </summary>
        protected override TypeSymbol LookupTopLevelTypeDefSymbol(ref MetadataTypeName emittedName, out bool isNoPiaLocalType)
        {
            isNoPiaLocalType = false;
            return moduleSymbol.LookupTopLevelMetadataType(ref emittedName);
        }

        protected override int GetIndexOfReferencedAssembly(AssemblyIdentity identity)
        {
            // Go through all assemblies referenced by the current module and
            // find the one which *exactly* matches the given identity.
            // No unification will be performed
            var assemblies = this.moduleSymbol.GetReferencedAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (identity.Equals(assemblies[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Perform a check whether the type or at least one of its generic arguments 
        /// is defined in the specified assemblies. The check is performed recursively. 
        /// </summary>
        public static bool IsOrClosedOverATypeFromAssemblies(TypeSymbol symbol, ImmutableArray<AssemblySymbol> assemblies)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.TypeParameter:
                    return false;

                case SymbolKind.ArrayType:
                    return IsOrClosedOverATypeFromAssemblies(((ArrayTypeSymbol)symbol).ElementType.TypeSymbol, assemblies);

                case SymbolKind.PointerType:
                    return IsOrClosedOverATypeFromAssemblies(((PointerTypeSymbol)symbol).PointedAtType.TypeSymbol, assemblies);

                case SymbolKind.DynamicType:
                    return false;

                case SymbolKind.ErrorType:
                    goto case SymbolKind.NamedType;
                case SymbolKind.NamedType:
                    var namedType = (NamedTypeSymbol)symbol;
                    AssemblySymbol containingAssembly = symbol.OriginalDefinition.ContainingAssembly;
                    int i;

                    if ((object)containingAssembly != null)
                    {
                        for (i = 0; i < assemblies.Length; i++)
                        {
                            if (ReferenceEquals(containingAssembly, assemblies[i]))
                            {
                                return true;
                            }
                        }
                    }

                    do
                    {
                        if (namedType.IsTupleType)
                        {
                            return IsOrClosedOverATypeFromAssemblies(namedType.TupleUnderlyingType, assemblies);
                        }

                        var arguments = namedType.TypeArgumentsNoUseSiteDiagnostics;
                        int count = arguments.Length;

                        for (i = 0; i < count; i++)
                        {
                            if (IsOrClosedOverATypeFromAssemblies(arguments[i].TypeSymbol, assemblies))
                            {
                                return true;
                            }
                        }

                        namedType = namedType.ContainingType;
                    }
                    while ((object)namedType != null);

                    return false;

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        protected override TypeSymbol SubstituteNoPiaLocalType(
            Type typeDef,
            ref MetadataTypeName name,
            string interfaceGuid,
            string scope,
            string identifier)
        {
            TypeSymbol result;

            try
            {
                bool isInterface =typeDef.IsInterface;
                TypeSymbol baseType = null;

                if (!isInterface)
                {
                    var baseToken = typeDef.BaseType;

                    if (baseToken!=null)
                    {
                        baseType = GetTypeOfToken(baseToken);
                    }
                }

                result = SubstituteNoPiaLocalType(
                    ref name,
                    isInterface,
                    baseType,
                    interfaceGuid,
                    scope,
                    identifier,
                    moduleSymbol.ContainingAssembly);
            }
            catch (BadImageFormatException mrEx)
            {
                result = GetUnsupportedMetadataTypeSymbol(mrEx);
            }

            Debug.Assert((object)result != null);

            ConcurrentDictionary<Type, TypeSymbol> cache = GetTypeHandleToTypeMap();
            Debug.Assert(cache != null);

            TypeSymbol newresult = cache.GetOrAdd(typeDef, result);
            Debug.Assert(ReferenceEquals(newresult, result) || (newresult.Kind == SymbolKind.ErrorType));

            return newresult;
        }

        /// <summary>
        /// Find canonical type for NoPia embedded type.
        /// </summary>
        /// <returns>
        /// Symbol for the canonical type or an ErrorTypeSymbol. Never returns null.
        /// </returns>
        internal static NamedTypeSymbol SubstituteNoPiaLocalType(
            ref MetadataTypeName name,
            bool isInterface,
            TypeSymbol baseType,
            string interfaceGuid,
            string scope,
            string identifier,
            AssemblySymbol referringAssembly)
        {
            NamedTypeSymbol result = null;

            Guid interfaceGuidValue = new Guid();
            bool haveInterfaceGuidValue = false;
            Guid scopeGuidValue = new Guid();
            bool haveScopeGuidValue = false;

            if (isInterface && interfaceGuid != null)
            {
                var asd = new Guid(interfaceGuid);
                haveInterfaceGuidValue = asd!=Guid.Empty;

                if (haveInterfaceGuidValue)
                {
                    // To have consistent errors.
                    scope = null;
                    identifier = null;
                }
            }

            if (scope != null)
            {
                var asd = new Guid(scope);
                haveScopeGuidValue = asd != Guid.Empty;

            
            }

            foreach (AssemblySymbol assembly in referringAssembly.GetNoPiaResolutionAssemblies())
            {
                Debug.Assert((object)assembly != null);
                if (ReferenceEquals(assembly, referringAssembly))
                {
                    continue;
                }

                NamedTypeSymbol candidate = assembly.LookupTopLevelMetadataType(ref name, digThroughForwardedTypes: false);
                Debug.Assert(!candidate.IsGenericType);

                // Ignore type forwarders, error symbols and non-public types
                if (candidate.Kind == SymbolKind.ErrorType ||
                    !ReferenceEquals(candidate.ContainingAssembly, assembly) ||
                    candidate.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                // Ignore NoPia local types.
                // If candidate is coming from metadata, we don't need to do any special check,
                // because we do not create symbols for local types. However, local types defined in source 
                // is another story. However, if compilation explicitly defines a local type, it should be
                // represented by a retargeting assembly, which is supposed to hide the local type.
                Debug.Assert(!(assembly is SourceAssemblySymbol) || !((SourceAssemblySymbol)assembly).SourceModule.MightContainNoPiaLocalTypes());

                string candidateGuid;
                bool haveCandidateGuidValue = false;
                Guid candidateGuidValue = new Guid();

                // The type must be of the same kind (interface, struct, delegate or enum).
                switch (candidate.TypeKind)
                {
                    case TypeKind.Interface:
                        if (!isInterface)
                        {
                            continue;
                        }

                        // Get candidate's Guid
                        if (candidate.GetGuidString(out candidateGuid) && candidateGuid != null)
                        {
                            var asd = new Guid(candidateGuid);
                            haveCandidateGuidValue = asd != Guid.Empty;

                        }

                        break;

                    case TypeKind.Delegate:
                    case TypeKind.Enum:
                    case TypeKind.Struct:

                        if (isInterface)
                        {
                            continue;
                        }

                        // Let's use a trick. To make sure the kind is the same, make sure
                        // base type is the same.
                        SpecialType baseSpecialType = (candidate.BaseTypeNoUseSiteDiagnostics?.SpecialType).GetValueOrDefault();
                        if (baseSpecialType == SpecialType.None || baseSpecialType != (baseType?.SpecialType).GetValueOrDefault())
                        {
                            continue;
                        }

                        break;

                    default:
                        continue;
                }

                if (haveInterfaceGuidValue || haveCandidateGuidValue)
                {
                    if (!haveInterfaceGuidValue || !haveCandidateGuidValue ||
                        candidateGuidValue != interfaceGuidValue)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!haveScopeGuidValue || identifier == null || !identifier.Equals(name.FullName))
                    {
                        continue;
                    }

                    // Scope guid must match candidate's assembly guid.
                    haveCandidateGuidValue = false;
                    if (assembly.GetGuidString(out candidateGuid) && candidateGuid != null)
                    {
                        var asd = new Guid(candidateGuid);
                        haveCandidateGuidValue = asd != Guid.Empty;
                    }

                    if (!haveCandidateGuidValue || scopeGuidValue != candidateGuidValue)
                    {
                        continue;
                    }
                }

                // OK. It looks like we found canonical type definition.
                if ((object)result != null)
                {
                    // Ambiguity 
                    result = new NoPiaAmbiguousCanonicalTypeSymbol(referringAssembly, result, candidate);
                    break;
                }

                result = candidate;
            }

            if ((object)result == null)
            {
                result = new NoPiaMissingCanonicalTypeSymbol(
                                referringAssembly,
                                name.FullName,
                                interfaceGuid,
                                scope,
                                identifier);
            }

            return result;
        }

     
        protected override FieldSymbol FindFieldSymbolInType(TypeSymbol typeSymbol, System.Reflection.FieldInfo fieldDef)
        {
            Debug.Assert(typeSymbol is AotNamedTypeSymbol || typeSymbol is ErrorTypeSymbol);

            foreach (Symbol member in typeSymbol.GetMembersUnordered())
            {
                AotFieldSymbol field = member as AotFieldSymbol;
                if ((object)field != null && field.Handle == fieldDef)
                {
                    return field;
                }
            }

            return null;
        }

        internal override Symbol GetSymbolForMemberRef(System.Reflection.MemberInfo memberRef, TypeSymbol scope = null, bool methodsOnly = false)
        {
            throw new Exception();
            //TypeSymbol targetTypeSymbol = GetMemberRefTypeSymbol(memberRef);

            //if ((object)scope != null)
            //{
            //    Debug.Assert(scope.Kind == SymbolKind.NamedType || scope.Kind == SymbolKind.ErrorType);

            //    // We only want to consider members that are at or above "scope" in the type hierarchy.
            //    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            //    if (!TypeSymbol.Equals(scope, targetTypeSymbol, TypeCompareKind.ConsiderEverything) &&
            //        !(targetTypeSymbol.IsInterfaceType()
            //            ? scope.AllInterfacesNoUseSiteDiagnostics.IndexOf((NamedTypeSymbol)targetTypeSymbol, 0, TypeSymbol.EqualsIgnoringNullableComparer) != -1
            //            : scope.IsDerivedFrom(targetTypeSymbol, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes, useSiteDiagnostics: ref useSiteDiagnostics)))
            //    {
            //        return null;
            //    }
            //}

            //// We're going to use a special decoder that can generate usable symbols for type parameters without full context.
            //// (We're not just using a different type - we're also changing the type context.)
            //var memberRefDecoder = new MemberRefMetadataDecoder(moduleSymbol, targetTypeSymbol);

            //return memberRefDecoder.FindMember(targetTypeSymbol, memberRef, methodsOnly);
        }

        protected override void EnqueueTypeSymbolInterfacesAndBaseTypes(Queue<Type> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, TypeSymbol typeSymbol)
        {
            foreach (NamedTypeSymbol @interface in typeSymbol.InterfacesNoUseSiteDiagnostics())
            {
                EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, @interface);
            }

            EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, typeSymbol.BaseTypeNoUseSiteDiagnostics);
        }

        protected override void EnqueueTypeSymbol(Queue<Type> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, TypeSymbol typeSymbol)
        {
            if ((object)typeSymbol != null)
            {
                AotNamedTypeSymbol peTypeSymbol = typeSymbol as AotNamedTypeSymbol;
                if ((object)peTypeSymbol != null && ReferenceEquals(peTypeSymbol.ContainingAotModule, moduleSymbol))
                {
                    typeDefsToSearch.Enqueue(peTypeSymbol.Handle);
                }
                else
                {
                    typeSymbolsToSearch.Enqueue(typeSymbol);
                }
            }
        }

        protected override System.Reflection.MethodBase GetMethodHandle(MethodSymbol method)
        {

            AotMethodSymbol peMethod = method as AotMethodSymbol;
            if ((object)peMethod != null && ReferenceEquals(peMethod.ContainingModule, moduleSymbol))
            {
                return peMethod.Handle;
            }

            return default(System.Reflection.MethodBase);
        }
    }
}
