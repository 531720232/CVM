
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
}
