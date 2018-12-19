
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class ArgumentSyntax
    {
        /// <summary>
        /// Pre C# 7.2 back-compat overload, which simply calls the replacement property <see cref="RefKindKeyword"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SyntaxToken RefOrOutKeyword
        {
            get => this.RefKindKeyword;
        }

        /// <summary>
        /// Pre C# 7.2 back-compat overload, which simply calls the replacement method <see cref="Update"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ArgumentSyntax WithRefOrOutKeyword(SyntaxToken refOrOutKeyword)
        {
            return this.Update(this.NameColon, refOrOutKeyword, this.Expression);
        }
    }
}
