using CVM.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
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
                        TypeSymbol decodedType = ((AotModuleSymbol)moduleSymbol).TypeHandleToTypeMap[token];
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

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            throw new NotImplementedException();
        }

    }
}
