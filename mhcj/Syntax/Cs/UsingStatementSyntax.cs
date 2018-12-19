
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class UsingStatementSyntax
    {
        public UsingStatementSyntax Update(SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return Update( default(SyntaxToken), usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);
        }

    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static UsingStatementSyntax UsingStatement(SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return UsingStatement(default(SyntaxToken), usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);
        }
    }
}
