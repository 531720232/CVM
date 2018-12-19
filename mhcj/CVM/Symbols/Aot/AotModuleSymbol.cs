using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CVM.Collections.Concurrent;
using CVM.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class AotModuleSymbol : ModuleSymbol

    {
        public override NamespaceSymbol GlobalNamespace => _globalNamespace;

        public override Symbol ContainingSymbol => _assemblySymbol;

        public override ImmutableArray<Location> Locations => throw new NotImplementedException();

        internal override int Ordinal => 0;

        internal override bool Bit32Required => true;

        internal override bool IsMissing => false;

        internal override bool HasUnifiedReferences => throw new NotImplementedException();

        internal override ICollection<string> TypeNames => types.AsCaseInsensitiveCollection();

        internal override ICollection<string> NamespaceNames => ns.AsCaseInsensitiveCollection();

        internal override bool HasAssemblyCompilationRelaxationsAttribute => throw new NotImplementedException();

        internal override bool HasAssemblyRuntimeCompatibilityAttribute => throw new NotImplementedException();

        internal override CharSet? DefaultMarshallingCharSet =>CharSet.Ansi;

        private  AssemblySymbol _assemblySymbol;

        public override AssemblySymbol ContainingAssembly =>_assemblySymbol;
        public override ModuleMetadata GetMetadata()
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<AssemblyIdentity> GetReferencedAssemblies()
        {
            return ImmutableArray<AssemblyIdentity>.Empty;
        }

        internal override ImmutableArray<AssemblySymbol> GetReferencedAssemblySymbols()
        {
            throw new NotImplementedException();
        }

        internal override bool GetUnificationUseSiteDiagnostic(ref DiagnosticInfo result, TypeSymbol dependentType)
        {
            throw new NotImplementedException();
        }

        internal override NamedTypeSymbol LookupTopLevelMetadataType(ref MetadataTypeName emittedName)
        {
            NamedTypeSymbol result;
            NamespaceSymbol scope = this.GlobalNamespace.LookupNestedNamespace(emittedName.NamespaceSegments);

            if ((object)scope == null)
            {
                // We failed to locate the namespace
                result = new MissingMetadataTypeSymbol.TopLevel(this, ref emittedName);
            }
            else
            {
                result = scope.LookupMetadataType(ref emittedName);
            }

            Debug.Assert((object)result != null);
            return result;
        }

        internal override void SetReferences(ModuleReferences<AssemblySymbol> moduleReferences, SourceAssemblySymbol originatingSourceAssemblyDebugOnly = null)
        {
            throw new NotImplementedException();
        }
        IdentifierCollection types;
        IdentifierCollection ns;

        /// <summary>
        /// Global namespace.
        /// </summary>
        private readonly AotNamespaceSymbol _globalNamespace;



        internal AotModuleSymbol(AssemblySymbol ad):base()
        {
            types = ComputeTypeNameCollection();
            ns = ComputeNamespaceNameCollection();

            _globalNamespace = new AotGlobalNamespaceSymbol(this);
  //          ((AotGlobalNamespaceSymbol)_globalNamespace).AutoBind();
            _assemblySymbol = ad;

            //var ty = typeof(object).Assembly;
            //foreach(var t in ty.GetTypes())
            //{

            //    if (!types.Contains(t.Name))
            //          { types.Add(t.Name); }

            //    if (!ns.Contains(t.Namespace))
            //    { ns.Add(t.Namespace); }
            //}
        }
        private IdentifierCollection ComputeNamespaceNameCollection()
        {
            try
            {

                var ts = typeof(object).Assembly.GetTypes();
                var full = new List<string>();
                foreach(var t in ts)
                {

                    if (full.Contains(t.Namespace) || CVM.AHelper.IsNullOrWhiteSpace(t.Namespace))
                        continue;
                        full.Add(t.Namespace);
                }
             

                var namespaceNames =
                    from fullName in full.Distinct()
                    from name in fullName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                    select name;

                return new IdentifierCollection(namespaceNames);
            }
            catch 
            {
                return new IdentifierCollection();
            }
        }
        private IdentifierCollection ComputeTypeNameCollection()
        {
            try
            {
                var allTypeDefs = typeof(object).Assembly.GetTypes();
                var typeNames =
                    from typeDef in allTypeDefs
                    let metadataName = typeDef.Name
                    let backtickIndex = metadataName.IndexOf('`')
                    select backtickIndex < 0 ? metadataName : metadataName.Substring(0, backtickIndex);

                return new IdentifierCollection(typeNames);
            }
            catch 
            {
                return new IdentifierCollection();
            }
        }
        internal IEnumerable<IGrouping<string, Type>> GroupTypesByNamespaceOrThrow()//StringComparer nameComparer)
        {
            // TODO: Consider if we should cache the result (not the IEnumerable, but the actual values).

            // NOTE:  Rather than use a sorted dictionary, we accumulate the groupings in a normal dictionary
            // and then sort the list.  We do this so that namespaces with distinct names are not
            // merged, even if they are equal according to the provided comparer.  This improves the error
            // experience because types retain their exact namespaces.

            Dictionary<string, ArrayBuilder<Type>> namespaces = new Dictionary<string, ArrayBuilder<Type>>();

            GetTypeNamespaceNamesOrThrow(namespaces);

            GetForwardedTypeNamespaceNamesOrThrow(namespaces);

            var result = new ArrayBuilder<IGrouping<string, Type>>(namespaces.Count);

            foreach (var pair in namespaces)
            {
                result.Add(new Grouping<string, Type>(pair.Key, pair.Value ?? SpecializedCollections.EmptyEnumerable<Type>()));
            }

           result.Sort(new TypesByNamespaceSortComparer(System.StringComparer.Ordinal));
            return result;
        }
        internal class TypesByNamespaceSortComparer : IComparer<IGrouping<string, Type>>
        {
            private readonly StringComparer _nameComparer;

            public TypesByNamespaceSortComparer(StringComparer nameComparer)
            {
                _nameComparer = nameComparer;
            }

            public int Compare(IGrouping<string, Type> left, IGrouping<string, Type> right)
            {
                if (left == right)
                {
                    return 0;
                }

                int result = _nameComparer.Compare(left.Key, right.Key);

                //if (result == 0)
                //{
                //    var fLeft = left.FirstOrDefault();
                //    var fRight = right.FirstOrDefault();

                //    if (fLeft==null ^ fRight==null)
                //    {
                //        result = fLeft==null ? +1 : -1;
                //    }
                //    else
                //    {
                //        result =(fLeft, fRight);
                //    }

                //    if (result == 0)
                //    {
                //        // This can only happen when both are for forwarded types.
                //        result = string.CompareOrdinal(left.Key, right.Key);
                //    }
                //}

                return result;
            }
        }

        private void GetForwardedTypeNamespaceNamesOrThrow(Dictionary<string, ArrayBuilder<Type>> namespaces)
        {
            //EnsureForwardTypeToAssemblyMap();

            //foreach (var typeName in _lazyForwardedTypesToAssemblyIndexMap.Keys)
            //{
            //    int index = typeName.LastIndexOf('.');
            //    string namespaceName = index >= 0 ? typeName.Substring(0, index) : "";
            //    if (!namespaces.ContainsKey(namespaceName))
            //    {
            //        namespaces.Add(namespaceName, null);
            //    }
            //}
        }
        private class NamespaceHandleEqualityComparer : IEqualityComparer<string>
        {
            public static readonly NamespaceHandleEqualityComparer Singleton = new NamespaceHandleEqualityComparer();

            private NamespaceHandleEqualityComparer()
            {
            }

            public bool Equals(string x, string y)
            {
                return x == y;
            }

            public int GetHashCode(string obj)
            {
                return obj.GetHashCode();
            }
        }
        public string GetTypeDefNameOrThrow(Type typeDef)
        {
        
            string name = typeDef.Name;
            Debug.Assert(name.Length == 0 || MetadataHelpers.IsValidMetadataIdentifier(name)); // Obfuscated assemblies can have types with empty names.

            // The problem is that the mangled name for an static machine type looks like 
            // "<" + methodName + ">d__" + uniqueId.However, methodName will have dots in 
            // it for explicit interface implementations (e.g. "<I.F>d__0").  Unfortunately, 
            // the native compiler emits such names in a very strange way: everything before 
            // the last dot goes in the namespace (!!) field of the typedef.Since state
            // machine types are always nested types and since nested types never have 
            // explicit namespaces (since they are in the same namespaces as their containing
            // types), it should be safe to check for a non-empty namespace name on a nested
            // type and prepend the namespace name and a dot to the type name.  After that, 
            // debugging support falls out.
            if (IsNestedTypeDefOrThrow(typeDef))
            {
                string namespaceName = typeDef.Namespace;
                if (namespaceName.Length > 0)
                {
                    // As explained above, this is not really the qualified name - the namespace
                    // name is actually the part of the name that preceded the last dot (in bad
                    // metadata).
                    name = namespaceName + "." + name;
                }
            }

            return name;
        }
        internal bool IsNestedTypeDefOrThrow(Type typeDef)
        {
            return  typeDef.IsNested;
        }
    
        private void GetTypeNamespaceNamesOrThrow(Dictionary<string, ArrayBuilder<Type>> namespaces)
        {
            // PERF: Group by namespace handle so we only have to allocate one string for every namespace
            var namespaceHandles = new Dictionary<string, ArrayBuilder<Type>>(0,NamespaceHandleEqualityComparer.Singleton);
            foreach (TypeDefToNamespace pair in GetTypeDefsOrThrow(topLevelOnly: true))
            {
                var nsHandle = pair.NamespaceHandle;
                var typeDef = pair.TypeDef;

                ArrayBuilder<Type> builder;
              if(nsHandle==null)
                {
                    nsHandle = "";
                }
                if (namespaceHandles.TryGetValue(nsHandle, out builder))
                {
                    builder.Add(typeDef);
                }
                else
                {
                    namespaceHandles.Add(nsHandle, new ArrayBuilder<Type> { typeDef });
                }
            }

            foreach (var kvp in namespaceHandles)
            {
                string @namespace = kvp.Key;

                ArrayBuilder<Type> builder;

                if (namespaces.TryGetValue(@namespace, out builder))
                {
                    builder.AddRange(kvp.Value);
                }
                else
                {
                    namespaces.Add(@namespace, kvp.Value);
                }
            }
        }

        internal bool HasDecimalConstantAttribute(MemberInfo handle, out ConstantValue defaultValue)
        {
            defaultValue = ConstantValue.Bad;
            try
            {
                var objs = handle.GetCustomAttributes(typeof(System.Runtime.CompilerServices.DecimalConstantAttribute), false);

                foreach(var obj in objs)
                {
                    if(obj is System.Runtime.CompilerServices.DecimalConstantAttribute d)
                    {
                        defaultValue = ConstantValue.Create(d.Value);
                        return true;
                    }
                }
              

            }
            catch
            {

            }
            return false;
        }

        internal ConstantValue GetConstantFieldValue(FieldInfo handle)
        {
       switch(handle.GetRawConstantValue())
            {
                case bool b:
                    return ConstantValue.Create(b);
                case char c :
                    return ConstantValue.Create(c);
                case sbyte s:
                    return ConstantValue.Create(s);
                case Int16 it16:
                    return ConstantValue.Create(it16);
                case Int32 i32:
                    return ConstantValue.Create(i32);
                case Int64 i64:
                    return ConstantValue.Create(i64);
                case byte be:
                    return ConstantValue.Create(be);
                case UInt16 u16:
                    return ConstantValue.Create(u16);
                case UInt32 u32:
                    return ConstantValue.Create(u32);
                case UInt64 u64:
                    return ConstantValue.Create(u64);
                case Single sg:
                    return ConstantValue.Create(sg);
                case Double de:
                    return ConstantValue.Create(de);
                case String sr:
                    return ConstantValue.Create(sr);
                case null:
                    return ConstantValue.Null;
                
                default:
                
                    return ConstantValue.Bad;
            }
        
        }

        private struct TypeDefToNamespace
        {
            internal readonly Type TypeDef;
            internal readonly string NamespaceHandle;

            internal TypeDefToNamespace(Type typeDef, string namespaceHandle)
            {
                TypeDef = typeDef;
                NamespaceHandle = namespaceHandle;
            }
        }
        private IEnumerable<TypeDefToNamespace> GetTypeDefsOrThrow(bool topLevelOnly)
        {
            foreach (var typeDef in typeof(object).Assembly.GetTypes())
            {
              

                if (topLevelOnly && typeDef.IsNested)
                {
                    continue;
                }

                yield return new TypeDefToNamespace(typeDef, typeDef.Namespace);
            }
        }
        internal readonly ConcurrentDictionary<Type, TypeSymbol> TypeHandleToTypeMap =
                                  new ConcurrentDictionary<Type, TypeSymbol>(concurrencyLevel: 2, capacity: 31);
        internal void OnNewTypeDeclarationsLoaded(
          Dictionary<string, ImmutableArray<AotNamedTypeSymbol>> typesDict)
        {
            bool keepLookingForDeclaredCorTypes =  _assemblySymbol.KeepLookingForDeclaredSpecialTypes;

            foreach (var types in typesDict.Values)
            {
                foreach (var type in types)
                {
                    bool added;
                    added = TypeHandleToTypeMap.TryAdd(type.Handle, type);
                    Debug.Assert(added);

                    // Register newly loaded COR types
                    if (keepLookingForDeclaredCorTypes && type.SpecialType != SpecialType.None)
                    {
                        _assemblySymbol.RegisterDeclaredSpecialType(type);
                        keepLookingForDeclaredCorTypes = _assemblySymbol.KeepLookingForDeclaredSpecialTypes;
                    }
                }
            }
        }


    }
}
