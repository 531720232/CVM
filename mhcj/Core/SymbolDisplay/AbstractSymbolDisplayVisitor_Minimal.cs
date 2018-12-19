// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.SymbolDisplay
{
    internal abstract partial class AbstractSymbolDisplayVisitor : SymbolVisitor
    {
        protected abstract bool ShouldRestrictMinimallyQualifyLookupToNamespacesAndTypes();

        protected bool IsMinimizing
        {
            get { return false; }
        }

     

     
    }
}
