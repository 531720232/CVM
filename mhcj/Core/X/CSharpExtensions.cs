﻿using CVM.Collections.Immutable;

using System;
using System.Collections.Generic;
using CVM;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Syntax;

namespace Microsoft.CodeAnalysis
{
    public static class CSharpExtensions
    {
        public static bool IsKind(this SyntaxToken token, SyntaxKind kind)
        {
            return token.RawKind == (int)kind;
        }

        public static bool IsKind(this SyntaxTrivia trivia, SyntaxKind kind)
        {
            return trivia.RawKind == (int)kind;
        }

        public static bool IsKind(this SyntaxNode node, SyntaxKind kind)
        {
            return node?.RawKind == (int)kind;
        }

        public static bool IsKind(this SyntaxNodeOrToken nodeOrToken, SyntaxKind kind)
        {
            return nodeOrToken.RawKind == (int)kind;
        }

        internal static SyntaxKind ContextualKind(this SyntaxToken token)
        {
            return (object)token.Language == (object)"C#" ? (SyntaxKind)token.RawContextualKind : SyntaxKind.None;
        }

        /// <summary>
        /// Returns the index of the first node of a specified kind in the node list.
        /// </summary>
        /// <param name="list">Node list.</param>
        /// <param name="kind">The <see cref="SyntaxKind"/> to find.</param>
        /// <returns>Returns non-negative index if the list contains a node which matches <paramref name="kind"/>, -1 otherwise.</returns>
        public static int IndexOf<TNode>(this SyntaxList<TNode> list, SyntaxKind kind)
            where TNode : SyntaxNode
        {
            return list.IndexOf((int)kind);
        }

        /// <summary>
        /// True if the list has at least one node of the specified kind.
        /// </summary>
        public static bool Any<TNode>(this SyntaxList<TNode> list, SyntaxKind kind)
            where TNode : SyntaxNode
        {
            return list.IndexOf(kind) >= 0;
        }

        /// <summary>
        /// Returns the index of the first node of a specified kind in the node list.
        /// </summary>
        /// <param name="list">Node list.</param>
        /// <param name="kind">The <see cref="SyntaxKind"/> to find.</param>
        /// <returns>Returns non-negative index if the list contains a node which matches <paramref name="kind"/>, -1 otherwise.</returns>
        public static int IndexOf<TNode>(this SeparatedSyntaxList<TNode> list, SyntaxKind kind)
            where TNode : SyntaxNode
        {
            return list.IndexOf((int)kind);
        }

        /// <summary>
        /// True if the list has at least one node of the specified kind.
        /// </summary>
        public static bool Any<TNode>(this SeparatedSyntaxList<TNode> list, SyntaxKind kind)
            where TNode : SyntaxNode
        {
            return list.IndexOf(kind) >= 0;
        }

        /// <summary>
        /// Returns the index of the first trivia of a specified kind in the trivia list.
        /// </summary>
        /// <param name="list">Trivia list.</param>
        /// <param name="kind">The <see cref="SyntaxKind"/> to find.</param>
        /// <returns>Returns non-negative index if the list contains a trivia which matches <paramref name="kind"/>, -1 otherwise.</returns>
        public static int IndexOf(this SyntaxTriviaList list, SyntaxKind kind)
        {
            return list.IndexOf((int)kind);
        }

        /// <summary>
        /// True if the list has at least one trivia of the specified kind.
        /// </summary>
        public static bool Any(this SyntaxTriviaList list, SyntaxKind kind)
        {
            return list.IndexOf(kind) >= 0;
        }

        /// <summary>
        /// Returns the index of the first token of a specified kind in the token list.
        /// </summary>
        /// <param name="list">Token list.</param>
        /// <param name="kind">The <see cref="SyntaxKind"/> to find.</param>
        /// <returns>Returns non-negative index if the list contains a token which matches <paramref name="kind"/>, -1 otherwise.</returns>
        public static int IndexOf(this SyntaxTokenList list, SyntaxKind kind)
        {
            return list.IndexOf((int)kind);
        }

        /// <summary>
        /// Tests whether a list contains a token of a particular kind.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="kind">The <see cref="CSharp.SyntaxKind"/> to test for.</param>
        /// <returns>Returns true if the list contains a token which matches <paramref name="kind"/></returns>
        public static bool Any(this SyntaxTokenList list, SyntaxKind kind)
        {
            return list.IndexOf(kind) >= 0;
        }

        internal static SyntaxToken FirstOrDefault(this SyntaxTokenList list, SyntaxKind kind)
        {
            int index = list.IndexOf(kind);
            return (index >= 0) ? list[index] : default(SyntaxToken);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public static class CSharpExtensions
    {
        internal static bool IsCSharpKind(int rawKind)
        {
            const int FirstVisualBasicKind = (int)SyntaxKind.List + 1;
            const int FirstCSharpKind = (int)SyntaxKind.TildeToken;

            // not in the range [FirstVisualBasicKind, FirstCSharpKind)
            return unchecked((uint)(rawKind - FirstVisualBasicKind)) > (FirstCSharpKind - 1 - FirstVisualBasicKind);
        }

        public static SyntaxKind Kind(this SyntaxToken token)
        {
            var rawKind = token.RawKind;
            return IsCSharpKind(rawKind) ? (SyntaxKind)rawKind : SyntaxKind.None;
        }

        public static SyntaxKind Kind(this SyntaxTrivia trivia)
        {
            var rawKind = trivia.RawKind;
            return IsCSharpKind(rawKind) ? (SyntaxKind)rawKind : SyntaxKind.None;
        }

        public static SyntaxKind Kind(this SyntaxNode node)
        {
            var rawKind = node.RawKind;
            return IsCSharpKind(rawKind) ? (SyntaxKind)rawKind : SyntaxKind.None;
        }

        public static SyntaxKind Kind(this SyntaxNodeOrToken nodeOrToken)
        {
            var rawKind = nodeOrToken.RawKind;
            return IsCSharpKind(rawKind) ? (SyntaxKind)rawKind : SyntaxKind.None;
        }

        public static bool IsKeyword(this SyntaxToken token)
        {
            return SyntaxFacts.IsKeywordKind(token.Kind());
        }

        public static bool IsContextualKeyword(this SyntaxToken token)
        {
            return SyntaxFacts.IsContextualKeyword(token.Kind());
        }

        public static bool IsReservedKeyword(this SyntaxToken token)
        {
            return SyntaxFacts.IsReservedKeyword(token.Kind());
        }

        public static bool IsVerbatimStringLiteral(this SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.StringLiteralToken) && token.Text.Length > 0 && token.Text[0] == '@';
        }

        public static bool IsVerbatimIdentifier(this SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.IdentifierToken) && token.Text.Length > 0 && token.Text[0] == '@';
        }

        public static VarianceKind VarianceKindFromToken(this SyntaxToken node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.OutKeyword: return VarianceKind.Out;
                case SyntaxKind.InKeyword: return VarianceKind.In;
                default: return VarianceKind.None;
            }
        }

        /// <summary>
        /// Insert one or more tokens in the list at the specified index.
        /// </summary>
        /// <returns>A new list with the tokens inserted.</returns>
        public static SyntaxTokenList Insert(this SyntaxTokenList list, int index, params SyntaxToken[] items)
        {
            if (index < 0 || index > list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (list.Count == 0)
            {
                return SyntaxFactory.TokenList(items);
            }
            else
            {
                var builder = new SyntaxTokenListBuilder(list.Count + items.Length);
                if (index > 0)
                {
                    builder.Add(list, 0, index);
                }

                builder.Add(items);

                if (index < list.Count)
                {
                    builder.Add(list, index, list.Count - index);
                }

                return builder.ToList();
            }
        }

        /// <summary>
        /// Creates a new token with the specified old trivia replaced with computed new trivia.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="trivia">The trivia to be replaced; descendants of the root token.</param>
        /// <param name="computeReplacementTrivia">A function that computes a replacement trivia for
        /// the argument trivia. The first argument is the original trivia. The second argument is
        /// the same trivia rewritten with replaced structure.</param>
        public static SyntaxToken ReplaceTrivia(this SyntaxToken token, IEnumerable<SyntaxTrivia> trivia, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia)
        {
            return Syntax.SyntaxReplacer.Replace(token, trivia: trivia, computeReplacementTrivia: computeReplacementTrivia);
        }

        /// <summary>
        /// Creates a new token with the specified old trivia replaced with a new trivia. The old trivia may appear in
        /// the token's leading or trailing trivia.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="oldTrivia">The trivia to be replaced.</param>
        /// <param name="newTrivia">The new trivia to use in the new tree in place of the old
        /// trivia.</param>
        public static SyntaxToken ReplaceTrivia(this SyntaxToken token, SyntaxTrivia oldTrivia, SyntaxTrivia newTrivia)
        {
            return Syntax.SyntaxReplacer.Replace(token, trivia: new[] { oldTrivia }, computeReplacementTrivia: (o, r) => newTrivia);
        }

        internal static Syntax.InternalSyntax.DirectiveStack ApplyDirectives(this SyntaxNode node, Syntax.InternalSyntax.DirectiveStack stack)
        {
            return ((Syntax.InternalSyntax.CSharpSyntaxNode)node.Green).ApplyDirectives(stack);
        }

        internal static Syntax.InternalSyntax.DirectiveStack ApplyDirectives(this SyntaxToken token, Syntax.InternalSyntax.DirectiveStack stack)
        {
            return ((Syntax.InternalSyntax.CSharpSyntaxNode)token.Node).ApplyDirectives(stack);
        }

        internal static Syntax.InternalSyntax.DirectiveStack ApplyDirectives(this SyntaxNodeOrToken nodeOrToken, Syntax.InternalSyntax.DirectiveStack stack)
        {
            if (nodeOrToken.IsToken)
            {
                return nodeOrToken.AsToken().ApplyDirectives(stack);
            }

            if (nodeOrToken.IsNode)
            {
                return nodeOrToken.AsNode().ApplyDirectives(stack);
            }

            return stack;
        }

        /// <summary>
        /// Returns this list as a <see cref="Microsoft.CodeAnalysis.SeparatedSyntaxList&lt;TNode&gt;"/>.
        /// </summary>
        /// <typeparam name="TOther">The type of the list elements in the separated list.</typeparam>
        /// <returns></returns>
        internal static SeparatedSyntaxList<TOther> AsSeparatedList<TOther>(this SyntaxNodeOrTokenList list) where TOther : SyntaxNode
        {
            var builder = SeparatedSyntaxListBuilder<TOther>.Create();
            foreach (var i in list)
            {
                var node = i.AsNode();
                if (node != null)
                {
                    builder.Add((TOther)node);
                }
                else
                {
                    builder.AddSeparator(i.AsToken());
                }
            }

            return builder.ToList();
        }

        #region SyntaxNode 
        internal static IList<DirectiveTriviaSyntax> GetDirectives(this SyntaxNode node, Func<DirectiveTriviaSyntax, bool> filter = null)
        {
            return ((CSharpSyntaxNode)node).GetDirectives(filter);
        }

        /// <summary>
        /// Gets the first directive of the tree rooted by this node.
        /// </summary>
        public static DirectiveTriviaSyntax GetFirstDirective(this SyntaxNode node, Func<DirectiveTriviaSyntax, bool> predicate = null)
        {
            return ((CSharpSyntaxNode)node).GetFirstDirective(predicate);
        }

        /// <summary>
        /// Gets the last directive of the tree rooted by this node.
        /// </summary>
        public static DirectiveTriviaSyntax GetLastDirective(this SyntaxNode node, Func<DirectiveTriviaSyntax, bool> predicate = null)
        {
            return ((CSharpSyntaxNode)node).GetLastDirective(predicate);
        }
        #endregion  

        #region SyntaxTree 
        public static CompilationUnitSyntax GetCompilationUnitRoot(this SyntaxTree tree, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (CompilationUnitSyntax)tree.GetRoot(cancellationToken);
        }

        internal static bool HasReferenceDirectives(this SyntaxTree tree)
        {
            var csharpTree = tree as CSharpSyntaxTree;
            return csharpTree != null && csharpTree.HasReferenceDirectives;
        }

        internal static bool HasReferenceOrLoadDirectives(this SyntaxTree tree)
        {
            var csharpTree = tree as CSharpSyntaxTree;
            return csharpTree != null && csharpTree.HasReferenceOrLoadDirectives;
        }

        internal static bool IsAnyPreprocessorSymbolDefined(this SyntaxTree tree, ImmutableArray<string> conditionalSymbols)
        {
            var csharpTree = tree as CSharpSyntaxTree;
            return csharpTree != null && csharpTree.IsAnyPreprocessorSymbolDefined(conditionalSymbols);
        }

        internal static bool IsPreprocessorSymbolDefined(this SyntaxTree tree, string symbolName, int position)
        {
            var csharpTree = tree as CSharpSyntaxTree;
            return csharpTree != null && csharpTree.IsPreprocessorSymbolDefined(symbolName, position);
        }

        // Given the error code and the source location, get the warning state based on pragma warning directives.
        internal static ReportDiagnostic GetPragmaDirectiveWarningState(this SyntaxTree tree, string id, int position)
        {
            return ((CSharpSyntaxTree)tree).GetPragmaDirectiveWarningState(id, position);
        }
        #endregion


  
    }
}
