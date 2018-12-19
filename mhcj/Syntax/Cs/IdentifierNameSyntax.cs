namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class IdentifierNameSyntax
    {
        internal override string ErrorDisplayName()
        {
            return Identifier.ValueText;
        }
    }
}
