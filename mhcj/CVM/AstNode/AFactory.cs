using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
   public class AFactory
    {

        /// <summary>
        /// Gets the plain text name associated with the expression syntax node
        /// </summary>
        /// <param name="syntax">Expression syntax node</param>
        /// <returns>Plain text name</returns>
        internal static string GetName(ExpressionSyntax syntax)
        {
            string nameString;
            var nameSyntax = GetNameSyntax(syntax, out nameString);
            if (nameSyntax != null)
            {
                return nameSyntax.GetUnqualifiedName().Identifier.ValueText;
            }
            return nameString;
        }
        internal static NameSyntax GetNameSyntax(SyntaxNode syntax, out string nameString)
        {
            nameString = string.Empty;
            while (true)
            {
                switch (syntax.Kind())
                {
                    case SyntaxKind.PredefinedType:
                        nameString = ((PredefinedTypeSyntax)syntax).Keyword.ValueText;
                        return null;
                    case SyntaxKind.SimpleLambdaExpression:
                        nameString = MessageID.IDS_Lambda.Localize().ToString();
                        return null;
                    case SyntaxKind.ParenthesizedExpression:
                        syntax = ((ParenthesizedExpressionSyntax)syntax).Expression;
                        continue;
                    case SyntaxKind.CastExpression:
                        syntax = ((CastExpressionSyntax)syntax).Expression;
                        continue;
                    case SyntaxKind.SimpleMemberAccessExpression:
                    case SyntaxKind.PointerMemberAccessExpression:
                        return ((MemberAccessExpressionSyntax)syntax).Name;
                    case SyntaxKind.MemberBindingExpression:
                        return ((MemberBindingExpressionSyntax)syntax).Name;
                    default:
                        return syntax as NameSyntax;
                }
            }

        }
    }
}
