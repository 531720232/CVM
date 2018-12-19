using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
 internal   class CVM_Object
    {
        internal object obj;
        internal Obj_Type type;

        internal static CVM_Object Null()
        {
            return new CVM_Object() { type = Obj_Type.Null };

        }
        internal CVM_Object(object obj=null)
        {
            this.obj = obj;
        //    type = Obj_Type.Object;
        }
    }
  
    internal enum Obj_Type
    {
        Null,
        Integer,
        Long,
        Float,
        Double,
        String,
        StackObjectReference,//Value = pointer, 
        StaticFieldReference,
        ValueTypeObjectReference,
        ValueTypeDescriptor,
        Object,
        FieldReference,//Value = objIdx, ValueLow = fieldIdx
        ArrayReference,//Value = objIdx, ValueLow = elemIdx
        CVM_Node
    }
}
