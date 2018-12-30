using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Runtime
{
    internal class ILIntepreter
    {
        internal CVM_Build build;
        FastStack<object> fast;
        FastStack<object> args;
        FastStack<object> bd=new FastStack<object>(4396);
       
        public ILIntepreter(CVM_Build cVM_Build)
        {
            this.build = cVM_Build;
        }

        internal object Run(IMethodSymbol m, object instance, object[] p)
        {
            if (m is SourceMethodSymbol source)
            {
              
                var il = build.bodys[source];
                var stc = il.MaxStack;
                fast = new FastStack<object>(stc);

                args = new FastStack<object>(p.Length+1);
                args.Push(null);
                foreach( var p1 in p)
                {
                    args.Push(p1);
                }
                Execute(source, il);
              

            }
            if(m is AotMethodSymbol aot)
            {
             return    aot.Handle.Invoke(instance, p);
            }
            return null;
           
        }
        internal object Execute(SourceMethodSymbol source,Cci.IMethodBody body)
        {

            FastStack<ILocalDefinition> loc=new FastStack<ILocalDefinition>(body.LocalVariables.Length);
            for(int i=0;i<body.LocalVariables.Length;i++)
            {
                var local = body.LocalVariables[i];

                loc.Push(local);
               
            }
            Dictionary<ILocalDefinition, object> vs = new Dictionary<ILocalDefinition, object>();
            var il = body.IL;
            var address = 0;
           
            bool ret = false;
            while(!ret)
            {

                try
                {
                    if(address>=il.Length)
                    {
                        ret = true;
                    }
                    var cur = il[address];
                switch(cur.opcode)
                    {

                        case CVM.ILOpCode.Nop:
                            address++;
                            break;
                        case CVM.ILOpCode.Ldstr:
                            bd.Push((string)cur.obj);

                            address++;
                            break;
                        case CVM.ILOpCode.Ldarg_1:
                            bd.Push(args[1]);
                            address++;

                            break;
                        case CVM.ILOpCode.Box:
                            var obj = bd.Pop();
                            var type = cur.obj;
                            if(type is AotNamedTypeSymbol t1)
                            {

                                bd.Push( Convert.ChangeType(obj, TypeCode.Object));

                              //  t1.Handle;
                            }

                            address++;

                            break;
                        case CVM.ILOpCode.Call:
                            var me = cur.obj;
                            object r = null;
                            if (me is AotMethodSymbol aotm)
                            {
                                var pg = aotm.ParameterCount;
                                List<object> temp = new List<object>();

                                for(int i=pg;i>0;i--)
                                {
                                    temp.Add(bd.Pop());
                                }

                                temp.Reverse();

                                if(aotm.IsStatic)
                                {
                                 r=   aotm.Handle.Invoke(null, temp.ToArray());
                                }
                            }
                            bd.Push(r);
                            address++;
                            break;
                        case CVM.ILOpCode.Stloc_0:
                          var l0=  loc[0];
                            vs[l0] = bd.Pop();
                            address++;
                            break;

                        case CVM.ILOpCode.Ldc_i4:

                            bd.Push(cur.obj);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_1:

                            bd.Push(1);
                            address++;
                            break;
                          
                        case CVM.ILOpCode.Newobj:

                           if(cur.obj is Microsoft.CodeAnalysis.CodeGen.ArrayMethod ar)
                            {
                              var g1=  ar.GetContainingType(default);
                                if(g1 is ArrayTypeSymbol ts)
                                {
                                 var r1=   ts.ElementType.TypeSymbol ;
                                    if(r1 is AotNamedTypeSymbol ns)
                                    {
                                     var han=   ns.Handle;
                                        var objs = ar.ParameterCount;
                                        var items = new List<int>();
                                        for (int i=0;i<objs;i++)
                                        {
                                            items.Add((int)bd.Pop());
                                        }
                                        items.Reverse();
                                     var inst=Array.CreateInstance(han, items.ToArray());
                                    }
                                }
                            }
                          
                            address++;
                            break;
                        default:
                            address++;
                            break;

                    }

                
                }
                catch
                {
                    address--;

                }
              
            }
            return null;
          

        }


    }
}