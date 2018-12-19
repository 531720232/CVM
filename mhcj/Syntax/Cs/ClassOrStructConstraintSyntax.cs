namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ClassOrStructConstraintSyntax
    {
        public ClassOrStructConstraintSyntax Update(SyntaxToken classOrStructKeyword)
        {
            return Update(classOrStructKeyword, QuestionToken);
        }
    }
}
