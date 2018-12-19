namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This is a marker attribute that can be put on an interface to denote that only internal implementations
    /// of that interface should exist.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    internal sealed class InternalImplementationOnlyAttribute : Attribute
    {
    }
}
