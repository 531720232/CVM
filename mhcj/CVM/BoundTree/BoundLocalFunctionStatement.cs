﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundLocalFunctionStatement
    {
        public BoundBlock Body { get => BlockBody ?? ExpressionBody; }
    }
}
