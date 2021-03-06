﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An abstract flow pass that takes some shortcuts in analyzing finally blocks, in order to enable
    /// the analysis to take place without tracking exceptions or repeating the analysis of a finally block
    /// for each exit from a try statement.  The shortcut results in a slightly less precise
    /// (but still conservative) analysis, but that less precise analysis is all that is required for
    /// the language specification.  The most significant shortcut is that we do not track the state
    /// where exceptions can arise.  That does not affect the soundness for most analyses, but for those
    /// analyses whose soundness would be affected (e.g. "data flows out"), we track "unassignments" to keep
    /// the analysis sound.
    /// </summary>
    internal abstract partial class AbstractFlowPass<TLocalState> : PreciseAbstractFlowPass<TLocalState>
        where TLocalState : PreciseAbstractFlowPass<TLocalState>.AbstractLocalState
    {
        private readonly bool _trackUnassignments; // for the data flows out walker, we track unassignments as well as assignments

        protected AbstractFlowPass(
            CVM_Zone compilation,
            Symbol member,
            BoundNode node,
            bool trackUnassignments = false)
            : base(compilation, member, node)
        {
            this._trackUnassignments = trackUnassignments;
        }

        protected AbstractFlowPass(
            CVM_Zone compilation,
            Symbol member,
            BoundNode node,
            BoundNode firstInRegion,
            BoundNode lastInRegion,
            bool trackRegions = true,
            bool trackUnassignments = false)
            : base(compilation, member, node, firstInRegion, lastInRegion, trackRegions)
        {
            this._trackUnassignments = trackUnassignments;
        }

        protected abstract void UnionWith(ref TLocalState self, ref TLocalState other);

        /// <summary>
        /// Nontrivial implementation is required for DataFlowsOutWalker or any flow analysis pass that "tracks
        /// unassignments" like the nullable walker. The result should be a state, for each variable, that is
        /// the strongest result possible (i.e. definitely assigned for the data flow passes, or not null for
        /// the nullable analysis).  Slightly more formally, this should be a reachable state that won't affect
        /// another reachable state when this is intersected with the other state.
        /// </summary>
        protected virtual TLocalState AllBitsSet()
        {
            return default(TLocalState);
        }

        #region TryStatements

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            var oldPending = SavePending(); // we do not allow branches into a try statement
            var initialState = this.State.Clone();

            // use this state to resolve all the branches introduced and internal to try/catch
            var pendingBeforeTry = SavePending(); 

            VisitTryBlockWithUnassignments(node.TryBlock, node, ref initialState);
            var finallyState = initialState.Clone();
            var endState = this.State;
            foreach (var catchBlock in node.CatchBlocks)
            {
                SetState(initialState.Clone());
                VisitCatchBlockWithUnassignments(catchBlock, ref finallyState);
                IntersectWith(ref endState, ref this.State);
            }

            // Give a chance to branches internal to try/catch to resolve.
            // Carry forward unresolved branches.
            RestorePending(pendingBeforeTry);

            // NOTE: At this point all branches that are internal to try or catch blocks have been resolved.
            //       However we have not yet restored the oldPending branches. Therefore all the branches 
            //       that are currently pending must have been introduced in try/catch and do not terminate inside those blocks.
            //
            //       With exception of YieldReturn, these branches logically go through finally, if such present,
            //       so we must Union/Intersect finally state as appropriate

            if (node.FinallyBlockOpt != null)
            {
                // branches from the finally block, while illegal, should still not be considered
                // to execute the finally block before occurring.  Also, we do not handle branches
                // *into* the finally block.
                SetState(finallyState);

                // capture tryAndCatchPending before going into finally
                // we will need pending branches as they were before finally later
                var tryAndCatchPending = SavePending();
                var unsetInFinally = AllBitsSet();
                VisitFinallyBlockWithUnassignments(node.FinallyBlockOpt, ref unsetInFinally);
                foreach (var pend in tryAndCatchPending.PendingBranches)
                {
                    if (pend.Branch == null) continue; // a tracked exception
                    if (pend.Branch.Kind != BoundKind.YieldReturnStatement)
                    {
                        UnionWith(ref pend.State, ref this.State);
                        if (_trackUnassignments) IntersectWith(ref pend.State, ref unsetInFinally);
                    }
                }

                RestorePending(tryAndCatchPending);
                UnionWith(ref endState, ref this.State);
                if (_trackUnassignments) IntersectWith(ref endState, ref unsetInFinally);
            }

            SetState(endState);
            RestorePending(oldPending);
            return null;
        }

        protected Optional<TLocalState> _tryState;

        private void VisitTryBlockWithUnassignments(BoundStatement tryBlock, BoundTryStatement node, ref TLocalState tryState)
        {
            if (_trackUnassignments)
            {
                Optional<TLocalState> oldTryState = _tryState;
                _tryState = AllBitsSet();
                VisitTryBlock(tryBlock, node, ref tryState);
                var tempTryStateValue = _tryState.Value;
                IntersectWith(ref tryState, ref tempTryStateValue);
                if (oldTryState.HasValue)
                {
                    var oldTryStateValue = oldTryState.Value;
                    IntersectWith(ref oldTryStateValue, ref tempTryStateValue);
                    oldTryState = oldTryStateValue;
                }

                _tryState = oldTryState;
            }
            else
            {
                VisitTryBlock(tryBlock, node, ref tryState);
            }
        }

        private void VisitCatchBlockWithUnassignments(BoundCatchBlock catchBlock, ref TLocalState finallyState)
        {
            if (_trackUnassignments)
            {
                Optional<TLocalState> oldTryState = _tryState;
                _tryState = AllBitsSet();
                VisitCatchBlock(catchBlock, ref finallyState);
                var tempTryStateValue = _tryState.Value;
                IntersectWith(ref finallyState, ref tempTryStateValue);
                if (oldTryState.HasValue)
                {
                    var oldTryStateValue = oldTryState.Value;
                    IntersectWith(ref oldTryStateValue, ref tempTryStateValue);
                    oldTryState = oldTryStateValue;
                }

                _tryState = oldTryState;
            }
            else
            {
                VisitCatchBlock(catchBlock, ref finallyState);
            }
        }

        private void VisitFinallyBlockWithUnassignments(BoundStatement finallyBlock, ref TLocalState unsetInFinally)
        {
            if (_trackUnassignments)
            {
                Optional<TLocalState> oldTryState = _tryState;
                _tryState = AllBitsSet();
                VisitFinallyBlock(finallyBlock, ref unsetInFinally);
                var tempTryStateValue = _tryState.Value;
                IntersectWith(ref unsetInFinally, ref tempTryStateValue);
                if (oldTryState.HasValue)
                {
                    var oldTryStateValue = oldTryState.Value;
                    IntersectWith(ref oldTryStateValue, ref tempTryStateValue);
                    oldTryState = oldTryStateValue;
                }

                _tryState = oldTryState;
            }
            else
            {
                VisitFinallyBlock(finallyBlock, ref unsetInFinally);
            }
        }

        protected virtual void VisitTryBlock(BoundStatement tryBlock, BoundTryStatement node, ref TLocalState tryState)
        {
            VisitStatement(tryBlock);
        }

        protected virtual void VisitCatchBlock(BoundCatchBlock catchBlock, ref TLocalState finallyState)
        {
            if (catchBlock.ExceptionSourceOpt != null)
            {
                VisitLvalue(catchBlock.ExceptionSourceOpt);
            }

            if (catchBlock.ExceptionFilterOpt != null)
            {
                VisitCondition(catchBlock.ExceptionFilterOpt);
                SetState(StateWhenTrue);
            }

            VisitStatement(catchBlock.Body);
        }

        protected virtual void VisitFinallyBlock(BoundStatement finallyBlock, ref TLocalState unsetInFinally)
        {
            VisitStatement(finallyBlock); // this should generate no pending branches
        }

        #endregion TryStatements
    }
}
