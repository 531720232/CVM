using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class MetadataDecoder
    {
        public MetadataDecoder()
        {
        }

        internal TypeSymbol GetTypeOfToken(Type token)
        {
            return default;
        }
        public static ObsoleteAttributeData GetOb(System.Reflection.MemberInfo member)
        {
            ObsoleteAttributeData data = ObsoleteAttributeData.Uninitialized;
         var objs=   member.GetCustomAttributes(typeof(System.ObsoleteAttribute),false);
            {
                foreach(var obj in objs)
                {
                    if(obj is ObsoleteAttribute attr)
                    {
                        data = new ObsoleteAttributeData(ObsoleteAttributeKind.Obsolete, attr.Message, attr.IsError);
                        break;
                    }
                }
            }
            return data;
        }

        internal static bool GetCustomAttribute(Attribute handle, out TypeSymbol attributeClass, out MethodSymbol attributeConstructor)
        {
            Type attr;
            attributeClass = null;
            attributeConstructor = null;
            System.Reflection.ConstructorInfo ctor;
            try
            {
                attr = handle.GetType();
                 
         //   attr.GetConstructor()
            }
            catch 
            {
             
             
            }
            return false;
        }
        internal static ParamInfo<TypeSymbol>[] GetSignatureForMethod(MethodInfo methodDef,  bool setParamHandles = true)
        {
            ParamInfo<TypeSymbol>[] paramInfo = null;
          

            try
            {
            

                int typeParameterCount; //CONSIDER: expose to caller?
                paramInfo = DecodeSignatureParametersOrThrow(methodDef, out typeParameterCount);

                if (setParamHandles)
                {
                    int paramInfoLength = paramInfo.Length;

                    // For each parameter, get corresponding row id from Param table. 
                    foreach (var param in methodDef.GetParameters())
                    {
                        int sequenceNumber = param.Position;

                        // Ignore possible errors in parameter table.
                        if (sequenceNumber >= 0 && sequenceNumber < paramInfoLength && paramInfo[sequenceNumber].Handle==null)
                        {
                            paramInfo[sequenceNumber].Handle = param;
                        }
                    }
                }

              //  metadataException = null;
            }
            catch (BadImageFormatException mrEx)
            {
            //    metadataException = mrEx;

                // An exception from metadata reader.
                if (paramInfo == null)
                {
                    // Pretend there are no parameters and capture error information in the return type.
                    paramInfo = new ParamInfo<TypeSymbol>[1];
                 //   paramInfo[0].Type = GetUnsupportedMetadataTypeSymbol(mrEx);
                }
            }

            return paramInfo;
        }
        internal static ParamInfo<TypeSymbol>[] DecodeSignatureParametersOrThrow(System.Reflection.MethodInfo mi, out int typeParameterCount)
        {
            int paramCount;


            GetSignatureCountsOrThrow(mi, out paramCount, out typeParameterCount);

            ParamInfo<TypeSymbol>[] paramInfo = new ParamInfo<TypeSymbol>[paramCount + 1];

            uint paramIndex = 0;

            try
            {
                // get the return type
               // DecodeParameterOrThrow(mi, ref paramInfo[0]);

                paramInfo[0] = new ParamInfo<TypeSymbol>();
                paramInfo[0].Type = AotAssemblySymbol.Inst.Aot.TypeHandleToTypeMap[mi.ReturnParameter.ParameterType];
                paramInfo[0].Handle = mi.ReturnParameter;
                // Get all of the parameters.
                for (paramIndex = 1; paramIndex <= paramCount; paramIndex++)
                {
                    // Figure out the type.
                    DecodeParameterOrThrow(mi, paramIndex, ref paramInfo[paramIndex]);
                }

             
            }
            catch (Exception e) 
            {
                //for (; paramIndex <= paramCount; paramIndex++)
                //{
                //    paramInfo[paramIndex].Type = GetUnsupportedMetadataTypeSymbol(e as BadImageFormatException);
                //}
            }

            return paramInfo;
        }

        internal static ParamInfo<TypeSymbol>[] GetSignatureForProperty(PropertyInfo propertyDef)
        {
            ParamInfo<TypeSymbol>[] paramInfo = null;
            try
            {
            

                int typeParameterCount; //CONSIDER: expose to caller?
                paramInfo = DecodeSignatureParametersOrThrow(propertyDef.GetAccessors()[0], out typeParameterCount);
              
            }
            catch 
            {

                // An exception from metadata reader.
                if (paramInfo == null)
                {
                    // Pretend there are no parameters and capture error information in the return type as well.
                    paramInfo = new ParamInfo<TypeSymbol>[1];
                }
            }
            throw new NotImplementedException();
        }

        private static void DecodeParameterOrThrow(MethodBase mi,uint id, ref ParamInfo<TypeSymbol> paramInfo)
        {

        var ps=    mi.GetParameters();
            var p = ps[id];
                paramInfo = new ParamInfo<TypeSymbol>();
                paramInfo.IsByRef = false;
                paramInfo.Handle = p;
                paramInfo.Type = AotAssemblySymbol.Inst.Aot.TypeHandleToTypeMap[p.ParameterType];
            
        }

        private static void GetSignatureCountsOrThrow(MethodBase mi, out int paramCount, out int typeParameterCount)
        {
            typeParameterCount= mi.GetGenericArguments().Length;
            paramCount = mi.GetParameters().Length;
        }
    }

    }


