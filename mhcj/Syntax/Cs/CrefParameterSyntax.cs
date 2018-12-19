
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class CrefParameterSyntax
    {
        /// <summary>
        /// Pre C# 7.2 back-compat overload, which simply calls the replacement property <see cref="RefKindKeyword"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SyntaxToken RefOrOutKeyword => this.RefKindKeyword;

        /// <summary>
        /// Pre C# 7.2 back-compat overload, which simply calls the replacement method <see cref="Update"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CrefParameterSyntax WithRefOrOutKeyword(SyntaxToken refOrOutKeyword)
        {
            return this.Update(refOrOutKeyword, this.Type);
        }
    }
}
