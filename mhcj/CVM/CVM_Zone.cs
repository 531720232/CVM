using CVM;
using CVM.Collections.Concurrent;
using CVM.Collections.Immutable;
using CVM.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents the possible compilation stages for which it is possible to get diagnostics
    /// (errors).
    /// </summary>
    internal enum CompilationStage
    {
        Parse,
        Declare,
        Compile,
        Emit
    }
    public class CVM_Zone
    {
        internal sealed class WellKnownMembersSignatureComparer : SpecialMembersSignatureComparer
        {
            private readonly CVM_Zone _compilation;

            public WellKnownMembersSignatureComparer(CVM_Zone compilation)
            {
                _compilation = compilation;
            }

            protected override bool MatchTypeToTypeId(TypeSymbol type, int typeId)
            {
                WellKnownType wellKnownId = (WellKnownType)typeId;
                if (wellKnownId.IsWellKnownType())
                {
                    return type.Equals(_compilation.GetWellKnownType(wellKnownId), TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
                }

                return base.MatchTypeToTypeId(type, typeId);
            }
        }
        internal readonly WellKnownMembersSignatureComparer WellKnownMemberSignatureComparer;

        internal readonly BuiltInOperators builtInOperators;

        internal class SpecialMembersSignatureComparer : SignatureComparer<MethodSymbol, FieldSymbol, PropertySymbol, TypeSymbol, ParameterSymbol>
        {
            // Fields
            public static readonly SpecialMembersSignatureComparer Instance = new SpecialMembersSignatureComparer();

            // Methods
            protected SpecialMembersSignatureComparer()
            {
            }

            protected override TypeSymbol GetMDArrayElementType(TypeSymbol type)
            {
                if (type.Kind != SymbolKind.ArrayType)
                {
                    return null;
                }
                ArrayTypeSymbol array = (ArrayTypeSymbol)type;
                if (array.IsSZArray)
                {
                    return null;
                }
                return array.ElementType.TypeSymbol;
            }

            protected override TypeSymbol GetFieldType(FieldSymbol field)
            {
                return field.Type.TypeSymbol;
            }

            protected override TypeSymbol GetPropertyType(PropertySymbol property)
            {
                return property.Type.TypeSymbol;
            }

            protected override TypeSymbol GetGenericTypeArgument(TypeSymbol type, int argumentIndex)
            {
                if (type.Kind != SymbolKind.NamedType)
                {
                    return null;
                }
                NamedTypeSymbol named = (NamedTypeSymbol)type;
                if (named.Arity <= argumentIndex)
                {
                    return null;
                }
                if ((object)named.ContainingType != null)
                {
                    return null;
                }
                return named.TypeArgumentsNoUseSiteDiagnostics[argumentIndex].TypeSymbol;
            }

            protected override TypeSymbol GetGenericTypeDefinition(TypeSymbol type)
            {
                if (type.Kind != SymbolKind.NamedType)
                {
                    return null;
                }
                NamedTypeSymbol named = (NamedTypeSymbol)type;
                if ((object)named.ContainingType != null)
                {
                    return null;
                }
                if (named.Arity == 0)
                {
                    return null;
                }
                return (NamedTypeSymbol)named.OriginalDefinition;
            }

            protected override ImmutableArray<ParameterSymbol> GetParameters(MethodSymbol method)
            {
                return method.Parameters;
            }

            protected override ImmutableArray<ParameterSymbol> GetParameters(PropertySymbol property)
            {
                return property.Parameters;
            }

            protected override TypeSymbol GetParamType(ParameterSymbol parameter)
            {
                return parameter.Type.TypeSymbol;
            }

            protected override TypeSymbol GetPointedToType(TypeSymbol type)
            {
                return type.Kind == SymbolKind.PointerType ? ((PointerTypeSymbol)type).PointedAtType.TypeSymbol : null;
            }

            protected override TypeSymbol GetReturnType(MethodSymbol method)
            {
                return method.ReturnType.TypeSymbol;
            }

            protected override TypeSymbol GetSZArrayElementType(TypeSymbol type)
            {
                if (type.Kind != SymbolKind.ArrayType)
                {
                    return null;
                }
                ArrayTypeSymbol array = (ArrayTypeSymbol)type;
                if (!array.IsSZArray)
                {
                    return null;
                }
                return array.ElementType.TypeSymbol;
            }

            protected override bool IsByRefParam(ParameterSymbol parameter)
            {
                return parameter.RefKind != RefKind.None;
            }

            protected override bool IsByRefMethod(MethodSymbol method)
            {
                return method.RefKind != RefKind.None;
            }

            protected override bool IsByRefProperty(PropertySymbol property)
            {
                return property.RefKind != RefKind.None;
            }

            protected override bool IsGenericMethodTypeParam(TypeSymbol type, int paramPosition)
            {
                if (type.Kind != SymbolKind.TypeParameter)
                {
                    return false;
                }
                TypeParameterSymbol typeParam = (TypeParameterSymbol)type;
                if (typeParam.ContainingSymbol.Kind != SymbolKind.Method)
                {
                    return false;
                }
                return (typeParam.Ordinal == paramPosition);
            }

            protected override bool IsGenericTypeParam(TypeSymbol type, int paramPosition)
            {
                if (type.Kind != SymbolKind.TypeParameter)
                {
                    return false;
                }
                TypeParameterSymbol typeParam = (TypeParameterSymbol)type;
                if (typeParam.ContainingSymbol.Kind != SymbolKind.NamedType)
                {
                    return false;
                }
                return (typeParam.Ordinal == paramPosition);
            }

            protected override bool MatchArrayRank(TypeSymbol type, int countOfDimensions)
            {
                if (type.Kind != SymbolKind.ArrayType)
                {
                    return false;
                }

                ArrayTypeSymbol array = (ArrayTypeSymbol)type;
                return (array.Rank == countOfDimensions);
            }

            protected override bool MatchTypeToTypeId(TypeSymbol type, int typeId)
            {
                if ((int)type.OriginalDefinition.SpecialType == typeId)
                {
                    if (type.IsDefinition)
                    {
                        return true;
                    }

                    return type.Equals(type.OriginalDefinition, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
                }

                return false;
            }
        }

        internal SynthesizedAttributeData SynthesizeDebuggerBrowsableNeverAttribute()
        {
            throw new NotImplementedException();
        }

        internal bool CanEmitBoolean()
        {
            throw new NotImplementedException();
        }

        internal bool CanEmitSpecialType(SpecialType system_String)
        {
            throw new NotImplementedException();
        }

        internal bool CheckIfIsByRefLikeAttributeShouldBeEmbedded(DiagnosticBag diagnosticsOpt, Location locationOpt)
        {
            return CheckIfAttributeShouldBeEmbedded(
                diagnosticsOpt,
                locationOpt,
                WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute,
                WellKnownMember.System_Runtime_CompilerServices_IsByRefLikeAttribute__ctor);
        }

        internal NamedTypeSymbol GetTypeByReflectionType(TypeSymbol submissionReturnTypeOpt, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        private bool _needsGeneratedIsByRefLikeAttribute_Value;

        internal void EnsureIsByRefLikeAttributeExists(DiagnosticBag diagnostics, Location location, bool modifyCompilation)
        {
            Debug.Assert(!modifyCompilation || !_needsGeneratedAttributes_IsFrozen);

            var isNeeded = CheckIfIsByRefLikeAttributeShouldBeEmbedded(diagnostics, location);

            if (isNeeded && modifyCompilation)
            {
                _needsGeneratedIsByRefLikeAttribute_Value = true;
            }
        }

        internal static Symbol GetRuntimeMember(NamedTypeSymbol declaringType, ref MemberDescriptor descriptor, SignatureComparer<MethodSymbol, FieldSymbol, PropertySymbol, TypeSymbol, ParameterSymbol> comparer, AssemblySymbol accessWithinOpt)
        {
            Symbol result = null;
            SymbolKind targetSymbolKind;
            MethodKind targetMethodKind = MethodKind.Ordinary;
            bool isStatic = (descriptor.Flags & MemberFlags.Static) != 0;

            switch (descriptor.Flags & MemberFlags.KindMask)
            {
                case MemberFlags.Constructor:
                    targetSymbolKind = SymbolKind.Method;
                    targetMethodKind = MethodKind.Constructor;
                    //  static constructors are never called explicitly
                    Debug.Assert(!isStatic);
                    break;

                case MemberFlags.Method:
                    targetSymbolKind = SymbolKind.Method;
                    break;

                case MemberFlags.PropertyGet:
                    targetSymbolKind = SymbolKind.Method;
                    targetMethodKind = MethodKind.PropertyGet;
                    break;

                case MemberFlags.Field:
                    targetSymbolKind = SymbolKind.Field;
                    break;

                case MemberFlags.Property:
                    targetSymbolKind = SymbolKind.Property;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(descriptor.Flags);
            }

            foreach (var member in declaringType.GetMembers(descriptor.Name))
            {
                Debug.Assert(member.Name.Equals(descriptor.Name));

                if (member.Kind != targetSymbolKind || member.IsStatic != isStatic ||
                    !(member.DeclaredAccessibility == Accessibility.Public || ((object)accessWithinOpt != null && Symbol.IsSymbolAccessible(member, accessWithinOpt))))
                {
                    continue;
                }

                switch (targetSymbolKind)
                {
                    case SymbolKind.Method:
                        {
                            MethodSymbol method = (MethodSymbol)member;
                            MethodKind methodKind = method.MethodKind;
                            // Treat user-defined conversions and operators as ordinary methods for the purpose
                            // of matching them here.
                            if (methodKind == MethodKind.Conversion || methodKind == MethodKind.UserDefinedOperator)
                            {
                                methodKind = MethodKind.Ordinary;
                            }

                            if (method.Arity != descriptor.Arity || methodKind != targetMethodKind ||
                                ((descriptor.Flags & MemberFlags.Virtual) != 0) != (method.IsVirtual || method.IsOverride || method.IsAbstract))
                            {
                                continue;
                            }

                            if (!comparer.MatchMethodSignature(method, descriptor.Signature))
                            {
                                continue;
                            }
                        }

                        break;

                    case SymbolKind.Property:
                        {
                            PropertySymbol property = (PropertySymbol)member;
                            if (((descriptor.Flags & MemberFlags.Virtual) != 0) != (property.IsVirtual || property.IsOverride || property.IsAbstract))
                            {
                                continue;
                            }

                            if (!comparer.MatchPropertySignature(property, descriptor.Signature))
                            {
                                continue;
                            }
                        }

                        break;

                    case SymbolKind.Field:
                        if (!comparer.MatchFieldSignature((FieldSymbol)member, descriptor.Signature))
                        {
                            continue;
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(targetSymbolKind);
                }

                // ambiguity
                if ((object)result != null)
                {
                    result = null;
                    break;
                }

                result = member;
            }
            return result;
        }
        public SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility = false)
        {
            return CommonGetSemanticModel(syntaxTree, ignoreAccessibility);
        }
        protected SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility)

        {
            throw new Exception();
       
}
        internal SynthesizedAttributeData SynthesizeDecimalConstantAttribute(decimal value)
        {
            bool isNegative;
            byte scale;
            uint low, mid, high;
            value.GetBits(out isNegative, out scale, out low, out mid, out high);
            var systemByte = GetSpecialType(SpecialType.System_Byte);
            Debug.Assert(!systemByte.HasUseSiteError);

            var systemUnit32 = GetSpecialType(SpecialType.System_UInt32);
            Debug.Assert(!systemUnit32.HasUseSiteError);

            return TrySynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                ImmutableArray.Create(
                    new TypedConstant(systemByte, TypedConstantKind.Primitive, scale),
                    new TypedConstant(systemByte, TypedConstantKind.Primitive, (byte)(isNegative ? 128 : 0)),
                    new TypedConstant(systemUnit32, TypedConstantKind.Primitive, high),
                    new TypedConstant(systemUnit32, TypedConstantKind.Primitive, mid),
                    new TypedConstant(systemUnit32, TypedConstantKind.Primitive, low)
                ));
        }

        /// <summary>
        /// Used to generate the dynamic attributes for the required typesymbol.
        /// </summary>
        internal static class DynamicTransformsEncoder
        {
            internal static ImmutableArray<TypedConstant> Encode(TypeSymbol type, RefKind refKind, int customModifiersCount, TypeSymbol booleanType)
            {
                var flagsBuilder = ArrayBuilder<bool>.GetInstance();
                Encode(type, customModifiersCount, refKind, flagsBuilder, addCustomModifierFlags: true);
                Debug.Assert(flagsBuilder.Any());
                Debug.Assert(flagsBuilder.Contains(true));

                var result = flagsBuilder.SelectAsArray((flag, constantType) => new TypedConstant(constantType, TypedConstantKind.Primitive, flag), booleanType);
                flagsBuilder.Free();
                return result;
            }

            internal static ImmutableArray<bool> Encode(TypeSymbol type, RefKind refKind, int customModifiersCount)
            {
                var builder = ArrayBuilder<bool>.GetInstance();
                Encode(type, customModifiersCount, refKind, builder, addCustomModifierFlags: true);
                return builder.ToImmutableAndFree();
            }

            internal static ImmutableArray<bool> EncodeWithoutCustomModifierFlags(TypeSymbol type, RefKind refKind)
            {
                var builder = ArrayBuilder<bool>.GetInstance();
                Encode(type, -1, refKind, builder, addCustomModifierFlags: false);
                return builder.ToImmutableAndFree();
            }

            internal static void Encode(TypeSymbol type, int customModifiersCount, RefKind refKind, ArrayBuilder<bool> transformFlagsBuilder, bool addCustomModifierFlags)
            {
                Debug.Assert(!transformFlagsBuilder.Any());

                if (refKind != RefKind.None)
                {
                    // Native compiler encodes an extra transform flag, always false, for ref/out parameters.
                    transformFlagsBuilder.Add(false);
                }

                if (addCustomModifierFlags)
                {
                    // Native compiler encodes an extra transform flag, always false, for each custom modifier.
                    HandleCustomModifiers(customModifiersCount, transformFlagsBuilder);
                    type.VisitType((typeSymbol, builder, isNested) => AddFlags(typeSymbol, builder, isNested, addCustomModifierFlags: true), transformFlagsBuilder);
                }
                else
                {
                    type.VisitType((typeSymbol, builder, isNested) => AddFlags(typeSymbol, builder, isNested, addCustomModifierFlags: false), transformFlagsBuilder);
                }
            }

            private static bool AddFlags(TypeSymbol type, ArrayBuilder<bool> transformFlagsBuilder, bool isNestedNamedType, bool addCustomModifierFlags)
            {
                // Encode transforms flag for this type and it's custom modifiers (if any).
                switch (type.TypeKind)
                {
                    case TypeKind.Dynamic:
                        transformFlagsBuilder.Add(true);
                        break;

                    case TypeKind.Array:
                        if (addCustomModifierFlags)
                        {
                            HandleCustomModifiers(((ArrayTypeSymbol)type).ElementType.CustomModifiers.Length, transformFlagsBuilder);
                        }

                        transformFlagsBuilder.Add(false);
                        break;

                    case TypeKind.Pointer:
                        if (addCustomModifierFlags)
                        {
                            HandleCustomModifiers(((PointerTypeSymbol)type).PointedAtType.CustomModifiers.Length, transformFlagsBuilder);
                        }

                        transformFlagsBuilder.Add(false);
                        break;

                    default:
                        // Encode transforms flag for this type.
                        // For nested named types, a single flag (false) is encoded for the entire type name, followed by flags for all of the type arguments.
                        // For example, for type "A<T>.B<dynamic>", encoded transform flags are:
                        //      {
                        //          false,  // Type "A.B"
                        //          false,  // Type parameter "T"
                        //          true,   // Type parameter "dynamic"
                        //      }

                        if (!isNestedNamedType)
                        {
                            transformFlagsBuilder.Add(false);
                        }
                        break;
                }

                // Continue walking types
                return false;
            }

            private static void HandleCustomModifiers(int customModifiersCount, ArrayBuilder<bool> transformFlagsBuilder)
            {
                for (int i = 0; i < customModifiersCount; i++)
                {
                    // Native compiler encodes an extra transforms flag, always false, for each custom modifier.
                    transformFlagsBuilder.Add(false);
                }
            }
        }

        internal static class TupleNamesEncoder
        {
            public static ImmutableArray<string> Encode(TypeSymbol type)
            {
                var namesBuilder = ArrayBuilder<string>.GetInstance();

                if (!TryGetNames(type, namesBuilder))
                {
                    namesBuilder.Free();
                    return default(ImmutableArray<string>);
                }

                return namesBuilder.ToImmutableAndFree();
            }

            public static ImmutableArray<TypedConstant> Encode(TypeSymbol type, TypeSymbol stringType)
            {
                var namesBuilder = ArrayBuilder<string>.GetInstance();

                if (!TryGetNames(type, namesBuilder))
                {
                    namesBuilder.Free();
                    return default(ImmutableArray<TypedConstant>);
                }

                var names = namesBuilder.SelectAsArray((name, constantType) =>
                    new TypedConstant(constantType, TypedConstantKind.Primitive, name), stringType);
                namesBuilder.Free();
                return names;
            }

            internal static bool TryGetNames(TypeSymbol type, ArrayBuilder<string> namesBuilder)
            {
                type.VisitType((t, builder, _ignore) => AddNames(t, builder), namesBuilder);
                return namesBuilder.Any(name => name != null);
            }

            private static bool AddNames(TypeSymbol type, ArrayBuilder<string> namesBuilder)
            {
                if (type.IsTupleType)
                {
                    if (type.TupleElementNames.IsDefaultOrEmpty)
                    {
                        // If none of the tuple elements have names, put
                        // null placeholders in.
                        // TODO(https://github.com/dotnet/roslyn/issues/12347):
                        // A possible optimization could be to emit an empty attribute
                        // if all the names are missing, but that has to be true
                        // recursively.
                        namesBuilder.AddMany(null, type.TupleElementTypes.Length);
                    }
                    else
                    {
                        namesBuilder.AddRange(type.TupleElementNames);
                    }
                }
                // Always recur into nested types
                return false;
            }
        }

        internal Imports GetSubmissionImports()
        {
            throw new NotImplementedException();
        }

        internal static ImmutableArray<string> Encode(TypeSymbol type)
        {
            var namesBuilder = ArrayBuilder<string>.GetInstance();

            if (!TryGetNames(type, namesBuilder))
            {
                namesBuilder.Free();
                return default(ImmutableArray<string>);
            }

            return namesBuilder.ToImmutableAndFree();
        }
        internal static bool TryGetNames(TypeSymbol type, ArrayBuilder<string> namesBuilder)
        {
            type.VisitType((t, builder, _ignore) => AddNames(t, builder), namesBuilder);
            return namesBuilder.Any(name => name != null);
        }
        private static bool AddNames(TypeSymbol type, ArrayBuilder<string> namesBuilder)
        {
            if (type.IsTupleType)
            {
                if (type.TupleElementNames.IsDefaultOrEmpty)
                {
                    // If none of the tuple elements have names, put
                    // null placeholders in.
                    // TODO(https://github.com/dotnet/roslyn/issues/12347):
                    // A possible optimization could be to emit an empty attribute
                    // if all the names are missing, but that has to be true
                    // recursively.
                    namesBuilder.AddMany(null, type.TupleElementTypes.Length);
                }
                else
                {
                    namesBuilder.AddRange(type.TupleElementNames);
                }
            }
            // Always recur into nested types
            return false;
        }

        public SourceReferenceResolver SourceReferenceResolver { get; protected set; }

        internal Imports GetImports(SingleNamespaceDeclaration declaration)
        {
            return GetBinderFactory(declaration.SyntaxReference.SyntaxTree).GetImportsBinder((CSharpSyntaxNode)declaration.SyntaxReference.GetSyntax()).GetImports(basesBeingResolved: null);
        }
        internal SynthesizedAttributeData SynthesizeDebuggerStepThroughAttribute()
        {
            if (OptimizationLevel !=0)
            {
                return null;
            }

            return TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerStepThroughAttribute__ctor);
        }


        internal  bool HasCodeToEmit()
        {
            foreach (var syntaxTree in this.SyntaxTrees)
            {
                var unit = syntaxTree.GetCompilationUnitRoot();
                if (unit.Members.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
        internal  CommonAnonymousTypeManager CommonAnonymousTypeManager
        {
            get
            {
                return AnonymousTypeManager;
            }
        }
        internal AnonymousTypeManager AnonymousTypeManager
        {
            get
            {
                return _anonymousTypeManager;
            }
        }
        /// <summary>
        /// Manages anonymous types declared in this compilation. Unifies types that are structurally equivalent.
        /// </summary>
        private readonly AnonymousTypeManager _anonymousTypeManager;

        internal bool Compile(
       CommonPEModuleBuilder moduleBuilder,
       DiagnosticBag diagnostics,
       Predicate<ISymbol> filterOpt,
       CancellationToken cancellationToken)
        {
            try
            {
                return CompileMethods(
                    moduleBuilder,
                    diagnostics: diagnostics,
                    filterOpt: filterOpt,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                moduleBuilder.CompilationFinished();
            }
        }
        public bool ReportSuppressedDiagnostics { get; protected set; }

        internal  Diagnostic FilterDiagnostic(Diagnostic diagnostic)
        {

            return null;
        }

        /// <summary>
        /// Filter out warnings based on the compiler options (/nowarn, /warn and /warnaserror) and the pragma warning directives.
        /// </summary>
        /// <returns>True when there is no error.</returns>
        internal bool FilterAndAppendDiagnostics(DiagnosticBag accumulator, IEnumerable<Diagnostic> incoming, HashSet<int> exclude)
        {
            bool hasError = false;
            bool reportSuppressedDiagnostics = ReportSuppressedDiagnostics;

            foreach (Diagnostic d in incoming)
            {
                if (exclude?.Contains(d.Code) == true)
                {
                    continue;
                }

                var filtered = FilterDiagnostic(d);
                if (filtered == null ||
                    (!reportSuppressedDiagnostics && filtered.IsSuppressed))
                {
                    continue;
                }
                else if (filtered.Severity == DiagnosticSeverity.Error)
                {
                    hasError = true;
                }

                accumulator.Add(filtered);
            }

            return !hasError;
        }
        /// <summary>
        /// Gets the all the diagnostics for the compilation, including syntax, declaration, and binding. Does not
        /// include any diagnostics that might be produced during emit.
        /// </summary>
        public  ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnostics(DefaultDiagnosticsStage, true, cancellationToken);
        }
        internal const CompilationStage DefaultDiagnosticsStage = CompilationStage.Compile;

        internal ImmutableArray<Diagnostic> GetDiagnostics(CompilationStage stage, bool includeEarlierStages, CancellationToken cancellationToken)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            GetDiagnostics(stage, includeEarlierStages, diagnostics, cancellationToken);
            return diagnostics.ToReadOnlyAndFree();
        }

        internal  void GetDiagnostics(CompilationStage stage, bool includeEarlierStages, DiagnosticBag diagnostics, CancellationToken cancellationToken = default)
        {
            var builder = DiagnosticBag.GetInstance();

            if (stage == CompilationStage.Parse || (stage > CompilationStage.Parse && includeEarlierStages))
            {
                var syntaxTrees = this.SyntaxTrees;
                //if (this.Options.ConcurrentBuild)
                //{
                //    var parallelOptions = cancellationToken.CanBeCanceled
                //                        ? new ParallelOptions() { CancellationToken = cancellationToken }
                //                        : DefaultParallelOptions;

                //    Parallel.For(0, syntaxTrees.Length, parallelOptions,
                //        UICultureUtilities.WithCurrentUICulture<int>(i =>
                //        {
                //            var syntaxTree = syntaxTrees[i];
                //            AppendLoadDirectiveDiagnostics(builder, _syntaxAndDeclarations, syntaxTree);
                //            builder.AddRange(syntaxTree.GetDiagnostics(cancellationToken));
                //        }));
                //}
                //else
                //{
                //    foreach (var syntaxTree in syntaxTrees)
                //    {
                //        cancellationToken.ThrowIfCancellationRequested();
                //        AppendLoadDirectiveDiagnostics(builder, _syntaxAndDeclarations, syntaxTree);

                //        cancellationToken.ThrowIfCancellationRequested();
                //        builder.AddRange(syntaxTree.GetDiagnostics(cancellationToken));
                //    }
                //}

                var parseOptionsReported = new HashSet<ParseOptions>();
                foreach (var syntaxTree in syntaxTrees)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!syntaxTree.Options.Errors.IsDefaultOrEmpty && parseOptionsReported.Add(syntaxTree.Options))
                    {
                        var location = syntaxTree.GetLocation(TextSpan.FromBounds(0, 0));
                        foreach (var error in syntaxTree.Options.Errors)
                        {
                            builder.Add(error.WithLocation(location));
                        }
                    }
                }
            }

            if (stage == CompilationStage.Declare || stage > CompilationStage.Declare && includeEarlierStages)
            {
           
                //builder.AddRange(Errors);

                //if (Options.Nullable.HasValue && LanguageVersion < MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion() &&
                //    _syntaxAndDeclarations.ExternalSyntaxTrees.Any())
                //{
                //    builder.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_NullableOptionNotAvailable,
                //                                 nameof(Options.Nullable), Options.Nullable, LanguageVersion.ToDisplayString(),
                //                                 new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion())), Location.None));
                //}

                //cancellationToken.ThrowIfCancellationRequested();

                //// the set of diagnostics related to establishing references.
                //builder.AddRange(GetBoundReferenceManager().Diagnostics);

                //cancellationToken.ThrowIfCancellationRequested();

                //builder.AddRange(GetSourceDeclarationDiagnostics(cancellationToken: cancellationToken));

            
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (stage == CompilationStage.Compile || stage > CompilationStage.Compile && includeEarlierStages)
            {
                //var methodBodyDiagnostics = DiagnosticBag.GetInstance();
                //GetDiagnosticsForAllMethodBodies(methodBodyDiagnostics, cancellationToken);
                //builder.AddRangeAndFree(methodBodyDiagnostics);
            }

            // Before returning diagnostics, we filter warnings
            // to honor the compiler options (e.g., /nowarn, /warnaserror and /warn) and the pragmas.
            FilterAndAppendAndFreeDiagnostics(diagnostics, ref builder);
        }
        internal  bool CompileMethods(
        CommonPEModuleBuilder moduleBuilder,
     
        DiagnosticBag diagnostics,
        Predicate<ISymbol> filterOpt,
        CancellationToken cancellationToken)
        {
            // The diagnostics should include syntax and declaration errors. We insert these before calling Emitter.Emit, so that the emitter
            // does not attempt to emit if there are declaration errors (but we do insert all errors from method body binding...)
            PooledHashSet<int> excludeDiagnostics = null;
      
            bool hasDeclarationErrors = !FilterAndAppendDiagnostics(diagnostics, GetDiagnostics(CompilationStage.Declare, true, cancellationToken), excludeDiagnostics);
            excludeDiagnostics?.Free();

            // TODO (tomat): NoPIA:
            // EmbeddedSymbolManager.MarkAllDeferredSymbolsAsReferenced(this)

            var moduleBeingBuilt = (PEModuleBuilder)moduleBuilder;

          
            
            {
            
                // Perform initial bind of method bodies in spite of earlier errors. This is the same
                // behavior as when calling GetDiagnostics()

                // Use a temporary bag so we don't have to refilter pre-existing diagnostics.
                DiagnosticBag methodBodyDiagnosticBag = DiagnosticBag.GetInstance();
                Predicate<Symbol> s1=default;
               if(filterOpt!=null)
                {
                    s1 = (x) => {return filterOpt.Invoke(x); };
                }
                MethodCompiler.CompileMethodBodies(
                    this,
                    moduleBeingBuilt,
                    hasDeclarationErrors,
                     methodBodyDiagnosticBag,
                     s1,
                     cancellationToken);

                bool hasMethodBodyErrorOrWarningAsError = !FilterAndAppendAndFreeDiagnostics(diagnostics, ref methodBodyDiagnosticBag);

                if (hasDeclarationErrors || hasMethodBodyErrorOrWarningAsError)
                {
                    return false;
                }
            }

            return true;
        }

        internal void EnsureIsUnmanagedAttributeExists(DiagnosticBag diagnostics, Location location, bool modifyCompilationForIsUnmanaged)
        {
            Debug.Assert(!modifyCompilationForIsUnmanaged || !_needsGeneratedAttributes_IsFrozen);

            var isNeeded = CheckIfIsUnmanagedAttributeShouldBeEmbedded(diagnostics, location);

            if (isNeeded && modifyCompilationForIsUnmanaged)
            {
                _needsGeneratedIsUnmanagedAttribute_Value = true;
            }
        }

        /// <summary>
        /// Filter out warnings based on the compiler options (/nowarn, /warn and /warnaserror) and the pragma warning directives.
        /// 'incoming' is freed.
        /// </summary>
        /// <param name="accumulator">Bag to which filtered diagnostics will be added.</param>
        /// <param name="incoming">Diagnostics to be filtered.</param>
        /// <returns>True if there were no errors or warnings-as-errors.</returns>
        internal bool FilterAndAppendAndFreeDiagnostics(DiagnosticBag accumulator, ref DiagnosticBag incoming)
        {
            bool result = FilterAndAppendDiagnostics(accumulator, incoming.AsEnumerableWithoutResolution(), exclude: null);
            incoming.Free();
            incoming = null;
            return result;
        }

        internal void EnsureAnonymousTypeTemplates(CancellationToken cancellationToken)
        {
            Debug.Assert(IsSubmission);

            if (this.GetSubmissionSlotIndex() >= 0 && HasCodeToEmit())
            {
                if (!this.CommonAnonymousTypeManager.AreTemplatesSealed)
                {
                    var discardedDiagnostics = DiagnosticBag.GetInstance();

                    var moduleBeingBuilt = this.CreateModuleBuilder(
                        emitOptions: EmitOptions.Default,
                       
                        diagnostics: discardedDiagnostics,
                        cancellationToken: cancellationToken);

                    if (moduleBeingBuilt != null)
                    {
                        Compile(
                            moduleBeingBuilt,
                            diagnostics: discardedDiagnostics,
                          
                            filterOpt: null,
                            cancellationToken: cancellationToken);
                    }

                    discardedDiagnostics.Free();
                }

                Debug.Assert(this.CommonAnonymousTypeManager.AreTemplatesSealed);
            }
            else
            {
                PreviousSubmission?.EnsureAnonymousTypeTemplates(cancellationToken);
            }
        }
        /// <summary>
        /// The previous submission, if any, or null.
        /// </summary>
        internal CVM_Zone PreviousSubmission
        {
            get;set;
        }
        /// <summary>
        /// A bag in which diagnostics that should be reported after code gen can be deposited.
        /// </summary>
        internal DiagnosticBag AdditionalCodegenWarnings
        {
            get
            {
                return _additionalCodegenWarnings;
            }
        }

        private readonly DiagnosticBag _additionalCodegenWarnings = new DiagnosticBag();
        internal DateTime CurrentLocalTime { get; private set; }

        internal bool EnableEditAndContinue
        {
            get
            {
                return OptimizationLevel == 0;
            }
        }

        internal SynthesizedAttributeData SynthesizeDebuggableAttribute()
        {
            TypeSymbol debuggableAttribute = GetWellKnownType(WellKnownType.System_Diagnostics_DebuggableAttribute);
            Debug.Assert((object)debuggableAttribute != null, "GetWellKnownType unexpectedly returned null");
            if (debuggableAttribute is MissingMetadataTypeSymbol)
            {
                return null;
            }

            TypeSymbol debuggingModesType = GetWellKnownType(WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes);
            Debug.Assert((object)debuggingModesType != null, "GetWellKnownType unexpectedly returned null");
            if (debuggingModesType is MissingMetadataTypeSymbol)
            {
                return null;
            }

            // IgnoreSymbolStoreDebuggingMode flag is checked by the CLR, it is not referred to by the debugger.
            // It tells the JIT that it doesn't need to load the PDB at the time it generates jitted code. 
            // The PDB would still be used by a debugger, or even by the runtime for putting source line information 
            // on exception stack traces. We always set this flag to avoid overhead of JIT loading the PDB. 
            // The theoretical scenario for not setting it would be a language compiler that wants their sequence points 
            // at specific places, but those places don't match what CLR's heuristics calculate when scanning the IL.
            var ignoreSymbolStoreDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__IgnoreSymbolStoreSequencePoints);
            if ((object)ignoreSymbolStoreDebuggingMode == null || !ignoreSymbolStoreDebuggingMode.HasConstantValue)
            {
                return null;
            }

            int constantVal = ignoreSymbolStoreDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;

            // Since .NET 2.0 the combinations of None, Default and DisableOptimizations have the following effect:
            // 
            // None                                         JIT optimizations enabled
            // Default                                      JIT optimizations enabled
            // DisableOptimizations                         JIT optimizations enabled
            // Default | DisableOptimizations               JIT optimizations disabled
            if (OptimizationLevel == 0)
            {
                var defaultDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__Default);
                if ((object)defaultDebuggingMode == null || !defaultDebuggingMode.HasConstantValue)
                {
                    return null;
                }

                var disableOptimizationsDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__DisableOptimizations);
                if ((object)disableOptimizationsDebuggingMode == null || !disableOptimizationsDebuggingMode.HasConstantValue)
                {
                    return null;
                }

                constantVal |= defaultDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
                constantVal |= disableOptimizationsDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
            }

            if (EnableEditAndContinue)
            {
                var enableEncDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__EnableEditAndContinue);
                if ((object)enableEncDebuggingMode == null || !enableEncDebuggingMode.HasConstantValue)
                {
                    return null;
                }

                constantVal |= enableEncDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
            }

            var typedConstantDebugMode = new TypedConstant(debuggingModesType, TypedConstantKind.Enum, constantVal);

            return TrySynthesizeAttribute(
                WellKnownMember.System_Diagnostics_DebuggableAttribute__ctorDebuggingModes,
                ImmutableArray.Create(typedConstantDebugMode));
        }
        private BinderFactory AddNewFactory(SyntaxTree syntaxTree, ref WeakReference<BinderFactory> slot)
        {
            var newFactory = new BinderFactory(this, syntaxTree);
            var newWeakReference = new WeakReference<BinderFactory>(newFactory);

            while (true)
            {
                BinderFactory previousFactory;
                WeakReference<BinderFactory> previousWeakReference = slot;
                if (previousWeakReference != null && previousWeakReference.TryGetTarget(out previousFactory))
                {
                    return previousFactory;
                }

                if (CVM.AHelper.CompareExchange(ref slot, newWeakReference, previousWeakReference) == previousWeakReference)
                {
                    return newFactory;
                }
            }
        }

        internal bool IsExceptionType(TypeSymbol typeSymbol, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            throw new NotImplementedException();
        }

        internal Binder GetBinder(CSharpSyntaxNode syntax)
        {
            return GetBinderFactory(syntax.SyntaxTree).GetBinder(syntax);
        }

        private WeakReference<BinderFactory>[] _binderFactories;
        internal BinderFactory GetBinderFactory(SyntaxTree syntaxTree)
        {
            var treeNum = GetSyntaxTreeOrdinal(syntaxTree);
            var binderFactories = _binderFactories;
            if (binderFactories == null)
            {
                binderFactories = new WeakReference<BinderFactory>[this.SyntaxTrees.Length];
                binderFactories = CVM.AHelper.CompareExchange(ref _binderFactories, binderFactories, null) ?? binderFactories;
            }

            BinderFactory previousFactory;
            var previousWeakReference = binderFactories[treeNum];
            if (previousWeakReference != null && previousWeakReference.TryGetTarget(out previousFactory))
            {
                return previousFactory;
            }

            return AddNewFactory(syntaxTree, ref binderFactories[treeNum]);
        }
        internal  CommonPEModuleBuilder CreateModuleBuilder(
             EmitOptions emitOptions,
            
            
             DiagnosticBag diagnostics,
             CancellationToken cancellationToken)
        {

         
            PEModuleBuilder moduleBeingBuilt;
            moduleBeingBuilt = new PEAssemblyBuilder(
               SourceAssembly,
               emitOptions,
               OutputKind
               
               );
            return moduleBeingBuilt;
        }



        public bool? Nullable { get; private set; }
        public bool AllowUnsafe { get; private set; }

        internal AssemblyIdentityComparer Default { get; } = AssemblyIdentityComparer.Default;
        internal  ISymbol CommonGetSpecialTypeMember(SpecialMember specialMember)
        {
            return GetSpecialTypeMember(specialMember);
        }

        internal IConvertibleConversion ClassifyConvertibleConversion(IOperation source, ITypeSymbol destination, out Optional<object> constantValue)
        {
            constantValue = default;

            if (destination is null)
            {
                return Conversion.NoConversion;
            }

            ITypeSymbol sourceType = source.Type;

            if (sourceType is null)
            {
                if (source.ConstantValue.HasValue && source.ConstantValue.Value is null && destination.IsReferenceType)
                {
                    constantValue = source.ConstantValue;
                    return Conversion.DefaultOrNullLiteral;
                }

                return Conversion.NoConversion;
            }

            Conversion result = ClassifyConversion(sourceType, destination);

            if (result.IsReference && source.ConstantValue.HasValue && source.ConstantValue.Value is null)
            {
                constantValue = source.ConstantValue;
            }

            return result;
        }

        internal bool IsPlatformType(ITypeReference type, PlatformType systemString)
        {
            throw new NotImplementedException();
        }

        internal void EnsureIsReadOnlyAttributeExists(DiagnosticBag diagnostics, Location diagnosticLocation, bool modifyCompilation)
        {
            throw new NotImplementedException();
        }

        // NOTE(cyrusn): There is a bit of a discoverability problem with this method and the same
        // named method in SyntaxTreeSemanticModel.  Technically, i believe these are the appropriate
        // locations for these methods.  This method has no dependencies on anything but the
        // compilation, while the other method needs a bindings object to determine what bound node
        // an expression syntax binds to.  Perhaps when we document these methods we should explain
        // where a user can find the other.
        public Conversion ClassifyConversion(ITypeSymbol source, ITypeSymbol destination)
        {
            // Note that it is possible for there to be both an implicit user-defined conversion
            // and an explicit built-in conversion from source to destination. In that scenario
            // this method returns the implicit conversion.

            if ((object)source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if ((object)destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var cssource = source.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>(nameof(source));
            var csdest = destination.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>(nameof(destination));

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return Conversions.ClassifyConversionFromType(cssource, csdest, ref useSiteDiagnostics);
        }
        internal static Cci.TypeMemberVisibility MemberVisibility(Symbol symbol)
        {
            //
            // We need to relax visibility of members in interactive submissions since they might be emitted into multiple assemblies.
            // 
            // Top-level:
            //   private                       -> public
            //   protected                     -> public (compiles with a warning)
            //   public                         
            //   internal                      -> public
            // 
            // In a nested class:
            //   
            //   private                       
            //   protected                     
            //   public                         
            //   internal                      -> public
            //
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return Cci.TypeMemberVisibility.Public;

                case Accessibility.Private:
                    if (symbol.ContainingType?.TypeKind == TypeKind.Submission)
                    {
                        // top-level private member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.Private;
                    }

                case Accessibility.Internal:
                    if (symbol.ContainingAssembly.IsInteractive)
                    {
                        // top-level or nested internal member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.Assembly;
                    }

                case Accessibility.Protected:
                    if (symbol.ContainingType.TypeKind == TypeKind.Submission)
                    {
                        // top-level protected member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.Family;
                    }

                case Accessibility.ProtectedAndInternal:
                    Debug.Assert(symbol.ContainingType.TypeKind != TypeKind.Submission);
                    return Cci.TypeMemberVisibility.FamilyAndAssembly;

                case Accessibility.ProtectedOrInternal:
                    if (symbol.ContainingAssembly.IsInteractive)
                    {
                        // top-level or nested protected internal member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.FamilyOrAssembly;
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.DeclaredAccessibility);
            }
        }
        private Conversions _conversions;
        internal Conversions Conversions
        {
            get
            {
                if (_conversions == null)
                {
                    CVM.AHelper.CompareExchange(ref _conversions, new BuckStopsHereBinder(this).Conversions, null);
                }

                return _conversions;
            }
        }
        internal static ImmutableArray<TypedConstant> Encode(TypeSymbol type, RefKind refKind, int customModifiersCount, TypeSymbol booleanType)
        {
            var flagsBuilder = ArrayBuilder<bool>.GetInstance();
            Encode(type, customModifiersCount, refKind, flagsBuilder, addCustomModifierFlags: true);
            Debug.Assert(flagsBuilder.Any());
            Debug.Assert(flagsBuilder.Contains(true));

            var result = flagsBuilder.SelectAsArray((flag, constantType) => new TypedConstant(constantType, TypedConstantKind.Primitive, flag), booleanType);
            flagsBuilder.Free();
            return result;
        }

        internal static void Encode(TypeSymbol type, int customModifiersCount, RefKind refKind, ArrayBuilder<bool> transformFlagsBuilder, bool addCustomModifierFlags)
        {
            Debug.Assert(!transformFlagsBuilder.Any());

            if (refKind != RefKind.None)
            {
                // Native compiler encodes an extra transform flag, always false, for ref/out parameters.
                transformFlagsBuilder.Add(false);
            }

            if (addCustomModifierFlags)
            {
                // Native compiler encodes an extra transform flag, always false, for each custom modifier.
                HandleCustomModifiers(customModifiersCount, transformFlagsBuilder);
                type.VisitType((typeSymbol, builder, isNested) => AddFlags(typeSymbol, builder, isNested, addCustomModifierFlags: true), transformFlagsBuilder);
            }
            else
            {
                type.VisitType((typeSymbol, builder, isNested) => AddFlags(typeSymbol, builder, isNested, addCustomModifierFlags: false), transformFlagsBuilder);
            }
        }

        internal bool IsMemberMissing(SpecialMember member)
        {
            throw new NotImplementedException();
        }

        private static void HandleCustomModifiers(int customModifiersCount, ArrayBuilder<bool> transformFlagsBuilder)
        {
            for (int i = 0; i < customModifiersCount; i++)
            {
                // Native compiler encodes an extra transforms flag, always false, for each custom modifier.
                transformFlagsBuilder.Add(false);
            }
        }
        private static bool AddFlags(TypeSymbol type, ArrayBuilder<bool> transformFlagsBuilder, bool isNestedNamedType, bool addCustomModifierFlags)
        {
            // Encode transforms flag for this type and it's custom modifiers (if any).
            switch (type.TypeKind)
            {
                case TypeKind.Dynamic:
                    transformFlagsBuilder.Add(true);
                    break;

                case TypeKind.Array:
                    if (addCustomModifierFlags)
                    {
                        HandleCustomModifiers(((ArrayTypeSymbol)type).ElementType.CustomModifiers.Length, transformFlagsBuilder);
                    }

                    transformFlagsBuilder.Add(false);
                    break;

                case TypeKind.Pointer:
                    if (addCustomModifierFlags)
                    {
                        HandleCustomModifiers(((PointerTypeSymbol)type).PointedAtType.CustomModifiers.Length, transformFlagsBuilder);
                    }

                    transformFlagsBuilder.Add(false);
                    break;

                default:
                    // Encode transforms flag for this type.
                    // For nested named types, a single flag (false) is encoded for the entire type name, followed by flags for all of the type arguments.
                    // For example, for type "A<T>.B<dynamic>", encoded transform flags are:
                    //      {
                    //          false,  // Type "A.B"
                    //          false,  // Type parameter "T"
                    //          true,   // Type parameter "dynamic"
                    //      }

                    if (!isNestedNamedType)
                    {
                        transformFlagsBuilder.Add(false);
                    }
                    break;
            }

            // Continue walking types
            return false;
        }
        private bool _needsGeneratedIsUnmanagedAttribute_Value;
        internal bool NeedsGeneratedIsUnmanagedAttribute
        {
            get
            {
                _needsGeneratedAttributes_IsFrozen = true;
                return  _needsGeneratedIsUnmanagedAttribute_Value;
            }
        }
        internal bool CheckIfIsUnmanagedAttributeShouldBeEmbedded(DiagnosticBag diagnosticsOpt, Location locationOpt)
        {
            return CheckIfAttributeShouldBeEmbedded(
                diagnosticsOpt,
                locationOpt,
                WellKnownType.System_Runtime_CompilerServices_IsUnmanagedAttribute,
                WellKnownMember.System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor);
        }
        internal void EnsureNullableAttributeExists()
        {
            Debug.Assert(!_needsGeneratedAttributes_IsFrozen);

            if (_needsGeneratedIsUnmanagedAttribute_Value || NeedsGeneratedIsUnmanagedAttribute)
            {
                return;
            }

            // Don't report any errors. They should be reported during binding.
            if (CheckIfIsUnmanagedAttributeShouldBeEmbedded(diagnosticsOpt: null, locationOpt: null))
            {
                _needsGeneratedIsUnmanagedAttribute_Value = true;
            }
        }

        /// <summary>
        /// Given a type <paramref name="type"/>, which is either dynamic type OR is a constructed type with dynamic type present in it's type argument tree,
        /// returns a synthesized DynamicAttribute with encoded dynamic transforms array.
        /// </summary>
        /// <remarks>This method is port of AttrBind::CompileDynamicAttr from the native C# compiler.</remarks>
        internal SynthesizedAttributeData SynthesizeDynamicAttribute(TypeSymbol type, int customModifiersCount, RefKind refKindOpt = RefKind.None)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(type.ContainsDynamic());

            if (type.IsDynamic() && refKindOpt == RefKind.None && customModifiersCount == 0)
            {
                return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctor);
            }
            else
            {
                NamedTypeSymbol booleanType = GetSpecialType(SpecialType.System_Boolean);
                Debug.Assert((object)booleanType != null);
                var transformFlags = Encode(type, refKindOpt, customModifiersCount, booleanType);
                var boolArray = ArrayTypeSymbol.CreateSZArray(booleanType.ContainingAssembly, TypeSymbolWithAnnotations.Create(booleanType));
                var arguments = ImmutableArray.Create<TypedConstant>(new TypedConstant(boolArray, transformFlags));
                return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags, arguments);
            }
        }
        internal SynthesizedAttributeData TrySynthesizeAttribute(
          WellKnownMember constructor,
          ImmutableArray<TypedConstant> arguments = default(ImmutableArray<TypedConstant>),
          ImmutableArray<KeyValuePair<WellKnownMember, TypedConstant>> namedArguments = default(ImmutableArray<KeyValuePair<WellKnownMember, TypedConstant>>),
          bool isOptionalUse = false)
        {
            DiagnosticInfo diagnosticInfo;
            var ctorSymbol = (MethodSymbol)Binder.GetWellKnownTypeMember(this, constructor, out diagnosticInfo, isOptional: true);

            if ((object)ctorSymbol == null)
            {
                // if this assert fails, UseSiteErrors for "member" have not been checked before emitting ...
                return null;
            }

            if (arguments.IsDefault)
            {
                arguments = ImmutableArray<TypedConstant>.Empty;
            }

            ImmutableArray<KeyValuePair<string, TypedConstant>> namedStringArguments;
            if (namedArguments.IsDefault)
            {
                namedStringArguments = ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;
            }
            else
            {
                var builder = new ArrayBuilder<KeyValuePair<string, TypedConstant>>(namedArguments.Length);
                foreach (var arg in namedArguments)
                {
                    var wellKnownMember = Binder.GetWellKnownTypeMember(this, arg.Key, out diagnosticInfo, isOptional: true);
                    if (wellKnownMember == null || wellKnownMember is ErrorTypeSymbol)
                    {
                        // if this assert fails, UseSiteErrors for "member" have not been checked before emitting ...
                        return null;
                    }
                    else
                    {
                        builder.Add(new KeyValuePair<string, TypedConstant>(
                            wellKnownMember.Name, arg.Value));
                    }
                }
                namedStringArguments = builder.ToImmutableAndFree();
            }

            return new SynthesizedAttributeData(ctorSymbol, arguments, namedStringArguments);
        }

        internal int GetSubmissionSlotIndex()
        {
         return 0;
        }

        private bool _needsGeneratedAttributes_IsFrozen;
        private bool _needsGeneratedNullableAttribute_Value;
        internal bool CheckIfNullableAttributeShouldBeEmbedded(DiagnosticBag diagnosticsOpt, Location locationOpt)
        {
            // Note: if the type exists, we'll check both constructors, regardless of which one(s) we'll eventually need
            return CheckIfAttributeShouldBeEmbedded(
                diagnosticsOpt,
                locationOpt,
                WellKnownType.System_Runtime_CompilerServices_NullableAttribute,
                WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorByte,
                WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags);
        }
        internal void EnsureNullableAttributeExists(DiagnosticBag diagnostics, Location location, bool modifyCompilation)
        {
            Debug.Assert(!modifyCompilation || !_needsGeneratedAttributes_IsFrozen);

            var isNeeded = CheckIfNullableAttributeShouldBeEmbedded(diagnostics, location);

            if (isNeeded && modifyCompilation)
            {
                _needsGeneratedNullableAttribute_Value = true;
            }
        }
        private bool CheckIfAttributeShouldBeEmbedded(DiagnosticBag diagnosticsOpt, Location locationOpt, WellKnownType attributeType, WellKnownMember attributeCtor, WellKnownMember? secondAttributeCtor = null)
        {
            var userDefinedAttribute = GetWellKnownType(attributeType);

            if (userDefinedAttribute is MissingMetadataTypeSymbol)
            {
                return true;
                //if (Options.OutputKind == OutputKind.NetModule)
                //{
                //    if (diagnosticsOpt != null)
                //    {
                //        var errorReported = Binder.ReportUseSiteDiagnostics(userDefinedAttribute, diagnosticsOpt, locationOpt);
                //        Debug.Assert(errorReported);
                //    }
                //}
                //else
                //{
                //    return true;
                //}
            }
            else if (diagnosticsOpt != null)
            {
                // This should produce diagnostics if the member is missing or bad
                var member = Binder.GetWellKnownTypeMember(this, attributeCtor, diagnosticsOpt, locationOpt);
                if (member != null && secondAttributeCtor != null)
                {
                    Binder.GetWellKnownTypeMember(this, secondAttributeCtor.Value, diagnosticsOpt, locationOpt);
                }
            }

            return false;
        }
        protected CVM_Zone()
        {
            _scriptClass = new Lazy1<ImplicitNamedTypeSymbol>(BindScriptClass);
           _anonymousTypeManager= new AnonymousTypeManager(this);
            this.builtInOperators = new BuiltInOperators(this);

        }
        private ImplicitNamedTypeSymbol BindScriptClass()
        {
            return (ImplicitNamedTypeSymbol)CommonBindScriptClass();
        }
        protected INamedTypeSymbol CommonBindScriptClass()
        {
            string scriptClassName = "Script";

            string[] parts = scriptClassName.Split('.');
            INamespaceSymbol container = this.SourceModule.GlobalNamespace;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                INamespaceSymbol next = container.GetNestedNamespace(parts[i]);
                if (next == null)
                {
                    AssertNoScriptTrees();
                    return null;
                }

                container = next;
            }

            foreach (INamedTypeSymbol candidate in container.GetTypeMembers(parts[parts.Length - 1]))
            {
                if (candidate.IsScriptClass)
                {
                    return candidate;
                }
            }

            AssertNoScriptTrees();
            return null;
        }
        [Conditional("DEBUG")]
        private void AssertNoScriptTrees()
        {
            foreach (var tree in this.SyntaxTrees)
            {
                Debug.Assert(tree.Options.Kind != SourceCodeKind.Script);
            }
        }

        /// <summary>
        /// A symbol representing the implicit Script class. This is null if the class is not
        /// defined in the compilation.
        /// </summary>
        internal NamedTypeSymbol ScriptClass
        {
            get { return _scriptClass.Value; }
        }
        private readonly Lazy1<ImplicitNamedTypeSymbol> _scriptClass;


        public bool CheckOverflow { get; protected set; }
        internal BinderFlags TopLevelBinderFlags { get; private set; }
        public string Name;
        private Dictionary<int, Microsoft.CodeAnalysis.SyntaxTree> Scc;

        private readonly ConcurrentDictionary<Symbol, object> _genericInstanceMap = new ConcurrentDictionary<Symbol, object>();


        public ImmutableArray<string> Usings { get; private set; }


        internal DiagnosticBag diag = new DiagnosticBag();
        //      internal BinderFlags TopLevelBinderFlags { get; private set; }



        //  ..    internal Dictionary<Type, Symbol> types = new Dictionary<Type, Symbol>();
        //      internal Dictionary<string, Type> types1 = new Dictionary<string, Type>();

        //internal void Reg(Type type,o symbol)
        //{
        //    types[type] = symbol;
        //    types1[type.FullName] = type;

        //}

        //internal void Reg_Quick()
        //{
        //    Reg(typeof(string), null);
        //    Reg(typeof(object), null);

        //}
        internal AliasSymbol GlobalNamespaceAlias
        {
            get
            {
                return _globalNamespaceAlias.Value;
            }
        }
        private AliasSymbol CreateGlobalNamespaceAlias()
        {
            return AliasSymbol.CreateGlobalNamespaceAlias(this.GlobalNamespace, new InContainerBinder(this.GlobalNamespace, new BuckStopsHereBinder(this)));
        }
        private readonly Lazy1<AliasSymbol> _globalNamespaceAlias;


        /// <summary>
        /// The syntax trees (parsed from source code) that this compilation was created with.
        /// </summary>
        public  ImmutableArray<SyntaxTree> SyntaxTrees
        {
            get { return _syntaxAndDeclarations.GetLazyState().SyntaxTrees; }
        }

        internal bool IsFeatureEnabled( MessageID feature)
        {
            return ((CSharpParseOptions)SyntaxTrees.FirstOrDefault()?.Options)?.IsFeatureEnabled(feature) == true;
        }
        //  ..,,   private readonly SyntaxAndDeclarationManager _syntaxAndDeclarations;
        public static CVM_Zone Create(string Env_name, IEnumerable<SyntaxTree> syntaxTrees)
        {
            var zone = new CVM_Zone();
            zone.Name = Env_name;

            zone.IsSubmission = false;
    //  ..      zone.Reg_Quick();
            zone._syntaxAndDeclarations = new SyntaxAndDeclarationManager(
                    ImmutableArray<SyntaxTree>.Empty,
                   "sc",
                    null,
                    CSharp.MessageProvider.Instance,
                     zone.IsSubmission,
                    state: null);

            return zone;
        }
        private readonly TokenMap<Cci.IReference> _referencesInILMap = new TokenMap<Cci.IReference>();
     

        /// <summary>
        /// Override the dynamic operation context type for all dynamic calls in the module.
        /// </summary>
        internal virtual NamedTypeSymbol GetDynamicOperationContextType(NamedTypeSymbol contextType)
        {
            return contextType;
        }

        internal bool IsSubmissionSyntaxTree(SyntaxTree syntaxTree)
        {
            throw new NotImplementedException();
        }

        public static CVM_Zone Create(string Env_name, params SyntaxTree[] syntaxTrees)
        {
            var zone = new CVM_Zone();
            zone.Name = Env_name;

            zone.IsSubmission = false;
            //  ..      zone.Reg_Quick();
            zone._syntaxAndDeclarations = new SyntaxAndDeclarationManager(
                    ImmutableArray<SyntaxTree>.Empty,
                   "sc",
                    null,
                    CSharp.MessageProvider.Instance,
                     zone.IsSubmission,
                    state: null);

            return zone;
        }

        internal IMethodReference Translate(MethodSymbol attributeConstructor, CSharpSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }
        private Cci.IMethodReference Translate(
          MethodSymbol methodSymbol,
          SyntaxNode syntaxNodeOpt,
          DiagnosticBag diagnostics,
          bool needDeclaration)
        {
            object reference;
            Cci.IMethodReference methodRef;
            NamedTypeSymbol container = methodSymbol.ContainingType;

            // Method of anonymous type being translated
            if (container.IsAnonymousType)
            {
                Debug.Assert(!needDeclaration);
                methodSymbol = AnonymousTypeManager.TranslateAnonymousTypeMethodSymbol(methodSymbol);
            }
            else if (methodSymbol.IsTupleMethod)
            {
                Debug.Assert(!needDeclaration);
                Debug.Assert(container.IsTupleType);
                container = container.TupleUnderlyingType;
                methodSymbol = methodSymbol.TupleUnderlyingMethod;
            }

            Debug.Assert(!container.IsTupleType);
            Debug.Assert(methodSymbol.IsDefinitionOrDistinct());

            if (!methodSymbol.IsDefinition)
            {
                Debug.Assert(!needDeclaration);

                return methodSymbol;
            }
            else if (!needDeclaration)
            {
                bool methodIsGeneric = methodSymbol.IsGenericMethod;
                bool typeIsGeneric = IsGenericType(container);

                if (methodIsGeneric || typeIsGeneric)
                {
                    if (_genericInstanceMap.TryGetValue(methodSymbol, out reference))
                    {
                        return (Cci.IMethodReference)reference;
                    }

                    if (methodIsGeneric)
                    {
                        if (typeIsGeneric)
                        {
                            // Specialized and generic instance at the same time.
                            methodRef = new SpecializedGenericMethodInstanceReference(methodSymbol);
                        }
                        else
                        {
                            methodRef = new GenericMethodInstanceReference(methodSymbol);
                        }
                    }
                    else
                    {
                        Debug.Assert(typeIsGeneric);
                        methodRef = new SpecializedMethodReference(methodSymbol);
                    }

                    methodRef = (Cci.IMethodReference)_genericInstanceMap.GetOrAdd(methodSymbol, methodRef);

                    return methodRef;
                }
            }

          

            return methodSymbol;
        }

        internal void RecordImport(ExternAliasDirectiveSyntax aliasSyntax)
        {
            throw new NotImplementedException();
        }
        internal void RecordImport(UsingDirectiveSyntax aliasSyntax)
        {
            throw new NotImplementedException();
        }
        internal Cci.IMethodReference Translate(
       MethodSymbol methodSymbol,
       SyntaxNode syntaxNodeOpt,
       DiagnosticBag diagnostics,
       BoundArgListOperator optArgList = null,
       bool needDeclaration = false)
        {
            Debug.Assert(!methodSymbol.IsDefaultValueTypeConstructor());
            Debug.Assert(optArgList == null || (methodSymbol.IsVararg && !needDeclaration));

            Cci.IMethodReference unexpandedMethodRef = Translate(methodSymbol, syntaxNodeOpt, diagnostics, needDeclaration);

            if (optArgList != null && optArgList.Arguments.Length > 0)
            {
                Cci.IParameterTypeInformation[] @params = new Cci.IParameterTypeInformation[optArgList.Arguments.Length];
                int ordinal = methodSymbol.ParameterCount;

                for (int i = 0; i < @params.Length; i++)
                {
                    @params[i] = new ArgListParameterTypeInformation(ordinal,
                                                                    !optArgList.ArgumentRefKindsOpt.IsDefaultOrEmpty && optArgList.ArgumentRefKindsOpt[i] != RefKind.None,
                                                                    Translate(optArgList.Arguments[i].Type, syntaxNodeOpt, diagnostics));
                    ordinal++;
                }

                return new ExpandedVarargsMethodReference(unexpandedMethodRef, @params.AsImmutableOrNull());
            }
            else
            {
                return unexpandedMethodRef;
            }
        }

        internal void MarkImportDirectiveAsUsed(CSharpSyntaxNode directive)
        {
            throw new NotImplementedException();
        }

        public void AddFiles(params string[] fs)
        {

            if (fs == null)
            {
                throw new ArgumentNullException();
            }
            var strings = new List<string>();

            foreach(var f in fs)
            {
          var str=      System.IO.File.ReadAllText(f);
                strings.Add(str);
             
            }

            AddStringsAndEval(strings.ToArray());

        }
        public void Restart()
        {
            Scc.Clear();
        }
        public void AddStringsAndEval(params string[] strings)
        {
            if (strings == null)
            {
                throw new ArgumentNullException();
            }
          foreach(var s in strings)
            {
                if(!Scc.ContainsKey(s.GetHashCode()))
                {
                    GetTree(s);
                }
            }

        }
        private void GetTree(string str)
        {

            var tree = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(str);
            Scc.Add(str.GetHashCode(), tree);
        }
        public void Eval()
        {
         var v=   Scc.Values;
       var em=     v.ToList();

            if (em == null)
            {
                throw new ArgumentNullException(nameof(em));
            }

            if (em.IsEmpty())
            {
                return ;
            }
            var externalSyntaxTrees = PooledHashSet<SyntaxTree>.GetInstance();


        }
        private void Find_Type(Declaration type)
        {
            //foreach (var b1 in type.Children)
            //{
            //    if (b1 is SingleTypeDeclaration a)
            //    {
            //        var type1 = a.Name;
            //        var new1 = new CVM.TypeWalker();
            //        var re = a.SyntaxReference.GetSyntax();
            //        new1.Visit(a.SyntaxReference.GetSyntax());
            //    //    Find_Type(type);
            //    }
            //    if (b1 is SingleNamespaceDeclaration b)
            //    {
            //        Find_Type(b);
            //    }

            //}

        }
      
        public void builder()
        {
       //     var sw = SourceAssembly;
       //     var me = MetadataTypeName.FromFullName("a111.fyindex",false,0);
       // var gw=    sw.GetTopLevelTypeByMetadataName(ref me,SourceAssembly.Identity,false,false,out Tuple2<AssemblySymbol,AssemblySymbol> ww);

       //var arrs=    gw.GetMembers().ToArray();
       //     var wr1 = arrs[0];
       //     var wrt1 = wr1.GetAttributes();
       //     var d = Declarations;
           var c = CreateModuleBuilder(EmitOptions.Default, this.diag, default);

          var we=  CompileMethods(c, diag, default, default);
      
            //var asw = new ToAster(this,tree);
         //  asw.Start();
     //       var tree1 = DeclarationTreeBuilder.ForTree(tree, "", true);
     //       Find_Type(b);
        }

        internal  int CompareSourceLocations(Location loc1, Location loc2)
        {
            Debug.Assert(loc1.IsInSource);
            Debug.Assert(loc2.IsInSource);

            var comparison = CompareSyntaxTreeOrdering(loc1.SourceTree, loc2.SourceTree);
            if (comparison != 0)
            {
                return comparison;
            }

            return loc1.SourceSpan.Start - loc2.SourceSpan.Start;
        }

        internal bool IsSystemTypeReference(ITypeSymbol type)
        {
            throw new NotImplementedException();
        }

        internal  int CompareSourceLocations(SyntaxReference loc1, SyntaxReference loc2)
        {
            var comparison = CompareSyntaxTreeOrdering(loc1.SyntaxTree, loc2.SyntaxTree);
            if (comparison != 0)
            {
                return comparison;
            }

            return loc1.Span.Start - loc2.Span.Start;
        }

        /// <summary>
        /// An array of cached well known types available for use in this Compilation.
        /// Lazily filled by GetWellKnownType method.
        /// </summary>
        private NamedTypeSymbol[] _lazyWellKnownTypes;


        /// <summary>
        /// Lazy cache of well known members.
        /// Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        /// </summary>
        private Symbol[] _lazyWellKnownTypeMembers;
        Dictionary<string, NamedTypeSymbol> aot;
        internal NamedTypeSymbol GetWellKnownType(WellKnownType t)
        {
            Debug.Assert(t.IsValid());
            int index = (int)t - (int)WellKnownType.First;

            if (_lazyWellKnownTypes == null || (object)_lazyWellKnownTypes[index] == null)
            {
                if (_lazyWellKnownTypes == null)
                {
                    CVM.AHelper.CompareExchange(ref _lazyWellKnownTypes, new NamedTypeSymbol[(int)WellKnownTypes.Count], null);
                }
                string mdName = t.GetMetadataName();
                var warnings = DiagnosticBag.GetInstance();
                NamedTypeSymbol result;
                CVM.Tuple2<AssemblySymbol, AssemblySymbol> conflicts = default;

                //    if (aot.TryGetValue(mdName, out result))
                //      return result;
                result = this.Assembly.GetTypeByMetadataName(
                         mdName, includeReferences: true, useCLSCompliantNameArityEncoding: true, isWellKnownType: true, conflicts: out conflicts,
                         warnings: null, ignoreCorLibraryDuplicatedTypes: false);
                if ((object)CVM.AHelper.CompareExchange(ref _lazyWellKnownTypes[index], result, null) != null)
                {
                    Debug.Assert(
                        result == _lazyWellKnownTypes[index] || (_lazyWellKnownTypes[index].IsErrorType() && result.IsErrorType())
                    );
                }
                else
                {
                    AdditionalCodegenWarnings.AddRange(warnings);
                }


            }
            return _lazyWellKnownTypes[index];
        }

        Dictionary<SpecialMember, Symbol> sps;
        internal Symbol GetSpecialTypeMember(SpecialMember t)
        {
            return sps[t];
        }

        internal Symbol GetWellKnownTypeMember(WellKnownMember t)
        {

            if (_lazyWellKnownTypeMembers == null || ReferenceEquals(_lazyWellKnownTypeMembers[(int)t], ErrorTypeSymbol.UnknownResultType))
            {
                if (_lazyWellKnownTypeMembers == null)
                {
                    var wellKnownTypeMembers = new Symbol[(int)WellKnownMember.Count];

                    for (int i = 0; i < wellKnownTypeMembers.Length; i++)
                    {
                        wellKnownTypeMembers[i] = ErrorTypeSymbol.UnknownResultType;
                    }

                    CVM.AHelper.CompareExchange(ref _lazyWellKnownTypeMembers, wellKnownTypeMembers, null);
                }
            }
                return _lazyWellKnownTypeMembers[(int)t];

        }

        /// <summary>
        /// The compiler needs to define an ordering among different partial class in different syntax trees
        /// in some cases, because emit order for fields in structures, for example, is semantically important.
        /// This function defines an ordering among syntax trees in this compilation.
        /// </summary>
        internal int CompareSyntaxTreeOrdering(SyntaxTree tree1, SyntaxTree tree2)
        {
            if (tree1 == tree2)
            {
                return 0;
            }

            Debug.Assert(this.ContainsSyntaxTree(tree1));
            Debug.Assert(this.ContainsSyntaxTree(tree2));

            return this.GetSyntaxTreeOrdinal(tree1) - this.GetSyntaxTreeOrdinal(tree2);
        }
        /// <summary>
        /// Returns true if this compilation contains the specified tree.  False otherwise.
        /// </summary>
        public  bool ContainsSyntaxTree(SyntaxTree syntaxTree)
        {
            return syntaxTree != null && _syntaxAndDeclarations.GetLazyState().RootNamespaces.ContainsKey(syntaxTree);
        }
        internal  int GetSyntaxTreeOrdinal(SyntaxTree tree)
        {
            Debug.Assert(this.ContainsSyntaxTree(tree));
            return _syntaxAndDeclarations.GetLazyState().OrdinalMap[tree];
        }
        private SyntaxAndDeclarationManager _syntaxAndDeclarations;



        internal DeclarationTable Declarations
        {
            get
            {

                return _syntaxAndDeclarations.GetLazyState().DeclarationTable;
            }
        }

        private bool _needsGeneratedIsReadOnlyAttribute_Value;
        internal bool NeedsGeneratedIsReadOnlyAttribute
        {
            get
            {
                _needsGeneratedAttributes_IsFrozen = true;
                return  _needsGeneratedIsReadOnlyAttribute_Value;
            }
        }

        internal void EnsureIsReadOnlyAttributeExists()
        {
            Debug.Assert(!_needsGeneratedAttributes_IsFrozen);

            if (_needsGeneratedIsReadOnlyAttribute_Value || NeedsGeneratedIsReadOnlyAttribute)
            {
                return;
            }

            // Don't report any errors. They should be reported during binding.
            if (CheckIfIsReadOnlyAttributeShouldBeEmbedded(diagnosticsOpt: null, locationOpt: null))
            {
                _needsGeneratedIsReadOnlyAttribute_Value = true;
            }
        }
        internal bool CheckIfIsReadOnlyAttributeShouldBeEmbedded(DiagnosticBag diagnosticsOpt, Location locationOpt)
        {
            return CheckIfAttributeShouldBeEmbedded(
                diagnosticsOpt,
                locationOpt,
                WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute,
                WellKnownMember.System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor);
        }

        internal MergedNamespaceDeclaration MergedRootDeclaration
        {
            get
            {
                return Declarations.GetMergedRoot(this);
            }
        }

        /// <summary>
        /// Creates a new compilation with additional syntax trees.
        /// </summary>
        public  CVM_Zone AddSyntaxTrees(params SyntaxTree[] trees)
        {
            return AddSyntaxTrees((IEnumerable<SyntaxTree>)trees);
        }

        /// <summary>
        /// Creates a new compilation with additional syntax trees.
        /// </summary>
        public  CVM_Zone AddSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            if (trees == null)
            {
                throw new ArgumentNullException(nameof(trees));
            }

            if (trees.IsEmpty())
            {
                return this;
            }

            // This HashSet is needed so that we don't allow adding the same tree twice
            // with a single call to AddSyntaxTrees.  Rather than using a separate HashSet,
            // ReplaceSyntaxTrees can just check against ExternalSyntaxTrees, because we
            // only allow replacing a single tree at a time.
            var externalSyntaxTrees = PooledHashSet<SyntaxTree>.GetInstance();
            var syntaxAndDeclarations = _syntaxAndDeclarations;
        externalSyntaxTrees.AddAll(syntaxAndDeclarations.ExternalSyntaxTrees);
            bool reuseReferenceManager = true;
            int i = 0;
            foreach (var tree in trees.Cast<CSharpSyntaxTree>())
            {
                if (tree == null)
                {
                    throw new ArgumentNullException($"{nameof(trees)}[{i}]");
                }

                if (!tree.HasCompilationUnitRoot)
                {
                    throw new ArgumentException();
                }

                if (externalSyntaxTrees.Contains(tree))
                {
                    throw new ArgumentException();
                }

                if (this.IsSubmission && tree.Options.Kind == SourceCodeKind.Regular)
                {
                    throw new ArgumentException();
                }

          externalSyntaxTrees.Add(tree);
                reuseReferenceManager &= !tree.HasReferenceOrLoadDirectives;

                i++;
            }
         externalSyntaxTrees.Free();

            if (this.IsSubmission && i > 1)
            {
                throw new ArgumentException();
            }

            syntaxAndDeclarations = syntaxAndDeclarations.AddSyntaxTrees(trees);

            return Update( reuseReferenceManager, syntaxAndDeclarations);
        }
        internal bool IsTypeMissing(SpecialType type)
        {
            return IsTypeMissing((int)type);
        }

        internal bool IsTypeMissing(WellKnownType type)
        {
            return IsTypeMissing((int)type);
        }

        private bool IsTypeMissing(int type)
        {
            return _lazyMakeWellKnownTypeMissingMap != null && _lazyMakeWellKnownTypeMissingMap.ContainsKey((int)type);
        }
        private SmallDictionary<int, bool> _lazyMakeWellKnownTypeMissingMap;

        /// <summary>
        /// Get the symbol for the predefined type from the Cor Library referenced by this
        /// compilation.
        /// </summary>
        internal NamedTypeSymbol GetSpecialType(SpecialType specialType)
        {
            if (specialType <= SpecialType.None || specialType > SpecialType.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(specialType), $"Unexpected SpecialType: '{(int)specialType}'.");
            }

            NamedTypeSymbol result;
            if (IsTypeMissing(specialType))
            {
                MetadataTypeName emittedName = MetadataTypeName.FromFullName(specialType.GetMetadataName(), useCLSCompliantNameArityEncoding: true);
                result = new MissingMetadataTypeSymbol.TopLevel(Assembly.CorLibrary.Modules[0], ref emittedName, specialType);
            }
            else
            {
                result = Assembly.GetSpecialType(specialType);
            }

            Debug.Assert(result.SpecialType == specialType);
            return result;
        }

        internal object GetInitArrayHelper()
        {
            throw new NotImplementedException();
        }

        public bool IsSubmission { get; private set; } = false;
        /// <summary>
        /// The AssemblySymbol that represents the assembly being created.
        /// </summary>
        internal SourceAssemblySymbol SourceAssembly
        {
            get
            {
               if(_lazyAssemblySymbol==null)

                   _lazyAssemblySymbol= CreateAndSetSourceAssemblyFullBind();
                return _lazyAssemblySymbol;
            }
        }
        internal AssemblySymbol Assembly
        {
            get
            {
                return SourceAssembly;
            }
        }
        /// <summary>
        /// Get a ModuleSymbol that refers to the module being created by compiling all of the code.
        /// By getting the GlobalNamespace property of that module, all of the namespaces and types
        /// defined in source code can be obtained.
        /// </summary>
        internal  ModuleSymbol SourceModule
        {
            get
            {
                return Assembly.Modules[0];
            }
        }
        public bool SupportsPrivateImplClass { get; internal set; }
        public bool EnableEnumArrayBlockInitialization { get; internal set; }

        private CVM_Zone Update(
        bool reuseReferenceManager,
        SyntaxAndDeclarationManager syntaxAndDeclarations)
        {
            var z = new CVM_Zone();
            z.Name = Name;
            z.IsSubmission = IsSubmission;
            z._syntaxAndDeclarations = syntaxAndDeclarations;

            return z;
                
              
        }

        /// <summary>
        /// string save
        /// </summary>
       
        internal ItemTokenMap<string> str_map;



        internal static Cci.IGenericParameterReference Translate(TypeParameterSymbol param)
        {
            if (!param.IsDefinition)
                throw new InvalidOperationException();

            return param;
        }
        internal static Cci.IArrayTypeReference Translate(ArrayTypeSymbol symbol)
        {
            return symbol;
        }

        internal static Cci.IPointerTypeReference Translate(PointerTypeSymbol symbol)
        {
            return symbol;
        }

        internal   Cci.ITypeReference Translate(
          TypeSymbol typeSymbol,
          SyntaxNode syntaxNodeOpt,
          DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            switch (typeSymbol.Kind)
            {
                case SymbolKind.DynamicType:
                    return Translate((DynamicTypeSymbol)typeSymbol, syntaxNodeOpt, diagnostics);

                case SymbolKind.ArrayType:
                    return Translate((ArrayTypeSymbol)typeSymbol);

                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                    return Translate((NamedTypeSymbol)typeSymbol, syntaxNodeOpt, diagnostics);

                case SymbolKind.PointerType:
                    return Translate((PointerTypeSymbol)typeSymbol);

                case SymbolKind.TypeParameter:
                    return Translate((TypeParameterSymbol)typeSymbol);
            }

            throw ExceptionUtilities.UnexpectedValue(typeSymbol.Kind);
        }

        internal bool HasDynamicEmitAttributes()
        {
            throw new NotImplementedException();
        }

        internal Cci.IFieldReference Translate(
         FieldSymbol fieldSymbol,
         SyntaxNode syntaxNodeOpt,
         DiagnosticBag diagnostics,
         bool needDeclaration = false)
        {
            Debug.Assert(fieldSymbol.IsDefinitionOrDistinct());
            Debug.Assert(!fieldSymbol.IsTupleField, "tuple fields should be rewritten to underlying by now");

            if (!fieldSymbol.IsDefinition)
            {
                Debug.Assert(!needDeclaration);

                return fieldSymbol;
            }
            else if (!needDeclaration && IsGenericType(fieldSymbol.ContainingType))
            {
                object reference;
                Cci.IFieldReference fieldRef;

                if (_genericInstanceMap.TryGetValue(fieldSymbol, out reference))
                {
                    return (Cci.IFieldReference)reference;
                }

                fieldRef = new SpecializedFieldReference(fieldSymbol);
                fieldRef = (Cci.IFieldReference)_genericInstanceMap.GetOrAdd(fieldSymbol, fieldRef);

                return fieldRef;
            }

            //if (_embeddedTypesManagerOpt != null)
            //{
            //    return _embeddedTypesManagerOpt.EmbedFieldIfNeedTo(fieldSymbol, syntaxNodeOpt, diagnostics);
            //}

            return fieldSymbol;
        }
        internal static bool IsGenericType(NamedTypeSymbol toCheck)
        {
            while ((object)toCheck != null)
            {
                if (toCheck.Arity > 0)
                {
                    return true;
                }

                toCheck = toCheck.ContainingType;
            }

            return false;
        }
        internal void GetUnaliasedReferencedAssemblies(ArrayBuilder<AssemblySymbol> assemblies)
        {
            assemblies.Add(SourceAssembly.CorLibrary);
            //dont use
        //    throw new NotImplementedException();
        }

        internal object GetBoundReferenceManager()
        {
            throw new NotImplementedException();
        }

        internal bool IsAttributeType(ITypeSymbol type)
        {
            return IsAttributeType((TypeSymbol)type);

        }
        internal bool IsAttributeType(TypeSymbol type)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return IsEqualOrDerivedFromWellKnownClass(type, WellKnownType.System_Attribute, ref useSiteDiagnostics);
        }
        internal bool IsEqualOrDerivedFromWellKnownClass(TypeSymbol type, WellKnownType wellKnownType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(wellKnownType == WellKnownType.System_Attribute ||
                         wellKnownType == WellKnownType.System_Exception);

            if (type.Kind != SymbolKind.NamedType || type.TypeKind != TypeKind.Class)
            {
                return false;
            }

            var wkType = GetWellKnownType(wellKnownType);
            return type.Equals(wkType, TypeCompareKind.ConsiderEverything) || type.IsDerivedFrom(wkType, TypeCompareKind.ConsiderEverything, useSiteDiagnostics: ref useSiteDiagnostics);
        }
        internal SynthesizedAttributeData SynthesizeTupleNamesAttribute(TypeSymbol typeSymbol)
        {
            throw new NotImplementedException();
        }

        internal SynthesizedAttributeData SynthesizeNullableAttribute(Symbol declaringSymbol, TypeSymbolWithAnnotations type)
        {
            throw new NotImplementedException();
        }

        internal bool IsReadOnlySpanType(TypeSymbol type)
        {
          
            throw new NotImplementedException();
        }

        internal IFieldReference GetModuleVersionId(ITypeReference typeReference, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            throw new Exception();
        }
        /// <summary>
        /// Get all modules in this compilation, including the source module, added modules, and all
        /// modules of referenced assemblies that do not come from an assembly with an extern alias.
        /// Metadata imported from aliased assemblies is not visible at the source level except through
        /// the use of an extern alias directive. So exclude them from this list which is used to construct
        /// the global namespace.
        /// </summary>
        private void GetAllUnaliasedModules(ArrayBuilder<ModuleSymbol> modules)
        {
            // NOTE: This includes referenced modules - they count as modules of the compilation assembly.
            modules.AddRange(Assembly.Modules);
            modules.AddRange(SourceAssembly.CorLibrary.Modules);

            //var referenceManager = GetBoundReferenceManager();

            //for (int i = 0; i < referenceManager.ReferencedAssemblies.Length; i++)
            //{
            //    if (referenceManager.DeclarationsAccessibleWithoutAlias(i))
            //    {
            //        modules.AddRange(referenceManager.ReferencedAssemblies[i].Modules);
            //    }
            //}
        }

        private NamespaceSymbol _lazyGlobalNamespace;
        /// <summary>
        /// Gets the root namespace that contains all namespaces and types defined in source code or in
        /// referenced metadata, merged into a single namespace hierarchy.
        /// </summary>
        internal  NamespaceSymbol GlobalNamespace
        {
            get
            {
                if ((object)_lazyGlobalNamespace == null)
                {
                    // Get the root namespace from each module, and merge them all together
                    // Get all modules in this compilation, ones referenced directly by the compilation
                    // as well as those referenced by all referenced assemblies.

                    var modules = ArrayBuilder<ModuleSymbol>.GetInstance();
                    GetAllUnaliasedModules(modules);

                    var result = MergedNamespaceSymbol.Create(
                        new NamespaceExtent(this),
                        null,
                        modules.SelectDistinct(m => m.GlobalNamespace));

                    modules.Free();

                    CVM.AHelper.CompareExchange(ref _lazyGlobalNamespace, result, null);
                }

                return _lazyGlobalNamespace;
            }
        }

        public LanguageVersion LanguageVersion { get; internal set; }
        public bool FeatureStrictEnabled { get; internal set; }
        public int OptimizationLevel { get; internal set; }
        internal TypeSymbol DynamicType { get; set; }
        public bool HasTupleNamesAttributes { get; internal set; }
        internal DiagnosticBag DeclarationDiagnostics { get { return this.diag; } }
        internal NamedTypeSymbol ObjectType { get;  set; }
        public OutputKind OutputKind { get; internal set; } = OutputKind.DynamicallyLinkedLibrary;
        public bool IsEmitDeterministic { get; internal set; }
        public bool NeedsGeneratedNullableAttribute { get; internal set; }
        public bool ConcurrentBuild { get; internal set; }
        public static string UnspecifiedModuleAssemblyName { get; internal set; }
        public bool NeedsGeneratedIsByRefLikeAttribute { get; internal set; }
        internal TypeSymbol ReturnTypeOpt { get;  set; }
        internal Imports GlobalImports { get; set; }


       


        internal string MakeSourceModuleName()
        {
            return  Name +".dll";
        }
        private SourceAssemblySymbol _lazyAssemblySymbol;

        internal static object SymbolCacheAndReferenceManagerStateGuard = new object();

        private SourceAssemblySymbol CreateAndSetSourceAssemblyFullBind()
        {
            try
            {
                var assemblySymbol = new SourceAssemblySymbol(this, Name, MakeSourceModuleName());

                var resolutionDiagnostics = DiagnosticBag.GetInstance();
                // var assemblyReferencesBySimpleName = PooledDictionary<string, List<ReferencedAssemblyIdentity>>.GetInstance();

                AssemblySymbol corLibrary;

                corLibrary = AotAssemblySymbol.Inst;

                ///设置伪核心库
                assemblySymbol.SetCorLibrary(corLibrary);



                var mod = new SourceModuleSymbol(assemblySymbol, this.Declarations, Name);

                var adc =  ImmutableArray<UnifiedAssembly<AssemblySymbol>>.Empty.ToBuilder();
                adc.Add(new UnifiedAssembly<AssemblySymbol>(corLibrary, corLibrary.Identity));

                mod.SetReferences(new ModuleReferences<AssemblySymbol>(new ImmutableArray<AssemblyIdentity>(new AssemblyIdentity[] { corLibrary.Identity }), new ImmutableArray<AssemblySymbol>(new AssemblySymbol[] { corLibrary }), adc.ToImmutable()));

                assemblySymbol._modules= assemblySymbol._modules.Add(mod);
              

              
                    lock (SymbolCacheAndReferenceManagerStateGuard)
                    {
                        if ((object)_lazyAssemblySymbol == null)
                        {
                       


                        
                            // Make sure that the given compilation holds on this instance of reference manager.
                    

                            // Finally, publish the source symbol after all data have been written.
                            // Once lazyAssemblySymbol is non-null other readers might start reading the data written above.
                            return  assemblySymbol;
                        }
                    }
                return null;
            }

            finally
            {

            
            }

        }


        /// <summary>
        /// Returns true if the type can be embedded. If the type is defined in a linked (/l-ed)
        /// assembly, but doesn't meet embeddable type requirements, this function returns false
        /// and reports appropriate diagnostics.
        /// </summary>
        internal static bool IsValidEmbeddableType(
            NamedTypeSymbol namedType,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics
           )
        {
            // We do not embed SpecialTypes (they must be defined in Core assembly), error types and 
            // types from assemblies that aren't linked.
            if (namedType.SpecialType != SpecialType.None || namedType.IsErrorType() || !namedType.ContainingAssembly.IsLinked)
            {
                // Assuming that we already complained about an error type, no additional diagnostics necessary.
                return false;
            }

            ErrorCode error = ErrorCode.Unknown;

            switch (namedType.TypeKind)
            {
                case TypeKind.Interface:
                case TypeKind.Struct:
                case TypeKind.Delegate:
                case TypeKind.Enum:

                    // We do not support nesting for embedded types.
                    // ERRID.ERR_InvalidInteropType/ERR_NoPIANestedType
                    if ((object)namedType.ContainingType != null)
                    {
                        error = ErrorCode.ERR_NoPIANestedType;
                        break;
                    }

                    // We do not support generic embedded types.
                    // ERRID.ERR_CannotEmbedInterfaceWithGeneric/ERR_GenericsUsedInNoPIAType
                    if (namedType.IsGenericType)
                    {
                        error = ErrorCode.ERR_GenericsUsedInNoPIAType;
                        break;
                    }

                    break;
                default:
                    // ERRID.ERR_CannotLinkClassWithNoPIA1/ERR_NewCoClassOnLink
                    error = ErrorCode.ERR_NewCoClassOnLink;
                    break;
            }

            if (error != ErrorCode.Unknown)
            {
             //   ReportNotEmbeddableSymbol(error, namedType, syntaxNodeOpt, diagnostics, optTypeManager);
                return false;
            }

            return true;
        }
        internal Cci.IFieldReference GetInstrumentationPayloadRoot(int analysisKind, Cci.ITypeReference payloadType, SyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            //PrivateImplementationDetails details = GetPrivateImplClass(syntaxOpt, diagnostics);
            //EnsurePrivateImplementationDetailsStaticConstructor(details, syntaxOpt, diagnostics);

            return null;//details.GetOrAddInstrumentationPayloadRoot(analysisKind, payloadType);
        }

        internal ITypeReference GetSpecialType(SpecialType system_Object, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        internal object GetPrivateImplClass(SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        internal ArrayTypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType)
        {
            throw new NotImplementedException();
        }

        internal object CommonGetWellKnownTypeMember(WellKnownMember system_Threading_Monitor__Enter2)
        {
            throw new NotImplementedException();
        }
    }
}
