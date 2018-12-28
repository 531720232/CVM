
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Compares objects based upon their reference identity.
    /// </summary>
    internal class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

        public static IEqualityComparer<object> iQ { get { return Instance; } }
        private ReferenceEqualityComparer()
        {
        }

        bool IEqualityComparer<object>.Equals(object a, object b)
        {
            return a == b;
        }

        int IEqualityComparer<object>.GetHashCode(object a)
        {
            return ReferenceEqualityComparer.GetHashCode(a);
        }

        public static int GetHashCode(object a)
        {
            return RuntimeHelpers.GetHashCode(a);
        }
    }
    internal class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T :class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

        public static IEqualityComparer<T> iQ { get { return Instance; } }
        private ReferenceEqualityComparer()
        {
        }

        bool IEqualityComparer<T>.Equals(T a, T b)
        {
            return a == b;
        }

        int IEqualityComparer<T>.GetHashCode(T a)
        {
            return ReferenceEqualityComparer.GetHashCode(a);
        }

        public static int GetHashCode(T a)
        {
            return RuntimeHelpers.GetHashCode(a);
        }
    }

    /// <summary>
    /// Compares objects based upon their reference identity.
    /// </summary>
    internal class ReferenceEqualityComparer2 : IEqualityComparer<TypeParameterSymbol>
    {
        public static readonly ReferenceEqualityComparer2 Instance = new ReferenceEqualityComparer2();

        public static IEqualityComparer<TypeParameterSymbol> iQ { get { return Instance; } }
        private ReferenceEqualityComparer2()
        {
        }

        bool IEqualityComparer<TypeParameterSymbol>.Equals(TypeParameterSymbol a, TypeParameterSymbol b)
        {
            return a == b;
        }

        int IEqualityComparer<TypeParameterSymbol>.GetHashCode(TypeParameterSymbol a)
        {
            return ReferenceEqualityComparer2.GetHashCode(a);
        }

        public static int GetHashCode(object a)
        {
            return RuntimeHelpers.GetHashCode(a);
        }
    }
    internal class ReferenceEqualityComparer3 : IEqualityComparer<BoundLoopStatement>
    {
        public static readonly ReferenceEqualityComparer3 Instance = new ReferenceEqualityComparer3();

        public static IEqualityComparer<BoundLoopStatement> iQ { get { return Instance; } }
        private ReferenceEqualityComparer3()
        {
        }

        bool IEqualityComparer<BoundLoopStatement>.Equals(BoundLoopStatement a, BoundLoopStatement b)
        {
            return a == b;
        }

        int IEqualityComparer<BoundLoopStatement>.GetHashCode(BoundLoopStatement a)
        {
            return ReferenceEqualityComparer3.GetHashCode(a);
        }

        public static int GetHashCode(object a)
        {
            return RuntimeHelpers.GetHashCode(a);
        }
    }
}
