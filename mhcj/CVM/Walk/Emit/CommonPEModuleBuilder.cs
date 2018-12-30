// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CVM.Collections.Concurrent;
using System.Collections.Generic;
using CVM.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using CVM.Collections;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class CommonPEModuleBuilder : Cci.IUnit, Cci.IModuleReference
    {
    
        internal readonly OutputKind OutputKind;

        
        internal Cci.IMethodReference PEEntryPoint;
        internal Cci.IMethodReference DebugEntryPoint;
       
        private readonly ConcurrentDictionary<IMethodSymbol, Cci.IMethodBody> _methodBodyMap;
        private readonly TokenMap<Cci.IReference> _referencesInILMap = new TokenMap<Cci.IReference>();
        private readonly ItemTokenMap<string> _stringsInILMap = new ItemTokenMap<string>();
        private readonly ItemTokenMap<Cci.DebugSourceDocument> _sourceDocumentsInILMap = new ItemTokenMap<Cci.DebugSourceDocument>();



        public CommonPEModuleBuilder(
         
            EmitOptions emitOptions,
            OutputKind outputKind,
         
            CSharp.CVM_Zone compilation)
        {
       
            Debug.Assert(compilation != null);

          
            OutputKind = outputKind;
            _methodBodyMap = new ConcurrentDictionary<IMethodSymbol, Cci.IMethodBody>();//((IEqualityComparer<IMethodSymbol>)ReferenceEqualityComparer.Instance);
      
        }

        /// <summary>
        /// EnC generation.
        /// </summary>
        public abstract int CurrentGenerationOrdinal { get; }

        /// <summary>
        /// If this module represents an assembly, name of the assembly used in AssemblyDef table. Otherwise name of the module same as <see cref="ModuleName"/>.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Name of the module. Used in ModuleDef table.
        /// </summary>
        internal abstract string ModuleName { get; }

        internal abstract Cci.IAssemblyReference Translate(IAssemblySymbol symbol, DiagnosticBag diagnostics);
        internal abstract Cci.ITypeReference Translate(ITypeSymbol symbol, SyntaxNode syntaxOpt, DiagnosticBag diagnostics);
        internal abstract Cci.IMethodReference Translate(IMethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration);
        internal abstract bool SupportsPrivateImplClass { get; }
        internal abstract ImmutableArray<Cci.INamespaceTypeDefinition> GetAnonymousTypes(EmitContext context);
        internal abstract CSharp.CVM_Zone CommonCompilation { get; }
        internal abstract IModuleSymbol CommonSourceModule { get; }

        internal abstract IAssemblySymbol CommonCorLibrary { get; }
        internal abstract CommonModuleCompilationState CommonModuleCompilationState { get; }
        internal abstract void CompilationFinished();
        internal abstract ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> GetSynthesizedMembers();
        internal abstract Cci.ITypeReference EncTranslateType(ITypeSymbol type, DiagnosticBag diagnostics);
        public abstract IEnumerable<Cci.ICustomAttribute> GetSourceAssemblyAttributes(bool isRefAssembly);
        public abstract IEnumerable<Cci.SecurityAttribute> GetSourceAssemblySecurityAttributes();
        public abstract IEnumerable<Cci.ICustomAttribute> GetSourceModuleAttributes();
        internal abstract Cci.ICustomAttribute SynthesizeAttribute(WellKnownMember attributeConstructor);

        /// <summary>
        /// Public types defined in other modules making up this assembly and to which other assemblies may refer to via this assembly
        /// followed by types forwarded to another assembly.
        /// </summary>
        public abstract ImmutableArray<Cci.ExportedType> GetExportedTypes(DiagnosticBag diagnostics);

        /// <summary>
        /// Used to distinguish which style to pick while writing native PDB information.
        /// </summary>
        /// <remarks>
        /// The PDB content for custom debug information is different between Visual Basic and CSharp.
        /// E.g. C# always includes a CustomMetadata Header (MD2) that contains the namespace scope counts, where 
        /// as VB only outputs namespace imports into the namespace scopes. 
        /// C# defines forwards in that header, VB includes them into the scopes list.
        /// 
        /// Currently the compiler doesn't allow mixing C# and VB method bodies. Thus this flag can be per module.
        /// It is possible to move this flag to per-method basis but native PDB CDI forwarding would need to be adjusted accordingly.
        /// </remarks>
        public abstract bool GenerateVisualBasicStylePdb { get; }

        /// <summary>
        /// Linked assembly names to be stored to native PDB (VB only).
        /// </summary>
        public abstract IEnumerable<string> LinkedAssembliesDebugInfo { get; }

        /// <summary>
        /// Project level imports (VB only, TODO: C# scripts).
        /// </summary>
        public abstract ImmutableArray<Cci.UsedNamespaceOrType> GetImports();

        /// <summary>
        /// Default namespace (VB only).
        /// </summary>
        public abstract string DefaultNamespace { get; }

        protected abstract Cci.IAssemblyReference GetCorLibraryReferenceToEmit(EmitContext context);
        protected abstract IEnumerable<Cci.IAssemblyReference> GetAssemblyReferencesFromAddedModules(DiagnosticBag diagnostics);
        public abstract Cci.ITypeReference GetPlatformType(Cci.PlatformType platformType, EmitContext context);
        public abstract bool IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType platformType);
        public abstract IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes(EmitContext context);
        
        /// <summary>
        /// A list of the files that constitute the assembly. Empty for netmodule. These are not the source language files that may have been
        /// used to compile the assembly, but the files that contain constituent modules of a multi-module assembly as well
        /// as any external resources. It corresponds to the File table of the .NET assembly file format.
        /// </summary>

        /// <summary>
        /// Builds symbol definition to location map used for emitting token -> location info
        /// into PDB to be consumed by WinMdExp.exe tool (only applicable for /t:winmdobj)
        /// </summary>
        public abstract MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> GetSymbolToLocationMap();

        /// <summary>
        /// Number of debug documents in the module. 
        /// Used to determine capacities of lists and indices when emitting debug info.
        /// </summary>

        public void Dispatch(Cci.MetadataVisitor visitor) => visitor.Visit(this);

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context) => SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            Debug.Assert(ReferenceEquals(context.Module, this));
            return this;
        }

        public abstract ISourceAssemblySymbolInternal SourceAssemblyOpt { get; }

        /// <summary>
        /// An approximate number of method definitions that can
        /// provide a basis for approximating the capacities of
        /// various databases used during Emit.
        /// </summary>
        public int HintNumberOfMethodDefinitions
            // Try to guess at the size of tables to prevent re-allocation. The method body
            // map is pretty close, but unfortunately it tends to undercount. x1.5 seems like
            // a healthy amount of room based on compiling Roslyn.
            => (int)(_methodBodyMap.Count * 1.5);

        internal Cci.IMethodBody GetMethodBody(IMethodSymbol methodSymbol)
        {
            Debug.Assert(methodSymbol.ContainingModule == CommonSourceModule);
            Debug.Assert(methodSymbol.IsDefinition);
            Debug.Assert(methodSymbol.PartialDefinitionPart == null); // Must be definition.

            Cci.IMethodBody body;

            if (_methodBodyMap.TryGetValue(methodSymbol, out body))
            {
                return body;
            }

            return null;
        }

        public void SetMethodBody(IMethodSymbol methodSymbol, Cci.IMethodBody body)
        {
            Debug.Assert(methodSymbol.ContainingModule == CommonSourceModule);
            Debug.Assert(methodSymbol.IsDefinition);
            Debug.Assert(methodSymbol.PartialDefinitionPart == null); // Must be definition.
            Debug.Assert(body == null || (object)methodSymbol == body.MethodDefinition);

            _methodBodyMap.TryAdd(methodSymbol, body);
        }

        internal void SetPEEntryPoint(IMethodSymbol method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition(method));
            Debug.Assert(OutputKind.IsApplication());

            PEEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        internal void SetDebugEntryPoint(IMethodSymbol method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition(method));

            DebugEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        private bool IsSourceDefinition(IMethodSymbol method)
        {
            return method.ContainingModule == CommonSourceModule && method.IsDefinition;
        }

        /// <summary>
        /// CorLibrary assembly referenced by this module.
        /// </summary>
        public Cci.IAssemblyReference GetCorLibrary(EmitContext context)
        {
            return Translate(CommonCorLibrary, context.Diagnostics);
        }

        public Cci.IAssemblyReference GetContainingAssembly(EmitContext context)
        {
            return OutputKind == OutputKind.NetModule ? null : (Cci.IAssemblyReference)this;
        }

        /// <summary>
        /// Returns User Strings referenced from the IL in the module. 
        /// </summary>
        public IEnumerable<string> GetStrings()
        {
            return _stringsInILMap.GetAllItems();
        }

        public uint GetFakeSymbolTokenForIL(Cci.IReference symbol, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            bool added;
            uint token = _referencesInILMap.GetOrAddTokenFor(symbol, out added);
            if (added)
            {
                ReferenceDependencyWalker.VisitReference(symbol, new EmitContext(this, syntaxNode, diagnostics, metadataOnly: false, includePrivateMembers: true));
            }
            return token;
        }

        public uint GetSourceDocumentIndexForIL(Cci.DebugSourceDocument document)
        {
            return _sourceDocumentsInILMap.GetOrAddTokenFor(document);
        }

        internal Cci.DebugSourceDocument GetSourceDocumentFromIndex(uint token)
        {
            return _sourceDocumentsInILMap.GetItem(token);
        }

        public Cci.IReference GetReferenceFromToken(uint token)
        {
            return _referencesInILMap.GetItem(token);
        }

        public uint GetFakeStringTokenForIL(string str)
        {
            return _stringsInILMap.GetOrAddTokenFor(str);
        }

        public string GetStringFromToken(uint token)
        {
            return _stringsInILMap.GetItem(token);
        }

        public IEnumerable<Cci.IReference> ReferencesInIL(out int count)
        {
            return _referencesInILMap.GetAllItemsAndCount(out count);
        }

    
        public IEnumerable<Cci.IAssemblyReference> GetAssemblyReferences(EmitContext context)
        {
            Cci.IAssemblyReference corLibrary = GetCorLibraryReferenceToEmit(context);

            // Only add Cor Library reference explicitly, PeWriter will add
            // other references implicitly on as needed basis.
            if (corLibrary != null)
            {
                yield return corLibrary;
            }

            if (OutputKind != OutputKind.NetModule)
            {
                // Explicitly add references from added modules
                foreach (var aRef in GetAssemblyReferencesFromAddedModules(context.Diagnostics))
                {
                    yield return aRef;
                }
            }
        }
        
    
  
    }

    /// <summary>
    /// Common base class for C# and VB PE module builder.
    /// </summary>
    internal abstract class PEModuleBuilder<TCompilation, TSourceModuleSymbol, TAssemblySymbol, TTypeSymbol, TNamedTypeSymbol, TMethodSymbol, TSyntaxNode, TModuleCompilationState> : CommonPEModuleBuilder, ITokenDeferral
        where TCompilation : CSharp.CVM_Zone
        where TSourceModuleSymbol : class, IModuleSymbol
        where TAssemblySymbol : class, IAssemblySymbol
        where TTypeSymbol : class
        where TNamedTypeSymbol : class, TTypeSymbol, Cci.INamespaceTypeDefinition
        where TMethodSymbol : class, Cci.IMethodDefinition
        where TSyntaxNode : SyntaxNode
        where TModuleCompilationState : ModuleCompilationState<TNamedTypeSymbol, TMethodSymbol>
    {
        private readonly Cci.RootModuleType _rootModuleType = new Cci.RootModuleType();

        internal readonly TSourceModuleSymbol SourceModule;
        internal readonly TCompilation Compilation;

        private PrivateImplementationDetails _privateImplementationDetails;
        private ArrayMethods _lazyArrayMethods;
        private HashSet<string> _namesOfTopLevelTypes;

        internal readonly TModuleCompilationState CompilationState;


        protected PEModuleBuilder(
            TCompilation compilation,
            TSourceModuleSymbol sourceModule,
            OutputKind outputKind,
            EmitOptions emitOptions,
            TModuleCompilationState compilationState) 
            : base( emitOptions, outputKind, compilation)
        {
            Debug.Assert(sourceModule != null);
          

            Compilation = compilation;
            SourceModule = sourceModule;
            this.CompilationState = compilationState;
        }

        internal sealed override void CompilationFinished()
        {
            this.CompilationState.Freeze();
        }

        internal override IAssemblySymbol CommonCorLibrary => CorLibrary;
        internal abstract TAssemblySymbol CorLibrary { get; }
        
        internal abstract Cci.INamedTypeReference GetSystemType(TSyntaxNode syntaxOpt, DiagnosticBag diagnostics);
        internal abstract Cci.INamedTypeReference GetSpecialType(SpecialType specialType, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);

        internal sealed override Cci.ITypeReference EncTranslateType(ITypeSymbol type, DiagnosticBag diagnostics)
        {
            return EncTranslateLocalVariableType((TTypeSymbol)type, diagnostics);
        }

        internal virtual Cci.ITypeReference EncTranslateLocalVariableType(TTypeSymbol type, DiagnosticBag diagnostics)
        {
            return Translate(type, null, diagnostics);
        }

        protected bool HaveDeterminedTopLevelTypes
        {
            get { return _namesOfTopLevelTypes != null; }
        }

        protected bool ContainsTopLevelType(string fullEmittedName)
        {
            return _namesOfTopLevelTypes.Contains(fullEmittedName);
        }

        internal abstract IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypesCore(EmitContext context);

        /// <summary>
        /// Returns all top-level (not nested) types defined in the module. 
        /// </summary>
        public override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes(EmitContext context)
        {
            Cci.TypeReferenceIndexer typeReferenceIndexer = null;
            HashSet<string> names;

            // First time through, we need to collect emitted names of all top level types.
            if (_namesOfTopLevelTypes == null)
            {
                names = new HashSet<string>();
            }
            else
            {
                names = null;
            }

            // First time through, we need to push things through TypeReferenceIndexer
            // to make sure we collect all to be embedded NoPia types and members.
         

            AddTopLevelType(names, _rootModuleType);
            VisitTopLevelType(typeReferenceIndexer, _rootModuleType);
            yield return _rootModuleType;

            foreach (var type in this.GetAnonymousTypes(context))
            {
                AddTopLevelType(names, type);
                VisitTopLevelType(typeReferenceIndexer, type);
                yield return type;
            }

            foreach (var type in this.GetTopLevelTypesCore(context))
            {
                AddTopLevelType(names, type);
                VisitTopLevelType(typeReferenceIndexer, type);
                yield return type;
            }

            var privateImpl = this.PrivateImplClass;
            if (privateImpl != null)
            {
                AddTopLevelType(names, privateImpl);
                VisitTopLevelType(typeReferenceIndexer, privateImpl);
                yield return privateImpl;
            }

     
            if (names != null)
            {
                Debug.Assert(_namesOfTopLevelTypes == null);
                _namesOfTopLevelTypes = names;
            }
        }

        internal abstract Cci.IAssemblyReference Translate(TAssemblySymbol symbol, DiagnosticBag diagnostics);
        internal abstract Cci.ITypeReference Translate(TTypeSymbol symbol, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
        internal abstract Cci.IMethodReference Translate(TMethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration);

        internal sealed override Cci.IAssemblyReference Translate(IAssemblySymbol symbol, DiagnosticBag diagnostics)
        {
            return Translate((TAssemblySymbol)symbol, diagnostics);
        }

        internal sealed override Cci.ITypeReference Translate(ITypeSymbol symbol, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            return Translate((TTypeSymbol)symbol, (TSyntaxNode)syntaxNodeOpt, diagnostics);
        }

        internal sealed override Cci.IMethodReference Translate(IMethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration)
        {
            return Translate((TMethodSymbol)symbol, diagnostics, needDeclaration);
        }

        internal sealed override IModuleSymbol CommonSourceModule => SourceModule;
        internal sealed override CSharp.CVM_Zone CommonCompilation => Compilation;
        internal sealed override CommonModuleCompilationState CommonModuleCompilationState => CompilationState;
        //internal sealed override CommonEmbeddedTypesManager CommonEmbeddedTypesManagerOpt => EmbeddedTypesManagerOpt;
        
        internal MetadataConstant CreateConstant(
            TTypeSymbol type,
            object value,
            TSyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            return new MetadataConstant(Translate(type, syntaxNodeOpt, diagnostics), value);
        }

        private static void AddTopLevelType(HashSet<string> names, Cci.INamespaceTypeDefinition type)
        {
            names?.Add(MetadataHelpers.BuildQualifiedName(type.NamespaceName, Cci.MetadataWriter.GetMangledName(type)));
        }

        private static void VisitTopLevelType(Cci.TypeReferenceIndexer noPiaIndexer, Cci.INamespaceTypeDefinition type)
        {
            noPiaIndexer?.Visit((Cci.ITypeDefinition)type);
        }

        internal Cci.IFieldReference GetModuleVersionId(Cci.ITypeReference mvidType, TSyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            PrivateImplementationDetails details = GetPrivateImplClass(syntaxOpt, diagnostics);
            EnsurePrivateImplementationDetailsStaticConstructor(details, syntaxOpt, diagnostics);

            return details.GetModuleVersionId(mvidType);
        }

        internal Cci.IFieldReference GetInstrumentationPayloadRoot(int analysisKind, Cci.ITypeReference payloadType, TSyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            PrivateImplementationDetails details = GetPrivateImplClass(syntaxOpt, diagnostics);
            EnsurePrivateImplementationDetailsStaticConstructor(details, syntaxOpt, diagnostics);

            return details.GetOrAddInstrumentationPayloadRoot(analysisKind, payloadType);
        }

        private void EnsurePrivateImplementationDetailsStaticConstructor(PrivateImplementationDetails details, TSyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            if (details.GetMethod(WellKnownMemberNames.StaticConstructorName) == null)
            {
                details.TryAddSynthesizedMethod(CreatePrivateImplementationDetailsStaticConstructor(details, syntaxOpt, diagnostics));
            }
        }

        protected abstract Cci.IMethodDefinition CreatePrivateImplementationDetailsStaticConstructor(PrivateImplementationDetails details, TSyntaxNode syntaxOpt, DiagnosticBag diagnostics);

        #region Synthesized Members

        /// <summary>
        /// Captures the set of synthesized definitions that should be added to a type
        /// during emit process.
        /// </summary>
        private sealed class SynthesizedDefinitions
        {
            public ConcurrentQueue<Cci.INestedTypeDefinition> NestedTypes;
            public ConcurrentQueue<Cci.IMethodDefinition> Methods;
            public ConcurrentQueue<Cci.IPropertyDefinition> Properties;
            public ConcurrentQueue<Cci.IFieldDefinition> Fields;

            public ImmutableArray<Cci.ITypeDefinitionMember> GetAllMembers()
            {
                var builder = ArrayBuilder<Cci.ITypeDefinitionMember>.GetInstance();

                if (Fields != null)
                {
                    foreach (var field in Fields)
                    {
                        builder.Add(field);
                    }
                }

                if (Methods != null)
                {
                    foreach (var method in Methods)
                    {
                        builder.Add(method);
                    }
                }

                if (Properties != null)
                {
                    foreach (var property in Properties)
                    {
                        builder.Add(property);
                    }
                }

                if (NestedTypes != null)
                {
                    foreach (var type in NestedTypes)
                    {
                        builder.Add(type);
                    }
                }

                return builder.ToImmutableAndFree();
            }
        }

        private readonly ConcurrentDictionary<TNamedTypeSymbol, SynthesizedDefinitions> _synthesizedDefs =
            new ConcurrentDictionary<TNamedTypeSymbol, SynthesizedDefinitions>();

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.INestedTypeDefinition nestedType)
        {
            Debug.Assert(nestedType != null);

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container);
            if (defs.NestedTypes == null)
            {
                CVM.AHelper.CompareExchange(ref defs.NestedTypes, new ConcurrentQueue<Cci.INestedTypeDefinition>(), null);
            }

            defs.NestedTypes.Enqueue(nestedType);
        }

        internal abstract IEnumerable<Cci.INestedTypeDefinition> GetSynthesizedNestedTypes(TNamedTypeSymbol container);

        /// <summary>
        /// Returns null if there are no compiler generated types.
        /// </summary>
        public IEnumerable<Cci.INestedTypeDefinition> GetSynthesizedTypes(TNamedTypeSymbol container)
        {
            IEnumerable<Cci.INestedTypeDefinition> declareTypes = GetSynthesizedNestedTypes(container);
            IEnumerable<Cci.INestedTypeDefinition> compileEmitTypes = null;

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container, addIfNotFound: false);
            if (defs != null)
            {
                compileEmitTypes = defs.NestedTypes;
            }

            if (declareTypes == null)
            {
                return compileEmitTypes;
            }

            if (compileEmitTypes == null)
            {
                return declareTypes;
            }

            return declareTypes.Concat(compileEmitTypes);
        }

        private SynthesizedDefinitions GetCacheOfSynthesizedDefinitions(TNamedTypeSymbol container, bool addIfNotFound = true)
        {
            Debug.Assert(((INamedTypeSymbol)container).IsDefinition);
            if (addIfNotFound)
            {
                return _synthesizedDefs.GetOrAdd(container, _ => new SynthesizedDefinitions());
            }

            SynthesizedDefinitions defs;
            _synthesizedDefs.TryGetValue(container, out defs);
            return defs;
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.IMethodDefinition method)
        {
            Debug.Assert(method != null);

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container);
            if (defs.Methods == null)
            {
                CVM.AHelper.CompareExchange(ref defs.Methods, new ConcurrentQueue<Cci.IMethodDefinition>(), null);
            }

            defs.Methods.Enqueue(method);
        }

        /// <summary>
        /// Returns null if there are no synthesized methods.
        /// </summary>
        public IEnumerable<Cci.IMethodDefinition> GetSynthesizedMethods(TNamedTypeSymbol container)
        {
            return GetCacheOfSynthesizedDefinitions(container, addIfNotFound: false)?.Methods;
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.IPropertyDefinition property)
        {
            Debug.Assert(property != null);

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container);
            if (defs.Properties == null)
            {
                CVM.AHelper.CompareExchange(ref defs.Properties, new ConcurrentQueue<Cci.IPropertyDefinition>(), null);
            }

            defs.Properties.Enqueue(property);
        }

        /// <summary>
        /// Returns null if there are no synthesized properties.
        /// </summary>
        public IEnumerable<Cci.IPropertyDefinition> GetSynthesizedProperties(TNamedTypeSymbol container)
        {
            return GetCacheOfSynthesizedDefinitions(container, addIfNotFound: false)?.Properties;
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.IFieldDefinition field)
        {
            Debug.Assert(field != null);

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container);
            if (defs.Fields == null)
            {
                CVM.AHelper.CompareExchange(ref defs.Fields, new ConcurrentQueue<Cci.IFieldDefinition>(), null);
            }

            defs.Fields.Enqueue(field);
        }

        /// <summary>
        /// Returns null if there are no synthesized fields.
        /// </summary>
        public IEnumerable<Cci.IFieldDefinition> GetSynthesizedFields(TNamedTypeSymbol container)
        {
            return GetCacheOfSynthesizedDefinitions(container, addIfNotFound: false)?.Fields;
        }

        internal override ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> GetSynthesizedMembers()
        {
            var builder = ImmutableDictionary.CreateBuilder<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>>();

            foreach (var entry in _synthesizedDefs)
            {
                builder.Add(entry.Key, entry.Value.GetAllMembers());
            }

            return builder.ToImmutable();
        }

        public ImmutableArray<Cci.ITypeDefinitionMember> GetSynthesizedMembers(Cci.ITypeDefinition container)
        {
            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions((TNamedTypeSymbol)container, addIfNotFound: false);
            if (defs == null)
            {
                return ImmutableArray<Cci.ITypeDefinitionMember>.Empty;
            }

            return defs.GetAllMembers();
        }

        #endregion

        #region Token Mapping

        Cci.IFieldReference ITokenDeferral.GetFieldForData(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            Debug.Assert(this.SupportsPrivateImplClass);

            var privateImpl = this.GetPrivateImplClass((TSyntaxNode)syntaxNode, diagnostics);

            // map a field to the block (that makes it addressable via a token)
            return privateImpl.CreateDataField(data);
        }

        public abstract Cci.IMethodReference GetInitArrayHelper();

        public ArrayMethods ArrayMethods
        {
            get
            {
                ArrayMethods result = _lazyArrayMethods;

                if (result == null)
                {
                    result = new ArrayMethods();

                    if (CVM.AHelper.CompareExchange(ref _lazyArrayMethods, result, null) != null)
                    {
                        result = _lazyArrayMethods;
                    }
                }

                return result;
            }
        }

        #endregion

        #region Private Implementation Details Type

        internal PrivateImplementationDetails GetPrivateImplClass(TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            var result = _privateImplementationDetails;

            if ((result == null) && this.SupportsPrivateImplClass)
            {
                result = new PrivateImplementationDetails(
                        this,
                        this.SourceModule.Name,
                        Compilation.GetSubmissionSlotIndex(),
                        this.GetSpecialType(SpecialType.System_Object, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_ValueType, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_Byte, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_Int16, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_Int32, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_Int64, syntaxNodeOpt, diagnostics),
                        SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

                if (CVM.AHelper.CompareExchange(ref _privateImplementationDetails, result, null) != null)
                {
                    result = _privateImplementationDetails;
                }
            }

            return result;
        }

        internal PrivateImplementationDetails PrivateImplClass
        {
            get { return _privateImplementationDetails; }
        }

        internal override bool SupportsPrivateImplClass
        {
            get { return true; }
        }

        #endregion

        public sealed override Cci.ITypeReference GetPlatformType(Cci.PlatformType platformType, EmitContext context)
        {
            Debug.Assert((object)this == context.Module);

            switch (platformType)
            {
                case Cci.PlatformType.SystemType:
                    return GetSystemType((TSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics);

                default:
                    return GetSpecialType((SpecialType)platformType, (TSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics);
            }
        }
    }
}
