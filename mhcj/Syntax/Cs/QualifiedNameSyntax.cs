﻿namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class QualifiedNameSyntax : NameSyntax
    {
        // This override is only intended to support cases where a caller has a value statically typed as NameSyntax in hand 
        // and neither knows nor cares to determine whether that name is qualified or not.
        // If a value is statically typed as a QualifiedNameSyntax calling Right directly is preferred.
        internal override SimpleNameSyntax GetUnqualifiedName()
        {
            return Right;
        }

        internal override string ErrorDisplayName()
        {
            return Left.ErrorDisplayName() + "." + Right.ErrorDisplayName();
        }
    }
}
