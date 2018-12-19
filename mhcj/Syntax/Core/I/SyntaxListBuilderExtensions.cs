namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal static class SyntaxListBuilderExtensions
    {
        public static SyntaxList<GreenNode> ToList(this SyntaxListBuilder builder)
        {
            return ToList<GreenNode>(builder);
        }

        public static SyntaxList<TNode> ToList<TNode>(this SyntaxListBuilder builder) where TNode : GreenNode
        {
            if (builder == null)
            {
                return default(SyntaxList<GreenNode>);
            }

            return new SyntaxList<TNode>(builder.ToListNode());
        }
    }
}
