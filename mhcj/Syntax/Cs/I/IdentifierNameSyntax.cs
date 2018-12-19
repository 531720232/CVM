namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class IdentifierNameSyntax
    {
        public override string ToString()
        {
            return this.Identifier.Text;
        }
    }
}
