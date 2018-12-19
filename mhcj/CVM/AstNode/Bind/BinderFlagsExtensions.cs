namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Extension methods for the <see cref="BinderFlags"/> type.
    /// </summary>
    internal static class BinderFlagsExtensions
    {
        public static bool Includes(this BinderFlags self, BinderFlags other)
        {
            return (self & other) == other;
        }

        public static bool IncludesAny(this BinderFlags self, BinderFlags other)
        {
            return (self & other) != 0;
        }
    }
}
