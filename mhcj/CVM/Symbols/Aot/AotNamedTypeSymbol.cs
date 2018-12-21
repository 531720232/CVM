using CVM.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The class to represent all types imported from  aot
    /// </summary>
    internal abstract class AotNamedTypeSymbol : NamedTypeSymbol
    {
        private static readonly Dictionary<string, ImmutableArray<AotNamedTypeSymbol>> s_emptyNestedTypes = new Dictionary<string, ImmutableArray<AotNamedTypeSymbol>>();

        public override Symbol ContainingSymbol => _container;

        private readonly NamespaceOrTypeSymbol _container;
        private readonly Type _handle;
        private readonly string _name;
        private readonly TypeAttributes _flags;
        private readonly SpecialType _corTypeId;
        private ICollection<string> _lazyMemberNames;
        private ImmutableArray<Symbol> _lazyMembersInDeclarationOrder;
        private Dictionary<string, ImmutableArray<Symbol>> _lazyMembersByName;
        private Dictionary<string, ImmutableArray<AotNamedTypeSymbol>> _lazyNestedTypes;
        private TypeKind _lazyKind;
        private NamedTypeSymbol _lazyBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces = default(ImmutableArray<NamedTypeSymbol>);
        private NamedTypeSymbol _lazyDeclaredBaseTypeWithoutNullability = ErrorTypeSymbol.UnknownResultType;
        private NamedTypeSymbol _lazyDeclaredBaseTypeWithNullability = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyDeclaredInterfaces = default(ImmutableArray<NamedTypeSymbol>);

        public override NamedTypeSymbol ConstructedFrom => this;




     
        internal AotModuleSymbol ContainingAotModule
        {
            get
            {
                Symbol s = _container;

                while (s.Kind != SymbolKind.Namespace)
                {
                    s = s.ContainingSymbol;
                }

                return ((AotNamespaceSymbol)s).ContainingAotModule;
            }
        }
        public override bool IsStatic
        {
            get
            {
                return
                    (_flags & TypeAttributes.Sealed) != 0 &&
                    (_flags & TypeAttributes.Abstract) != 0;
            }
        }



        public override bool IsAbstract
        {
            get
            {
                return
                    (_flags & TypeAttributes.Abstract) != 0 &&
                    (_flags & TypeAttributes.Sealed) == 0;
            }
        }
        internal override bool IsMetadataAbstract
        {
            get
            {
                return (_flags & TypeAttributes.Abstract) != 0;
            }
        }
        public override bool IsSealed
        {
            get
            {
                return
                    (_flags & TypeAttributes.Sealed) != 0 &&
                    (_flags & TypeAttributes.Abstract) == 0;
            }
        }
        internal override bool IsMetadataSealed
        {
            get
            {
                return (_flags & TypeAttributes.Sealed) != 0;
            }
        }
        internal override bool IsInterface => (_flags & TypeAttributes.Interface) != 0;

        internal override bool IsByRefLikeType => false;

        internal override bool IsReadOnly => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData => default;

        internal override bool IsComImport => (_flags & TypeAttributes.Import) != 0;

        internal override bool ShouldAddWinRTMembers
        {
            get { return IsWindowsRuntimeImport; }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get
            {
                return false;
            }
        }
        internal override CharSet MarshallingCharSet
        {
            get
            {
                CharSet c = CharSet.Ansi;

                if ((Handle.Attributes & TypeAttributes.AnsiClass) != 0)
                {
                    c = CharSet.Ansi;
                } else
                if ((Handle.Attributes & TypeAttributes.AutoClass) != 0)
                {
                    c = CharSet.Auto;
                }
                else if ((Handle.Attributes & TypeAttributes.UnicodeClass) != 0)
                {
                    c = CharSet.Unicode;
                }

                return c;
            }
        }
       

        internal override bool HasSpecialName => (_flags & TypeAttributes.SpecialName) != 0;
        public override bool IsSerializable
        {
            get { return (_flags & TypeAttributes.Serializable) != 0; }
        }
        internal override TypeLayout Layout { get {
              
                return La();


            } }
        TypeLayout _l;
        TypeLayout La()
        {
            try
            {
                var r = Handle.GetCustomAttributes(typeof(System.Runtime.InteropServices.StructLayoutAttribute), false);

                var r1 =(StructLayoutAttribute) r[0];

                var t = new TypeLayout(r1.Value,r1.Size,(byte)r1.Pack);
                return t;


            }
            finally
            {

            }
           
        }
        internal override bool HasDeclarativeSecurity
        {
            get { return (_flags & TypeAttributes.HasSecurity) != 0; }
        }
        internal override bool HasCodeAnalysisEmbeddedAttribute =>false;
        internal override bool GetGuidString(out string guidString)
        {
            guidString= Handle.GUID.ToString();
            return true;
        }
        internal static AotNamedTypeSymbol Create(AotModuleSymbol aot, NamespaceOrTypeSymbol ab,Type type,string em)
        {
            var gen = type.GetGenericArguments();
            var arity = gen.Count();

            AotNamedTypeSymbol result;
            if (arity == 0)
            {
                result = new AotNamedTypeSymbolNonGeneric(aot, ab, type, em);
            }
            else
            {
                result = new AotNamedTypeSymbolGeneric(
                    aot,
                    ab,
                    type,
                    em,
                    gen,
                    arity
                    );
            }
            return result;
        }

        internal AotNamedTypeSymbol(
         AotModuleSymbol moduleSymbol,
         NamespaceOrTypeSymbol container,
         Type handle,
         string emittedNamespaceName,
         int arity
       )
        {
         

            string metadataName;
            bool makeBad = false;

            try
            {
               
                metadataName = moduleSymbol.GetTypeDefNameOrThrow(handle);
            }
            catch (BadImageFormatException)
            {
                metadataName = string.Empty;
                makeBad = true;
            }

            _handle = handle;
            _container = container;

            try
            {
                _flags = handle.Attributes;
            }
            catch (BadImageFormatException)
            {
                makeBad = true;
            }

            if (arity == 0)
            {
                _name = metadataName;
            //    mangleName = false;
            }
            else
            {
                // Unmangle name for a generic type.
                _name = MetadataHelpers.UnmangleMetadataNameForArity(metadataName, arity);
         //       Debug.Assert(ReferenceEquals(_name, metadataName) == (_name == metadataName));
          //      mangleName = !ReferenceEquals(_name, metadataName);
            }

            // check if this is one of the COR library types
            //if (emittedNamespaceName != null &&
            //    moduleSymbol.ContainingAssembly.KeepLookingForDeclaredSpecialTypes &&
            //    this.DeclaredAccessibility == Accessibility.Public) // NB: this.flags was set above.
            //{
          //  ..    _corTypeId = SpecialTypes.GetTypeFromMetadataName(MetadataHelpers.BuildQualifiedName(emittedNamespaceName, metadataName));
            //}
            //else
            //{
            //    _corTypeId = SpecialType.None;
            //}
            _corTypeId = SpecialTypes.GetTypeFromMetadataName(MetadataHelpers.BuildQualifiedName(emittedNamespaceName, metadataName));
            if (makeBad)
            {
              //  _lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this);
            }
            WeKind();
        }
        public override SpecialType SpecialType
        {
            get
            {
                return _corTypeId;
            }
        }
        internal override ModuleSymbol ContainingModule
        {
            get
            {
                Symbol s = _container;

                while (s.Kind != SymbolKind.Namespace)
                {
                    s = s.ContainingSymbol;
                }

                return s.ContainingModule;
            }
        }
        public abstract override int Arity
        {
            get;
        }

        internal abstract override bool MangleName
        {
            get ; 
        }
        internal abstract int MetadataArity
        {
            get;
        }
        internal Type Handle
        {
            get
            {
                return _handle;
            }
        }
        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                if (ReferenceEquals(_lazyBaseType, ErrorTypeSymbol.UnknownResultType))
                {
                 _lazyBaseType= MakeAcyclicBaseType();
                }

                return _lazyBaseType;
            }
        }
        private NamedTypeSymbol MakeAcyclicBaseType()
        {
            NamedTypeSymbol declaredBase = GetDeclaredBaseType(null);

            // implicit base is not interesting for metadata cycle detection
            if ((object)declaredBase == null)
            {
                return null;
            }

            if (BaseTypeAnalysis.ClassDependsOn(declaredBase, this))
            {
                return CyclicInheritanceError(this, declaredBase);
            }

            this.SetKnownToHaveNoDeclaredBaseCycles();
            return declaredBase;
        }

        private NamedTypeSymbol MakeDeclaredBaseType()
        {
            if ((_flags & TypeAttributes.Interface) == 0)
            {
                try
                {
                    var moduleSymbol = ContainingModule;
                    var token = _handle.BaseType;

                    if (token!=null)
                    {

                        var mw = MetadataTypeName.FromFullName(token.FullName);
                        TypeSymbol decodedType = ((AotModuleSymbol)moduleSymbol).LookupTopLevelMetadataType(ref mw);
                        //decodedType = DynamicTypeDecoder.TransformType(decodedType, 0, _handle, moduleSymbol);
                        return (NamedTypeSymbol)decodedType;  //return (NamedTypeSymbol)TupleTypeDecoder.DecodeTupleTypesIfApplicable(decodedType,
                        //                                                                      _handle,
                        //                                                                      moduleSymbol);
                    }
                }
                catch (BadImageFormatException mrEx)
                {
                    return new UnsupportedMetadataTypeSymbol(mrEx);
                }
            }

            return null;
        }
        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            return GetDeclaredBaseType(ignoreNullability: false);
        }
        private NamedTypeSymbol GetDeclaredBaseType(bool ignoreNullability)
        {
            if (ReferenceEquals(_lazyDeclaredBaseTypeWithoutNullability, ErrorTypeSymbol.UnknownResultType))
            {
                CVM.AHelper.CompareExchange(ref _lazyDeclaredBaseTypeWithoutNullability, MakeDeclaredBaseType(), ErrorTypeSymbol.UnknownResultType);
            }

            if (ignoreNullability)
            {
                return _lazyDeclaredBaseTypeWithoutNullability;
            }

            if (ReferenceEquals(_lazyDeclaredBaseTypeWithNullability, ErrorTypeSymbol.UnknownResultType))
            {
                var declaredBase = _lazyDeclaredBaseTypeWithoutNullability;
                //if ((object)declaredBase != null)
                //{
                //    declaredBase = (NamedTypeSymbol)NullableTypeDecoder.TransformType(
                //        TypeSymbolWithAnnotations.Create(declaredBase), _handle, ContainingModule).TypeSymbol;
                //}
                CVM.AHelper.CompareExchange(ref _lazyDeclaredBaseTypeWithNullability, declaredBase, ErrorTypeSymbol.UnknownResultType);
            }

            return _lazyDeclaredBaseTypeWithNullability;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return InterfacesNoUseSiteDiagnostics();
        }
        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved = null)
        {
            if (_lazyInterfaces.IsDefault)
            {
                _lazyInterfaces= MakeAcyclicInterfaces();
            }

            return _lazyInterfaces;
        }
        private ImmutableArray<NamedTypeSymbol> MakeAcyclicInterfaces()
        {
            var declaredInterfaces = GetDeclaredInterfaces(null);
            if (!IsInterface)
            {
                // only interfaces needs to check for inheritance cycles via interfaces.
                return declaredInterfaces;
            }

            return declaredInterfaces
                .SelectAsArray(t => BaseTypeAnalysis.InterfaceDependsOn(t, this) ? CyclicInheritanceError(this, t) : t);
        }
        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            if (_lazyDeclaredInterfaces.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyDeclaredInterfaces, MakeDeclaredInterfaces(), default(ImmutableArray<NamedTypeSymbol>));
            }

            return _lazyDeclaredInterfaces;
        }
        private ImmutableArray<NamedTypeSymbol> MakeDeclaredInterfaces()
        {
            try
            {
                var moduleSymbol = ContainingAotModule;
                var interfaceImpls = _handle.GetInterfaces();

                if (interfaceImpls.Length > 0)
                {
                    var symbols = ArrayBuilder<NamedTypeSymbol>.GetInstance(interfaceImpls.Length);

                    foreach (var interfaceImpl in interfaceImpls)
                    {
                        TypeSymbol typeSymbol = moduleSymbol.TypeHandleToTypeMap[interfaceImpl];


                        var namedTypeSymbol = typeSymbol as NamedTypeSymbol ?? new UnsupportedMetadataTypeSymbol(); // interface list contains a bad type
                        symbols.Add(namedTypeSymbol);
                    }

                    return symbols.ToImmutableAndFree();
                }

                return ImmutableArray<NamedTypeSymbol>.Empty;
            }
            catch (BadImageFormatException mrEx)
            {
                return ImmutableArray.Create<NamedTypeSymbol>(new UnsupportedMetadataTypeSymbol(mrEx));
            }
        }


        private static ExtendedErrorTypeSymbol CyclicInheritanceError(AotNamedTypeSymbol type, TypeSymbol declaredBase)
        {
            var info = new CSDiagnosticInfo(ErrorCode.ERR_ImportedCircularBase, declaredBase, type);
            return new ExtendedErrorTypeSymbol(declaredBase, LookupResultKind.NotReferencable, info, true);
        }
        public override string Name =>_name;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            throw new NotImplementedException();
        }
        private IEnumerable<AotNamedTypeSymbol> CreateNestedTypes()
        {
            var module = (AotModuleSymbol) this.ContainingModule;
         

            ImmutableArray<Type> nestedTypeDefs;

            try
            {
             
                nestedTypeDefs = (_handle).GetNestedTypes().AsImmutable();
            }
            catch 
            {
                yield break;
            }

            foreach (var typeRid in nestedTypeDefs)
            {
              
                    yield return AotNamedTypeSymbol.Create(module, this, typeRid,null);
                
            }
        }
        private static Dictionary<string, ImmutableArray<Symbol>> GroupByName(ArrayBuilder<Symbol> symbols)
        {
            return symbols.ToDictionary(s => s.Name, StringOrdinalComparer.Instance);
        }
        private static Dictionary<string, ImmutableArray<AotNamedTypeSymbol>> GroupByName(ArrayBuilder<AotNamedTypeSymbol> symbols)
        {
            return symbols.ToDictionary(s => s.Name, StringOrdinalComparer.Instance);
        }
        private void EnsureNestedTypesAreLoaded()
        {
            if (_lazyNestedTypes == null)
            {
                var types = ArrayBuilder<AotNamedTypeSymbol>.GetInstance();
                types.AddRange(this.CreateNestedTypes());
                var typesDict = GroupByName(types);

                var exchangeResult = CVM.AHelper.CompareExchange(ref _lazyNestedTypes, typesDict, null);
                if (exchangeResult == null)
                {
                    // Build cache of TypeDef Tokens
                    // Potentially this can be done in the background.
                    var moduleSymbol =(AotModuleSymbol) this.ContainingModule;
                    moduleSymbol.OnNewTypeDeclarationsLoaded(typesDict);
                }
                types.Free();
            }
        }
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            EnsureNestedTypesAreLoaded();

            ImmutableArray<AotNamedTypeSymbol> t;

            if (_lazyNestedTypes.TryGetValue(name, out t))
            {
                return StaticCast<NamedTypeSymbol>.From(t);
            }

            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return GetTypeMembers(name).WhereAsArray(type => type.Arity == arity);

        }
        TypeKind _typekind;
        void WeKind()
        {
            var res = TypeKind.Unknown;
            if (Handle.BaseType == typeof(Delegate)||Handle.BaseType==typeof(MulticastDelegate))
            {
                res = TypeKind.Delegate;
            }else
            if (Handle.IsInterface)
            {
                res = TypeKind.Interface;
            }else
            if (Handle.IsArray)
            {
                res = TypeKind.Array;
            }else
            if (Handle.IsClass)
            {
                res = TypeKind.Class;
            }
            else
            if (Handle.IsEnum)
            {
                res = TypeKind.Enum;
            }
            else
            if (Handle.IsPointer)
            {
                res = TypeKind.Pointer;
            }
            else
            if (!Handle.IsClass&&!Handle.IsEnum)
            {
                res = TypeKind.Struct;
            }
         
            _typekind = res;
        }
        public override TypeKind TypeKind => _typekind;

        List<AotFieldSymbol> ffs = new List<AotFieldSymbol>();
        void Load()
        {
            Type wee;
           
       var fs=     Handle.GetMembers();
            foreach(var f in fs)
            {
              switch (f)

                {
                    case FieldInfo field:
                        var n_f = new AotFieldSymbol(this.ContainingAotModule, this, field);
                        ffs.Add(n_f);
                        break;
                    case MethodBase mb:
                        var m_f = new AotMethodSymbol( mb);
                        // ffs.Add(n_f);
                        break;
                    case EventInfo ev:
                        var e_f = new AotEventSymbol(ContainingAotModule, this, ev, default);
                        break;
                    case PropertyInfo pr:

                        break;
                }               
             
            }   
      
          
        }
        private void EnsureAllMembersAreLoaded()
        {
            if (_lazyMembersByName == null)
            {
                LoadMembers();
            }
        }
        private MultiDictionary<string, AotFieldSymbol> CreateFields(ArrayBuilder<AotFieldSymbol> fieldMembers)
        {
            var privateFieldNameToSymbols = new MultiDictionary<string, AotFieldSymbol>();

            var moduleSymbol = this.ContainingAotModule;
            var module = moduleSymbol;

            // for ordinary struct types we import private fields so that we can distinguish empty structs from non-empty structs
            var isOrdinaryStruct = false;
            // for ordinary embeddable struct types we import private members so that we can report appropriate errors if the structure is used 
            var isOrdinaryEmbeddableStruct = false;

            if (this.TypeKind == TypeKind.Struct)
            {
                if (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.None)
                {
                    isOrdinaryStruct = true;
                    isOrdinaryEmbeddableStruct = this.ContainingAssembly.IsLinked;
                }
                else
                {
                    isOrdinaryStruct = (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Nullable_T);
                }
            }

            try
            {
                foreach (var fieldRid in (_handle).GetFields())
                {
                    try
                    {
                        if (!(isOrdinaryEmbeddableStruct ||
                            (isOrdinaryStruct && (fieldRid.Attributes & FieldAttributes.Static) == 0) 
                            ))
                        {
                            continue;
                        }
                    }
                    catch (BadImageFormatException)
                    { }

                    var symbol = new AotFieldSymbol(moduleSymbol, this, fieldRid);
                    fieldMembers.Add(symbol);

                    // Only private fields are potentially backing fields for field-like events.
                    if (symbol.DeclaredAccessibility == Accessibility.Private)
                    {
                        var name = symbol.Name;
                        if (name.Length > 0)
                        {
                            privateFieldNameToSymbols.Add(name, symbol);
                        }
                    }
                }
            }
            catch 
            { }

            return privateFieldNameToSymbols;
        }

        private void LoadMembers()
        {
            ArrayBuilder<Symbol> members = null;

            if (_lazyMembersInDeclarationOrder.IsDefault)
            {
                EnsureNestedTypesAreLoaded();

                members = ArrayBuilder<Symbol>.GetInstance();

                Debug.Assert(SymbolKind.Field.ToSortOrder() < SymbolKind.Method.ToSortOrder());
                Debug.Assert(SymbolKind.Method.ToSortOrder() < SymbolKind.Property.ToSortOrder());
                Debug.Assert(SymbolKind.Property.ToSortOrder() < SymbolKind.Event.ToSortOrder());
                Debug.Assert(SymbolKind.Event.ToSortOrder() < SymbolKind.NamedType.ToSortOrder());

                if (this.TypeKind == TypeKind.Enum)
                {
           //    ..     EnsureEnumUnderlyingTypeIsLoaded(this.GetUncommonProperties());

                    var module = this.ContainingAotModule;
              

                    try
                    {
                        foreach (var fieldDef in _handle.GetFields())
                        {
                            FieldAttributes fieldFlags;

                            try
                            {
                                fieldFlags = fieldDef.Attributes;
                                if ((fieldFlags & FieldAttributes.Static) == 0)
                                {
                                    continue;
                                }
                            }
                            catch 
                            {
                                fieldFlags = 0;
                            }

                          
                            {
                                var field = new AotFieldSymbol(module, this, fieldDef);
                                members.Add(field);
                            }
                        }
                    }
                    catch 
                    { }

                    var syntheticCtor = new SynthesizedInstanceConstructor(this);
                    members.Add(syntheticCtor);
                }
                else
                {
                    ArrayBuilder<AotFieldSymbol> fieldMembers = ArrayBuilder<AotFieldSymbol>.GetInstance();
                    ArrayBuilder<Symbol> nonFieldMembers = ArrayBuilder<Symbol>.GetInstance();

                    MultiDictionary<string, AotFieldSymbol> privateFieldNameToSymbols = this.CreateFields(fieldMembers);

                    // A method may be referenced as an accessor by one or more properties. And,
                    // any of those properties may be "bogus" if one of the property accessors
                    // does not match the property signature. If the method is referenced by at
                    // least one non-bogus property, then the method is created as an accessor,
                    // and (for purposes of error reporting if the method is referenced directly) the
                    // associated property is set (arbitrarily) to the first non-bogus property found
                    // in metadata. If the method is not referenced by any non-bogus properties,
                    // then the method is created as a normal method rather than an accessor.

                    // Create a dictionary of method symbols indexed by metadata handle
                    // (to allow efficient lookup when matching property accessors).
                    PooledDictionary<MethodInfo, AotMethodSymbol> methodHandleToSymbol = this.CreateMethods(nonFieldMembers);

                    if (this.TypeKind == TypeKind.Struct)
                    {
                        bool haveParameterlessConstructor = false;
                        foreach (MethodSymbol method in nonFieldMembers)
                        {
                            if (method.IsParameterlessConstructor())
                            {
                                haveParameterlessConstructor = true;
                                break;
                            }
                        }

                        // Structs have an implicit parameterless constructor, even if it
                        // does not appear in metadata (11.3.8)
                        if (!haveParameterlessConstructor)
                        {
                            nonFieldMembers.Insert(0, new SynthesizedInstanceConstructor(this));
                        }
                    }

                    this.CreateProperties(methodHandleToSymbol, nonFieldMembers);
                    this.CreateEvents(privateFieldNameToSymbols, methodHandleToSymbol, nonFieldMembers);

                    foreach (AotFieldSymbol field in fieldMembers)
                    {
                        if ((object)field.AssociatedSymbol == null)
                        {
                            members.Add(field);
                        }
                        else
                        {
                            // As for source symbols, our public API presents the fiction that all
                            // operations are performed on the event, rather than on the backing field.  
                            // The backing field is not accessible through the API.  As an additional 
                            // bonus, lookup is easier when the names don't collide.
                            Debug.Assert(field.AssociatedSymbol.Kind == SymbolKind.Event);
                        }
                    }

                    members.AddRange(nonFieldMembers);

                    nonFieldMembers.Free();
                    fieldMembers.Free();

                    methodHandleToSymbol.Free();
                }

                // Now add types to the end.
                int membersCount = members.Count;

                foreach (var typeArray in _lazyNestedTypes.Values)
                {
                    members.AddRange(typeArray);
                }

                // Sort the types based on row id.
           //     members.Sort(membersCount, DeclarationOrderTypeSymbolComparer.Instance);

                var membersInDeclarationOrder = members.ToImmutable();

#if DEBUG
                ISymbol previous = null;

                foreach (var s in membersInDeclarationOrder)
                {
                    if (previous == null)
                    {
                        previous = s;
                    }
                    else
                    {
                        ISymbol current = s;
                        Debug.Assert(previous.Kind.ToSortOrder() <= current.Kind.ToSortOrder());
                        previous = current;
                    }
                }
#endif

                if (!ImmutableInterlocked.InterlockedInitialize(ref _lazyMembersInDeclarationOrder, membersInDeclarationOrder))
                {
                    members.Free();
                    members = null;
                }
                else
                {
                    // remove the types
                    members.Clip(membersCount);
                }
            }

            if (_lazyMembersByName == null)
            {
                if (members == null)
                {
                    members = ArrayBuilder<Symbol>.GetInstance();
                    foreach (var member in _lazyMembersInDeclarationOrder)
                    {
                        if (member.Kind == SymbolKind.NamedType)
                        {
                            break;
                        }
                        members.Add(member);
                    }
                }

                Dictionary<string, ImmutableArray<Symbol>> membersDict = GroupByName(members);

                var exchangeResult = CVM.AHelper.CompareExchange(ref _lazyMembersByName, membersDict, null);
                if (exchangeResult == null)
                {
                    // we successfully swapped in the members dictionary.

                    // Now, use these as the canonical member names.  This saves us memory by not having
                    // two collections around at the same time with redundant data in them.
                    //
                    // NOTE(cyrusn): We must use an interlocked exchange here so that the full
                    // construction of this object will be seen from 'MemberNames'.  Also, doing a
                    // straight InterlockedExchange here is the right thing to do.  Consider the case
                    // where one thread is calling in through "MemberNames" while we are in the middle
                    // of this method.  Either that thread will compute the member names and store it
                    // first (in which case we overwrite it), or we will store first (in which case
                    // their CompareExchange(..., ..., null) will fail.  Either way, this will be certain
                    // to become the canonical set of member names.
                    //
                    // NOTE(cyrusn): This means that it is possible (and by design) for people to get a
                    // different object back when they call MemberNames multiple times.  However, outside
                    // of object identity, both collections should appear identical to the user.
                    var memberNames = SpecializedCollections.ReadOnlyCollection(membersDict.Keys);
                    CVM.AHelper.Exchange(ref _lazyMemberNames, memberNames);
                }
            }

            if (members != null)
            {
                members.Free();
            }
        }
        private PooledDictionary<MethodInfo, AotMethodSymbol> CreateMethods(ArrayBuilder<Symbol> members)
        {
            var module = this.ContainingAotModule;
            var map = PooledDictionary<MethodInfo, AotMethodSymbol>.GetInstance();

            // for ordinary embeddable struct types we import private members so that we can report appropriate errors if the structure is used 
            var isOrdinaryEmbeddableStruct = (this.TypeKind == TypeKind.Struct) && (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.None) && this.ContainingAssembly.IsLinked;

            try
            {
                foreach (var methodHandle in _handle.GetMethods())
                {
                  //  if (isOrdinaryEmbeddableStruct || module.ShouldImportMethod(methodHandle, moduleSymbol.ImportOptions))
                    {
                        var method = new AotMethodSymbol(module, this, methodHandle);
                        members.Add(method);
                        map.Add(methodHandle, method);
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            return map;
        }

        private AotMethodSymbol GetAccessorMethod(Dictionary<MethodInfo, AotMethodSymbol> methodHandleToSymbol, MethodInfo methodDef)
        {
            if (methodDef==null)
            {
                return null;
            }

            AotMethodSymbol method;
            bool found = methodHandleToSymbol.TryGetValue(methodDef, out method);
            Debug.Assert(found);
            return method;
        }
        private void CreateProperties(Dictionary<MethodInfo, AotMethodSymbol> methodHandleToSymbol, ArrayBuilder<Symbol> members)
        {
            var module = this.ContainingAotModule;

            try
            {
                foreach (var propertyDef in (_handle).GetProperties())
                {
                    try
                    {

                        AotMethodSymbol getMethod = GetAccessorMethod( methodHandleToSymbol, propertyDef.GetGetMethod());
                        AotMethodSymbol setMethod = GetAccessorMethod( methodHandleToSymbol, propertyDef.GetSetMethod());

                        if (((object)getMethod != null) || ((object)setMethod != null))
                        {
                            members.Add(AotPropertySymbol.Create(module, this, propertyDef, getMethod, setMethod));
                        }
                    }
                    catch 
                    { }
                }
            }
            catch 
            { }
        }
        public override ImmutableArray<Symbol> GetMembers()
        {
            EnsureAllMembersAreLoaded();
            return _lazyMembersInDeclarationOrder;
        }
        public override ImmutableArray<Symbol> GetMembers(string name)
        {

            EnsureAllMembersAreLoaded();

            ImmutableArray<Symbol> m;
            if (!_lazyMembersByName.TryGetValue(name, out m))
            {
                m = ImmutableArray<Symbol>.Empty;
            }

            // nested types are not common, but we need to check just in case
            ImmutableArray<AotNamedTypeSymbol> t;
            if (_lazyNestedTypes.TryGetValue(name, out t))
            {
                m = m.Concat(StaticCast<Symbol>.From(t));
            }

            return m;
        }
        private void CreateEvents(
         MultiDictionary<string, AotFieldSymbol> privateFieldNameToSymbols,
         Dictionary<MethodInfo, AotMethodSymbol> methodHandleToSymbol,
         ArrayBuilder<Symbol> members)
        {
            var module = this.ContainingAotModule;

            try
            {
                foreach (var eventRid in (_handle).GetEvents())
                {
                    try
                    {
                     

                        // NOTE: C# ignores all other accessors (most notably, raise/fire).
                        AotMethodSymbol addMethod = GetAccessorMethod( methodHandleToSymbol, eventRid.GetAddMethod());
                        AotMethodSymbol removeMethod = GetAccessorMethod( methodHandleToSymbol, eventRid.GetRemoveMethod());

                        // NOTE: both accessors are required, but that will be reported separately.
                        // Create the symbol unless both accessors are missing.
                        if (((object)addMethod != null) || ((object)removeMethod != null))
                        {
                            members.Add(new AotEventSymbol(module, this, eventRid, addMethod, removeMethod, privateFieldNameToSymbols));
                        }
                    }
                    catch 
                    { }
                }
            }
            catch (BadImageFormatException)
            { }
        }


    }
}
