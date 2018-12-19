// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using CVM.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal sealed class NullableDirectiveMap
    {
        private static readonly NullableDirectiveMap Empty = new NullableDirectiveMap(ImmutableArray<Ps>.Empty);

        public class Ps
        {
            public int Position;
            public bool State;
            public Ps(int a,bool b)
            {
                Position = a;
                State = b;
            }
        }
        private readonly ImmutableArray<Ps> _directives;

        internal static NullableDirectiveMap Create(SyntaxTree tree)
        {
            var directives = GetDirectives(tree);
            return directives.IsEmpty ? Empty : new NullableDirectiveMap(directives);
        }

        private NullableDirectiveMap(ImmutableArray<Ps> directives)
        {
#if DEBUG
            for (int i = 1; i < directives.Length; i++)
            {
                Debug.Assert(directives[i - 1].Position < directives[i].Position);
            }
#endif
            _directives = directives;
        }

        /// <summary>
        /// Returns true if the `#nullable` directive preceding the position is
        /// `enable`, false if `disable`, and null if no preceding directive.
        /// </summary>
        internal bool? GetDirectiveState(int position)
        {
            int index = _directives.BinarySearch(new Ps(position, false), PositionComparer.Instance);
            if (index < 0)
            {
                // If no exact match, BinarySearch returns the complement
                // of the index of the next higher value.
                index = ~index - 1;
            }
            if (index < 0)
            {
                return null;
            }
            Debug.Assert(_directives[index].Position <= position);
            Debug.Assert(index == _directives.Length - 1 || position < _directives[index + 1].Position);
            return _directives[index].State;
        }

        private static ImmutableArray<Ps> GetDirectives(SyntaxTree tree)
        {
            var builder = ArrayBuilder<Ps>.GetInstance();
            foreach (var d in tree.GetRoot().GetDirectives())
            {
                if (d.Kind() != SyntaxKind.NullableDirectiveTrivia)
                {
                    continue;
                }
                var nn = (NullableDirectiveTriviaSyntax)d;
                if (nn.SettingToken.IsMissing || !nn.IsActive)
                {
                    continue;
                }
                builder.Add(new Ps(nn.Location.SourceSpan.End, nn.SettingToken.Kind() == SyntaxKind.EnableKeyword));
            }
            return builder.ToImmutableAndFree();
        }

        private sealed class PositionComparer : IComparer<Ps>
        {
            internal static readonly PositionComparer Instance = new PositionComparer();

            public int Compare(Ps x, Ps y)
            {
                return x.Position.CompareTo(y.Position);
            }
        }
    }
}
