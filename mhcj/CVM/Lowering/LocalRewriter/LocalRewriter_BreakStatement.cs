﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitBreakStatement(BoundBreakStatement node)
        {
            BoundStatement result = new BoundGotoStatement(node.Syntax, node.Label, node.HasErrors);
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                result = _instrumenter.InstrumentBreakStatement(node, result);
            }

            return result;
        }
    }
}
