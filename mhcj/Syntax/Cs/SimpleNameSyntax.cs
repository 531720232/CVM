﻿namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class SimpleNameSyntax
    {
        // This override is only intended to support cases where a caller has a value statically typed as NameSyntax in hand 
        // and neither knows nor cares to determine whether that name is qualified or not.
        // If a value is statically typed as a SimpleNameSyntax calling this method is a waste.
        internal sealed override SimpleNameSyntax GetUnqualifiedName()
        {
            return this;
        }
    }
}
