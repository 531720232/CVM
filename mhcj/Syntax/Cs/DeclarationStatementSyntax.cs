namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LocalDeclarationStatementSyntax : StatementSyntax
    {
        public bool IsConst
        {
            get
            {
                return this.Modifiers.Any(SyntaxKind.ConstKeyword);
            }
        }
    }
}
