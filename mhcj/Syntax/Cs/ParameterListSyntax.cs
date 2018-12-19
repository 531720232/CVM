namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ParameterListSyntax
    {
        internal int ParameterCount
        {
            get
            {
                int count = 0;
                foreach (ParameterSyntax parameter in this.Parameters)
                {
                    // __arglist does not affect the parameter count.
                    if (!parameter.IsArgList)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }
}
