﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    internal class SyntaxNodeLocationComparer : IComparer<SyntaxNode>
    {

        public SyntaxNodeLocationComparer()
        {
        }

        public int Compare(SyntaxNode x, SyntaxNode y)
        {
            return 1;
           // return _compilation.CompareSourceLocations(x.GetLocation(), y.GetLocation());
        }
    }
}
