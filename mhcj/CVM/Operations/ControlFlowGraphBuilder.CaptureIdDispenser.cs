﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal sealed partial class ControlFlowGraphBuilder
    {
        internal class CaptureIdDispenser
        {
            private int _captureId = -1;

            public int GetNextId()
            {
                return CVM.AHelper.Increment(ref _captureId);
            }

            public int GetCurrentId()
            {
                return _captureId;
            }
        }
    }
}
