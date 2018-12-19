using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CVM_Test
{
    class Program
    {
        static void Main(string[] args)
        {
             var text = System.IO.File.ReadAllText("f:/test/c1.cs");

            CVM.GlobalDefine.Instance.InDebug();
            var tree = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(text);


            var zone = Microsoft.CodeAnalysis.CSharp.CVM_Zone.Create("fyindex", tree);
       zone=     zone.AddSyntaxTrees(tree);
            zone.builder();
            // zone.builder(tree);
          //  cvm_.Visit(tree.GetRoot());
        }
    }
}
