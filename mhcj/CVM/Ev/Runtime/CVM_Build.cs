using CVM;
using CVM.Collections.Immutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp.Runtime
{
    internal static class Help_Symbol
    {
        public static object Invoke(this IMethodSymbol symbol, CVM_Build build,object[] param)
        {
         
         
            if (symbol is SourceMethodSymbol m1)
            {
              
                if (build.bodys.ContainsKey(m1))
                {

                    var body = build.bodys[m1];
                     
                    var str = new System.Text.StringBuilder();
                    foreach (var bo in body.IL)
                    {
                        str.AppendLine(bo.ToString());
                    }
                       if(m1.ParameterCount!=param.Length)
                    {
                        throw new Exception();
                    }
                    return str;
                }
            }
            if (symbol is AotMethodSymbol m2)
            {
              
            return    m2.Handle.Invoke(null, null);
            }
            return null;
     
           

        }
    }

    internal  class CVM_Build
    {
        Microsoft.CodeAnalysis.Emit.EmitContext EmitContext;
        CancellationToken _cancellationToken;
        protected readonly CommonPEModuleBuilder module;


        internal CVM_Build(Microsoft.CodeAnalysis.Emit.EmitContext context,
            CancellationToken cancellationToken=default)
        {
            EmitContext = context;
            module = context.Module;

            int numMethods = this.module.HintNumberOfMethodDefinitions;
            int numTypeDefsGuess = numMethods / 6;
            int numFieldDefsGuess = numTypeDefsGuess * 4;
            int numPropertyDefsGuess = numMethods / 4;

            _typeDefs = new DefinitionIndex<ITypeDefinition>(numTypeDefsGuess);
            _eventDefs = new DefinitionIndex<IEventDefinition>(0);
            _fieldDefs = new DefinitionIndex<IFieldDefinition>(numFieldDefsGuess);
            _methodDefs = new DefinitionIndex<IMethodDefinition>(numMethods);
            _propertyDefs = new DefinitionIndex<IPropertyDefinition>(numPropertyDefsGuess);
            _parameterDefs = new DefinitionIndex<IParameterDefinition>(numMethods);
            _genericParameters = new DefinitionIndex<IGenericParameter>(0);

            _fieldDefIndex = new Dictionary<ITypeDefinition, int>(numTypeDefsGuess);
            _methodDefIndex = new Dictionary<ITypeDefinition, int>(numTypeDefsGuess);
            _parameterListIndex = new Dictionary<IMethodDefinition, int>(numMethods);
            CreateIndices();
            SerializeMethodBodies();
        }
        private void CreateIndices()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            this.CreateUserStringIndices();
            this.CreateIndicesForModule();

            // Find all references and assign tokens.
            //_referenceVisitor = this.CreateReferenceVisitor();
            //_referenceVisitor.Visit(module);

            this.CreateMethodBodyReferenceIndex();

        //    this.OnIndicesCreated();
        }

        protected  IList<IMethodDefinition> GetMethodDefs()
        {
            return _methodDefs.Rows;
        }
        public static MethodAttributes GetMethodAttributes(IMethodDefinition methodDef)
        {
            var result = (MethodAttributes)methodDef.Visibility;
            if (methodDef.IsStatic)
            {
                result |= MethodAttributes.Static;
            }

            if (methodDef.IsSealed)
            {
                result |= MethodAttributes.Final;
            }

            if (methodDef.IsVirtual)
            {
                result |= MethodAttributes.Virtual;
            }

            if (methodDef.IsHiddenBySignature)
            {
                result |= MethodAttributes.HideBySig;
            }

            if (methodDef.IsNewSlot)
            {
                result |= MethodAttributes.NewSlot;
            }

            if (methodDef.IsAccessCheckedOnOverride)
            {
                result |= MethodAttributes.CheckAccessOnOverride;
            }

            if (methodDef.IsAbstract)
            {
                result |= MethodAttributes.Abstract;
            }

            if (methodDef.IsSpecialName)
            {
                result |= MethodAttributes.SpecialName;
            }

            if (methodDef.IsRuntimeSpecial)
            {
                result |= MethodAttributes.RTSpecialName;
            }

            if (methodDef.IsPlatformInvoke)
            {
                result |= MethodAttributes.PinvokeImpl;
            }

            if (methodDef.HasDeclarativeSecurity)
            {
                result |= MethodAttributes.HasSecurity;
            }

            if (methodDef.RequiresSecurityObject)
            {
                result |= MethodAttributes.RequireSecObject;
            }

            return result;
        }
        public Dictionary<IMethodDefinition, IMethodBody> bodys=new Dictionary<IMethodDefinition, IMethodBody>();

        internal NamedTypeSymbol GetType(string ty)
        {
            NamedTypeSymbol type;

            type= module.CommonCompilation.SourceAssembly.GetTypeByMetadataName(ty);
          if(type==null)
            {
                type= AotAssemblySymbol.Inst.GetTypeByMetadataName(ty);

            }
            return type;
        }
        internal TypeSymbol GetType(Type ty)
        {
            TypeSymbol type;

            type = module.CommonCompilation.SourceAssembly.GetTypeByReflectionType(ty,true);
            if (type == null)
            {
                type = AotAssemblySymbol.Inst.GetTypeByReflectionType(ty,false);

            }
            return type;
        }

        private int[] SerializeMethodBodies()
        {

            var methods = this.GetMethodDefs();
            int[] bodyOffsets = new int[methods.Count];

       

            int methodRid = 1;
            foreach (IMethodDefinition method in methods)
            {
         
                _cancellationToken.ThrowIfCancellationRequested();
                int bodyOffset;
                IMethodBody body;

                if (method.HasBody())
                {
                    body = method.GetBody(EmitContext);

                    if (body != null)
                    {

                        bodys[method] = body;
                    }
                    else
                    {
                        bodyOffset = 0;
                    
                    }
                }
                else
                {
                    // 0 is actually written to metadata when the row is serialized
                    bodyOffset = -1;
                    body = null;
                
                }

             

                methodRid++;
            }

            return bodyOffsets;
        }
        private void CreateIndicesForModule()
        {
            var nestedTypes = new Queue<INestedTypeDefinition>();

            foreach (INamespaceTypeDefinition typeDef in this.GetTopLevelTypes(this.module))
            {
                this.CreateIndicesFor(typeDef, nestedTypes);
            }

            while (nestedTypes.Count > 0)
            {
                var nestedType = nestedTypes.Dequeue();
                this.CreateIndicesFor(nestedType, nestedTypes);
            }
        }
        private struct DefinitionIndex<T> where T : IReference
        {
            // IReference to RowId
            private readonly Dictionary<T, int> _index;
            private readonly List<T> _rows;

            public DefinitionIndex(int capacity)
            {
                _index = new Dictionary<T, int>(capacity);
                _rows = new List<T>(capacity);
            }

            public bool TryGetValue(T item, out int rowId)
            {
                return _index.TryGetValue(item, out rowId);
            }

            public int this[T item]
            {
                get { return _index[item]; }
            }

            public T this[int rowId]
            {
                get { return _rows[rowId - 1]; }
            }

            public IList<T> Rows
            {
                get { return _rows; }
            }

            public int NextRowId
            {
                get { return _rows.Count + 1; }
            }

            public void Add(T item)
            {
                _index.Add(item, NextRowId);
                _rows.Add(item);
            }
        }
        private readonly DefinitionIndex<ITypeDefinition> _typeDefs;
        private readonly DefinitionIndex<IEventDefinition> _eventDefs;
        private readonly DefinitionIndex<IFieldDefinition> _fieldDefs;
        private readonly DefinitionIndex<IMethodDefinition> _methodDefs;
        private readonly DefinitionIndex<IPropertyDefinition> _propertyDefs;
        private readonly DefinitionIndex<IParameterDefinition> _parameterDefs;
        private readonly DefinitionIndex<IGenericParameter> _genericParameters;
        private readonly Dictionary<ITypeDefinition, int> _fieldDefIndex;
        private readonly Dictionary<ITypeDefinition, int> _methodDefIndex;
        private readonly Dictionary<IMethodDefinition, int> _parameterListIndex;
        protected IEnumerable<IGenericTypeParameter> GetConsolidatedTypeParameters(ITypeDefinition typeDef)
        {
            INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(EmitContext);
            if (nestedTypeDef == null)
            {
                if (typeDef.IsGeneric)
                {
                    return typeDef.GenericParameters;
                }

                return null;
            }

            return this.GetConsolidatedTypeParameters(typeDef, typeDef);
        }

        private List<IGenericTypeParameter> GetConsolidatedTypeParameters(ITypeDefinition typeDef, ITypeDefinition owner)
        {
            List<IGenericTypeParameter> result = null;
            INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(EmitContext);
            if (nestedTypeDef != null)
            {
                result = this.GetConsolidatedTypeParameters(nestedTypeDef.ContainingTypeDefinition, owner);
            }

            if (typeDef.GenericParameterCount > 0)
            {
                ushort index = 0;
                if (result == null)
                {
                    result = new List<IGenericTypeParameter>();
                }
                else
                {
                    index = (ushort)result.Count;
                }

                if (typeDef == owner && index == 0)
                {
                    result.AddRange(typeDef.GenericParameters);
                }
                else
                {
                    foreach (IGenericTypeParameter genericParameter in typeDef.GenericParameters)
                    {
                     //   result.Add(new InheritedTypeParameter(index++, owner, genericParameter));
                    }
                }
            }

            return result;
        }
        protected void CreateIndicesForNonTypeMembers(ITypeDefinition typeDef)
        {
            _typeDefs.Add(typeDef);

            IEnumerable<IGenericTypeParameter> typeParameters = this.GetConsolidatedTypeParameters(typeDef);
            if (typeParameters != null)
            {
                foreach (IGenericTypeParameter genericParameter in typeParameters)
                {
                    _genericParameters.Add(genericParameter);
                }
            }

            foreach (MethodImplementation methodImplementation in typeDef.GetExplicitImplementationOverrides(EmitContext))
            {
             //   this.methodImplList.Add(methodImplementation);
            }

            foreach (IEventDefinition eventDef in typeDef.GetEvents(EmitContext))
            {
                _eventDefs.Add(eventDef);
            }

            _fieldDefIndex.Add(typeDef, _fieldDefs.NextRowId);
            foreach (IFieldDefinition fieldDef in typeDef.GetFields(EmitContext))
            {
                _fieldDefs.Add(fieldDef);
            }

            _methodDefIndex.Add(typeDef, _methodDefs.NextRowId);
            foreach (IMethodDefinition methodDef in typeDef.GetMethods(EmitContext))
            {
                this.CreateIndicesFor(methodDef);
                _methodDefs.Add(methodDef);
            }

            foreach (IPropertyDefinition propertyDef in typeDef.GetProperties(EmitContext))
            {
                _propertyDefs.Add(propertyDef);
            }
        }
        protected ImmutableArray<IParameterDefinition> GetParametersToEmit(IMethodDefinition methodDef)
        {
            if (methodDef.ParameterCount == 0 && !(methodDef.ReturnValueIsMarshalledExplicitly || IteratorHelper.EnumerableIsNotEmpty(methodDef.GetReturnValueAttributes(EmitContext))))
            {
                return ImmutableArray<IParameterDefinition>.Empty;
            }

            return GetParametersToEmitCore(methodDef);
        }

        private ImmutableArray<IParameterDefinition> GetParametersToEmitCore(IMethodDefinition methodDef)
        {
            ArrayBuilder<IParameterDefinition> builder = null;
            var parameters = methodDef.Parameters;

            if (methodDef.ReturnValueIsMarshalledExplicitly || IteratorHelper.EnumerableIsNotEmpty(methodDef.GetReturnValueAttributes(EmitContext)))
            {
                builder = ArrayBuilder<IParameterDefinition>.GetInstance(parameters.Length + 1);
          //      builder.Add(new ReturnValueParameter(methodDef));
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                IParameterDefinition parDef = parameters[i];

                // No explicit param row is needed if param has no flags (other than optionally IN),
                // no name and no references to the param row, such as CustomAttribute, Constant, or FieldMarshal
                if (parDef.Name != String.Empty ||
                    parDef.HasDefaultValue || parDef.IsOptional || parDef.IsOut || parDef.IsMarshalledExplicitly ||
                    IteratorHelper.EnumerableIsNotEmpty(parDef.GetAttributes(EmitContext)))
                {
                    if (builder != null)
                    {
                        builder.Add(parDef);
                    }
                }
                else
                {
                    // we have a parameter that does not need to be emitted (not common)
                    if (builder == null)
                    {
                        builder = ArrayBuilder<IParameterDefinition>.GetInstance(parameters.Length);
                        builder.AddRange(parameters, i);
                    }
                }
            }

            return builder?.ToImmutableAndFree() ?? parameters;
        }
        private void CreateIndicesFor(IMethodDefinition methodDef)
        {
            _parameterListIndex.Add(methodDef, _parameterDefs.NextRowId);

            foreach (var paramDef in this.GetParametersToEmit(methodDef))
            {
                _parameterDefs.Add(paramDef);
            }

            if (methodDef.GenericParameterCount > 0)
            {
                foreach (IGenericMethodParameter genericParameter in methodDef.GenericParameters)
                {
                    _genericParameters.Add(genericParameter);
                }
            }
        }
        private void CreateIndicesFor(ITypeDefinition typeDef, Queue<INestedTypeDefinition> nestedTypes)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            this.CreateIndicesForNonTypeMembers(typeDef);

            // Metadata spec:
            // The TypeDef table has a special ordering constraint:
            // the definition of an enclosing class shall precede the definition of all classes it encloses.
            foreach (var nestedType in typeDef.GetNestedTypes(EmitContext))
            {
                nestedTypes.Enqueue(nestedType);
            }
        }

        private IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes(CommonPEModuleBuilder module)
        {
          return module.GetTopLevelTypes(this.EmitContext);
        }

        private System.Reflection.MemberInfo[] _pseudoSymbolTokenToTokenMap;
        private void CreateMethodBodyReferenceIndex()
        {
           
            int count;
            var referencesInIL = module.ReferencesInIL(out count);

            _pseudoSymbolTokenToReferenceMap = new IReference[count];
            _pseudoSymbolTokenToTokenMap = new System.Reflection.MemberInfo[count];
            int cur = 0;
            foreach (IReference o in referencesInIL)
            {
                _pseudoSymbolTokenToReferenceMap[cur] = o;
                cur++;
            }
        }

        private void CreateUserStringIndices()
        {
            strsmap = new Dictionary<int, string>();
            int i = 0;
            foreach(var str in module.GetStrings())
            {
                strsmap.Add(i, str);
           i ++;
            }
           
        }

        private Dictionary<int, string> strsmap;
        private IReference[] _pseudoSymbolTokenToReferenceMap;


        /// <summary>
        /// Invokes a specific method
        /// </summary>
        /// <param name="m">Method</param>
        /// <param name="instance">object instance</param>
        /// <param name="p">Parameters</param>
        /// <returns></returns>
        public object Invoke(IMethodSymbol m, object instance, params object[] p)
        {
            object res = null;
            if (m is SourceMethodSymbol s1)
            {
                ILIntepreter inteptreter = RequestILIntepreter();
                try
                {
                    res = inteptreter.Run(m, instance, p);
                }
                finally
                {
                    FreeILIntepreter(inteptreter);
                }
            }

            return res;
        }
        Queue<ILIntepreter> freeIntepreters = new Queue<ILIntepreter>();

        internal void FreeILIntepreter(ILIntepreter inteptreter)
        {
            lock (freeIntepreters)
            {
            
            }
        }
        ILIntepreter RequestILIntepreter()
        {
            ILIntepreter inteptreter = null;
            lock (freeIntepreters)
            {
                if (freeIntepreters.Count > 0)
                {
                    inteptreter = freeIntepreters.Dequeue();
                    //Clear debug state, because it may be in ShouldBreak State
                 //  .. inteptreter.ClearDebugState();
                }
                else
                {
                    inteptreter = new ILIntepreter(this);

                }
            }

            return inteptreter;
        }
        public object Invoke(string type, string method, object instance, params object[] p)
        {


            var t = GetType(type);
            if(t==null)
            {
                return null;
            }
            var m = t.GetMethodsToEmit();// (method, p != null ? p.Length : 0);
            var pl = p.Length;
            foreach (var m1 in m)
            {
                if (m1.Name != method)
                    continue;
                if (m1.ParameterCount != pl)
                    continue;

                if (bodys.ContainsKey(m1))
                {

                   

                   
                   
                    return Invoke(m1,instance,p);
                }
             

            }
            return null;


        }
    }
}
