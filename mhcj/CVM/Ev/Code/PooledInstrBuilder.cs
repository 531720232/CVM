using CVM.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class PooledInstrBuilder:IDisposable
    {

        private List<Instruction> Instructions;

        private static PooledInstrBuilder _inst;
      
        private PooledInstrBuilder()
        {
            Instructions = new List<Instruction>();
        }
        internal int Count;
        internal Instruction Append(Instruction instruction)
        {
          
            Instructions.Add(instruction);
            return instruction;
        }
        //internal void Append(CVM.ILOpCode code,object obj=null)
        //{
        //    Instructions.Add(new Instruction() { cc = obj, opcode = code });
        //}
        //private const int PoolSize = 128;
        public static PooledInstrBuilder GetInstance()
        {

           if(_inst==null)
            {
                _inst = new PooledInstrBuilder();
            }
            return _inst;
        }
        private  void Free()
        {
            _inst = null;
        //    Instructions = null;
        }

        public void Dispose()
        {
            Free();
        }

        internal ImmutableArray<Instruction> ToImmutableArray()
        {
        return    _inst.ToImmutableArray();
        }
        private Instruction c;
      internal Instruction Peek()
        {
            if(c==null)
            {
                c = new Instruction();
                Instructions.Add(c);
            }
            return c;
            
        }
        internal Instruction PeekPush(object obj)
        {
            if (c == null)
            {
                c = new Instruction();
                Instructions.Add(c);
            }
            c.cc.Push(obj);
            return c;

        }
        internal void Next()
        {
            c = null;
        }
    }
}
