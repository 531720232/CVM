
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ForEachVariableStatementSyntax
    {
        public ForEachVariableStatementSyntax Update(SyntaxToken forEachKeyword, SyntaxToken openParenToken, ExpressionSyntax variable, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return Update( default, forEachKeyword, openParenToken, variable, inKeyword, expression, closeParenToken, statement);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ForEachVariableStatementSyntax ForEachVariableStatement(SyntaxToken forEachKeyword, SyntaxToken openParenToken, ExpressionSyntax variable, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return ForEachVariableStatement( default, forEachKeyword, openParenToken, variable, inKeyword, expression, closeParenToken, statement);
        }
    }
}
