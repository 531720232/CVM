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
            var ad = typeof(Program).Assembly;
            var test_cs = ad.GetManifestResourceNames()[0];
            //
        
            
            var text =new System.IO.StreamReader(ad.GetManifestResourceStream(test_cs)).ReadToEnd();

            CVM.GlobalDefine.Instance.InDebug();
            CVM.GlobalDefine.Instance.log += Instance_log;
            var tree = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(text);


            var zone = Microsoft.CodeAnalysis.CSharp.CVM_Zone.Create("fyindex", tree);
       zone=     zone.AddSyntaxTrees(tree);
         
            zone.builder();
         
            // zone.builder(tree);
            //  cvm_.Visit(tree.GetRoot());
        }

        private static void Instance_log(string obj)
        {
            Console.WriteLine(obj);
        }
    }
}
