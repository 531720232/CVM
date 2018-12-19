namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public abstract partial class TypeSyntax
    {
        public bool IsVar => ((InternalSyntax.TypeSyntax)this.Green).IsVar;

        public bool IsUnmanaged => ((InternalSyntax.TypeSyntax)this.Green).IsUnmanaged;
    }
}
