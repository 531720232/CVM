using CVM.Collections.Immutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CodeGen.SequencePointList;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Holds on to the method body data.
    /// </summary>
    internal class MethodBody : Cci.IMethodBody
    {
        private readonly ushort _maxStack;

        private readonly ImmutableArray<Instruction> _ilBits;

        private readonly ImmutableArray<Cci.ILocalDefinition> _locals;
        private readonly ImmutableArray<Cci.ExceptionHandlerRegion> _exceptionHandlers;

        public ImmutableArray<ExceptionHandlerRegion> ExceptionRegions => _exceptionHandlers;

        private readonly ImmutableArray<Cci.SequencePoint> _sequencePoints;
        private readonly ImmutableArray<Cci.LocalScope> _localScopes;
        private readonly Cci.IImportScope _importScopeOpt;
        private readonly string _stateMachineTypeNameOpt;
        private readonly ImmutableArray<StateMachineHoistedLocalScope> _stateMachineHoistedLocalScopes;
        private readonly bool _hasDynamicLocalVariables;
        private readonly StateMachineMoveNextBodyDebugInfo _stateMachineMoveNextDebugInfoOpt;

        // Debug information emitted to Debug PDBs supporting EnC:
        private readonly DebugId _methodId;
        private readonly ImmutableArray<EncHoistedLocalInfo> _stateMachineHoistedLocalSlots;
        private readonly ImmutableArray<LambdaDebugInfo> _lambdaDebugInfo;
        private readonly ImmutableArray<ClosureDebugInfo> _closureDebugInfo;

        // Data used when emitting EnC delta:
        private readonly ImmutableArray<Cci.ITypeReference> _stateMachineAwaiterSlots;

        // Data used when emitting Dynamic Analysis resource:
        private readonly DynamicAnalysisMethodBodyData _dynamicAnalysisDataOpt;

        private readonly Cci.IMethodDefinition _parent;

        public bool LocalsAreZeroed => true;


        public ImmutableArray<ILocalDefinition> LocalVariables => _locals;

        public IMethodDefinition MethodDefinition => _parent;

        public StateMachineMoveNextBodyDebugInfo MoveNextBodyInfo => _stateMachineMoveNextDebugInfoOpt;

        public ushort MaxStack => _maxStack;

        public ImmutableArray<Instruction> IL => _ilBits;

        public ImmutableArray<SequencePoint> SequencePoints => _sequencePoints;

        public bool HasDynamicLocalVariables => _hasDynamicLocalVariables;

        public ImmutableArray<LocalScope> LocalScopes =>_localScopes;

        public IImportScope ImportScope => _importScopeOpt;

        public DebugId MethodId => _methodId;

        public ImmutableArray<StateMachineHoistedLocalScope> StateMachineHoistedLocalScopes => _stateMachineHoistedLocalScopes;

        public string StateMachineTypeName => _stateMachineTypeNameOpt;

        public ImmutableArray<EncHoistedLocalInfo> StateMachineHoistedLocalSlots => _stateMachineHoistedLocalSlots;

        public ImmutableArray<ITypeReference> StateMachineAwaiterSlots => _stateMachineAwaiterSlots;

        public ImmutableArray<ClosureDebugInfo> ClosureDebugInfo => _closureDebugInfo;

        public ImmutableArray<LambdaDebugInfo> LambdaDebugInfo => _lambdaDebugInfo;

        public DynamicAnalysisMethodBodyData DynamicAnalysisData => _dynamicAnalysisDataOpt;

        public MethodBody(
             ImmutableArray<Instruction> ilBits,
             ushort maxStack,
             Cci.IMethodDefinition parent,
             DebugId methodId,
             ImmutableArray<Cci.ILocalDefinition> locals,
             SequencePointList sequencePoints,
             DebugDocumentProvider debugDocumentProvider,
             ImmutableArray<Cci.ExceptionHandlerRegion> exceptionHandlers,
             ImmutableArray<Cci.LocalScope> localScopes,
             bool hasDynamicLocalVariables,
             Cci.IImportScope importScopeOpt,
             ImmutableArray<LambdaDebugInfo> lambdaDebugInfo,
             ImmutableArray<ClosureDebugInfo> closureDebugInfo,
             string stateMachineTypeNameOpt,
             ImmutableArray<StateMachineHoistedLocalScope> stateMachineHoistedLocalScopes,
             ImmutableArray<EncHoistedLocalInfo> stateMachineHoistedLocalSlots,
             ImmutableArray<Cci.ITypeReference> stateMachineAwaiterSlots,
             StateMachineMoveNextBodyDebugInfo stateMachineMoveNextDebugInfoOpt,
             DynamicAnalysisMethodBodyData dynamicAnalysisDataOpt)
        {
            Debug.Assert(!locals.IsDefault);
            Debug.Assert(!exceptionHandlers.IsDefault);
            Debug.Assert(!localScopes.IsDefault);

            _ilBits = ilBits;
            _maxStack = maxStack;
            _parent = parent;
            _methodId = methodId;
            _locals = locals;
            _exceptionHandlers = exceptionHandlers;
            _localScopes = localScopes;
            _hasDynamicLocalVariables = hasDynamicLocalVariables;
            _importScopeOpt = importScopeOpt;
            _lambdaDebugInfo = lambdaDebugInfo;
            _closureDebugInfo = closureDebugInfo;
            _stateMachineTypeNameOpt = stateMachineTypeNameOpt;
            _stateMachineHoistedLocalScopes = stateMachineHoistedLocalScopes;
            _stateMachineHoistedLocalSlots = stateMachineHoistedLocalSlots;
            _stateMachineAwaiterSlots = stateMachineAwaiterSlots;
            _stateMachineMoveNextDebugInfoOpt = stateMachineMoveNextDebugInfoOpt;
            _dynamicAnalysisDataOpt = dynamicAnalysisDataOpt;
            if (sequencePoints == null || sequencePoints.IsEmpty)
            {
                _sequencePoints= ImmutableArray<Cci.SequencePoint>.Empty;
            }
        }

    }
}
