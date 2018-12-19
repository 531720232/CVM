
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class RefTypeSyntax
    {
        public RefTypeSyntax Update(SyntaxToken refKeyword, TypeSyntax type)
        {
            return Update(refKeyword, default(SyntaxToken), type);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new RefTypeSyntax instance.</summary>
        public static RefTypeSyntax RefType(SyntaxToken refKeyword, TypeSyntax type)
        {
            return RefType(refKeyword, default(SyntaxToken), type);
        }
    }
}
