namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    public class AOT_AssemblySymbol : AssemblySymbol
    {
               override internal  NamedTypeSymbol GetDeclaredSpecialType(SpecialType type)
{
           switch(type)
            {
                case SpecialType.System_Object:
                    break;
            }
}
    }
}
