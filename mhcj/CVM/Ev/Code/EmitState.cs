﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal partial class ILBuilder
    {
        /// <summary>
        /// Abstract Execution state. 
        /// If we know something interesting about IL stream we put it here.
        /// </summary>
        private struct EmitState
        {
            private int _maxStack;
            private int _curStack;
            private int _instructionsEmitted;


            internal int InstructionsEmitted
            {
                get
                {
                    return _instructionsEmitted;
                }
            }

            //private Instruction _curr;
            //internal Instruction Curr
            //{
            //    get
            //    {
            //        if(_curr==null)
            //        {
            //            _curr= PooledInstrBuilder.GetInstance().Append(new Instruction());
            //        }
            //        return _curr;
            //    }
            //}

            internal void InstructionAdded()
            {
                _instructionsEmitted += 1;
           
            }

            /// <summary>
            /// Eval stack's high watermark.
            /// </summary>
            internal int MaxStack
            {
                get
                {
                    return _maxStack;
                }
                private set
                {
                    Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                    _maxStack = value;
                }
            }

            /// <summary>
            /// Current evaluation stack depth.
            /// </summary>
            internal int CurStack
            {
                get
                {
                    return _curStack;
                }
                private set
                {
                    Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                    _curStack = value;
                }
            }

            //TODO: for debugging we could also record what we have in the stack (I, F, O, &, ...)

            /// <summary>
            /// Record effects of that currently emitted instruction on the eval stack.
            /// </summary>
            internal void AdjustStack(int count)
            {
                CurStack += count;
                MaxStack = Math.Max(MaxStack, CurStack);
            }
        }
    }
}
