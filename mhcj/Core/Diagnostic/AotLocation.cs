using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    ///
    /// </summary>
    internal sealed class AotLocation : Location, IEquatable<AotLocation>
    {

        public override int GetHashCode()
        {
            return 0;
        }
        public override LocationKind Kind
        {
            get { return LocationKind.MetadataFile; }
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as AotLocation);
        }

        public bool Equals(AotLocation other)
        {
            return other != null                 ;
        }
    }
}
