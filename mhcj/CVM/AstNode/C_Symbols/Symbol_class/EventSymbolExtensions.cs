namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class EventSymbolExtensions
    {
        internal static MethodSymbol GetOwnOrInheritedAccessor(this EventSymbol @event, bool isAdder)
        {
            return isAdder
                ? @event.GetOwnOrInheritedAddMethod()
                : @event.GetOwnOrInheritedRemoveMethod();
        }
    }
}
