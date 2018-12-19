using CVM.Collections.Immutable;
using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.AstNode
{
    internal   class Node
    {
        public virtual ImmutableArray<Node> Children => ImmutableArray<Node>.Empty;

        internal readonly BoundKind _kind;
        private BoundNodeAttributes _attributes;
        public readonly SyntaxNode Syntax;
        [Flags()]
        private enum BoundNodeAttributes : byte
        {
            HasErrors = 1 << 0,
            CompilerGenerated = 1 << 1,
#if DEBUG
            /// <summary>
            /// Captures the fact that consumers of the node already checked the state of the WasCompilerGenerated bit.
            /// Allows to assert on attempts to set WasCompilerGenerated bit after that.
            /// </summary>
            WasCompilerGeneratedIsChecked = 1 << 2,
#endif
        }
        protected Node(BoundKind kind, SyntaxNode syntax)
        {
            Debug.Assert(kind == BoundKind.SequencePoint || kind == BoundKind.SequencePointExpression || syntax != null);

            _kind = kind;
            this.Syntax = syntax;
        }
        protected Node(BoundKind kind, SyntaxNode syntax, bool hasErrors)
        : this(kind, syntax)
        {
            if (hasErrors)
            {
                _attributes = BoundNodeAttributes.HasErrors;
            }
        }
        public bool HasErrors
        {
            get
            {
                return (_attributes & BoundNodeAttributes.HasErrors) != 0;
            }
        }
        /// <summary>
        /// Determines if a bound node, or associated syntax or type has an error (not a warning) 
        /// diagnostic associated with it.
        /// 
        /// Typically used in the binder as a way to prevent cascading errors. 
        /// In most other cases a more lightweight HasErrors should be used.
        /// </summary>
        public bool HasAnyErrors
        {
            get
            {
                // NOTE: check Syntax rather than WasCompilerGenerated because sequence points can have null syntax.
                if (this.HasErrors || this.Syntax != null && this.Syntax.HasErrors)
                {
                    return true;
                }
                return false;

                //var expression = this as BoundExpression;
                //return expression != null && !ReferenceEquals(expression.Type, null) && expression.Type.IsErrorType();
            }
        }
        internal void Comilper()
        {

        }

        public virtual object Eval(object thread,params object[] objs)
        {
            throw new Exception();
        }
    }
}
