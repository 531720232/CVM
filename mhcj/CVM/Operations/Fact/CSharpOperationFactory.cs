// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CVM.Collections.Concurrent;
using System.Collections.Generic;
using CVM.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using CVM.Linq;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class CSharpOperationFactory
    {
        private readonly ConcurrentDictionary<BoundNode, IOperation> _cache =
            new ConcurrentDictionary<BoundNode, IOperation>(concurrencyLevel: 2, capacity: 10);

        private readonly SemanticModel _semanticModel;

        public CSharpOperationFactory(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public IOperation Create(BoundNode boundNode)
        {
            if (boundNode == null)
            {
                return null;
            }

            // implicit receiver can be shared between multiple bound nodes.
            // always return cloned one
            if (boundNode.Kind == BoundKind.ImplicitReceiver)
            {
                return OperationCloner.CloneOperation(CreateInternal(boundNode));
            }

            return _cache.GetOrAdd(boundNode, n => CreateInternal(n));
        }

        private IOperation CreateInternal(BoundNode boundNode)
        {
            switch (boundNode.Kind)
            {
                case BoundKind.DeconstructValuePlaceholder:
                    return CreateBoundDeconstructValuePlaceholderOperation((BoundDeconstructValuePlaceholder)boundNode);
                case BoundKind.DeconstructionAssignmentOperator:
                    return CreateBoundDeconstructionAssignmentOperator((BoundDeconstructionAssignmentOperator)boundNode);
                case BoundKind.Call:
                    return CreateBoundCallOperation((BoundCall)boundNode);
                case BoundKind.Local:
                    return CreateBoundLocalOperation((BoundLocal)boundNode);
                case BoundKind.FieldAccess:
                    return CreateBoundFieldAccessOperation((BoundFieldAccess)boundNode);
                case BoundKind.PropertyAccess:
                    return CreateBoundPropertyAccessOperation((BoundPropertyAccess)boundNode);
                case BoundKind.IndexerAccess:
                    return CreateBoundIndexerAccessOperation((BoundIndexerAccess)boundNode);
                case BoundKind.EventAccess:
                    return CreateBoundEventAccessOperation((BoundEventAccess)boundNode);
                case BoundKind.EventAssignmentOperator:
                    return CreateBoundEventAssignmentOperatorOperation((BoundEventAssignmentOperator)boundNode);
                case BoundKind.Parameter:
                    return CreateBoundParameterOperation((BoundParameter)boundNode);
                case BoundKind.Literal:
                    return CreateBoundLiteralOperation((BoundLiteral)boundNode);
                case BoundKind.DynamicInvocation:
                    return CreateBoundDynamicInvocationExpressionOperation((BoundDynamicInvocation)boundNode);
                case BoundKind.DynamicIndexerAccess:
                    return CreateBoundDynamicIndexerAccessExpressionOperation((BoundDynamicIndexerAccess)boundNode);
                case BoundKind.ObjectCreationExpression:
                    return CreateBoundObjectCreationExpressionOperation((BoundObjectCreationExpression)boundNode);
                case BoundKind.DynamicObjectCreationExpression:
                    return CreateBoundDynamicObjectCreationExpressionOperation((BoundDynamicObjectCreationExpression)boundNode);
                case BoundKind.ObjectInitializerExpression:
                    return CreateBoundObjectInitializerExpressionOperation((BoundObjectInitializerExpression)boundNode);
                case BoundKind.CollectionInitializerExpression:
                    return CreateBoundCollectionInitializerExpressionOperation((BoundCollectionInitializerExpression)boundNode);
                case BoundKind.ObjectInitializerMember:
                    return CreateBoundObjectInitializerMemberOperation((BoundObjectInitializerMember)boundNode);
                case BoundKind.CollectionElementInitializer:
                    return CreateBoundCollectionElementInitializerOperation((BoundCollectionElementInitializer)boundNode);
                case BoundKind.DynamicObjectInitializerMember:
                    return CreateBoundDynamicObjectInitializerMemberOperation((BoundDynamicObjectInitializerMember)boundNode);
                case BoundKind.DynamicMemberAccess:
                    return CreateBoundDynamicMemberAccessOperation((BoundDynamicMemberAccess)boundNode);
                case BoundKind.DynamicCollectionElementInitializer:
                    return CreateBoundDynamicCollectionElementInitializerOperation((BoundDynamicCollectionElementInitializer)boundNode);
                case BoundKind.UnboundLambda:
                    return CreateUnboundLambdaOperation((UnboundLambda)boundNode);
                case BoundKind.Lambda:
                    return CreateBoundLambdaOperation((BoundLambda)boundNode);
                case BoundKind.Conversion:
                    return CreateBoundConversionOperation((BoundConversion)boundNode);
                case BoundKind.AsOperator:
                    return CreateBoundAsOperatorOperation((BoundAsOperator)boundNode);
                case BoundKind.IsOperator:
                    return CreateBoundIsOperatorOperation((BoundIsOperator)boundNode);
                case BoundKind.SizeOfOperator:
                    return CreateBoundSizeOfOperatorOperation((BoundSizeOfOperator)boundNode);
                case BoundKind.TypeOfOperator:
                    return CreateBoundTypeOfOperatorOperation((BoundTypeOfOperator)boundNode);
                case BoundKind.ArrayCreation:
                    return CreateBoundArrayCreationOperation((BoundArrayCreation)boundNode);
                case BoundKind.ArrayInitialization:
                    return CreateBoundArrayInitializationOperation((BoundArrayInitialization)boundNode);
                case BoundKind.DefaultExpression:
                    return CreateBoundDefaultExpressionOperation((BoundDefaultExpression)boundNode);
                case BoundKind.BaseReference:
                    return CreateBoundBaseReferenceOperation((BoundBaseReference)boundNode);
                case BoundKind.ThisReference:
                    return CreateBoundThisReferenceOperation((BoundThisReference)boundNode);
                case BoundKind.AssignmentOperator:
                    return CreateBoundAssignmentOperatorOrMemberInitializerOperation((BoundAssignmentOperator)boundNode);
                case BoundKind.CompoundAssignmentOperator:
                    return CreateBoundCompoundAssignmentOperatorOperation((BoundCompoundAssignmentOperator)boundNode);
                case BoundKind.IncrementOperator:
                    return CreateBoundIncrementOperatorOperation((BoundIncrementOperator)boundNode);
                case BoundKind.BadExpression:
                    return CreateBoundBadExpressionOperation((BoundBadExpression)boundNode);
                case BoundKind.NewT:
                    return CreateBoundNewTOperation((BoundNewT)boundNode);
                case BoundKind.NoPiaObjectCreationExpression:
                    return CreateNoPiaObjectCreationExpressionOperation((BoundNoPiaObjectCreationExpression)boundNode);
                case BoundKind.UnaryOperator:
                    return CreateBoundUnaryOperatorOperation((BoundUnaryOperator)boundNode);
                case BoundKind.BinaryOperator:
                    return CreateBoundBinaryOperatorOperation((BoundBinaryOperator)boundNode);
                case BoundKind.UserDefinedConditionalLogicalOperator:
                    return CreateBoundUserDefinedConditionalLogicalOperator((BoundUserDefinedConditionalLogicalOperator)boundNode);
                case BoundKind.TupleBinaryOperator:
                    return CreateBoundTupleBinaryOperatorOperation((BoundTupleBinaryOperator)boundNode);
                case BoundKind.ConditionalOperator:
                    return CreateBoundConditionalOperatorOperation((BoundConditionalOperator)boundNode);
                case BoundKind.NullCoalescingOperator:
                    return CreateBoundNullCoalescingOperatorOperation((BoundNullCoalescingOperator)boundNode);
                case BoundKind.AwaitExpression:
                    return CreateBoundAwaitExpressionOperation((BoundAwaitExpression)boundNode);
                case BoundKind.ArrayAccess:
                    return CreateBoundArrayAccessOperation((BoundArrayAccess)boundNode);
                case BoundKind.NameOfOperator:
                    return CreateBoundNameOfOperatorOperation((BoundNameOfOperator)boundNode);
                case BoundKind.ThrowExpression:
                    return CreateBoundThrowExpressionOperation((BoundThrowExpression)boundNode);
                case BoundKind.AddressOfOperator:
                    return CreateBoundAddressOfOperatorOperation((BoundAddressOfOperator)boundNode);
                case BoundKind.ImplicitReceiver:
                    return CreateBoundImplicitReceiverOperation((BoundImplicitReceiver)boundNode);
                case BoundKind.ConditionalAccess:
                    return CreateBoundConditionalAccessOperation((BoundConditionalAccess)boundNode);
                case BoundKind.ConditionalReceiver:
                    return CreateBoundConditionalReceiverOperation((BoundConditionalReceiver)boundNode);
                case BoundKind.FieldEqualsValue:
                    return CreateBoundFieldEqualsValueOperation((BoundFieldEqualsValue)boundNode);
                case BoundKind.PropertyEqualsValue:
                    return CreateBoundPropertyEqualsValueOperation((BoundPropertyEqualsValue)boundNode);
                case BoundKind.ParameterEqualsValue:
                    return CreateBoundParameterEqualsValueOperation((BoundParameterEqualsValue)boundNode);
                case BoundKind.Block:
                    return CreateBoundBlockOperation((BoundBlock)boundNode);
                case BoundKind.ContinueStatement:
                    return CreateBoundContinueStatementOperation((BoundContinueStatement)boundNode);
                case BoundKind.BreakStatement:
                    return CreateBoundBreakStatementOperation((BoundBreakStatement)boundNode);
                case BoundKind.YieldBreakStatement:
                    return CreateBoundYieldBreakStatementOperation((BoundYieldBreakStatement)boundNode);
                case BoundKind.GotoStatement:
                    return CreateBoundGotoStatementOperation((BoundGotoStatement)boundNode);
                case BoundKind.NoOpStatement:
                    return CreateBoundNoOpStatementOperation((BoundNoOpStatement)boundNode);
                case BoundKind.IfStatement:
                    return CreateBoundIfStatementOperation((BoundIfStatement)boundNode);
                case BoundKind.WhileStatement:
                    return CreateBoundWhileStatementOperation((BoundWhileStatement)boundNode);
                case BoundKind.DoStatement:
                    return CreateBoundDoStatementOperation((BoundDoStatement)boundNode);
                case BoundKind.ForStatement:
                    return CreateBoundForStatementOperation((BoundForStatement)boundNode);
                case BoundKind.ForEachStatement:
                    return CreateBoundForEachStatementOperation((BoundForEachStatement)boundNode);
                case BoundKind.SwitchStatement:
                    return CreateBoundSwitchStatementOperation((BoundSwitchStatement)boundNode);
                case BoundKind.SwitchLabel:
                    return CreateBoundSwitchLabelOperation((BoundSwitchLabel)boundNode);
                case BoundKind.TryStatement:
                    return CreateBoundTryStatementOperation((BoundTryStatement)boundNode);
                case BoundKind.CatchBlock:
                    return CreateBoundCatchBlockOperation((BoundCatchBlock)boundNode);
                case BoundKind.FixedStatement:
                    return CreateBoundFixedStatementOperation((BoundFixedStatement)boundNode);
                case BoundKind.UsingStatement:
                    return CreateBoundUsingStatementOperation((BoundUsingStatement)boundNode);
                case BoundKind.ThrowStatement:
                    return CreateBoundThrowStatementOperation((BoundThrowStatement)boundNode);
                case BoundKind.ReturnStatement:
                    return CreateBoundReturnStatementOperation((BoundReturnStatement)boundNode);
                case BoundKind.YieldReturnStatement:
                    return CreateBoundYieldReturnStatementOperation((BoundYieldReturnStatement)boundNode);
                case BoundKind.LockStatement:
                    return CreateBoundLockStatementOperation((BoundLockStatement)boundNode);
                case BoundKind.BadStatement:
                    return CreateBoundBadStatementOperation((BoundBadStatement)boundNode);
                case BoundKind.LocalDeclaration:
                    return CreateBoundLocalDeclarationOperation((BoundLocalDeclaration)boundNode);
                case BoundKind.MultipleLocalDeclarations:
                    return CreateBoundMultipleLocalDeclarationsOperation((BoundMultipleLocalDeclarations)boundNode);
                case BoundKind.LabelStatement:
                    return CreateBoundLabelStatementOperation((BoundLabelStatement)boundNode);
                case BoundKind.LabeledStatement:
                    return CreateBoundLabeledStatementOperation((BoundLabeledStatement)boundNode);
                case BoundKind.ExpressionStatement:
                    return CreateBoundExpressionStatementOperation((BoundExpressionStatement)boundNode);
                case BoundKind.TupleLiteral:
                    return CreateBoundTupleLiteralOperation((BoundTupleLiteral)boundNode);
                case BoundKind.ConvertedTupleLiteral:
                    return CreateBoundConvertedTupleLiteralOperation((BoundConvertedTupleLiteral)boundNode);
                case BoundKind.InterpolatedString:
                    return CreateBoundInterpolatedStringExpressionOperation((BoundInterpolatedString)boundNode);
                case BoundKind.StringInsert:
                    return CreateBoundInterpolationOperation((BoundStringInsert)boundNode);
                case BoundKind.LocalFunctionStatement:
                    return CreateBoundLocalFunctionStatementOperation((BoundLocalFunctionStatement)boundNode);
                case BoundKind.AnonymousObjectCreationExpression:
                    return CreateBoundAnonymousObjectCreationExpressionOperation((BoundAnonymousObjectCreationExpression)boundNode);
                case BoundKind.AnonymousPropertyDeclaration:
                    throw ExceptionUtilities.Unreachable;
                case BoundKind.ConstantPattern:
                    return CreateBoundConstantPatternOperation((BoundConstantPattern)boundNode);
                case BoundKind.DeclarationPattern:
                    return CreateBoundDeclarationPatternOperation((BoundDeclarationPattern)boundNode);
                case BoundKind.WildcardPattern:
                    throw ExceptionUtilities.Unreachable;
                case BoundKind.PatternSwitchStatement:
                    return CreateBoundPatternSwitchStatementOperation((BoundPatternSwitchStatement)boundNode);
                case BoundKind.PatternSwitchLabel:
                    return CreateBoundPatternSwitchLabelOperation((BoundPatternSwitchLabel)boundNode);
                case BoundKind.IsPatternExpression:
                    return CreateBoundIsPatternExpressionOperation((BoundIsPatternExpression)boundNode);
                case BoundKind.QueryClause:
                    return CreateBoundQueryClauseOperation((BoundQueryClause)boundNode);
                case BoundKind.DelegateCreationExpression:
                    return CreateBoundDelegateCreationExpressionOperation((BoundDelegateCreationExpression)boundNode);
                case BoundKind.RangeVariable:
                    return CreateBoundRangeVariableOperation((BoundRangeVariable)boundNode);
                case BoundKind.ConstructorMethodBody:
                    return CreateConstructorBodyOperation((BoundConstructorMethodBody)boundNode);
                case BoundKind.NonConstructorMethodBody:
                    return CreateMethodBodyOperation((BoundNonConstructorMethodBody)boundNode);
                case BoundKind.DiscardExpression:
                    return CreateDiscardExpressionOperation((BoundDiscardExpression)boundNode);
                case BoundKind.NullCoalescingAssignmentOperator:
                    return CreateBoundNullCoalescingAssignmentOperatorOperation((BoundNullCoalescingAssignmentOperator)boundNode);
                case BoundKind.FromEndIndexExpression:
                    return CreateFromEndIndexExpressionOperation((BoundFromEndIndexExpression)boundNode);
                case BoundKind.RangeExpression:
                    return CreateRangeExpressionOperation((BoundRangeExpression)boundNode);

                case BoundKind.Attribute:
                case BoundKind.ArgList:
                case BoundKind.ArgListOperator:
                case BoundKind.ConvertedStackAllocExpression:
                case BoundKind.FixedLocalCollectionInitializer:
                case BoundKind.GlobalStatementInitializer:
                case BoundKind.HostObjectMemberReference:
                case BoundKind.MakeRefOperator:
                case BoundKind.MethodGroup:
                case BoundKind.NamespaceExpression:
                case BoundKind.PointerElementAccess:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PreviousSubmissionReference:
                case BoundKind.RefTypeOperator:
                case BoundKind.RefValueOperator:
                case BoundKind.Sequence:
                case BoundKind.StackAllocArrayCreation:
                case BoundKind.SuppressNullableWarningExpression:
                case BoundKind.TypeExpression:
                case BoundKind.TypeOrValueExpression:

                    Optional<object> constantValue = ConvertToOptional((boundNode as BoundExpression)?.ConstantValue);
                    bool isImplicit = boundNode.WasCompilerGenerated;

                    if (!isImplicit)
                    {
                        switch (boundNode.Kind)
                        {
                            case BoundKind.FixedLocalCollectionInitializer:
                                isImplicit = true;
                                break;
                        }
                    }

                    return Operation.CreateOperationNone(_semanticModel, boundNode.Syntax, constantValue, getChildren: () => GetIOperationChildren(boundNode), isImplicit: isImplicit);

                default:
                    throw ExceptionUtilities.UnexpectedValue(boundNode.Kind); 
            }
        }

        private IMethodBodyOperation CreateMethodBodyOperation(BoundNonConstructorMethodBody boundNode)
        {
            Lazy1<IBlockOperation> blockBody = new Lazy1<IBlockOperation>(() => (IBlockOperation)Create(boundNode.BlockBody));
            Lazy1<IBlockOperation> expressionBody = new Lazy1<IBlockOperation>(() => (IBlockOperation)Create(boundNode.ExpressionBody));
            return new LazyMethodBodyOperation(_semanticModel, boundNode.Syntax, blockBody, expressionBody);
        }

        private IConstructorBodyOperation CreateConstructorBodyOperation(BoundConstructorMethodBody boundNode)
        {
            Lazy1<IOperation> initializer = new Lazy1<IOperation>(() => (IOperation)Create(boundNode.Initializer));
            Lazy1<IBlockOperation> blockBody = new Lazy1<IBlockOperation>(() => (IBlockOperation)Create(boundNode.BlockBody));
            Lazy1<IBlockOperation> expressionBody = new Lazy1<IBlockOperation>(() => (IBlockOperation)Create(boundNode.ExpressionBody));
            return new LazyConstructorBodyOperation(boundNode.Locals.As<ILocalSymbol>(), _semanticModel, boundNode.Syntax, initializer, blockBody, expressionBody);
        }

        private ImmutableArray<IOperation> GetIOperationChildren(BoundNode boundNode)
        {
            var boundNodeWithChildren = (IBoundNodeWithIOperationChildren)boundNode;
            var children = boundNodeWithChildren.Children;
            if (children.IsDefaultOrEmpty)
            {
                return ImmutableArray<IOperation>.Empty;
            }

            var builder = ArrayBuilder<IOperation>.GetInstance(children.Length);
            foreach (BoundNode childNode in children)
            {
                IOperation operation = Create(childNode);
                if (operation == null)
                {
                    continue;
                }

                builder.Add(operation);
            }

            return builder.ToImmutableAndFree();
        }

        private IVariableDeclaratorOperation CreateVariableDeclarator(BoundLocalDeclaration boundNode)
        {
            return (IVariableDeclaratorOperation)_cache.GetOrAdd(boundNode, n => CreateVariableDeclaratorInternal((BoundLocalDeclaration)n, n.Syntax));
        }

        private IPlaceholderOperation CreateBoundDeconstructValuePlaceholderOperation(BoundDeconstructValuePlaceholder boundDeconstructValuePlaceholder)
        {
            SyntaxNode syntax = boundDeconstructValuePlaceholder.Syntax;
            ITypeSymbol type = boundDeconstructValuePlaceholder.Type;
            Optional<object> constantValue = ConvertToOptional(boundDeconstructValuePlaceholder.ConstantValue);
            bool isImplicit = boundDeconstructValuePlaceholder.WasCompilerGenerated;
            return new PlaceholderExpression(PlaceholderKind.Unspecified, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDeconstructionAssignmentOperation CreateBoundDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator boundDeconstructionAssignmentOperator)
        {
            Lazy1<IOperation> target = new Lazy1<IOperation>(() => Create(boundDeconstructionAssignmentOperator.Left));
            // Skip the synthetic deconstruction conversion wrapping the right operand.
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundDeconstructionAssignmentOperator.Right.Operand));
            SyntaxNode syntax = boundDeconstructionAssignmentOperator.Syntax;
            ITypeSymbol type = boundDeconstructionAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundDeconstructionAssignmentOperator.ConstantValue);
            bool isImplicit = boundDeconstructionAssignmentOperator.WasCompilerGenerated;
            return new LazyDeconstructionAssignmentExpression(target, value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundCallOperation(BoundCall boundCall)
        {
            MethodSymbol targetMethod = boundCall.Method;
            SyntaxNode syntax = boundCall.Syntax;
            ITypeSymbol type = boundCall.Type;
            Optional<object> constantValue = ConvertToOptional(boundCall.ConstantValue);
            bool isImplicit = boundCall.WasCompilerGenerated;

            if (!boundCall.OriginalMethodsOpt.IsDefault || IsMethodInvalid(boundCall.ResultKind, targetMethod))
            {
                return CreateInvalidExpressionForHasArgumentsExpression(boundCall.ReceiverOpt, boundCall.Arguments, null, syntax, type, constantValue, isImplicit);
            }

            Lazy1<IOperation> instance = CreateReceiverOperation(boundCall.ReceiverOpt, targetMethod);
            bool isVirtual = IsCallVirtual(targetMethod, boundCall.ReceiverOpt);
            Lazy1<ImmutableArray<IArgumentOperation>> arguments = new Lazy1<ImmutableArray<IArgumentOperation>>(() =>
            {
                return DeriveArguments(
                    boundCall,
                    boundCall.BinderOpt,
                    targetMethod,
                    targetMethod,
                    boundCall.Arguments,
                    boundCall.ArgumentNamesOpt,
                    boundCall.ArgsToParamsOpt,
                    boundCall.ArgumentRefKindsOpt,
                    boundCall.Method.Parameters,
                    boundCall.Expanded,
                    syntax,
                    boundCall.InvokedAsExtensionMethod);
            });
            return new LazyInvocationExpression(targetMethod, instance, isVirtual, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundLocalOperation(BoundLocal boundLocal)
        {
            ILocalSymbol local = boundLocal.LocalSymbol;
            bool isDeclaration = boundLocal.DeclarationKind != BoundLocalDeclarationKind.None;
            SyntaxNode syntax = boundLocal.Syntax;
            ITypeSymbol type = boundLocal.Type;
            Optional<object> constantValue = ConvertToOptional(boundLocal.ConstantValue);
            bool isImplicit = boundLocal.WasCompilerGenerated;
            if (isDeclaration && syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;
                Lazy1<IOperation> localReferenceExpression = new Lazy1<IOperation>(() =>
                    new LocalReferenceExpression(local, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit));
                return new LazyDeclarationExpression(localReferenceExpression, _semanticModel, declarationExpressionSyntax, type, constantValue: default, isImplicit: false);
            }
            return new LocalReferenceExpression(local, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundFieldAccessOperation(BoundFieldAccess boundFieldAccess)
        {
            IFieldSymbol field = boundFieldAccess.FieldSymbol;
            bool isDeclaration = boundFieldAccess.IsDeclaration;
            Lazy1<IOperation> instance = CreateReceiverOperation(boundFieldAccess.ReceiverOpt, field);
            SyntaxNode syntax = boundFieldAccess.Syntax;
            ITypeSymbol type = boundFieldAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundFieldAccess.ConstantValue);
            bool isImplicit = boundFieldAccess.WasCompilerGenerated;
            if (isDeclaration && syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;
                Lazy1<IOperation> fieldReferenceExpression = new Lazy1<IOperation>(() =>
                    new LazyFieldReferenceExpression(field, isDeclaration, instance, _semanticModel, syntax, type, constantValue, isImplicit));
                return new LazyDeclarationExpression(fieldReferenceExpression, _semanticModel, declarationExpressionSyntax, type, constantValue: default, isImplicit: false);
            }
            return new LazyFieldReferenceExpression(field, isDeclaration, instance, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IPropertyReferenceOperation CreateBoundPropertyAccessOperation(BoundPropertyAccess boundPropertyAccess)
        {
            IPropertySymbol property = boundPropertyAccess.PropertySymbol;
            Lazy1<IOperation> instance = CreateReceiverOperation(boundPropertyAccess.ReceiverOpt, property);
            Lazy1<ImmutableArray<IArgumentOperation>> arguments = new Lazy1<ImmutableArray<IArgumentOperation>>(() => ImmutableArray<IArgumentOperation>.Empty);
            SyntaxNode syntax = boundPropertyAccess.Syntax;
            ITypeSymbol type = boundPropertyAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundPropertyAccess.ConstantValue);
            bool isImplicit = boundPropertyAccess.WasCompilerGenerated;
            return new LazyPropertyReferenceExpression(property, instance, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundIndexerAccessOperation(BoundIndexerAccess boundIndexerAccess)
        {
            PropertySymbol property = boundIndexerAccess.Indexer;
            SyntaxNode syntax = boundIndexerAccess.Syntax;
            ITypeSymbol type = boundIndexerAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundIndexerAccess.ConstantValue);
            bool isImplicit = boundIndexerAccess.WasCompilerGenerated;

            MethodSymbol accessor = boundIndexerAccess.UseSetterForDefaultArgumentGeneration
                ? property.GetOwnOrInheritedSetMethod()
                : property.GetOwnOrInheritedGetMethod();

            if (!boundIndexerAccess.OriginalIndexersOpt.IsDefault || boundIndexerAccess.ResultKind == LookupResultKind.OverloadResolutionFailure || accessor == null || accessor.OriginalDefinition is ErrorMethodSymbol)
            {
                return CreateInvalidExpressionForHasArgumentsExpression(boundIndexerAccess.ReceiverOpt, boundIndexerAccess.Arguments, null, syntax, type, constantValue, isImplicit);
            }

            Lazy1<IOperation> instance = CreateReceiverOperation(boundIndexerAccess.ReceiverOpt, property);
            Lazy1<ImmutableArray<IArgumentOperation>> arguments = new Lazy1<ImmutableArray<IArgumentOperation>>(() =>
                DeriveArguments(
                    boundIndexerAccess,
                    boundIndexerAccess.BinderOpt,
                    property,
                    accessor,
                    boundIndexerAccess.Arguments,
                    boundIndexerAccess.ArgumentNamesOpt,
                    boundIndexerAccess.ArgsToParamsOpt,
                    boundIndexerAccess.ArgumentRefKindsOpt,
                    property.Parameters,
                    boundIndexerAccess.Expanded,
                    syntax));
            return new LazyPropertyReferenceExpression(property, instance, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IEventReferenceOperation CreateBoundEventAccessOperation(BoundEventAccess boundEventAccess)
        {
            IEventSymbol @event = boundEventAccess.EventSymbol;
            Lazy1<IOperation> instance = CreateReceiverOperation(boundEventAccess.ReceiverOpt, @event);
            SyntaxNode syntax = boundEventAccess.Syntax;
            ITypeSymbol type = boundEventAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundEventAccess.ConstantValue);
            bool isImplicit = boundEventAccess.WasCompilerGenerated;
            return new LazyEventReferenceExpression(@event, instance, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IEventAssignmentOperation CreateBoundEventAssignmentOperatorOperation(BoundEventAssignmentOperator boundEventAssignmentOperator)
        {
            Lazy1<IOperation> eventReference = new Lazy1<IOperation>(() => CreateBoundEventAccessOperation(boundEventAssignmentOperator));
            Lazy1<IOperation> handlerValue = new Lazy1<IOperation>(() => Create(boundEventAssignmentOperator.Argument));
            SyntaxNode syntax = boundEventAssignmentOperator.Syntax;
            bool adds = boundEventAssignmentOperator.IsAddition;
            ITypeSymbol type = boundEventAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundEventAssignmentOperator.ConstantValue);
            bool isImplicit = boundEventAssignmentOperator.WasCompilerGenerated;
            return new LazyEventAssignmentOperation(eventReference, handlerValue, adds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IParameterReferenceOperation CreateBoundParameterOperation(BoundParameter boundParameter)
        {
            IParameterSymbol parameter = boundParameter.ParameterSymbol;
            SyntaxNode syntax = boundParameter.Syntax;
            ITypeSymbol type = boundParameter.Type;
            Optional<object> constantValue = ConvertToOptional(boundParameter.ConstantValue);
            bool isImplicit = boundParameter.WasCompilerGenerated;
            return new ParameterReferenceExpression(parameter, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILiteralOperation CreateBoundLiteralOperation(BoundLiteral boundLiteral, bool @implicit = false)
        {
            SyntaxNode syntax = boundLiteral.Syntax;
            ITypeSymbol type = boundLiteral.Type;
            Optional<object> constantValue = ConvertToOptional(boundLiteral.ConstantValue);
            bool isImplicit = boundLiteral.WasCompilerGenerated || @implicit;
            return new LiteralExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IAnonymousObjectCreationOperation CreateBoundAnonymousObjectCreationExpressionOperation(BoundAnonymousObjectCreationExpression boundAnonymousObjectCreationExpression)
        {
            Lazy1<ImmutableArray<IOperation>> memberInitializers = new Lazy1<ImmutableArray<IOperation>>(() => GetAnonymousObjectCreationInitializers(boundAnonymousObjectCreationExpression));
            SyntaxNode syntax = boundAnonymousObjectCreationExpression.Syntax;
            ITypeSymbol type = boundAnonymousObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundAnonymousObjectCreationExpression.ConstantValue);
            bool isImplicit = boundAnonymousObjectCreationExpression.WasCompilerGenerated;
            return new LazyAnonymousObjectCreationExpression(memberInitializers, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IPropertyReferenceOperation CreateBoundAnonymousPropertyDeclarationOperation(BoundAnonymousPropertyDeclaration boundAnonymousPropertyDeclaration, InstanceReferenceExpression receiver)
        {
            PropertySymbol property = boundAnonymousPropertyDeclaration.Property;
            Lazy1<IOperation> instance = new Lazy1<IOperation>(() => receiver);
            Lazy1<ImmutableArray<IArgumentOperation>> arguments = new Lazy1<ImmutableArray<IArgumentOperation>>(() => ImmutableArray<IArgumentOperation>.Empty);
            SyntaxNode syntax = boundAnonymousPropertyDeclaration.Syntax;
            ITypeSymbol type = boundAnonymousPropertyDeclaration.Type;
            Optional<object> constantValue = ConvertToOptional(boundAnonymousPropertyDeclaration.ConstantValue);
            bool isImplicit = boundAnonymousPropertyDeclaration.WasCompilerGenerated;
            return new LazyPropertyReferenceExpression(property, instance, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundObjectCreationExpressionOperation(BoundObjectCreationExpression boundObjectCreationExpression)
        {
            MethodSymbol constructor = boundObjectCreationExpression.Constructor;
            SyntaxNode syntax = boundObjectCreationExpression.Syntax;
            ITypeSymbol type = boundObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectCreationExpression.ConstantValue);
            bool isImplicit = boundObjectCreationExpression.WasCompilerGenerated;

            if (boundObjectCreationExpression.ResultKind == LookupResultKind.OverloadResolutionFailure || constructor == null || constructor.OriginalDefinition is ErrorMethodSymbol)
            {
                return CreateInvalidExpressionForHasArgumentsExpression(null, boundObjectCreationExpression.Arguments, boundObjectCreationExpression.InitializerExpressionOpt, syntax, type, constantValue, isImplicit);
            }
            else if (boundObjectCreationExpression.Type.IsAnonymousType)
            {
                // Workaround for https://github.com/dotnet/roslyn/issues/28157
                Debug.Assert(isImplicit);
                var memberInitializers = new Lazy1<ImmutableArray<IOperation>>(() => GetAnonymousObjectCreationInitializers(
                                                                                        boundObjectCreationExpression.Arguments,
                                                                                        declarations: ImmutableArray<BoundAnonymousPropertyDeclaration>.Empty,
                                                                                        syntax,
                                                                                        type,
                                                                                        isImplicit));
                return new LazyAnonymousObjectCreationExpression(memberInitializers, _semanticModel, syntax, type, constantValue, isImplicit);
            }

            Lazy1<IObjectOrCollectionInitializerOperation> initializer = new Lazy1<IObjectOrCollectionInitializerOperation>(() => (IObjectOrCollectionInitializerOperation)Create(boundObjectCreationExpression.InitializerExpressionOpt));
            Lazy1<ImmutableArray<IArgumentOperation>> arguments = new Lazy1<ImmutableArray<IArgumentOperation>>(() =>
            {
                return DeriveArguments(
                    boundObjectCreationExpression,
                    boundObjectCreationExpression.BinderOpt,
                    constructor,
                    constructor,
                    boundObjectCreationExpression.Arguments,
                    boundObjectCreationExpression.ArgumentNamesOpt,
                    boundObjectCreationExpression.ArgsToParamsOpt,
                    boundObjectCreationExpression.ArgumentRefKindsOpt,
                    constructor.Parameters,
                    boundObjectCreationExpression.Expanded,
                    syntax);
            });
            return new LazyObjectCreationExpression(constructor, initializer, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicObjectCreationOperation CreateBoundDynamicObjectCreationExpressionOperation(BoundDynamicObjectCreationExpression boundDynamicObjectCreationExpression)
        {
            Lazy1<ImmutableArray<IOperation>> arguments = new Lazy1<ImmutableArray<IOperation>>(() => boundDynamicObjectCreationExpression.Arguments.SelectAsArray(n => Create(n)));
            ImmutableArray<string> argumentNames = boundDynamicObjectCreationExpression.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicObjectCreationExpression.ArgumentRefKindsOpt.NullToEmpty();
            Lazy1<IObjectOrCollectionInitializerOperation> initializer = new Lazy1<IObjectOrCollectionInitializerOperation>(() => (IObjectOrCollectionInitializerOperation)Create(boundDynamicObjectCreationExpression.InitializerExpressionOpt));
            SyntaxNode syntax = boundDynamicObjectCreationExpression.Syntax;
            ITypeSymbol type = boundDynamicObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicObjectCreationExpression.ConstantValue);
            bool isImplicit = boundDynamicObjectCreationExpression.WasCompilerGenerated;
            return new LazyDynamicObjectCreationExpression(arguments, argumentNames, argumentRefKinds, initializer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicInvocationOperation CreateBoundDynamicInvocationExpressionOperation(BoundDynamicInvocation boundDynamicInvocation)
        {
            Lazy1<IOperation> expression;
            if (boundDynamicInvocation.Expression.Kind == BoundKind.MethodGroup)
            {
                var methodGroup = (BoundMethodGroup)boundDynamicInvocation.Expression;
                expression = new Lazy1<IOperation>(() => CreateBoundDynamicMemberAccessOperation(methodGroup.ReceiverOpt, TypeMap.AsTypeSymbols(methodGroup.TypeArgumentsOpt),
                    methodGroup.Name, methodGroup.Syntax, methodGroup.Type, methodGroup.ConstantValue, methodGroup.WasCompilerGenerated));
            }
            else
            {
                expression = new Lazy1<IOperation>(() => Create(boundDynamicInvocation.Expression));
            }
            Lazy1<ImmutableArray<IOperation>> arguments = new Lazy1<ImmutableArray<IOperation>>(() => boundDynamicInvocation.Arguments.SelectAsArray(n => Create(n)));
            ImmutableArray<string> argumentNames = boundDynamicInvocation.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicInvocation.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicInvocation.Syntax;
            ITypeSymbol type = boundDynamicInvocation.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicInvocation.ConstantValue);
            bool isImplicit = boundDynamicInvocation.WasCompilerGenerated;
            return new LazyDynamicInvocationExpression(expression, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicIndexerAccessOperation CreateBoundDynamicIndexerAccessExpressionOperation(BoundDynamicIndexerAccess boundDynamicIndexerAccess)
        {
            Lazy1<IOperation> expression = new Lazy1<IOperation>(() => Create(boundDynamicIndexerAccess.ReceiverOpt));
            Lazy1<ImmutableArray<IOperation>> arguments = new Lazy1<ImmutableArray<IOperation>>(() => boundDynamicIndexerAccess.Arguments.SelectAsArray(n => Create(n)));
            ImmutableArray<string> argumentNames = boundDynamicIndexerAccess.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicIndexerAccess.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicIndexerAccess.Syntax;
            ITypeSymbol type = boundDynamicIndexerAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicIndexerAccess.ConstantValue);
            bool isImplicit = boundDynamicIndexerAccess.WasCompilerGenerated;
            return new LazyDynamicIndexerAccessExpression(expression, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IObjectOrCollectionInitializerOperation CreateBoundObjectInitializerExpressionOperation(BoundObjectInitializerExpression boundObjectInitializerExpression)
        {
            Lazy1<ImmutableArray<IOperation>> initializers = new Lazy1<ImmutableArray<IOperation>>(() => BoundObjectCreationExpression.GetChildInitializers(boundObjectInitializerExpression).SelectAsArray(n => Create(n)));
            SyntaxNode syntax = boundObjectInitializerExpression.Syntax;
            ITypeSymbol type = boundObjectInitializerExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectInitializerExpression.ConstantValue);
            bool isImplicit = boundObjectInitializerExpression.WasCompilerGenerated;
            return new LazyObjectOrCollectionInitializerExpression(initializers, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IObjectOrCollectionInitializerOperation CreateBoundCollectionInitializerExpressionOperation(BoundCollectionInitializerExpression boundCollectionInitializerExpression)
        {
            Lazy1<ImmutableArray<IOperation>> initializers = new Lazy1<ImmutableArray<IOperation>>(() => BoundObjectCreationExpression.GetChildInitializers(boundCollectionInitializerExpression).SelectAsArray(n => Create(n)));
            SyntaxNode syntax = boundCollectionInitializerExpression.Syntax;
            ITypeSymbol type = boundCollectionInitializerExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundCollectionInitializerExpression.ConstantValue);
            bool isImplicit = boundCollectionInitializerExpression.WasCompilerGenerated;
            return new LazyObjectOrCollectionInitializerExpression(initializers, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundObjectInitializerMemberOperation(BoundObjectInitializerMember boundObjectInitializerMember, bool isObjectOrCollectionInitializer = false)
        {
            Symbol memberSymbol = boundObjectInitializerMember.MemberSymbol;

            Lazy1<IOperation> instance = memberSymbol?.IsStatic == true ?
                                            OperationFactory.NullOperation :
                                            new Lazy1<IOperation>(() => CreateImplicitReciever(boundObjectInitializerMember.Syntax, boundObjectInitializerMember.ReceiverType));

            SyntaxNode syntax = boundObjectInitializerMember.Syntax;
            ITypeSymbol type = boundObjectInitializerMember.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectInitializerMember.ConstantValue);
            bool isImplicit = boundObjectInitializerMember.WasCompilerGenerated;

            if ((object)memberSymbol == null)
            {
                Debug.Assert(boundObjectInitializerMember.Type.IsDynamic());

                Lazy1<ImmutableArray<IOperation>> arguments = new Lazy1<ImmutableArray<IOperation>>(() => boundObjectInitializerMember.Arguments.SelectAsArray(n => Create(n)));
                ImmutableArray<string> argumentNames = boundObjectInitializerMember.ArgumentNamesOpt.NullToEmpty();
                ImmutableArray<RefKind> argumentRefKinds = boundObjectInitializerMember.ArgumentRefKindsOpt.NullToEmpty();
                return new LazyDynamicIndexerAccessExpression(instance, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit);
            }

            switch (memberSymbol.Kind)
            {
                case SymbolKind.Field:
                    var field = (FieldSymbol)memberSymbol;
                    bool isDeclaration = false;
                    return new LazyFieldReferenceExpression(field, isDeclaration, instance, _semanticModel, syntax, type, constantValue, isImplicit);
                case SymbolKind.Event:
                    var eventSymbol = (EventSymbol)memberSymbol;
                    return new LazyEventReferenceExpression(eventSymbol, instance, _semanticModel, syntax, type, constantValue, isImplicit);
                case SymbolKind.Property:
                    var property = (PropertySymbol)memberSymbol;
                    Lazy1<ImmutableArray<IArgumentOperation>> arguments;
                    if (!boundObjectInitializerMember.Arguments.Any())
                    {
                        // Simple property reference.
                        arguments = new Lazy1<ImmutableArray<IArgumentOperation>>(() => ImmutableArray<IArgumentOperation>.Empty);
                    }
                    else
                    {
                        // In nested member initializers, the property is not actually set. Instead, it is retrieved for a series of Add method calls or nested property setter calls,
                        // so we need to use the getter for this property
                        MethodSymbol accessor = isObjectOrCollectionInitializer ? property.GetOwnOrInheritedGetMethod() : property.GetOwnOrInheritedSetMethod();
                        if (accessor == null || boundObjectInitializerMember.ResultKind == LookupResultKind.OverloadResolutionFailure || accessor.OriginalDefinition is ErrorMethodSymbol)
                        {
                            Lazy1<ImmutableArray<IOperation>> children = new Lazy1<ImmutableArray<IOperation>>(() =>
                                boundObjectInitializerMember.Arguments.SelectAsArray(a => Create(a)));
                            return new LazyInvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
                        }
                        // Indexed property reference.
                        arguments = new Lazy1<ImmutableArray<IArgumentOperation>>(() =>
                        {
                            return DeriveArguments(
                                boundObjectInitializerMember,
                                boundObjectInitializerMember.BinderOpt,
                                property,
                                accessor,
                                boundObjectInitializerMember.Arguments,
                                boundObjectInitializerMember.ArgumentNamesOpt,
                                boundObjectInitializerMember.ArgsToParamsOpt,
                                boundObjectInitializerMember.ArgumentRefKindsOpt,
                                property.Parameters,
                                boundObjectInitializerMember.Expanded,
                                boundObjectInitializerMember.Syntax);
                        });
                    }

                    return new LazyPropertyReferenceExpression(property, instance, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private IOperation CreateBoundDynamicObjectInitializerMemberOperation(BoundDynamicObjectInitializerMember boundDynamicObjectInitializerMember)
        {
            Lazy1<IOperation> instanceRecevier = new Lazy1<IOperation>(() =>
                CreateImplicitReciever(boundDynamicObjectInitializerMember.Syntax, boundDynamicObjectInitializerMember.ReceiverType));

            string memberName = boundDynamicObjectInitializerMember.MemberName;
            ImmutableArray<ITypeSymbol> typeArguments = ImmutableArray<ITypeSymbol>.Empty;
            ITypeSymbol containingType = boundDynamicObjectInitializerMember.ReceiverType;
            SyntaxNode syntax = boundDynamicObjectInitializerMember.Syntax;
            ITypeSymbol type = boundDynamicObjectInitializerMember.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicObjectInitializerMember.ConstantValue);
            bool isImplicit = boundDynamicObjectInitializerMember.WasCompilerGenerated;

            return new LazyDynamicMemberReferenceExpression(instanceRecevier, memberName, typeArguments, containingType, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundCollectionElementInitializerOperation(BoundCollectionElementInitializer boundCollectionElementInitializer)
        {
            MethodSymbol addMethod = boundCollectionElementInitializer.AddMethod;
            SyntaxNode syntax = boundCollectionElementInitializer.Syntax;
            ITypeSymbol type = boundCollectionElementInitializer.Type;
            Optional<object> constantValue = ConvertToOptional(boundCollectionElementInitializer.ConstantValue);
            bool isImplicit = boundCollectionElementInitializer.WasCompilerGenerated;

            if (IsMethodInvalid(boundCollectionElementInitializer.ResultKind, addMethod))
            {
                return CreateInvalidExpressionForHasArgumentsExpression(boundCollectionElementInitializer.ImplicitReceiverOpt, boundCollectionElementInitializer.Arguments, null, syntax, type, constantValue, isImplicit);
            }

            Lazy1<IOperation> receiver = CreateReceiverOperation(boundCollectionElementInitializer.ImplicitReceiverOpt, addMethod);

            Lazy1<ImmutableArray<IArgumentOperation>> arguments = new Lazy1<ImmutableArray<IArgumentOperation>>(() =>
                DeriveArguments(
                    boundCollectionElementInitializer,
                    boundCollectionElementInitializer.BinderOpt,
                    boundCollectionElementInitializer.AddMethod,
                    boundCollectionElementInitializer.AddMethod,
                    boundCollectionElementInitializer.Arguments,
                    argumentNamesOpt: default,
                    boundCollectionElementInitializer.ArgsToParamsOpt,
                    argumentRefKindsOpt: default,
                    boundCollectionElementInitializer.AddMethod.Parameters,
                    boundCollectionElementInitializer.Expanded,
                    boundCollectionElementInitializer.Syntax,
                    boundCollectionElementInitializer.InvokedAsExtensionMethod));
            bool isVirtual = IsCallVirtual(addMethod, boundCollectionElementInitializer.ImplicitReceiverOpt);
            return new LazyInvocationExpression(addMethod, receiver, isVirtual, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicMemberReferenceOperation CreateBoundDynamicMemberAccessOperation(BoundDynamicMemberAccess boundDynamicMemberAccess)
        {
            return CreateBoundDynamicMemberAccessOperation(boundDynamicMemberAccess.Receiver, TypeMap.AsTypeSymbols(boundDynamicMemberAccess.TypeArgumentsOpt), boundDynamicMemberAccess.Name,
                boundDynamicMemberAccess.Syntax, boundDynamicMemberAccess.Type, boundDynamicMemberAccess.ConstantValue, boundDynamicMemberAccess.WasCompilerGenerated);
        }

        private IDynamicMemberReferenceOperation CreateBoundDynamicMemberAccessOperation(
            BoundExpression receiverOpt,
            ImmutableArray<TypeSymbol> typeArgumentsOpt,
            string memberName,
            SyntaxNode syntaxNode,
            ITypeSymbol type,
            ConstantValue value,
            bool isImplicit)
        {
            Lazy1<IOperation> instance = receiverOpt == null || receiverOpt.Kind == BoundKind.TypeExpression ? 
                                            OperationFactory.NullOperation :
                                            new Lazy1<IOperation>(() => Create(receiverOpt));

            ImmutableArray<ITypeSymbol> typeArguments = ImmutableArray<ITypeSymbol>.Empty;
            if (!typeArgumentsOpt.IsDefault)
            {
                typeArguments = ImmutableArray<ITypeSymbol>.CastUp(typeArgumentsOpt);
            }
            ITypeSymbol containingType = receiverOpt?.Kind == BoundKind.TypeExpression ? receiverOpt.Type : null;
            Optional<object> constantValue = ConvertToOptional(value);
            return new LazyDynamicMemberReferenceExpression(instance, memberName, typeArguments, containingType, _semanticModel, syntaxNode, type, constantValue, isImplicit);
        }

        private IDynamicInvocationOperation CreateBoundDynamicCollectionElementInitializerOperation(BoundDynamicCollectionElementInitializer boundCollectionElementInitializer)
        {
            Lazy1<IOperation> operation = new Lazy1<IOperation>(() => Create(boundCollectionElementInitializer.ImplicitReceiver));
            Lazy1<ImmutableArray<IOperation>> arguments = new Lazy1<ImmutableArray<IOperation>>(() => boundCollectionElementInitializer.Arguments.SelectAsArray(n => Create(n)));
            SyntaxNode syntax = boundCollectionElementInitializer.Syntax;
            ITypeSymbol type = boundCollectionElementInitializer.Type;
            Optional<object> constantValue = ConvertToOptional(boundCollectionElementInitializer.ConstantValue);
            bool isImplicit = boundCollectionElementInitializer.WasCompilerGenerated;
            BoundImplicitReceiver implicitReceiver = boundCollectionElementInitializer.ImplicitReceiver;
            Lazy1<IOperation> addReference = new Lazy1<IOperation>(() =>
                CreateBoundDynamicMemberAccessOperation(implicitReceiver, typeArgumentsOpt: ImmutableArray<TypeSymbol>.Empty, memberName: "Add",
                                                        implicitReceiver.Syntax, type: null, value: default, isImplicit: true));
            return new LazyDynamicInvocationExpression(addReference, arguments, argumentNames: ImmutableArray<string>.Empty, argumentRefKinds: ImmutableArray<RefKind>.Empty, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateUnboundLambdaOperation(UnboundLambda unboundLambda)
        {
            // We want to ensure that we never see the UnboundLambda node, and that we don't end up having two different IOperation
            // nodes for the lambda expression. So, we ask the semantic model for the IOperation node for the unbound lambda syntax.
            // We are counting on the fact that will do the error recovery and actually create the BoundLambda node appropriate for
            // this syntax node.
            BoundLambda boundLambda = unboundLambda.BindForErrorRecovery();
            return CreateInternal(boundLambda);
        }

        private IAnonymousFunctionOperation CreateBoundLambdaOperation(BoundLambda boundLambda)
        {
            IMethodSymbol symbol = boundLambda.Symbol;
            Lazy1<IBlockOperation> body = new Lazy1<IBlockOperation>(() => (IBlockOperation)Create(boundLambda.Body));
            SyntaxNode syntax = boundLambda.Syntax;
            // This matches the SemanticModel implementation. This is because in VB, lambdas by themselves
            // do not have a type. To get the type of a lambda expression in the SemanticModel, you need to look at
            // TypeInfo.ConvertedType, rather than TypeInfo.Type. We replicate that behavior here. To get the type of
            // an IAnonymousFunctionExpression, you need to look at the parent IConversionExpression.
            ITypeSymbol type = null;
            Optional<object> constantValue = ConvertToOptional(boundLambda.ConstantValue);
            bool isImplicit = boundLambda.WasCompilerGenerated;
            return new LazyAnonymousFunctionExpression(symbol, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILocalFunctionOperation CreateBoundLocalFunctionStatementOperation(BoundLocalFunctionStatement boundLocalFunctionStatement)
        {
            IMethodSymbol symbol = boundLocalFunctionStatement.Symbol;
            Lazy1<IBlockOperation> body = new Lazy1<IBlockOperation>(() => (IBlockOperation)Create(boundLocalFunctionStatement.Body));
            Lazy1<IBlockOperation> ignoredBody = new Lazy1<IBlockOperation>(() => boundLocalFunctionStatement.BlockBody != null && boundLocalFunctionStatement.ExpressionBody != null ?
                                                                                        (IBlockOperation)Create(boundLocalFunctionStatement.ExpressionBody) :
                                                                                        null);
            SyntaxNode syntax = boundLocalFunctionStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLocalFunctionStatement.WasCompilerGenerated;
            return new LazyLocalFunctionStatement(symbol, body, ignoredBody, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundConversionOperation(BoundConversion boundConversion)
        {
            bool isImplicit = boundConversion.WasCompilerGenerated || !boundConversion.ExplicitCastInCode;
            BoundExpression boundOperand = boundConversion.Operand;
            if (boundConversion.ConversionKind == CSharp.ConversionKind.MethodGroup)
            {
                // We don't check HasErrors on the conversion here because if we actually have a MethodGroup conversion,
                // overload resolution succeeded. The resulting method could be invalid for other reasons, but we don't
                // hide the resolved method.
                Lazy1<IOperation> target = new Lazy1<IOperation>(() =>
                        CreateBoundMethodGroupSingleMethodOperation((BoundMethodGroup)boundOperand,
                                                                    boundConversion.SymbolOpt,
                                                                    boundConversion.SuppressVirtualCalls));
                SyntaxNode syntax = boundConversion.Syntax;
                ITypeSymbol type = boundConversion.Type;
                Optional<object> constantValue = ConvertToOptional(boundConversion.ConstantValue);
                return new LazyDelegateCreationExpression(target, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else
            {
                SyntaxNode syntax = boundConversion.Syntax;

                if (syntax.IsMissing)
                {
                    // If the underlying syntax IsMissing, then that means we're in case where the compiler generated a piece of syntax to fill in for
                    // an error, such as this case:
                    //
                    //  int i = ;
                    //
                    // Semantic model has a special case here that we match: if the underlying syntax is missing, don't create a conversion expression,
                    // and instead directly return the operand, which will be a BoundBadExpression. When we generate a node for the BoundBadExpression,
                    // the resulting IOperation will also have a null Type.
                    Debug.Assert(boundOperand.Kind == BoundKind.BadExpression ||
                                 ((boundOperand as BoundLambda)?.Body.Statements.SingleOrDefault() as BoundReturnStatement)?.
                                     ExpressionOpt?.Kind == BoundKind.BadExpression);
                    return Create(boundOperand);
                }

                Lazy1<IOperation> operand = null;
                Conversion conversion = boundConversion.Conversion;

                if (boundOperand.Syntax == boundConversion.Syntax)
                {
                    if (boundOperand.Kind == BoundKind.ConvertedTupleLiteral && boundOperand.Type == boundConversion.Type)
                    {
                        // Erase this conversion, this is an artificial conversion added on top of BoundConvertedTupleLiteral
                        // in Binder.CreateTupleLiteralConversion
                        return Create(boundOperand);
                    }
                    else 
                    {
                        // Make this conversion implicit
                        isImplicit = true;
                    }
                }

                if (boundConversion.ExplicitCastInCode && conversion.IsIdentity && boundOperand.Kind == BoundKind.Conversion)
                {
                    var nestedConversion = (BoundConversion)boundOperand;
                    BoundExpression nestedOperand = nestedConversion.Operand;

                    if (nestedConversion.Syntax == nestedOperand.Syntax && nestedConversion.ExplicitCastInCode &&
                        nestedOperand.Kind == BoundKind.ConvertedTupleLiteral &&
                        nestedConversion.Type != nestedOperand.Type)
                    {
                        // Let's erase the nested conversion, this is an artificial conversion added on top of BoundConvertedTupleLiteral
                        // in Binder.CreateTupleLiteralConversion.
                        // We need to use conversion information from the nested conversion because that is where the real conversion 
                        // information is stored.
                        conversion = nestedConversion.Conversion;
                        operand = new Lazy1<IOperation>(() => Create(nestedOperand));
                    }
                }

                if (operand == null)
                {
                    operand = new Lazy1<IOperation>(() => Create(boundOperand));
                }

                ITypeSymbol type = boundConversion.Type;
                Optional<object> constantValue = ConvertToOptional(boundConversion.ConstantValue);

                // If this is a lambda or method group conversion to a delegate type, we return a delegate creation instead of a conversion
                if ((boundOperand.Kind == BoundKind.Lambda ||
                     boundOperand.Kind == BoundKind.UnboundLambda ||
                     boundOperand.Kind == BoundKind.MethodGroup) &&
                    boundConversion.Type.IsDelegateType())
                {
                    return new LazyDelegateCreationExpression(operand, _semanticModel, syntax, type, constantValue, isImplicit);
                }
                else
                {
                    bool isTryCast = false;
                    // Checked conversions only matter if the conversion is a Numeric conversion. Don't have true unless the conversion is actually numeric.
                    bool isChecked = conversion.IsNumeric && boundConversion.Checked;
                    return new LazyConversionOperation(operand, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit);
                }
            }
        }

        private IConversionOperation CreateBoundAsOperatorOperation(BoundAsOperator boundAsOperator)
        {
            Lazy1<IOperation> operand = new Lazy1<IOperation>(() => Create(boundAsOperator.Operand));
            SyntaxNode syntax = boundAsOperator.Syntax;
            Conversion conversion = boundAsOperator.Conversion;
            bool isTryCast = true;
            bool isChecked = false;
            ITypeSymbol type = boundAsOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAsOperator.ConstantValue);
            bool isImplicit = boundAsOperator.WasCompilerGenerated;
            return new LazyConversionOperation(operand, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDelegateCreationOperation CreateBoundDelegateCreationExpressionOperation(BoundDelegateCreationExpression boundDelegateCreationExpression)
        {
            Lazy1<IOperation> target = new Lazy1<IOperation>(() =>
            {
                if (boundDelegateCreationExpression.Argument.Kind == BoundKind.MethodGroup &&
                    boundDelegateCreationExpression.MethodOpt != null)
                {
                    // If this is a method binding, and a valid candidate method was found, then we want to expose
                    // this child as an IMethodBindingReference. Otherwise, we want to just delegate to the standard
                    // CSharpOperationFactory behavior. Note we don't check HasErrors here because if we have a method group,
                    // overload resolution succeeded, even if the resulting method isn't valid for some other reason.
                    BoundMethodGroup boundMethodGroup = (BoundMethodGroup)boundDelegateCreationExpression.Argument;
                    return CreateBoundMethodGroupSingleMethodOperation(boundMethodGroup, boundDelegateCreationExpression.MethodOpt, boundMethodGroup.SuppressVirtualCalls);
                }
                else
                {
                    return Create(boundDelegateCreationExpression.Argument);
                }
            });
            SyntaxNode syntax = boundDelegateCreationExpression.Syntax;
            ITypeSymbol type = boundDelegateCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDelegateCreationExpression.ConstantValue);
            bool isImplicit = boundDelegateCreationExpression.WasCompilerGenerated;
            return new LazyDelegateCreationExpression(target, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IMethodReferenceOperation CreateBoundMethodGroupSingleMethodOperation(BoundMethodGroup boundMethodGroup, IMethodSymbol methodSymbol, bool suppressVirtualCalls)
        {
            bool isVirtual = (methodSymbol.IsAbstract || methodSymbol.IsOverride || methodSymbol.IsVirtual) && !suppressVirtualCalls;
            Lazy1<IOperation> instance = CreateReceiverOperation(boundMethodGroup.ReceiverOpt, methodSymbol);
            SyntaxNode bindingSyntax = boundMethodGroup.Syntax;
            ITypeSymbol bindingType = null;
            Optional<object> bindingConstantValue = ConvertToOptional(boundMethodGroup.ConstantValue);
            bool isImplicit = boundMethodGroup.WasCompilerGenerated;
            return new LazyMethodReferenceExpression(methodSymbol, isVirtual, instance, _semanticModel, bindingSyntax, bindingType, bindingConstantValue, boundMethodGroup.WasCompilerGenerated);
        }

        private IIsTypeOperation CreateBoundIsOperatorOperation(BoundIsOperator boundIsOperator)
        {
            Lazy1<IOperation> valueOperand = new Lazy1<IOperation>(() => Create(boundIsOperator.Operand));
            ITypeSymbol typeOperand = boundIsOperator.TargetType.Type;
            SyntaxNode syntax = boundIsOperator.Syntax;
            ITypeSymbol type = boundIsOperator.Type;
            bool isNegated = false;
            Optional<object> constantValue = ConvertToOptional(boundIsOperator.ConstantValue);
            bool isImplicit = boundIsOperator.WasCompilerGenerated;
            return new LazyIsTypeExpression(valueOperand, typeOperand, isNegated, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ISizeOfOperation CreateBoundSizeOfOperatorOperation(BoundSizeOfOperator boundSizeOfOperator)
        {
            ITypeSymbol typeOperand = boundSizeOfOperator.SourceType.Type;
            SyntaxNode syntax = boundSizeOfOperator.Syntax;
            ITypeSymbol type = boundSizeOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundSizeOfOperator.ConstantValue);
            bool isImplicit = boundSizeOfOperator.WasCompilerGenerated;
            return new SizeOfExpression(typeOperand, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITypeOfOperation CreateBoundTypeOfOperatorOperation(BoundTypeOfOperator boundTypeOfOperator)
        {
            ITypeSymbol typeOperand = boundTypeOfOperator.SourceType.Type;
            SyntaxNode syntax = boundTypeOfOperator.Syntax;
            ITypeSymbol type = boundTypeOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundTypeOfOperator.ConstantValue);
            bool isImplicit = boundTypeOfOperator.WasCompilerGenerated;
            return new TypeOfExpression(typeOperand, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IArrayCreationOperation CreateBoundArrayCreationOperation(BoundArrayCreation boundArrayCreation)
        {
            Lazy1<ImmutableArray<IOperation>> dimensionSizes = new Lazy1<ImmutableArray<IOperation>>(() => boundArrayCreation.Bounds.SelectAsArray(n => Create(n)));
            Lazy1<IArrayInitializerOperation> initializer = new Lazy1<IArrayInitializerOperation>(() => (IArrayInitializerOperation)Create(boundArrayCreation.InitializerOpt));
            SyntaxNode syntax = boundArrayCreation.Syntax;
            ITypeSymbol type = boundArrayCreation.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayCreation.ConstantValue);
            bool isImplicit = boundArrayCreation.WasCompilerGenerated ||
                              (boundArrayCreation.InitializerOpt?.Syntax == syntax && !boundArrayCreation.InitializerOpt.WasCompilerGenerated);
            return new LazyArrayCreationExpression(dimensionSizes, initializer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IArrayInitializerOperation CreateBoundArrayInitializationOperation(BoundArrayInitialization boundArrayInitialization)
        {
            Lazy1<ImmutableArray<IOperation>> elementValues = new Lazy1<ImmutableArray<IOperation>>(() => boundArrayInitialization.Initializers.SelectAsArray(n => Create(n)));
            SyntaxNode syntax = boundArrayInitialization.Syntax;
            Optional<object> constantValue = ConvertToOptional(boundArrayInitialization.ConstantValue);
            bool isImplicit = boundArrayInitialization.WasCompilerGenerated;
            return new LazyArrayInitializer(elementValues, _semanticModel, syntax, constantValue, isImplicit);
        }

        private IDefaultValueOperation CreateBoundDefaultExpressionOperation(BoundDefaultExpression boundDefaultExpression)
        {
            SyntaxNode syntax = boundDefaultExpression.Syntax;
            ITypeSymbol type = boundDefaultExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDefaultExpression.ConstantValue);
            bool isImplicit = boundDefaultExpression.WasCompilerGenerated;
            return new DefaultValueExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundBaseReferenceOperation(BoundBaseReference boundBaseReference)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ContainingTypeInstance;
            SyntaxNode syntax = boundBaseReference.Syntax;
            ITypeSymbol type = boundBaseReference.Type;
            Optional<object> constantValue = ConvertToOptional(boundBaseReference.ConstantValue);
            bool isImplicit = boundBaseReference.WasCompilerGenerated;
            return new InstanceReferenceExpression(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundThisReferenceOperation(BoundThisReference boundThisReference)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ContainingTypeInstance;
            SyntaxNode syntax = boundThisReference.Syntax;
            ITypeSymbol type = boundThisReference.Type;
            Optional<object> constantValue = ConvertToOptional(boundThisReference.ConstantValue);
            bool isImplicit = boundThisReference.WasCompilerGenerated;
            return new InstanceReferenceExpression(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundAssignmentOperatorOrMemberInitializerOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            return IsMemberInitializer(boundAssignmentOperator) ?
                (IOperation)CreateBoundMemberInitializerOperation(boundAssignmentOperator) :
                CreateBoundAssignmentOperatorOperation(boundAssignmentOperator);
        }

        private static bool IsMemberInitializer(BoundAssignmentOperator boundAssignmentOperator) =>
            boundAssignmentOperator.Right?.Kind == BoundKind.ObjectInitializerExpression ||
            boundAssignmentOperator.Right?.Kind == BoundKind.CollectionInitializerExpression;

        private ISimpleAssignmentOperation CreateBoundAssignmentOperatorOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            Debug.Assert(!IsMemberInitializer(boundAssignmentOperator));

            Lazy1<IOperation> target = new Lazy1<IOperation>(() => Create(boundAssignmentOperator.Left));
            bool isRef = boundAssignmentOperator.IsRef;
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundAssignmentOperator.Right));
            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol type = boundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAssignmentOperator.ConstantValue);
            bool isImplicit = boundAssignmentOperator.WasCompilerGenerated;
            return new LazySimpleAssignmentExpression(target, isRef, value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IMemberInitializerOperation CreateBoundMemberInitializerOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            Debug.Assert(IsMemberInitializer(boundAssignmentOperator));

            Lazy1<IOperation> target = new Lazy1<IOperation>(() => {
                // We can have bad expressions on the left, fall back to standard creation if that's this case
                switch (boundAssignmentOperator.Left.Kind)
                {
                    case BoundKind.ObjectInitializerMember:
                        return _cache.GetOrAdd(boundAssignmentOperator.Left, key =>
                            CreateBoundObjectInitializerMemberOperation((BoundObjectInitializerMember)key, isObjectOrCollectionInitializer: true));
                    case BoundKind.DynamicObjectInitializerMember:
                        return _cache.GetOrAdd(boundAssignmentOperator.Left, key =>
                            CreateBoundDynamicObjectInitializerMemberOperation((BoundDynamicObjectInitializerMember)key));
                    default:
                        return Create(boundAssignmentOperator.Left);
                }
            });
            Lazy1<IObjectOrCollectionInitializerOperation> value = new Lazy1<IObjectOrCollectionInitializerOperation>(() => (IObjectOrCollectionInitializerOperation)Create(boundAssignmentOperator.Right));
            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol type = boundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAssignmentOperator.ConstantValue);
            bool isImplicit = boundAssignmentOperator.WasCompilerGenerated;
            return new LazyMemberInitializerExpression(target, value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICompoundAssignmentOperation CreateBoundCompoundAssignmentOperatorOperation(BoundCompoundAssignmentOperator boundCompoundAssignmentOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundCompoundAssignmentOperator.Operator.Kind);
            Lazy1<IOperation> target = new Lazy1<IOperation>(() => Create(boundCompoundAssignmentOperator.Left));
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundCompoundAssignmentOperator.Right));
            Conversion inConversion = boundCompoundAssignmentOperator.LeftConversion;
            Conversion outConversion = boundCompoundAssignmentOperator.FinalConversion;
            bool isLifted = boundCompoundAssignmentOperator.Operator.Kind.IsLifted();
            bool isChecked = boundCompoundAssignmentOperator.Operator.Kind.IsChecked();
            IMethodSymbol operatorMethod = boundCompoundAssignmentOperator.Operator.Method;
            SyntaxNode syntax = boundCompoundAssignmentOperator.Syntax;
            ITypeSymbol type = boundCompoundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundCompoundAssignmentOperator.ConstantValue);
            bool isImplicit = boundCompoundAssignmentOperator.WasCompilerGenerated;
            return new LazyCompoundAssignmentOperation(target, value, inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IIncrementOrDecrementOperation CreateBoundIncrementOperatorOperation(BoundIncrementOperator boundIncrementOperator)
        {
            bool isDecrement = Helper.IsDecrement(boundIncrementOperator.OperatorKind);
            bool isPostfix = Helper.IsPostfixIncrementOrDecrement(boundIncrementOperator.OperatorKind);
            bool isLifted = boundIncrementOperator.OperatorKind.IsLifted();
            bool isChecked = boundIncrementOperator.OperatorKind.IsChecked();
            Lazy1<IOperation> target = new Lazy1<IOperation>(() => Create(boundIncrementOperator.Operand));
            IMethodSymbol operatorMethod = boundIncrementOperator.MethodOpt;
            SyntaxNode syntax = boundIncrementOperator.Syntax;
            ITypeSymbol type = boundIncrementOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundIncrementOperator.ConstantValue);
            bool isImplicit = boundIncrementOperator.WasCompilerGenerated;
            return new LazyIncrementExpression(isDecrement, isPostfix, isLifted, isChecked, target, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInvalidOperation CreateBoundBadExpressionOperation(BoundBadExpression boundBadExpression)
        {
            Lazy1<ImmutableArray<IOperation>> children = new Lazy1<ImmutableArray<IOperation>>(() => boundBadExpression.ChildBoundNodes.Select(n => Create(n)).WhereNotNull().ToImmutableArray());
            SyntaxNode syntax = boundBadExpression.Syntax;
            // We match semantic model here: if the expression IsMissing, we have a null type, rather than the ErrorType of the bound node.
            ITypeSymbol type = syntax.IsMissing ? null : boundBadExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundBadExpression.ConstantValue);

            // if child has syntax node point to same syntax node as bad expression, then this invalid expression is implicit
            bool isImplicit = boundBadExpression.WasCompilerGenerated || boundBadExpression.ChildBoundNodes.Any(e => e?.Syntax == boundBadExpression.Syntax);
            return new LazyInvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITypeParameterObjectCreationOperation CreateBoundNewTOperation(BoundNewT boundNewT)
        {
            Lazy1<IObjectOrCollectionInitializerOperation> initializer = new Lazy1<IObjectOrCollectionInitializerOperation>(() => (IObjectOrCollectionInitializerOperation)Create(boundNewT.InitializerExpressionOpt));
            SyntaxNode syntax = boundNewT.Syntax;
            ITypeSymbol type = boundNewT.Type;
            Optional<object> constantValue = ConvertToOptional(boundNewT.ConstantValue);
            bool isImplicit = boundNewT.WasCompilerGenerated;
            return new LazyTypeParameterObjectCreationExpression(initializer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private INoPiaObjectCreationOperation CreateNoPiaObjectCreationExpressionOperation(BoundNoPiaObjectCreationExpression creation)
        {
            Lazy1<IObjectOrCollectionInitializerOperation> initializer = new Lazy1<IObjectOrCollectionInitializerOperation>(() => (IObjectOrCollectionInitializerOperation)Create(creation.InitializerExpressionOpt));
            SyntaxNode syntax = creation.Syntax;
            ITypeSymbol type = creation.Type;
            Optional<object> constantValue = ConvertToOptional(creation.ConstantValue);
            bool isImplicit = creation.WasCompilerGenerated;
            return new LazyNoPiaObjectCreationOperation(initializer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IUnaryOperation CreateBoundUnaryOperatorOperation(BoundUnaryOperator boundUnaryOperator)
        {
            UnaryOperatorKind unaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUnaryOperator.OperatorKind);
            Lazy1<IOperation> operand = new Lazy1<IOperation>(() => Create(boundUnaryOperator.Operand));
            IMethodSymbol operatorMethod = boundUnaryOperator.MethodOpt;
            SyntaxNode syntax = boundUnaryOperator.Syntax;
            ITypeSymbol type = boundUnaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundUnaryOperator.ConstantValue);
            bool isLifted = boundUnaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundUnaryOperator.OperatorKind.IsChecked();
            bool isImplicit = boundUnaryOperator.WasCompilerGenerated;
            return new LazyUnaryOperatorExpression(unaryOperatorKind, operand, isLifted, isChecked, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBinaryOperation CreateBoundBinaryOperatorOperation(BoundBinaryOperator boundBinaryOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundBinaryOperator.OperatorKind);
            Lazy1<IOperation> leftOperand = new Lazy1<IOperation>(() => Create(boundBinaryOperator.Left));
            Lazy1<IOperation> rightOperand = new Lazy1<IOperation>(() => Create(boundBinaryOperator.Right));
            IMethodSymbol operatorMethod = boundBinaryOperator.MethodOpt;
            IMethodSymbol unaryOperatorMethod = null;

            // For dynamic logical operator MethodOpt is actually the unary true/false operator
            if (boundBinaryOperator.Type.IsDynamic() &&
                (operatorKind == BinaryOperatorKind.ConditionalAnd || operatorKind == BinaryOperatorKind.ConditionalOr) &&
                operatorMethod?.Parameters.Length == 1)
            {
                unaryOperatorMethod = operatorMethod;
                operatorMethod = null;
            }

            SyntaxNode syntax = boundBinaryOperator.Syntax;
            ITypeSymbol type = boundBinaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundBinaryOperator.ConstantValue);
            bool isLifted = boundBinaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundBinaryOperator.OperatorKind.IsChecked();
            bool isCompareText = false;
            bool isImplicit = boundBinaryOperator.WasCompilerGenerated;
            return new LazyBinaryOperatorExpression(operatorKind, leftOperand, rightOperand, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod,
                                                    _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBinaryOperation CreateBoundUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator boundBinaryOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundBinaryOperator.OperatorKind);
            Lazy1<IOperation> leftOperand = new Lazy1<IOperation>(() => Create(boundBinaryOperator.Left));
            Lazy1<IOperation> rightOperand = new Lazy1<IOperation>(() => Create(boundBinaryOperator.Right));
            IMethodSymbol operatorMethod = boundBinaryOperator.LogicalOperator;
            IMethodSymbol unaryOperatorMethod = boundBinaryOperator.OperatorKind.Operator() == CSharp.BinaryOperatorKind.And ? 
                                                    boundBinaryOperator.FalseOperator : 
                                                    boundBinaryOperator.TrueOperator;
            SyntaxNode syntax = boundBinaryOperator.Syntax;
            ITypeSymbol type = boundBinaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundBinaryOperator.ConstantValue);
            bool isLifted = boundBinaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundBinaryOperator.OperatorKind.IsChecked();
            bool isCompareText = false;
            bool isImplicit = boundBinaryOperator.WasCompilerGenerated;
            return new LazyBinaryOperatorExpression(operatorKind, leftOperand, rightOperand, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod,
                                                    _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITupleBinaryOperation CreateBoundTupleBinaryOperatorOperation(BoundTupleBinaryOperator boundTupleBinaryOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundTupleBinaryOperator.OperatorKind);
            Lazy1<IOperation> leftOperand = new Lazy1<IOperation>(() => Create(boundTupleBinaryOperator.ConvertedLeft));
            Lazy1<IOperation> rightOperand = new Lazy1<IOperation>(() => Create(boundTupleBinaryOperator.ConvertedRight));
            SyntaxNode syntax = boundTupleBinaryOperator.Syntax;
            ITypeSymbol type = boundTupleBinaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundTupleBinaryOperator.ConstantValue);
            bool isImplicit = boundTupleBinaryOperator.WasCompilerGenerated;
            return new LazyTupleBinaryOperatorExpression(operatorKind, leftOperand, rightOperand, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalOperation CreateBoundConditionalOperatorOperation(BoundConditionalOperator boundConditionalOperator)
        {
            Lazy1<IOperation> condition = new Lazy1<IOperation>(() => Create(boundConditionalOperator.Condition));
            Lazy1<IOperation> whenTrue = new Lazy1<IOperation>(() => Create(boundConditionalOperator.Consequence));
            Lazy1<IOperation> whenFalse = new Lazy1<IOperation>(() => Create(boundConditionalOperator.Alternative));
            bool isRef = boundConditionalOperator.IsRef;
            SyntaxNode syntax = boundConditionalOperator.Syntax;
            ITypeSymbol type = boundConditionalOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalOperator.ConstantValue);
            bool isImplicit = boundConditionalOperator.WasCompilerGenerated;
            return new LazyConditionalOperation(condition, whenTrue, whenFalse, isRef, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICoalesceOperation CreateBoundNullCoalescingOperatorOperation(BoundNullCoalescingOperator boundNullCoalescingOperator)
        {
            Lazy1<IOperation> expression = new Lazy1<IOperation>(() => Create(boundNullCoalescingOperator.LeftOperand));
            Lazy1<IOperation> whenNull = new Lazy1<IOperation>(() => Create(boundNullCoalescingOperator.RightOperand));
            SyntaxNode syntax = boundNullCoalescingOperator.Syntax;
            ITypeSymbol type = boundNullCoalescingOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundNullCoalescingOperator.ConstantValue);
            bool isImplicit = boundNullCoalescingOperator.WasCompilerGenerated;
            Conversion valueConversion = boundNullCoalescingOperator.LeftConversion;

            if (valueConversion.Exists && !valueConversion.IsIdentity &&
                boundNullCoalescingOperator.Type.Equals(boundNullCoalescingOperator.LeftOperand.Type?.StrippedType(), TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
            {
                valueConversion = Conversion.Identity;
            }

            return new LazyCoalesceExpression(expression, whenNull, valueConversion, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundNullCoalescingAssignmentOperatorOperation(BoundNullCoalescingAssignmentOperator boundNode)
        {
            Lazy1<IOperation> target = new Lazy1<IOperation>(() => Create(boundNode.LeftOperand));
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundNode.RightOperand));
            SyntaxNode syntax = boundNode.Syntax;
            ITypeSymbol type = boundNode.Type;
            Optional<object> constantValue = ConvertToOptional(boundNode.ConstantValue);
            bool isImplicit = boundNode.WasCompilerGenerated;

            return new LazyCoalesceAssignmentOperation(target, value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IAwaitOperation CreateBoundAwaitExpressionOperation(BoundAwaitExpression boundAwaitExpression)
        {
            Lazy1<IOperation> awaitedValue = new Lazy1<IOperation>(() => Create(boundAwaitExpression.Expression));
            SyntaxNode syntax = boundAwaitExpression.Syntax;
            ITypeSymbol type = boundAwaitExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundAwaitExpression.ConstantValue);
            bool isImplicit = boundAwaitExpression.WasCompilerGenerated;
            return new LazyAwaitExpression(awaitedValue, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IArrayElementReferenceOperation CreateBoundArrayAccessOperation(BoundArrayAccess boundArrayAccess)
        {
            // The compiler will dedupe the boundArrayAccess.Expression between different array references. Some example code:
            //
            // class C
            // {
            //     int[] a;

            //     static void Main()
            //     {
            //         // Compiler dedupes the array access receiver for [0] and [1]
            //         var a = new C { a = { [0] = 1, [1] = 2 } };
            //     }
            // }
            //
            // In order to prevent parent pointer from having an issue with this, we intentionally create a new IOperation node every time
            // we encounter an array access. Since we create from the top down, it should be impossible for us to see the node in
            // boundArrayAccess.Expression before seeing the boundArrayAccess itself, so this should not create any other parent pointer
            // issues.
            Lazy1<IOperation> arrayReference = new Lazy1<IOperation>(() => CreateInternal(boundArrayAccess.Expression));
            Lazy1<ImmutableArray<IOperation>> indices = new Lazy1<ImmutableArray<IOperation>>(() => boundArrayAccess.Indices.SelectAsArray(n => Create(n)));
            SyntaxNode syntax = boundArrayAccess.Syntax;
            ITypeSymbol type = boundArrayAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayAccess.ConstantValue);
            bool isImplicit = boundArrayAccess.WasCompilerGenerated;

            return new LazyArrayElementReferenceExpression(arrayReference, indices, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private INameOfOperation CreateBoundNameOfOperatorOperation(BoundNameOfOperator boundNameOfOperator)
        {
            Lazy1<IOperation> argument = new Lazy1<IOperation>(() => Create(boundNameOfOperator.Argument));
            SyntaxNode syntax = boundNameOfOperator.Syntax;
            ITypeSymbol type = boundNameOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundNameOfOperator.ConstantValue);
            bool isImplicit = boundNameOfOperator.WasCompilerGenerated;
            return new LazyNameOfExpression(argument, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IThrowOperation CreateBoundThrowExpressionOperation(BoundThrowExpression boundThrowExpression)
        {
            Lazy1<IOperation> expression = new Lazy1<IOperation>(() => Create(boundThrowExpression.Expression));
            SyntaxNode syntax = boundThrowExpression.Syntax;
            ITypeSymbol type = boundThrowExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundThrowExpression.ConstantValue);
            bool isImplicit = boundThrowExpression.WasCompilerGenerated;
            return new LazyThrowExpression(expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IAddressOfOperation CreateBoundAddressOfOperatorOperation(BoundAddressOfOperator boundAddressOfOperator)
        {
            Lazy1<IOperation> reference = new Lazy1<IOperation>(() => Create(boundAddressOfOperator.Operand));
            SyntaxNode syntax = boundAddressOfOperator.Syntax;
            ITypeSymbol type = boundAddressOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAddressOfOperator.ConstantValue);
            bool isImplicit = boundAddressOfOperator.WasCompilerGenerated;
            return new LazyAddressOfExpression(reference, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundImplicitReceiverOperation(BoundImplicitReceiver boundImplicitReceiver)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ImplicitReceiver;
            SyntaxNode syntax = boundImplicitReceiver.Syntax;
            ITypeSymbol type = boundImplicitReceiver.Type;
            Optional<object> constantValue = ConvertToOptional(boundImplicitReceiver.ConstantValue);
            bool isImplicit = boundImplicitReceiver.WasCompilerGenerated;
            return new InstanceReferenceExpression(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalAccessOperation CreateBoundConditionalAccessOperation(BoundConditionalAccess boundConditionalAccess)
        {
            Lazy1<IOperation> whenNotNull = new Lazy1<IOperation>(() => Create(boundConditionalAccess.AccessExpression));
            Lazy1<IOperation> expression = new Lazy1<IOperation>(() => Create(boundConditionalAccess.Receiver));
            SyntaxNode syntax = boundConditionalAccess.Syntax;
            ITypeSymbol type = boundConditionalAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalAccess.ConstantValue);
            bool isImplicit = boundConditionalAccess.WasCompilerGenerated;

            return new LazyConditionalAccessExpression(whenNotNull, expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalAccessInstanceOperation CreateBoundConditionalReceiverOperation(BoundConditionalReceiver boundConditionalReceiver)
        {
            SyntaxNode syntax = boundConditionalReceiver.Syntax;
            ITypeSymbol type = boundConditionalReceiver.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalReceiver.ConstantValue);
            bool isImplicit = boundConditionalReceiver.WasCompilerGenerated;
            return new ConditionalAccessInstanceExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IFieldInitializerOperation CreateBoundFieldEqualsValueOperation(BoundFieldEqualsValue boundFieldEqualsValue)
        {
            ImmutableArray<IFieldSymbol> initializedFields = ImmutableArray.Create<IFieldSymbol>(boundFieldEqualsValue.Field);
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundFieldEqualsValue.Value));
            OperationKind kind = OperationKind.FieldInitializer;
            SyntaxNode syntax = boundFieldEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundFieldEqualsValue.WasCompilerGenerated;
            return new LazyFieldInitializer(boundFieldEqualsValue.Locals.As<ILocalSymbol>(), initializedFields, value, kind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IPropertyInitializerOperation CreateBoundPropertyEqualsValueOperation(BoundPropertyEqualsValue boundPropertyEqualsValue)
        {
            ImmutableArray<IPropertySymbol> initializedProperties = ImmutableArray.Create<IPropertySymbol>(boundPropertyEqualsValue.Property);
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundPropertyEqualsValue.Value));
            OperationKind kind = OperationKind.PropertyInitializer;
            SyntaxNode syntax = boundPropertyEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundPropertyEqualsValue.WasCompilerGenerated;
            return new LazyPropertyInitializer(boundPropertyEqualsValue.Locals.As<ILocalSymbol>(), initializedProperties, value, kind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IParameterInitializerOperation CreateBoundParameterEqualsValueOperation(BoundParameterEqualsValue boundParameterEqualsValue)
        {
            IParameterSymbol parameter = boundParameterEqualsValue.Parameter;
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundParameterEqualsValue.Value));
            OperationKind kind = OperationKind.ParameterInitializer;
            SyntaxNode syntax = boundParameterEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundParameterEqualsValue.WasCompilerGenerated;
            return new LazyParameterInitializer(boundParameterEqualsValue.Locals.As<ILocalSymbol>(), parameter, value, kind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBlockOperation CreateBoundBlockOperation(BoundBlock boundBlock)
        {
            Lazy1<ImmutableArray<IOperation>> statements =
                new Lazy1<ImmutableArray<IOperation>>(() => boundBlock.Statements.Select(s => Create(s)).ToImmutableArray());

            ImmutableArray<ILocalSymbol> locals = boundBlock.Locals.As<ILocalSymbol>();
            SyntaxNode syntax = boundBlock.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundBlock.WasCompilerGenerated;
            return new LazyBlockStatement(statements, locals, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBranchOperation CreateBoundContinueStatementOperation(BoundContinueStatement boundContinueStatement)
        {
            ILabelSymbol target = boundContinueStatement.Label;
            BranchKind branchKind = BranchKind.Continue;
            SyntaxNode syntax = boundContinueStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundContinueStatement.WasCompilerGenerated;
            return new BranchStatement(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBranchOperation CreateBoundBreakStatementOperation(BoundBreakStatement boundBreakStatement)
        {
            ILabelSymbol target = boundBreakStatement.Label;
            BranchKind branchKind = BranchKind.Break;
            SyntaxNode syntax = boundBreakStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundBreakStatement.WasCompilerGenerated;
            return new BranchStatement(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IReturnOperation CreateBoundYieldBreakStatementOperation(BoundYieldBreakStatement boundYieldBreakStatement)
        {
            Lazy1<IOperation> returnedValue = new Lazy1<IOperation>(() => Create(null));
            SyntaxNode syntax = boundYieldBreakStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundYieldBreakStatement.WasCompilerGenerated;
            return new LazyReturnStatement(OperationKind.YieldBreak, returnedValue, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBranchOperation CreateBoundGotoStatementOperation(BoundGotoStatement boundGotoStatement)
        {
            ILabelSymbol target = boundGotoStatement.Label;
            BranchKind branchKind = BranchKind.GoTo;
            SyntaxNode syntax = boundGotoStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundGotoStatement.WasCompilerGenerated;
            return new BranchStatement(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IEmptyOperation CreateBoundNoOpStatementOperation(BoundNoOpStatement boundNoOpStatement)
        {
            SyntaxNode syntax = boundNoOpStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundNoOpStatement.WasCompilerGenerated;
            return new EmptyStatement(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalOperation CreateBoundIfStatementOperation(BoundIfStatement boundIfStatement)
        {
            Lazy1<IOperation> condition = new Lazy1<IOperation>(() => Create(boundIfStatement.Condition));
            Lazy1<IOperation> ifTrueStatement = new Lazy1<IOperation>(() => Create(boundIfStatement.Consequence));
            Lazy1<IOperation> ifFalseStatement = new Lazy1<IOperation>(() => Create(boundIfStatement.AlternativeOpt));
            bool isRef = false;
            SyntaxNode syntax = boundIfStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundIfStatement.WasCompilerGenerated;
            return new LazyConditionalOperation(condition, ifTrueStatement, ifFalseStatement, isRef, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IWhileLoopOperation CreateBoundWhileStatementOperation(BoundWhileStatement boundWhileStatement)
        {
            Lazy1<IOperation> condition = new Lazy1<IOperation>(() => Create(boundWhileStatement.Condition));
            Lazy1<IOperation> body = new Lazy1<IOperation>(() => Create(boundWhileStatement.Body));
            Lazy1<IOperation> ignoredCondition = OperationFactory.NullOperation;
            ImmutableArray<ILocalSymbol> locals = boundWhileStatement.Locals.As<ILocalSymbol>();
            ILabelSymbol continueLabel = boundWhileStatement.ContinueLabel;
            ILabelSymbol exitLabel = boundWhileStatement.BreakLabel;
            bool conditionIsTop = true;
            bool conditionIsUntil = false;
            SyntaxNode syntax = boundWhileStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundWhileStatement.WasCompilerGenerated;
            return new LazyWhileLoopStatement(condition, body, ignoredCondition, locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IWhileLoopOperation CreateBoundDoStatementOperation(BoundDoStatement boundDoStatement)
        {
            Lazy1<IOperation> condition = new Lazy1<IOperation>(() => Create(boundDoStatement.Condition));
            Lazy1<IOperation> body = new Lazy1<IOperation>(() => Create(boundDoStatement.Body));
            Lazy1<IOperation> ignoredCondition = OperationFactory.NullOperation;
            ILabelSymbol continueLabel = boundDoStatement.ContinueLabel;
            ILabelSymbol exitLabel = boundDoStatement.BreakLabel;
            bool conditionIsTop = false;
            bool conditionIsUntil = false;
            ImmutableArray<ILocalSymbol> locals = boundDoStatement.Locals.As<ILocalSymbol>();
            SyntaxNode syntax = boundDoStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundDoStatement.WasCompilerGenerated;
            return new LazyWhileLoopStatement(condition, body, ignoredCondition, locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IForLoopOperation CreateBoundForStatementOperation(BoundForStatement boundForStatement)
        {
            Lazy1<ImmutableArray<IOperation>> before = new Lazy1<ImmutableArray<IOperation>>(() => ToStatements(boundForStatement.Initializer));
            Lazy1<IOperation> condition = new Lazy1<IOperation>(() => Create(boundForStatement.Condition));
            Lazy1<ImmutableArray<IOperation>> atLoopBottom = new Lazy1<ImmutableArray<IOperation>>(() => ToStatements(boundForStatement.Increment));
            ImmutableArray<ILocalSymbol> locals = boundForStatement.OuterLocals.As<ILocalSymbol>();
            ImmutableArray<ILocalSymbol> conditionLocals = boundForStatement.InnerLocals.As<ILocalSymbol>();
            Lazy1<IOperation> body = new Lazy1<IOperation>(() => Create(boundForStatement.Body));
            ILabelSymbol continueLabel = boundForStatement.ContinueLabel;
            ILabelSymbol exitLabel = boundForStatement.BreakLabel;
            SyntaxNode syntax = boundForStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundForStatement.WasCompilerGenerated;
            return new LazyForLoopStatement(before, condition, atLoopBottom, locals, conditionLocals, continueLabel, exitLabel, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IForEachLoopOperation CreateBoundForEachStatementOperation(BoundForEachStatement boundForEachStatement)
        {
            ImmutableArray<ILocalSymbol> locals = boundForEachStatement.IterationVariables.As<ILocalSymbol>();
            Lazy1<IOperation> loopControlVariable;
            if (boundForEachStatement.DeconstructionOpt != null)
            {
                loopControlVariable = new Lazy1<IOperation>(() => Create(boundForEachStatement.DeconstructionOpt.DeconstructionAssignment.Left));
            }
            else if (boundForEachStatement.IterationErrorExpressionOpt != null)
            {
                loopControlVariable = new Lazy1<IOperation>(() => Create(boundForEachStatement.IterationErrorExpressionOpt));
            }
            else
            {
                Debug.Assert(locals.Length == 1);
                var local = (LocalSymbol)locals[0];
                // We use iteration variable type syntax as the underlying syntax node as there is no variable declarator syntax in the syntax tree.
                var declaratorSyntax = boundForEachStatement.IterationVariableType.Syntax;
                loopControlVariable = new Lazy1<IOperation>(() => new VariableDeclarator(local, initializer: null, ignoredArguments: ImmutableArray<IOperation>.Empty, semanticModel: _semanticModel, syntax: declaratorSyntax, type: null, constantValue: default, isImplicit: false));
            }

            Lazy1<IOperation> collection = new Lazy1<IOperation>(() => Create(boundForEachStatement.Expression));
            Lazy1<IOperation> body = new Lazy1<IOperation>(() => Create(boundForEachStatement.Body));
            ForEachEnumeratorInfo enumeratorInfoOpt = boundForEachStatement.EnumeratorInfoOpt;
            ForEachLoopOperationInfo info;

            if (enumeratorInfoOpt != null)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var compilation = _semanticModel.Compilation;

                info = new ForEachLoopOperationInfo(enumeratorInfoOpt.ElementType.TypeSymbol,
                                                    enumeratorInfoOpt.GetEnumeratorMethod,
                                                    (PropertySymbol)enumeratorInfoOpt.CurrentPropertyGetter.AssociatedSymbol,
                                                    enumeratorInfoOpt.MoveNextMethod,
                                                    enumeratorInfoOpt.NeedsDisposeMethod,
                                                    knownToImplementIDisposable: enumeratorInfoOpt.NeedsDisposeMethod && (object)enumeratorInfoOpt.GetEnumeratorMethod != null ?
                                                                                     compilation.Conversions.
                                                                                         ClassifyImplicitConversionFromType(enumeratorInfoOpt.GetEnumeratorMethod.ReturnType.TypeSymbol,
                                                                                                                            compilation.GetSpecialType(SpecialType.System_IDisposable),
                                                                                                                            ref useSiteDiagnostics).IsImplicit :
                                                                                     false,
                                                    enumeratorInfoOpt.CurrentConversion,
                                                    boundForEachStatement.ElementConversion);
            }
            else
            {
                info = default;
            }

            Lazy1<ImmutableArray<IOperation>> nextVariables = new Lazy1<ImmutableArray<IOperation>>(() => ImmutableArray<IOperation>.Empty);
            ILabelSymbol continueLabel = boundForEachStatement.ContinueLabel;
            ILabelSymbol exitLabel = boundForEachStatement.BreakLabel;
            SyntaxNode syntax = boundForEachStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundForEachStatement.WasCompilerGenerated;
            return new LazyForEachLoopStatement(locals, continueLabel, exitLabel, loopControlVariable, collection, nextVariables, body, info, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ISwitchOperation CreateBoundSwitchStatementOperation(BoundSwitchStatement boundSwitchStatement)
        {
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundSwitchStatement.Expression));
            Lazy1<ImmutableArray<ISwitchCaseOperation>> cases = new Lazy1<ImmutableArray<ISwitchCaseOperation>>(() => GetSwitchStatementCases(boundSwitchStatement));
            ImmutableArray<ILocalSymbol> locals = boundSwitchStatement.InnerLocals.As<ILocalSymbol>();
            ILabelSymbol exitLabel = boundSwitchStatement.BreakLabel;
            SyntaxNode syntax = boundSwitchStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundSwitchStatement.WasCompilerGenerated;
            return new LazySwitchStatement(locals, value, cases, exitLabel: exitLabel, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICaseClauseOperation CreateBoundSwitchLabelOperation(BoundSwitchLabel boundSwitchLabel)
        {
            SyntaxNode syntax = boundSwitchLabel.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundSwitchLabel.WasCompilerGenerated;

            if (boundSwitchLabel.ExpressionOpt != null)
            {
                Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundSwitchLabel.ExpressionOpt));
                return new LazySingleValueCaseClause(boundSwitchLabel.Label, value, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else
            {
                return new DefaultCaseClause(boundSwitchLabel.Label, _semanticModel, syntax, type, constantValue, isImplicit);
            }
        }

        private ITryOperation CreateBoundTryStatementOperation(BoundTryStatement boundTryStatement)
        {
            Lazy1<IBlockOperation> body = new Lazy1<IBlockOperation>(() => (IBlockOperation)Create(boundTryStatement.TryBlock));
            Lazy1<ImmutableArray<ICatchClauseOperation>> catches = new Lazy1<ImmutableArray<ICatchClauseOperation>>(() => boundTryStatement.CatchBlocks.SelectAsArray(n => (ICatchClauseOperation)Create(n)));
            Lazy1<IBlockOperation> finallyHandler = new Lazy1<IBlockOperation>(() => (IBlockOperation)Create(boundTryStatement.FinallyBlockOpt));
            SyntaxNode syntax = boundTryStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundTryStatement.WasCompilerGenerated;
            return new LazyTryStatement(body, catches, finallyHandler, exitLabel: null, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICatchClauseOperation CreateBoundCatchBlockOperation(BoundCatchBlock boundCatchBlock)
        {
            var exceptionSourceOpt = (BoundLocal)boundCatchBlock.ExceptionSourceOpt;
            Lazy1<IOperation> expressionDeclarationOrExpression = new Lazy1<IOperation>(() => exceptionSourceOpt != null ? CreateVariableDeclarator(exceptionSourceOpt) : null);
            ITypeSymbol exceptionType = boundCatchBlock.ExceptionTypeOpt ?? (ITypeSymbol)_semanticModel.Compilation.ObjectType;
            ImmutableArray<ILocalSymbol> locals = boundCatchBlock.Locals.As<ILocalSymbol>();
            Lazy1<IOperation> filter = new Lazy1<IOperation>(() => Create(boundCatchBlock.ExceptionFilterOpt));
            Lazy1<IBlockOperation> handler = new Lazy1<IBlockOperation>(() => (IBlockOperation)Create(boundCatchBlock.Body));
            SyntaxNode syntax = boundCatchBlock.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundCatchBlock.WasCompilerGenerated;
            return new LazyCatchClause(expressionDeclarationOrExpression, exceptionType, locals, filter, handler, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IFixedOperation CreateBoundFixedStatementOperation(BoundFixedStatement boundFixedStatement)
        {
            ImmutableArray<ILocalSymbol> locals = boundFixedStatement.Locals.As<ILocalSymbol>();
            Lazy1<IVariableDeclarationGroupOperation> variables = new Lazy1<IVariableDeclarationGroupOperation>(() => (IVariableDeclarationGroupOperation)Create(boundFixedStatement.Declarations));
            Lazy1<IOperation> body = new Lazy1<IOperation>(() => Create(boundFixedStatement.Body));
            SyntaxNode syntax = boundFixedStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundFixedStatement.WasCompilerGenerated;
            return new LazyFixedStatement(locals, variables, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IUsingOperation CreateBoundUsingStatementOperation(BoundUsingStatement boundUsingStatement)
        {
            Lazy1<IOperation> resources = new Lazy1<IOperation>(() => Create((BoundNode)boundUsingStatement.DeclarationsOpt ?? boundUsingStatement.ExpressionOpt));
            Lazy1<IOperation> body = new Lazy1<IOperation>(() => Create(boundUsingStatement.Body));
            ImmutableArray<ILocalSymbol> locals = ImmutableArray<ILocalSymbol>.CastUp(boundUsingStatement.Locals);
            SyntaxNode syntax = boundUsingStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundUsingStatement.WasCompilerGenerated;
            return new LazyUsingStatement(resources, body, locals, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IThrowOperation CreateBoundThrowStatementOperation(BoundThrowStatement boundThrowStatement)
        {
            Lazy1<IOperation> thrownObject = new Lazy1<IOperation>(() => Create(boundThrowStatement.ExpressionOpt));
            SyntaxNode syntax = boundThrowStatement.Syntax;
            ITypeSymbol statementType = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundThrowStatement.WasCompilerGenerated;
            return new LazyThrowExpression(thrownObject, _semanticModel, syntax, statementType, constantValue, isImplicit);
        }

        private IReturnOperation CreateBoundReturnStatementOperation(BoundReturnStatement boundReturnStatement)
        {
            Lazy1<IOperation> returnedValue = new Lazy1<IOperation>(() => Create(boundReturnStatement.ExpressionOpt));
            SyntaxNode syntax = boundReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundReturnStatement.WasCompilerGenerated;
            return new LazyReturnStatement(OperationKind.Return, returnedValue, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IReturnOperation CreateBoundYieldReturnStatementOperation(BoundYieldReturnStatement boundYieldReturnStatement)
        {
            Lazy1<IOperation> returnedValue = new Lazy1<IOperation>(() => Create(boundYieldReturnStatement.Expression));
            SyntaxNode syntax = boundYieldReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundYieldReturnStatement.WasCompilerGenerated;
            return new LazyReturnStatement(OperationKind.YieldReturn, returnedValue, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILockOperation CreateBoundLockStatementOperation(BoundLockStatement boundLockStatement)
        {
            Lazy1<IOperation> expression = new Lazy1<IOperation>(() => Create(boundLockStatement.Argument));
            Lazy1<IOperation> body = new Lazy1<IOperation>(() => Create(boundLockStatement.Body));
            // If there is no Enter2 method, then there will be no lock taken reference
            bool legacyMode = _semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter2) == null;
            ILocalSymbol lockTakenSymbol =
                legacyMode ? null : new SynthesizedLocal(_semanticModel.GetEnclosingSymbol(boundLockStatement.Syntax.SpanStart) as MethodSymbol,
                                                         TypeSymbolWithAnnotations.Create((TypeSymbol)_semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean)),
                                                         SynthesizedLocalKind.LockTaken,
                                                         syntaxOpt: boundLockStatement.Argument.Syntax);
            SyntaxNode syntax = boundLockStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLockStatement.WasCompilerGenerated;

            return new LazyLockStatement(expression, body, lockTakenSymbol, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInvalidOperation CreateBoundBadStatementOperation(BoundBadStatement boundBadStatement)
        {
            Lazy1<ImmutableArray<IOperation>> children = new Lazy1<ImmutableArray<IOperation>>(() => boundBadStatement.ChildBoundNodes.Select(n => Create(n)).WhereNotNull().ToImmutableArray());
            SyntaxNode syntax = boundBadStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);

            // if child has syntax node point to same syntax node as bad statement, then this invalid statement is implicit
            bool isImplicit = boundBadStatement.WasCompilerGenerated || boundBadStatement.ChildBoundNodes.Any(e => e?.Syntax == boundBadStatement.Syntax);
            return new LazyInvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundLocalDeclarationOperation(BoundLocalDeclaration boundLocalDeclaration)
        {
            var node = boundLocalDeclaration.Syntax;
            var kind = node.Kind();

            SyntaxNode varStatement;
            SyntaxNode varDeclaration;
            SyntaxNode varDeclarator;
            switch (kind)
            {
                case SyntaxKind.LocalDeclarationStatement:
                {
                    var statement = (LocalDeclarationStatementSyntax)node;

                    // this happen for simple int i = 0;
                    // var statement points to LocalDeclarationStatementSyntax
                    varStatement = statement;

                    varDeclaration = statement.Declaration;

                    varDeclarator = statement.Declaration.Variables.First();
                    break;
                }
                case SyntaxKind.VariableDeclarator:
                {
                    // this happen for 'for loop' initializer
                    // We generate a DeclarationGroup for this scenario to maintain tree shape consistency across IOperation.
                    // var statement points to VariableDeclarationSyntax
                    varStatement = node.Parent;

                    varDeclaration = node.Parent;

                    // var declaration points to VariableDeclaratorSyntax
                    varDeclarator = node;
                    break;
                }
                default:
                {
                    Debug.Fail($"Unexpected syntax: {kind}");

                    // otherwise, they points to whatever bound nodes are pointing to.
                    varStatement = varDeclaration = varDeclarator = node;
                    break;
                }
            }

            Lazy1<ImmutableArray<IVariableDeclaratorOperation>> declarations = new Lazy1<ImmutableArray<IVariableDeclaratorOperation>>(() => ImmutableArray.Create(CreateVariableDeclaratorInternal(boundLocalDeclaration, varDeclarator)));
            bool multiVariableImplicit = boundLocalDeclaration.WasCompilerGenerated;
            // In C#, the MultiVariable initializer will always be null, but we can't pass null as the actual lazy. We assume that all lazy elements always exist
            Lazy1<IVariableInitializerOperation> initializer = OperationFactory.NullInitializer;
            IVariableDeclarationOperation multiVariableDeclaration = new LazyVariableDeclaration(declarations, initializer, _semanticModel, varDeclaration, null, default, multiVariableImplicit);
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            // In the case of a for loop, varStatement and varDeclaration will be the same syntax node.
            // We can only have one explicit operation, so make sure this node is implicit in that scenario.
            bool isImplicit = (varStatement == varDeclaration) || boundLocalDeclaration.WasCompilerGenerated;
            return new VariableDeclarationGroupOperation(ImmutableArray.Create(multiVariableDeclaration), _semanticModel, varStatement, type, constantValue, isImplicit);
        }

        private IVariableDeclarationGroupOperation CreateBoundMultipleLocalDeclarationsOperation(BoundMultipleLocalDeclarations boundMultipleLocalDeclarations)
        {
            Lazy1<ImmutableArray<IVariableDeclaratorOperation>> declarators = new Lazy1<ImmutableArray<IVariableDeclaratorOperation>>(() =>
                boundMultipleLocalDeclarations.LocalDeclarations.SelectAsArray(declaration => CreateVariableDeclarator(declaration)));
            // In C#, the MultiVariable initializer will always be null, but we can't pass null as the actual lazy. We assume that all lazy elements always exist
            Lazy1<IVariableInitializerOperation> initializer = OperationFactory.NullInitializer;

            // The syntax for the boundMultipleLocalDeclarations can either be a LocalDeclarationStatement or a VariableDeclaration, depending on the context
            // (using/fixed statements vs variable declaration)
            // We generate a DeclarationGroup for these scenarios (using/fixed) to maintain tree shape consistency across IOperation.
            SyntaxNode declarationGroupSyntax = boundMultipleLocalDeclarations.Syntax;
            SyntaxNode declarationSyntax = declarationGroupSyntax.IsKind(SyntaxKind.LocalDeclarationStatement) ?
                    ((LocalDeclarationStatementSyntax)declarationGroupSyntax).Declaration :
                    declarationGroupSyntax;
            bool declarationIsImplicit = boundMultipleLocalDeclarations.WasCompilerGenerated;
            IVariableDeclarationOperation multiVariableDeclaration = new LazyVariableDeclaration(declarators, initializer, _semanticModel, declarationSyntax, null, default, declarationIsImplicit);

            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            // If the syntax was the same, we're in a fixed statement or using statement. We make the Group operation implicit in this scenario, as the
            // syntax itself is a VariableDeclaration
            bool isImplicit = declarationGroupSyntax == declarationSyntax || boundMultipleLocalDeclarations.WasCompilerGenerated;
            return new VariableDeclarationGroupOperation(ImmutableArray.Create(multiVariableDeclaration), _semanticModel, declarationGroupSyntax, type, constantValue, isImplicit);
        }

        private ILabeledOperation CreateBoundLabelStatementOperation(BoundLabelStatement boundLabelStatement)
        {
            ILabelSymbol label = boundLabelStatement.Label;
            Lazy1<IOperation> statement = new Lazy1<IOperation>(() => Create(null));
            SyntaxNode syntax = boundLabelStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLabelStatement.WasCompilerGenerated;
            return new LazyLabeledStatement(label, statement, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILabeledOperation CreateBoundLabeledStatementOperation(BoundLabeledStatement boundLabeledStatement)
        {
            ILabelSymbol label = boundLabeledStatement.Label;
            Lazy1<IOperation> labeledStatement = new Lazy1<IOperation>(() => Create(boundLabeledStatement.Body));
            SyntaxNode syntax = boundLabeledStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLabeledStatement.WasCompilerGenerated;
            return new LazyLabeledStatement(label, labeledStatement, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IExpressionStatementOperation CreateBoundExpressionStatementOperation(BoundExpressionStatement boundExpressionStatement)
        {
            Lazy1<IOperation> expression = new Lazy1<IOperation>(() => Create(boundExpressionStatement.Expression));
            SyntaxNode syntax = boundExpressionStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);

            // lambda body can point to expression directly and binder can insert expression statement there. and end up statement pointing to
            // expression syntax node since there is no statement syntax node to point to. this will mark such one as implicit since it doesn't
            // actually exist in code
            bool isImplicit = boundExpressionStatement.WasCompilerGenerated || boundExpressionStatement.Syntax == boundExpressionStatement.Expression.Syntax;
            return new LazyExpressionStatement(expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundTupleLiteralOperation(BoundTupleLiteral boundTupleLiteral)
        {
            return CreateTupleOperation(boundTupleLiteral, boundTupleLiteral.Type);
        }

        private IOperation CreateBoundConvertedTupleLiteralOperation(BoundConvertedTupleLiteral boundConvertedTupleLiteral)
        {
            return CreateTupleOperation(boundConvertedTupleLiteral, boundConvertedTupleLiteral.NaturalTypeOpt);
        }

        private IOperation CreateTupleOperation(BoundTupleExpression boundTupleExpression, ITypeSymbol naturalType)
        {
            Lazy1<ImmutableArray<IOperation>> elements = new Lazy1<ImmutableArray<IOperation>>(() => boundTupleExpression.Arguments.SelectAsArray(element => Create(element)));
            SyntaxNode syntax = boundTupleExpression.Syntax;
            bool isImplicit = boundTupleExpression.WasCompilerGenerated;
            ITypeSymbol type = boundTupleExpression.Type;
            Optional<object> constantValue = default;
            if (syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                var tupleSyntax = declarationExpressionSyntax.Designation;
                Lazy1<IOperation> tupleExpression = new Lazy1<IOperation>(() => new LazyTupleExpression(elements, _semanticModel, tupleSyntax, type, naturalType, constantValue, isImplicit));
                return new LazyDeclarationExpression(tupleExpression, _semanticModel, declarationExpressionSyntax, type, constantValue: default, isImplicit: false);
            }

            return new LazyTupleExpression(elements, _semanticModel, syntax, type, naturalType, constantValue, isImplicit);
        }

        private IInterpolatedStringOperation CreateBoundInterpolatedStringExpressionOperation(BoundInterpolatedString boundInterpolatedString)
        {
            Lazy1<ImmutableArray<IInterpolatedStringContentOperation>> parts = new Lazy1<ImmutableArray<IInterpolatedStringContentOperation>>(() =>
                boundInterpolatedString.Parts.SelectAsArray(interpolatedStringContent => CreateBoundInterpolatedStringContentOperation(interpolatedStringContent)));
            SyntaxNode syntax = boundInterpolatedString.Syntax;
            ITypeSymbol type = boundInterpolatedString.Type;
            Optional<object> constantValue = ConvertToOptional(boundInterpolatedString.ConstantValue);
            bool isImplicit = boundInterpolatedString.WasCompilerGenerated;
            return new LazyInterpolatedStringExpression(parts, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInterpolatedStringContentOperation CreateBoundInterpolatedStringContentOperation(BoundNode boundNode)
        {
            if (boundNode.Kind == BoundKind.StringInsert)
            {
                return (IInterpolatedStringContentOperation)Create(boundNode);
            }
            else
            {
                return CreateBoundInterpolatedStringTextOperation((BoundLiteral)boundNode);
            }
        }

        private IInterpolationOperation CreateBoundInterpolationOperation(BoundStringInsert boundStringInsert)
        {
            Lazy1<IOperation> expression = new Lazy1<IOperation>(() => Create(boundStringInsert.Value));
            Lazy1<IOperation> alignment = new Lazy1<IOperation>(() => Create(boundStringInsert.Alignment));
            Lazy1<IOperation> format = new Lazy1<IOperation>(() => Create(boundStringInsert.Format));
            SyntaxNode syntax = boundStringInsert.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundStringInsert.WasCompilerGenerated;
            return new LazyInterpolation(expression, alignment, format, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInterpolatedStringTextOperation CreateBoundInterpolatedStringTextOperation(BoundLiteral boundNode)
        {
            Lazy1<IOperation> text = new Lazy1<IOperation>(() => CreateBoundLiteralOperation(boundNode, @implicit: true));
            SyntaxNode syntax = boundNode.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundNode.WasCompilerGenerated;
            return new LazyInterpolatedStringText(text, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConstantPatternOperation CreateBoundConstantPatternOperation(BoundConstantPattern boundConstantPattern)
        {
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundConstantPattern.Value));
            SyntaxNode syntax = boundConstantPattern.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundConstantPattern.WasCompilerGenerated;
            return new LazyConstantPattern(value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDeclarationPatternOperation CreateBoundDeclarationPatternOperation(BoundDeclarationPattern boundDeclarationPattern)
        {
            ISymbol variable = boundDeclarationPattern.Variable;
            if (variable == null && boundDeclarationPattern.VariableAccess.Kind == BoundKind.DiscardExpression)
            {
                variable = ((BoundDiscardExpression)boundDeclarationPattern.VariableAccess).ExpressionSymbol;
            }

            SyntaxNode syntax = boundDeclarationPattern.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundDeclarationPattern.WasCompilerGenerated;
            return new DeclarationPattern(variable, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ISwitchOperation CreateBoundPatternSwitchStatementOperation(BoundPatternSwitchStatement boundPatternSwitchStatement)
        {
            Lazy1<IOperation> value = new Lazy1<IOperation>(() => Create(boundPatternSwitchStatement.Expression));
            Lazy1<ImmutableArray<ISwitchCaseOperation>> cases = new Lazy1<ImmutableArray<ISwitchCaseOperation>>(() => GetPatternSwitchStatementCases(boundPatternSwitchStatement));
            ImmutableArray<ILocalSymbol> locals = boundPatternSwitchStatement.InnerLocals.As<ILocalSymbol>();
            ILabelSymbol exitLabel = boundPatternSwitchStatement.BreakLabel;
            SyntaxNode syntax = boundPatternSwitchStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundPatternSwitchStatement.WasCompilerGenerated;
            return new LazySwitchStatement(locals, value, cases, exitLabel, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICaseClauseOperation CreateBoundPatternSwitchLabelOperation(BoundPatternSwitchLabel boundPatternSwitchLabel)
        {
            SyntaxNode syntax = boundPatternSwitchLabel.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundPatternSwitchLabel.WasCompilerGenerated;
            LabelSymbol label = boundPatternSwitchLabel.Label;

            if (boundPatternSwitchLabel.Pattern.Kind == BoundKind.WildcardPattern)
            {
                // Default switch label in pattern switch statement is represented as a default case clause.
                return new DefaultCaseClause(label, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else
            {
                Lazy1<IPatternOperation> pattern = new Lazy1<IPatternOperation>(() => (IPatternOperation)Create(boundPatternSwitchLabel.Pattern));
                Lazy1<IOperation> guardExpression = new Lazy1<IOperation>(() => Create(boundPatternSwitchLabel.Guard));
                return new LazyPatternCaseClause(label, pattern, guardExpression, _semanticModel, syntax, type, constantValue, isImplicit);
            }
        }

        private IIsPatternOperation CreateBoundIsPatternExpressionOperation(BoundIsPatternExpression boundIsPatternExpression)
        {
            Lazy1<IOperation> expression = new Lazy1<IOperation>(() => Create(boundIsPatternExpression.Expression));
            Lazy1<IPatternOperation> pattern = new Lazy1<IPatternOperation>(() => (IPatternOperation)Create(boundIsPatternExpression.Pattern));
            SyntaxNode syntax = boundIsPatternExpression.Syntax;
            ITypeSymbol type = boundIsPatternExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundIsPatternExpression.ConstantValue);
            bool isImplicit = boundIsPatternExpression.WasCompilerGenerated;
            return new LazyIsPatternExpression(expression, pattern, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundQueryClauseOperation(BoundQueryClause boundQueryClause)
        {
            if (boundQueryClause.Syntax.Kind() != SyntaxKind.QueryExpression)
            {
                // Currently we have no IOperation APIs for different query clauses or continuation.
                return Create(boundQueryClause.Value);
            }

            Lazy1<IOperation> expression = new Lazy1<IOperation>(() => Create(boundQueryClause.Value));
            SyntaxNode syntax = boundQueryClause.Syntax;
            ITypeSymbol type = boundQueryClause.Type;
            Optional<object> constantValue = ConvertToOptional(boundQueryClause.ConstantValue);
            bool isImplicit = boundQueryClause.WasCompilerGenerated;
            return new LazyTranslatedQueryExpression(expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundRangeVariableOperation(BoundRangeVariable boundRangeVariable)
        {
            // We do not have operation nodes for the bound range variables, just it's value.
            return Create(boundRangeVariable.Value);
        }

        private IOperation CreateDiscardExpressionOperation(BoundDiscardExpression boundNode)
        {
            return new DiscardOperation((IDiscardSymbol)boundNode.ExpressionSymbol,
                                        _semanticModel,
                                        boundNode.Syntax,
                                        boundNode.Type,
                                        ConvertToOptional(boundNode.ConstantValue),
                                        isImplicit: boundNode.WasCompilerGenerated);
        }

        private IOperation CreateFromEndIndexExpressionOperation(BoundFromEndIndexExpression boundIndex)
        {
            return new LazyFromEndIndexOperation(
                isLifted: boundIndex.Type.IsNullableType(),
                isImplicit: boundIndex.WasCompilerGenerated,
                _semanticModel,
                boundIndex.Syntax,
                boundIndex.Type,
                operand: new Lazy1<IOperation>(() => Create(boundIndex.Operand)),
                symbol: boundIndex.MethodOpt);
        }

        private IOperation CreateRangeExpressionOperation(BoundRangeExpression boundRange)
        {
            return new LazyRangeOperation(
                isLifted: boundRange.Type.IsNullableType(),
                isImplicit: boundRange.WasCompilerGenerated,
                _semanticModel,
                boundRange.Syntax,
                boundRange.Type,
                leftOperand: new Lazy1<IOperation>(() => Create(boundRange.LeftOperand)),
                rightOperand: new Lazy1<IOperation>(() => Create(boundRange.RightOperand)),
                symbol: boundRange.MethodOpt);
        }
    }
}
