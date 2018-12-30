using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class A2Factory
    {
    private static CVM.Collections.Concurrent.ConcurrentDictionary<Guid, List<string>> usings = new CVM.Collections.Concurrent.ConcurrentDictionary<Guid, List<string>>();

      
        internal static void AddUsing(ToAster a,string us)
        {
            if (!usings.TryGetValue(a.guid, out var sl))
            {
               
                usings[a.guid] = new List<string>(new string[] { us});
            }
            else
            {
                sl.Add(us);
            }
            }
        internal static void AddUsings(ToAster a,params string[] us)
        {
            if (!usings.TryGetValue(a.guid, out var sl))
            {

                usings[a.guid] = new List<string>( us );
            }
            else
            {
                sl.AddRange(us);
            }
        }
    }
}
