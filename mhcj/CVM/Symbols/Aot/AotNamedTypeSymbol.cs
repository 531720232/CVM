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
        internal string DefaultMemberName
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return "";
                }

                if (uncommon.lazyDefaultMemberName == null)
                {
                    string defaultMemberName;
                   
                    this.ContainingAotModule.HasDefaultMemberAttribute(_handle, out defaultMemberName);

                    // NOTE: the default member name is frequently null (e.g. if there is not indexer in the type).
                    // Make sure we set a non-null value so that we don't recompute it repeatedly.
                    // CONSIDER: this makes it impossible to distinguish between not having the attribute and
                    // having the attribute with a value of "".
                    CVM.AHelper.CompareExchange(ref uncommon.lazyDefaultMemberName, defaultMemberName ?? "", null);
                }
                return uncommon.lazyDefaultMemberName;
            }
        }
        private static readonly Dictionary<string, ImmutableArray<AotNamedTypeSymbol>> s_emptyNestedTypes = new Dictionary<string, ImmutableArray<AotNamedTypeSymbol>>();

        public override Symbol ContainingSymbol => _container;

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                foreach(ObsoleteAttribute v in Handle.GetCustomAttributes(typeof(ObsoleteAttribute),false))
                {
                    var ob=new ObsoleteAttributeData(ObsoleteAttributeKind.Obsolete, v.Message, v.IsError);
                    return ob;
                }

                return default;
            }
        }

        protected internal readonly NamespaceOrTypeSymbol _container;
        private readonly Type _handle;
        private readonly string _name;
        private readonly TypeAttributes _flags;
        private readonly SpecialType _corTypeId;
        private ICollection<string> _lazyMemberNames;
        private ImmutableArray<Symbol> _lazyMembersInDeclarationOrder;
        internal Dictionary<string, ImmutableArray<Symbol>> _lazyMembersByName;
        private Dictionary<string, ImmutableArray<AotNamedTypeSymbol>> _lazyNestedTypes;
        private TypeKind _lazyKind;
        private NamedTypeSymbol _lazyBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces = default(ImmutableArray<NamedTypeSymbol>);
        private NamedTypeSymbol _lazyDeclaredBaseTypeWithoutNullability = ErrorTypeSymbol.UnknownResultType;
        private NamedTypeSymbol _lazyDeclaredBaseTypeWithNullability = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyDeclaredInterfaces = default(ImmutableArray<NamedTypeSymbol>);

        public override NamedTypeSymbol ConstructedFrom => this;

        private bool IsUncommon()
        {
            if ((_handle).GetCustomAttributes(false).Length>0)
            {
                return true;
            }

            if (this.TypeKind == TypeKind.Enum)
            {
                return true;
            }

            return false;
        }

        internal class UncommonProperties
        {
            /// <summary>
            /// Need to import them for an enum from a linked assembly, when we are embedding it. These symbols are not included into lazyMembersInDeclarationOrder.  
            /// </summary>
            internal ImmutableArray<AotFieldSymbol> lazyInstanceEnumFields;
            internal NamedTypeSymbol lazyEnumUnderlyingType;

            // CONSIDER: Should we use a CustomAttributeBag for PE symbols?
            internal ImmutableArray<CSharpAttributeData> lazyCustomAttributes;
            internal ImmutableArray<string> lazyConditionalAttributeSymbols;
            internal ObsoleteAttributeData lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
            internal AttributeUsageInfo lazyAttributeUsageInfo = AttributeUsageInfo.Null;
            internal ThreeState lazyContainsExtensionMethods;
            internal ThreeState lazyIsByRefLike;
            internal ThreeState lazyIsReadOnly;
            internal string lazyDefaultMemberName;
            internal NamedTypeSymbol lazyComImportCoClassType = ErrorTypeSymbol.UnknownResultType;
            internal ThreeState lazyHasEmbeddedAttribute = ThreeState.Unknown;

            internal bool IsDefaultValue()
            {
                return lazyInstanceEnumFields.IsDefault &&
                    (object)lazyEnumUnderlyingType == null &&
                    lazyCustomAttributes.IsDefault &&
                    lazyConditionalAttributeSymbols.IsDefault &&
                    lazyObsoleteAttributeData == ObsoleteAttributeData.Uninitialized &&
                    lazyAttributeUsageInfo.IsNull &&
                    !lazyContainsExtensionMethods.HasValue() &&
                    lazyDefaultMemberName == null &&
                    (object)lazyComImportCoClassType == (object)ErrorTypeSymbol.UnknownResultType &&
                    !lazyHasEmbeddedAttribute.HasValue();
            }
        }

        private UncommonProperties _lazyUncommonProperties;
        private static readonly UncommonProperties s_noUncommonProperties = new UncommonProperties();

        private UncommonProperties GetUncommonProperties()
        {
            var result = _lazyUncommonProperties;
            if (result != null)
            {
                Debug.Assert(result != s_noUncommonProperties || result.IsDefaultValue(), "default value was modified");
                return result;
            }

            if (this.IsUncommon())
            {
                result = new UncommonProperties();
                return CVM.AHelper.CompareExchange(ref _lazyUncommonProperties, result, null) ?? result;
            }

            _lazyUncommonProperties = result = s_noUncommonProperties;
            return result;
        }
        private IEnumerable<FieldSymbol> GetEnumFieldsToEmit()
        {
            var uncommon = GetUncommonProperties();
            if (uncommon == s_noUncommonProperties)
            {
                yield break;
            }

            var moduleSymbol = this.ContainingAotModule;
            var module = moduleSymbol;

            // Non-static fields of enum types are not imported by default because they are not bindable,
            // but we need them for NoPia.

            var fieldDefs = ArrayBuilder<FieldInfo>.GetInstance();

            try
            {
                foreach (var fieldDef in _handle.GetFields())
                {
                    fieldDefs.Add(fieldDef);
                }
            }
            catch (BadImageFormatException)
            { }

            if (uncommon.lazyInstanceEnumFields.IsDefault)
            {
                var builder = ArrayBuilder<AotFieldSymbol>.GetInstance();

                foreach (var fieldDef in fieldDefs)
                {
                    try
                    {
                        FieldAttributes fieldFlags =fieldDef.Attributes;
                        if ((fieldFlags & FieldAttributes.Static) == 0)
                        {
                            builder.Add(new AotFieldSymbol(moduleSymbol, this, fieldDef));
                        }
                    }
                    catch (BadImageFormatException)
                    { }
                }

                ImmutableInterlocked.InterlockedInitialize(ref uncommon.lazyInstanceEnumFields, builder.ToImmutableAndFree());
            }

            int staticIndex = 0;
            ImmutableArray<Symbol> staticFields = GetMembers();
            int instanceIndex = 0;

            foreach (var fieldDef in fieldDefs)
            {
                if (instanceIndex < uncommon.lazyInstanceEnumFields.Length && uncommon.lazyInstanceEnumFields[instanceIndex].Handle == fieldDef)
                {
                    yield return uncommon.lazyInstanceEnumFields[instanceIndex];
                    instanceIndex++;
                    continue;
                }

                if (staticIndex < staticFields.Length && staticFields[staticIndex].Kind == SymbolKind.Field)
                {
                    var field = (AotFieldSymbol)staticFields[staticIndex];

                    if (field.Handle == fieldDef)
                    {
                        yield return field;
                        staticIndex++;
                        continue;
                    }
                }
            }

            fieldDefs.Free();

            Debug.Assert(instanceIndex == uncommon.lazyInstanceEnumFields.Length);
            Debug.Assert(staticIndex == staticFields.Length || staticFields[staticIndex].Kind != SymbolKind.Field);
        }
        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            if (this.TypeKind == TypeKind.Enum)
            {
                return GetEnumFieldsToEmit();
            }
            else
            {
                // If there are any non-event fields, they are at the very beginning.
                IEnumerable<FieldSymbol> nonEventFields = GetMembers<FieldSymbol>(this.GetMembers(), SymbolKind.Field, offset: 0);

                // Event backing fields are not part of the set returned by GetMembers. Let's add them manually.
                ArrayBuilder<FieldSymbol> eventFields = null;

                foreach (var eventSymbol in GetEventsToEmit())
                {
                    FieldSymbol associatedField = eventSymbol.AssociatedField;
                    if ((object)associatedField != null)
                    {
                        Debug.Assert((object)associatedField.AssociatedSymbol != null);
                        Debug.Assert(!nonEventFields.Contains(associatedField));

                        if (eventFields == null)
                        {
                            eventFields = ArrayBuilder<FieldSymbol>.GetInstance();
                        }

                        eventFields.Add(associatedField);
                    }
                }

                if (eventFields == null)
                {
                    // Simple case
                    return nonEventFields;
                }

                // We need to merge non-event fields with event fields while preserving their relative declaration order
                var handleToFieldMap = new SmallDictionary<FieldInfo, FieldSymbol>();
                int count = 0;

                foreach (AotFieldSymbol field in nonEventFields)
                {
                    handleToFieldMap.Add(field.Handle, field);
                    count++;
                }

                foreach (AotFieldSymbol field in eventFields)
                {
                    handleToFieldMap.Add(field.Handle, field);
                }

                count += eventFields.Count;
                eventFields.Free();

                var result = ArrayBuilder<FieldSymbol>.GetInstance(count);

                try
                {
                    foreach (var handle in (_handle).GetFields())
                    {
                        FieldSymbol field;
                        if (handleToFieldMap.TryGetValue(handle, out field))
                        {
                            result.Add(field);
                        }
                    }
                }
                catch (BadImageFormatException)
                { }

                Debug.Assert(result.Count == count);

                return result.ToImmutableAndFree();
            }
        }
        private static IEnumerable<TSymbol> GetMembers<TSymbol>(ImmutableArray<Symbol> members, SymbolKind kind, int offset = -1)
           where TSymbol : Symbol
        {
            if (offset < 0)
            {
                offset = GetIndexOfFirstMember(members, kind);
            }
            int n = members.Length;
            for (int i = offset; i < n; i++)
            {
                var member = members[i];
                if (member.Kind != kind)
                {
                    yield break;
                }
                yield return (TSymbol)member;
            }
        }
        private static int GetIndexOfFirstMember(ImmutableArray<Symbol> members, SymbolKind kind)
        {
            int n = members.Length;
            for (int i = 0; i < n; i++)
            {
                if (members[i].Kind == kind)
                {
                    return i;
                }
            }
            return n;
        }
        public override Accessibility DeclaredAccessibility
        {
            get
            {
                Accessibility access = Accessibility.Private;

                switch (_flags & TypeAttributes.VisibilityMask)
                {
                    case TypeAttributes.NestedAssembly:
                        access = Accessibility.Internal;
                        break;

                    case TypeAttributes.NestedFamORAssem:
                        access = Accessibility.ProtectedOrInternal;
                        break;

                    case TypeAttributes.NestedFamANDAssem:
                        access = Accessibility.ProtectedAndInternal;
                        break;

                    case TypeAttributes.NestedPrivate:
                        access = Accessibility.Private;
                        break;

                    case TypeAttributes.Public:
                    case TypeAttributes.NestedPublic:
                        access = Accessibility.Public;
                        break;

                    case TypeAttributes.NestedFamily:
                        access = Accessibility.Protected;
                        break;

                    case TypeAttributes.NotPublic:
                        access = Accessibility.Internal;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_flags & TypeAttributes.VisibilityMask);
                }

                return access;
            }
        }
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }
        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ContainingAotModule.Locations;
            }
        }
        public override IEnumerable<string> MemberNames
        {
            get
            {
                EnsureNonTypeMemberNamesAreLoaded();
                return _lazyMemberNames;
            }
        }
        private void EnsureNonTypeMemberNamesAreLoaded()
        {
            if (_lazyMemberNames == null)
            {
                var moduleSymbol = ContainingAotModule;
                var module = moduleSymbol;

                var names = new HashSet<string>();

                try
                {
                    foreach (var methodDef in (_handle).GetMethods())
                    {
                        try
                        {
                            names.Add((methodDef).Name);
                        }
                        catch 
                        { }
                    }
                }
                catch 
                { }

                try
                {
                    foreach (var propertyDef in (_handle).GetProperties())
                    {
                        try
                        {
                            names.Add(propertyDef.Name);
                        }
                        catch (BadImageFormatException)
                        { }
                    }
                }
                catch (BadImageFormatException)
                { }

                try
                {
                    foreach (var eventDef in (_handle).GetEvents())
                    {
                        try
                        {
                            names.Add((eventDef).Name);
                        }
                        catch (BadImageFormatException)
                        { }
                    }
                }
                catch (BadImageFormatException)
                { }

                try
                {
                    foreach (var fieldDef in (_handle).GetFields())
                    {
                        try
                        {
                            names.Add(fieldDef.Name);
                        }
                        catch (BadImageFormatException)
                        { }
                    }
                }
                catch (BadImageFormatException)
                { }

                // From C#'s perspective, structs always have a public constructor
                // (even if it's not in metadata).  Add it unconditionally and let
                // the hash set de-dup.
                if (this.IsValueType)
                {
                    names.Add(WellKnownMemberNames.InstanceConstructorName);
                }

                CVM.AHelper.CompareExchange(ref _lazyMemberNames, CreateReadOnlyMemberNames(names), null);
            }
        }
        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }
        public override bool MightContainExtensionMethods
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return false;
                }

                if (!uncommon.lazyContainsExtensionMethods.HasValue())
                {
                    var contains = ThreeState.False;
                    // Dev11 supports extension methods defined on non-static
                    // classes, structs, delegates, and generic types.
                    switch (this.TypeKind)
                    {
                        case TypeKind.Class:
                        case TypeKind.Struct:
                        case TypeKind.Delegate:
                            var moduleSymbol = this.ContainingAotModule;
                            var module = moduleSymbol;
                           
                            bool moduleHasExtension = _handle.GetCustomAttributes(typeof(System.Runtime.CompilerServices.ExtensionAttribute),false).Length>0;

                            var containingAssembly = this.ContainingAssembly as AotAssemblySymbol;
                            if ((object)containingAssembly != null)
                            {
                                contains = (moduleHasExtension
                                    && containingAssembly.MightContainExtensionMethods).ToThreeState();
                            }
                            else
                            {
                                contains = moduleHasExtension.ToThreeState();
                            }
                            break;
                    }

                    uncommon.lazyContainsExtensionMethods = contains;
                }

                return uncommon.lazyContainsExtensionMethods.Value();
            }
        }
        private static ICollection<string> CreateReadOnlyMemberNames(HashSet<string> names)
        {
            switch (names.Count)
            {
                case 0:
                    return SpecializedCollections.EmptySet<string>();

                case 1:
                    return SpecializedCollections.SingletonCollection(names.First());

                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    // PERF: Small collections can be implemented as ImmutableArray.
                    // While lookup is O(n), when n is small, the memory savings are more valuable.
                    // Size 6 was chosen because that represented 50% of the names generated in the Picasso end to end.
                    // This causes boxing, but that's still superior to a wrapped HashSet
                    return ImmutableArray.CreateRange(names);

                default:
                    return (names);
            }
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
          
            var uncommon = GetUncommonProperties();
            if (uncommon == s_noUncommonProperties)
            {
                return ((object)this.BaseTypeNoUseSiteDiagnostics != null) ? this.BaseTypeNoUseSiteDiagnostics.GetAttributeUsageInfo() : AttributeUsageInfo.Default;
            }

            if (uncommon.lazyAttributeUsageInfo.IsNull)
            {
                uncommon.lazyAttributeUsageInfo = this.DecodeAttributeUsageInfo();
            }

            return uncommon.lazyAttributeUsageInfo;


        }
        private AttributeUsageInfo DecodeAttributeUsageInfo()
        {
            var objs = _handle.GetCustomAttributes(typeof(AttributeUsageAttribute), false);
            foreach(var obj in objs)
            {
                if(obj is AttributeUsageAttribute atr)
                {
                    AttributeUsageInfo info = new AttributeUsageInfo(atr.ValidOn,atr.AllowMultiple,atr.Inherited);
                   return info.HasValidAttributeTargets ? info : AttributeUsageInfo.Default;
                }
            }
         

            return ((object)this.BaseTypeNoUseSiteDiagnostics != null) ? this.BaseTypeNoUseSiteDiagnostics.GetAttributeUsageInfo() : AttributeUsageInfo.Default;
        }
        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

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
            var arity = gen.Length;
            bool m;
            var nw = type.Name;
            AotNamedTypeSymbol result;
            if (arity == 0)
            {
                result = new AotNamedTypeSymbolNonGeneric(aot, ab, type, em,out m);
            }
            else
            {
                result = new AotNamedTypeSymbolGeneric(
                    aot,
                    ab,
                    type,
                    em,
                    gen,
                    arity,out m
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
            , out bool mangleName
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
                mangleName = false;
            }
            else
            {
                // Unmangle name for a generic type.
                _name = MetadataHelpers.UnmangleMetadataNameForArity(metadataName, arity);
                Debug.Assert(ReferenceEquals(_name, metadataName) == (_name == metadataName));
              mangleName = !ReferenceEquals(_name, metadataName);
            }

            // check if this is one of the COR library types
            if (emittedNamespaceName != null &&
                moduleSymbol.ContainingAssembly.KeepLookingForDeclaredSpecialTypes &&
                this.DeclaredAccessibility == Accessibility.Public) // NB: this.flags was set above.
            {
                _corTypeId = SpecialTypes.GetTypeFromMetadataName(MetadataHelpers.BuildQualifiedName(emittedNamespaceName, metadataName));
            }
            else
            {
                _corTypeId = SpecialType.None;
            }
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
                var dc = new MetadataDecoder(moduleSymbol);
                var interfaceImpls = _handle.GetInterfaces();

                if (interfaceImpls.Length > 0)
                {
                    var symbols = ArrayBuilder<NamedTypeSymbol>.GetInstance(interfaceImpls.Length);

                    foreach (var interfaceImpl in interfaceImpls)
                    {
                        TypeSymbol typeSymbol = dc.GetTypeOfToken(interfaceImpl);


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
        public override TypeKind TypeKind
        {
            get
            {
                TypeKind result = _lazyKind;

                if (result == TypeKind.Unknown)
                {
                    if ((_flags&TypeAttributes.Interface)!=0)
                    {
                        result = TypeKind.Interface;
                    }
                    else
                    {
                        TypeSymbol @base = GetDeclaredBaseType(ignoreNullability: true);

                        result = TypeKind.Class;

                        if ((object)@base != null)
                        {
                            SpecialType baseCorTypeId = @base.SpecialType;

                            switch (baseCorTypeId)
                            {
                                case SpecialType.System_Enum:
                                    // Enum
                                    result = TypeKind.Enum;
                                    break;

                                case SpecialType.System_MulticastDelegate:
                                    // Delegate
                                    result = TypeKind.Delegate;
                                    break;

                                case SpecialType.System_ValueType:
                                    // System.Enum is the only class that derives from ValueType
                                    if (this.SpecialType != SpecialType.System_Enum)
                                    {
                                        // Struct
                                        result = TypeKind.Struct;
                                    }
                                    break;
                            }
                        }
                    }

                    _lazyKind = result;
                }

                return result;
            }
        }
        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return this.GetMembersUnordered();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            return this.GetMembers(name);
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                return ImmutableArray<TypeSymbolWithAnnotations>.Empty;
            }
        }
        //    List<AotFieldSymbol> ffs = new List<AotFieldSymbol>();
        // void Load()
        // {
        //     Type wee;

        //var fs=     Handle.GetMembers();
        //     foreach(var f in fs)
        //     {
        //       switch (f)

        //         {
        //             case FieldInfo field:
        //                 var n_f = new AotFieldSymbol(this.ContainingAotModule, this, field);
        //                 ffs.Add(n_f);
        //                 break;
        //             case MethodBase mb:
        //                 var m_f = new AotMethodSymbol( mb);
        //                 // ffs.Add(n_f);
        //                 break;
        //             case EventInfo ev:
        //                 var e_f = new AotEventSymbol(ContainingAotModule, this, ev, default);
        //                 break;
        //             case PropertyInfo pr:

        //                 break;
        //         }               

        //     }   


        // }
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
                    //try
                    //{
                    //    if (!(isOrdinaryEmbeddableStruct ||
                    //        (isOrdinaryStruct && (fieldRid.Attributes & FieldAttributes.Static) == 0) 
                    //        ))
                    //    {
                    //        continue;
                    //    }
                    //}
                    //catch (BadImageFormatException)
                    //{ }

                    var symbol = new AotFieldSymbol(moduleSymbol, this, fieldRid);
                    fieldMembers.Add(symbol);

                    var v = symbol.ConstantValue;
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
                   this.CreateConstructor(nonFieldMembers);

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
        private void CreateConstructor(ArrayBuilder<Symbol> members)
        {
            var module = this.ContainingAotModule;

            // for ordinary embeddable struct types we import private members so that we can report appropriate errors if the structure is used 
            var isOrdinaryEmbeddableStruct = (this.TypeKind == TypeKind.Struct) && (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.None) && this.ContainingAssembly.IsLinked;

            try
            {
                foreach (var methodHandle in _handle.GetConstructors())
                {
                    //  if (isOrdinaryEmbeddableStruct || module.ShouldImportMethod(methodHandle, moduleSymbol.ImportOptions))
                    {
                        var method = new AotMethodSymbol(module, this, methodHandle);
                        members.Add(method);
                      var a1=  method.Parameters;
                     //   map.Add(methodHandle, method);
                    }
                }

            }
            catch
            { }

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
            catch 
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
          var pw=      _handle.GetProperties();
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
