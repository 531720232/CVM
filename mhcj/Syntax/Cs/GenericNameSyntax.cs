using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class GenericNameSyntax
    {
        public bool IsUnboundGenericName
        {
            get
            {
                return this.TypeArgumentList.Arguments.Any(SyntaxKind.OmittedTypeArgument);
            }
        }

        internal override string ErrorDisplayName()
        {
            var pb = PooledStringBuilder.GetInstance();
            pb.Builder.Append(Identifier.ValueText).Append("<").Append(',', Arity - 1).Append(">");
            return pb.ToStringAndFree();
        }
    }
}
