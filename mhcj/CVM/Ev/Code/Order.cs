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

        internal void Push(object obj)
        {
            cc.Push(obj);
        }
    }
}
