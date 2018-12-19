using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BinderFactory
    {
        private sealed class BinderFactoryVisitor : CSharpSyntaxVisitor<object>
        {
            private int _position;
            private CSharpSyntaxNode _memberDeclarationOpt;
            private readonly BinderFactory _factory;
            internal BinderFactoryVisitor(BinderFactory factory)
            {
                _factory = factory;
            }

            internal void Initialize(int position, CSharpSyntaxNode memberDeclarationOpt)
            {

                _position = position;
                _memberDeclarationOpt = memberDeclarationOpt;
            }
            private SyntaxTree syntaxTree
            {
                get
                {
                    return _factory._syntaxTree;
                }
            }
            private CVM_Zone compilation
            {
                get
                {
                    return _factory._compilation;
                }
            }
            internal object VisitCompilationUnit(CompilationUnitSyntax compilationUnit, bool inUsing, bool inScript)
            {
                if (compilationUnit != syntaxTree.GetRoot())
                {
                    throw new ArgumentOutOfRangeException(nameof(compilationUnit), "node not part of tree");
                }
                
                var extraInfo = inUsing
                   ? (inScript ? NodeUsage.CompilationUnitScriptUsings : NodeUsage.CompilationUnitUsings)
                   : (inScript ? NodeUsage.CompilationUnitScript : NodeUsage.Normal);
                return compilation.GlobalImports;

            }

            }
    }
}
