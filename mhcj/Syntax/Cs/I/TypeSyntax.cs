namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal abstract partial class TypeSyntax
    {
        public bool IsVar => this is IdentifierNameSyntax name && name.Identifier.ToString() == "var";

        public bool IsUnmanaged => this is IdentifierNameSyntax name && name.Identifier.ToString() == "unmanaged";
    }
}
