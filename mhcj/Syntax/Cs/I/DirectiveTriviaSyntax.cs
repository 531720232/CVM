namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class DirectiveTriviaSyntax
    {
        internal override DirectiveStack ApplyDirectives(DirectiveStack stack)
        {
            return stack.Add(new Directive(this));
        }
    }
}
