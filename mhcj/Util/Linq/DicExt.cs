using System;
using System.Collections.Generic;

namespace CVM.Linq
{
    public static  class DicExt
    {
        public static K GetOrAdd<T,K>(this Dictionary<T,K> dic, T syntaxTree, Func<T,K> s_createSetCallback)
        {
            var item = s_createSetCallback.Invoke(syntaxTree);
            if(dic.ContainsKey(syntaxTree))
            {
                dic[syntaxTree] = item;
               
            }
            else
            {
                dic.Add(syntaxTree, item);
               
            }
            return item;
        }
    }
}
