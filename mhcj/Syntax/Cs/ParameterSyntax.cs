namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ParameterSyntax
    {
        internal bool IsArgList
        {
            get
            {
                return this.Type == null && this.Identifier.ContextualKind() == SyntaxKind.ArgListKeyword;
            }
        }
    }
}
