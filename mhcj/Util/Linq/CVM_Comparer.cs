using System;
using System.Collections.Generic;


public class CVM_Comparer<T> : Comparer<T>
{
    public static IComparer<T> Create(Comparison<T> comparison)
    {
        var obj = new CVM_Comparer<T>();
        obj.action = comparison;
        return obj;
    }
    private Comparison<T> action;
    public override int Compare(T x, T y)
    {
        return action.Invoke(x, y);
       // throw new NotImplementedException();
    }
}

