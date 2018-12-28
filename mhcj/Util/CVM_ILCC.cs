using CVM.Collections.Immutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CVM
{
  public  class CVM_ILCC
    {
        private const int PoolSize = 128;
        public const int LongSize = sizeof(long);
        public const int intSize = sizeof(int);
        public const int sbyteSize = sizeof(sbyte);
        public const int floatSize = sizeof(float);
        public const int doubleSize = sizeof(double);
        public const int byteSize = sizeof(byte);
        public const int u32Size = sizeof(uint);

        private static ObjectPool<CVM_ILCC> s_chunkPool = new ObjectPool<CVM_ILCC>(() => new CVM_ILCC(), PoolSize);
        public static CVM_ILCC GetInstance()
        {
            // TODO: use size
            return s_chunkPool.Allocate();
        }
        public int Count { get; private set; }
        public void CountUp(int i = 1)
        {
            Count += i;

        }
        public List<object> objs = new List<object>();
      
        public void WriteOpCode(ILOpCode op)
        {
            var size = op.Size();

            objs.Add(op);
            if (size == 1)
            {
                CountUp(byteSize);
               // writer.WriteByte((byte)code);
            }
            else
            {
             

                Debug.Assert(size == 2);
                CountUp(byteSize);
                CountUp(byteSize);

     
            }
        }

        public void WriteInt64(long op)
        {
            objs.Add(op);
            CountUp(LongSize);
        }
        public void WriteInt32(int op)
        {
            objs.Add(op);
            CountUp(intSize);
        }

        internal void WriteUInt32(uint? token)
        {
            objs.Add(token);
            CountUp(u32Size);
        }

        public void WriteSByte(sbyte op)
        {
            objs.Add(op);
            CountUp(sbyteSize);
        }
        public void WriteObject(object op)
        {
            objs.Add(op);
        }
        public void SetCount(int in1)
        {
            this.Count = in1;
        }
        internal void WriteContentTo(CVM_ILCC writer)
        {
            writer.objs.AddRange(this.objs);
            writer.SetCount(writer.Count+this.Count);
        }

        internal void WriteUInt32E(uint token, object value)
        {
            CountUp(u32Size);
            objs.Add(value);
        }

        internal ImmutableArray<Instruction> ToIL()
        {
            var ils = ImmutableArray<Instruction>.Empty;

            var inst = new List<Instruction>();
            foreach(var item in objs)
            {
                if (item is ILOpCode op)
                {
                    inst.Add(new Instruction() { Q=true});
                    var i1 = inst.Last();
                    i1.opcode = op;
                    continue;
                }
                Instruction il2;
              if ((il2 =inst.Last())!= null&& il2.Q==true)
                {
                    il2.Push(item);
                  //  inst.Enqueue(il2);

                }
            }
            ils = inst.ToImmutableArray();

            return ils;
        }
    }
}
