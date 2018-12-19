using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BinderFactory
    {
        internal enum NodeUsage : byte
        {
            Normal = 0,
            MethodTypeParameters = 1 << 0,
            MethodBody = 1 << 1,

            ConstructorBodyOrInitializer = 1 << 0,
            AccessorBody = 1 << 0,
            OperatorBody = 1 << 0,

            NamedTypeBodyOrTypeParameters = 1 << 0,
            NamedTypeBaseList = 1 << 1,

            NamespaceBody = 1 << 0,
            NamespaceUsings = 1 << 1,

            CompilationUnitUsings = 1 << 0,
            CompilationUnitScript = 1 << 1,
            CompilationUnitScriptUsings = 1 << 2,

            DocumentationCommentParameter = 1 << 0,
            DocumentationCommentTypeParameter = 1 << 1,
            DocumentationCommentTypeParameterReference = 1 << 2,

            CrefParameterOrReturnType = 1 << 0,
        }

        // key in the binder cache.
        // PERF: we are not using ValueTuple because its Equals is relatively slow.
        private struct BinderCacheKey : IEquatable<BinderCacheKey>
        {
            public readonly CSharpSyntaxNode syntaxNode;
            public readonly NodeUsage usage;

            public BinderCacheKey(CSharpSyntaxNode syntaxNode, NodeUsage usage)
            {
                this.syntaxNode = syntaxNode;
                this.usage = usage;
            }

            bool IEquatable<BinderCacheKey>.Equals(BinderCacheKey other)
            {
                return syntaxNode == other.syntaxNode && this.usage == other.usage;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(syntaxNode.GetHashCode(), (int)usage);
            }

            public override bool Equals(object obj)
            {
                throw new NotSupportedException();
            }
        }
        // This dictionary stores contexts so we don't have to recreate them, which can be
        // expensive. 
        //private readonly ConcurrentCache<BinderCacheKey, Binder> _binderCache;
        private readonly CVM_Zone _compilation;
        private readonly SyntaxTree _syntaxTree;
        //private readonly BuckStopsHereBinder _buckStopsHereBinder;
        internal BinderFactory(CVM_Zone compilation, SyntaxTree syntaxTree)
        {
            _compilation = compilation;
            _syntaxTree = syntaxTree;

      //   .   _binderFactoryVisitorPool = new ObjectPool<BinderFactoryVisitor>(() => new BinderFactoryVisitor(this), 64);

            // 50 is more or less a guess, but it seems to work fine for scenarios that I tried.
            // we need something big enough to keep binders for most classes and some methods 
            // in a typical syntax tree.
            // On the other side, note that the whole factory is weakly referenced and therefore short lived, 
            // making this cache big is not very useful.
            // I noticed that while compiling Roslyn C# compiler most caches never see 
            // more than 50 items added before getting collected.
       //     _binderCache = new ConcurrentCache<BinderCacheKey, Binder>(50);

        //    _buckStopsHereBinder = new BuckStopsHereBinder(compilation);
        }
        internal SyntaxTree SyntaxTree
        {
            get
            {
                return _syntaxTree;
            }
        }

        private bool InScript
        {
            get
            {
                return _syntaxTree.Options.Kind == SourceCodeKind.Script;
            }
        }
        private readonly ObjectPool<BinderFactoryVisitor> _binderFactoryVisitorPool;

        /// <summary>
        /// Returns binder that binds usings and aliases 
        /// </summary>
        /// <param name="unit">
        /// Specify <see cref="NamespaceDeclarationSyntax"/> imports in the corresponding namespace, or
        /// <see cref="CompilationUnitSyntax"/> for top-level imports.
        /// </param>
        /// <param name="inUsing">True if the binder will be used to bind a using directive.</param>
        internal object GetImportsBinder(CSharpSyntaxNode unit, bool inUsing = false)
        {
            switch (unit.Kind())
            {
                case SyntaxKind.NamespaceDeclaration:
                    {
                        BinderFactoryVisitor visitor = _binderFactoryVisitorPool.Allocate();
                        visitor.Initialize(0, null);
                        var result = visitor.VisitNamespaceDeclaration((NamespaceDeclarationSyntax)unit, unit.SpanStart, inBody: true, inUsing: inUsing);
                        _binderFactoryVisitorPool.Free(visitor);
                        return result;
                    }

                case SyntaxKind.CompilationUnit:
                    // imports are bound by the Script class binder:
                    {
                        BinderFactoryVisitor visitor = _binderFactoryVisitorPool.Allocate();
                        visitor.Initialize(0, null);
                        var result = visitor.VisitCompilationUnit((CompilationUnitSyntax)unit, inUsing: inUsing, inScript: InScript);
                        _binderFactoryVisitorPool.Free(visitor);
                        return result;
                    }

                default:
                    return null;
            }
        }

    }
}
