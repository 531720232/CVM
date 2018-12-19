using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the value of #r reference along with its source location.
    /// </summary>
    internal struct ReferenceDirective
    {
        public readonly string File;
        public readonly Location Location;

        public ReferenceDirective(string file, Location location)
        {
            Debug.Assert(file != null);
            Debug.Assert(location != null);

            File = file;
            Location = location;
        }
    }
}
