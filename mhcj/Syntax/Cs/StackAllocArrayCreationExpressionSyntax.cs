﻿
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class StackAllocArrayCreationExpressionSyntax
    {
        public StackAllocArrayCreationExpressionSyntax Update(SyntaxToken stackAllocKeyword, TypeSyntax type)
        {
            return Update(StackAllocKeyword, type, default(InitializerExpressionSyntax));
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static StackAllocArrayCreationExpressionSyntax StackAllocArrayCreationExpression(SyntaxToken stackAllocKeyword, TypeSyntax type)
        {
            return StackAllocArrayCreationExpression(stackAllocKeyword, type, default(InitializerExpressionSyntax));
        }
    }
}
