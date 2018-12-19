using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.AstNode
{
    internal class A_Attribute:Node
    {
        internal A_Attribute(BoundKind kind,SyntaxNode node,bool error=false):base(kind,node)
        {

        }
    }
}
