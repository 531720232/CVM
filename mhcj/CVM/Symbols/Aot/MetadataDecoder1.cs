using System;

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

    }

    }


