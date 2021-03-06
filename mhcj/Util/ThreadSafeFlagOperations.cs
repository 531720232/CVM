﻿namespace Roslyn.Utilities
{
    internal static class ThreadSafeFlagOperations
    {
        public static bool Set(ref int flags, int toSet)
        {
            int oldState, newState;
            do
            {
                oldState = flags;
                newState = oldState | toSet;
                if (newState == oldState)
                {
                    return false;
                }
            }
            while (CVM.AHelper.CompareExchange(ref flags, newState, oldState) != oldState);
            return true;
        }

        public static bool Clear(ref int flags, int toClear)
        {
            int oldState, newState;
            do
            {
                oldState = flags;
                newState = oldState & ~toClear;
                if (newState == oldState)
                {
                    return false;
                }
            }
            while (CVM.AHelper.CompareExchange(ref flags, newState, oldState) != oldState);
            return true;
        }
    }
}
