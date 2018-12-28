using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CodeGen
{
  internal  class Instruction
    {
        internal CVM.ILOpCode opcode;
        internal Stack<object> cc;

        internal object obj;
        internal bool Q=false;
        internal void Push(object obj)
        {
            this.obj=(obj);
        }
        public override string ToString()
        {
            return "指令->"+opcode+",Token->"+obj;
        }
    }
}
