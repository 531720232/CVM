using Microsoft.CodeAnalysis.CSharp;

namespace CVM
{
    public class Program
    {
       public static Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax AStart(string[] args)
        {

           var text = System.IO.File.ReadAllText("f:/test/a1.cs");

            CVM.GlobalDefine.Instance.InDebug();
           var tree = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(text);
      var we=      tree.GetDiagnostics();
      
            var ds = tree.GetRoot().GetDirectives();
            ////var so=        Microsoft.CodeAnalysis.Text.SourceText.From(text);
            ////        var lex = new Lexer(so,Microsoft.CodeAnalysis.CSharp.CSharpParseOptions.Default);

            //var directive = tree.GetRoot().GetDirectives();


    var c=        tree.GetCompilationUnitRoot();
       
            //if(c.HasErrors)
            //{
            //    throw new Exception("抛出错误");
            //}
            return c;
            //   var w=     lex.Lex(LexerMode.Syntax);
       
        }
     
    }
}
