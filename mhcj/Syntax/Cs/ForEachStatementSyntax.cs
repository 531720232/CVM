
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ForEachStatementSyntax
    {
        public ForEachStatementSyntax Update(SyntaxToken forEachKeyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return Update( default, forEachKeyword, openParenToken, type, identifier, inKeyword, expression, closeParenToken, statement);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ForEachStatementSyntax ForEachStatement(SyntaxToken forEachKeyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return ForEachStatement( default, forEachKeyword, openParenToken, type, identifier, inKeyword, expression, closeParenToken, statement);
        }
    }
}
