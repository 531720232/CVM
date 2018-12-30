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
              return  Execute(source, il);
              

            }
            if(m is AotMethodSymbol aot)//如果是原生方法直接运行
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

            FastStack<object> bd = new FastStack<object>(body.MaxStack);

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
                        case CVM.ILOpCode.Readonly:
                        case CVM.ILOpCode.Nop:
                        case CVM.ILOpCode.NullStack:
                            address++;
                            break;
                        case CVM.ILOpCode.Ldstr:
                            var ld= (uint)cur.obj;
                            bd.Push(build.GetString(ld));

                            address++;
                            break;
                      
                        case CVM.ILOpCode.Box:
                            var obj = bd.Pop();
                            var type =(uint) cur.obj;
                            var type_box = build.GetToken(type);
                            if(type_box is IReference t1)
                            {
                                if(t1 is AotNamedTypeSymbol aot_t1)
                                {
                                    bd.Push(Convert.ChangeType(obj, aot_t1.Handle));

                                }
                                
                              //  bd.Push( Convert.ChangeType(obj, t1.Handle));

                              //  t1.Handle;
                            }

                            address++;

                            break;
                        case CVM.ILOpCode.Callvirt:
                        case CVM.ILOpCode.Call:
                            var me =build.GetToken((uint) cur.obj);
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
                                else
                                {
                                  
                                    r = aotm.Handle.Invoke(bd.Pop(), temp.ToArray());
                                }
                            }
                            bd.Push(r);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldloc_0:
                            bd.Push(vs[loc[0]]);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldloc_1:
                            bd.Push(vs[loc[1]]);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldloc_2:
                            bd.Push(vs[loc[2]]);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldloc_3:
                            bd.Push(vs[loc[3]]);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldloc_s:
                            var sb_s = (sbyte)cur.obj;
                            var loc_s = loc[sb_s];
                            var vs_s = vs[loc_s];
                            bd.Push(vs_s);
                            address++;
                            break;
                        case CVM.ILOpCode.Stloc_0:
                          var l0=  loc[0];
                            vs[l0] = bd.Pop();
                            address++;
                            break;
                        case CVM.ILOpCode.Stloc_1:
                            var l1 = loc[1];
                            vs[l1] = bd.Pop();
                            address++;
                            break;
                        case CVM.ILOpCode.Stloc_2:
                            var l2 = loc[2];
                            vs[l2] = bd.Pop();
                            address++;
                            break;
                        case CVM.ILOpCode.Stloc_3:
                            var l3 = loc[3];
                            vs[l3] = bd.Pop();
                            address++;
                            break;
                        case CVM.ILOpCode.Stloc_s:
                            var ls = loc[(sbyte)cur.obj];
                            vs[ls] = bd.Pop();
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4:

                            bd.Push(cur.obj);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_0:

                            bd.Push(0);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_1:

                            bd.Push(1);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_2:

                            bd.Push(2);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_3:

                            bd.Push(3);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_4:

                            bd.Push(4);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_5:

                            bd.Push(5);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_6:

                            bd.Push(6);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_7:

                            bd.Push(7);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_8:

                            bd.Push(8);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_m1:

                            bd.Push(-1);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i4_s:
                            var by_ =(int)cur.obj;
                            bd.Push(by_);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_i8:
                         
                            bd.Push((long)cur.obj);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_r4:
                            var p1 = (int)cur.obj;
                            var p2 = BitConverter.ToSingle(BitConverter.GetBytes(p1), 0);
                            bd.Push(p2);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldc_r8:
                            var p1_R8 = (long)cur.obj;
                            var p2_R8 = BitConverter.Int64BitsToDouble(p1_R8);
                            bd.Push(p2_R8);
                            address++;
                            break;
                        case CVM.ILOpCode.Newobj:
                            var n_obj =build.GetToken((uint)cur.obj);
                           if (n_obj is Microsoft.CodeAnalysis.CodeGen.ArrayMethod ar)
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
                                        bd.Push(inst);
                                    }
                                }
                            }
                          
                            address++;
                            break;
                        case CVM.ILOpCode.Ret:
                            ret = true;
                            break;
                        case CVM.ILOpCode.Ldarg:
                            bd.Push(args[(short)cur.obj]);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldarg_0://don't use,zero is null
                           bd.Push(args[0]);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldarg_1:
                            bd.Push(args[1]);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldarg_2:
                            bd.Push(args[2]);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldarga:
                            var add_ = (short)cur.obj;
                            var add_1 = add_ + address;
                            bd.Push(add_1);
                            address++;
                            break;
                        case CVM.ILOpCode.Ldarga_s:
                            var add_s = (byte)cur.obj;
                            var add_s1 = add_s + address;
                            bd.Push(add_s1);
                            address++;
                            break;
                        case CVM.ILOpCode.Leave:
                        case CVM.ILOpCode.Leave_s:
                        case CVM.ILOpCode.Br:
                        case CVM.ILOpCode.Br_s:
                            if(cur.obj is sbyte)
                            {
                              if((sbyte)cur.obj==0)
                                {
                                    address++;
                                    break;
                                }
                                address += (sbyte)cur.obj+1;
                            }
                            else if(cur.obj is int)
                            {
                                address += (int)cur.obj;
                            }
                          
                            break;
                        case CVM.ILOpCode.Beq_s:

                            var beq_s_a = bd.Pop();
                            var beq_s_b = bd.Pop();
                            if(beq_s_a.Equals(beq_s_b))
                            {
                                address += (short)cur.obj;
                            }
                            address++;
                            break;
                        case CVM.ILOpCode.Ckfinite:
                            if(!(cur.obj is int))
                            {
                                throw new ArithmeticException(cur.ToString());
                            }
                            break;
                        case CVM.ILOpCode.Add:
                            var a = bd.Pop();
                            var b = bd.Pop();
                           
                            if(a is int)
                            {
                                var c = (int)a + (int)b;
                                bd.Push(c);
                            }else
                            if (a is long)
                            {
                                var c = (long)a + (long)b;
                                bd.Push(c);
                            }else
                            if (a is float)
                            {
                                var c = (float)a + (float)b;
                                bd.Push(c);
                            }else
                            if (a is double)
                            {
                                var c = (double)a + (double)b;
                                bd.Push(c);
                            }else
                            {
                                throw new NotImplementedException();
                            }
                            address++;
                            break;
                        case CVM.ILOpCode.Pop:

                            bd.Pop();  
                            address++;
                            
                            break;
                        case CVM.ILOpCode.Ldobj:

                            var obj_type = cur.obj;//
                           
                            address++;

                            break;
                        default:
                            throw new NotSupportedException("Intended support-> " + cur.opcode);

                    }

                
                }
                catch(Exception ex)
                {
                  

                ret=true;//
                }
              
            }
            var ret_item = bd.Pop();
            bd.Clear();
            return ret_item;
          

        }


    }
}