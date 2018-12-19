// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.SymbolDisplay
{
    internal abstract partial class AbstractSymbolDisplayVisitor : SymbolVisitor
    {
        protected readonly ArrayBuilder<SymbolDisplayPart> builder;
        protected readonly SymbolDisplayFormat format;
        protected readonly bool isFirstSymbolVisited;
        protected readonly bool inNamespaceOrType;

        protected readonly int positionOpt;

        protected readonly SemanticModel semanticModelOpt;

        private AbstractSymbolDisplayVisitor _lazyNotFirstVisitor;
        private AbstractSymbolDisplayVisitor _lazyNotFirstVisitorNamespaceOrType;

        protected AbstractSymbolDisplayVisitor(
            ArrayBuilder<SymbolDisplayPart> builder,
            SymbolDisplayFormat format,
            bool isFirstSymbolVisited,
                       SemanticModel semanticModelOpt,
            int positionOpt,
            bool inNamespaceOrType = false)
        {
            Debug.Assert(format != null);

            this.builder = builder;
            this.format = format;
            this.isFirstSymbolVisited = isFirstSymbolVisited;

            this.semanticModelOpt = semanticModelOpt;

            this.positionOpt = positionOpt;
            this.inNamespaceOrType = inNamespaceOrType;

            // If we're not the first symbol visitor, then we will just recurse into ourselves.
            if (!isFirstSymbolVisited)
            {
                _lazyNotFirstVisitor = this;
            }
        }

        protected AbstractSymbolDisplayVisitor NotFirstVisitor
        {
            get
            {
                if (_lazyNotFirstVisitor == null)
                {
                    _lazyNotFirstVisitor = MakeNotFirstVisitor();
                }

                return _lazyNotFirstVisitor;
            }
        }

        protected AbstractSymbolDisplayVisitor NotFirstVisitorNamespaceOrType
        {
            get
            {
                if (_lazyNotFirstVisitorNamespaceOrType == null)
                {
                    _lazyNotFirstVisitorNamespaceOrType = MakeNotFirstVisitor(inNamespaceOrType: true);
                }

                return _lazyNotFirstVisitorNamespaceOrType;
            }
        }

        protected abstract AbstractSymbolDisplayVisitor MakeNotFirstVisitor(bool inNamespaceOrType = false);

        protected abstract void AddLiteralValue(SpecialType type, object value);
        protected abstract void AddSpace();
        protected abstract void AddBitwiseOr();

    
    }
}
