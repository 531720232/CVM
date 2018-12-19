
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class ReadOnly
        {
            internal class Enumerable<TUnderlying, T> : Enumerable<TUnderlying>, IEnumerable<T>
                where TUnderlying : IEnumerable<T>
            {
                public Enumerable(TUnderlying underlying)
                    : base(underlying)
                {
                }

                public new IEnumerator<T> GetEnumerator()
                {
                    return this.Underlying.GetEnumerator();
                }
            }
        }
    }
}
