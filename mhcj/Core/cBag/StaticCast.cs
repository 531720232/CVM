using CVM.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal static class StaticCast<T>
    {
        internal static ImmutableArray<T> From<TDerived>(ImmutableArray<TDerived> from) where TDerived : class, T
        {
            return ImmutableArray<T>.CastUp(from);
        }
    }
}
