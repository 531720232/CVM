namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ArrayRankSpecifierSyntax
    {
        public int Rank
        {
            get
            {
                return this.Sizes.Count;
            }
        }

        public ArrayRankSpecifierSyntax Update(SyntaxToken openBracketToken, SeparatedSyntaxList<ExpressionSyntax> sizes, SyntaxToken closeBracketToken)
        {
            return this.Update(openBracketToken, sizes, closeBracketToken, this.QuestionToken);
        }
    }
}
