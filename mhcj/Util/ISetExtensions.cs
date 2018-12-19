
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static class ISetExtensions
    {
        public static bool AddAll<T>(this HashSet<T> set, IEnumerable<T> values)
        {
            var result = false;
            foreach (var v in values)
            {
                result |= set.Add(v);
            }

            return result;
        }

        public static bool RemoveAll<T>(this HashSet<T> set, IEnumerable<T> values)
        {
            var result = false;
            foreach (var v in values)
            {
                result |= set.Remove(v);
            }

            return result;
        }
    }
}
