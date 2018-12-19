﻿namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies how to display extension methods.
    /// </summary>
    public enum SymbolDisplayExtensionMethodStyle
    {
        /// <summary>
        /// Displays the extension method based on its <see cref="MethodKind"/>.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Displays the extension method in the form of an instance method. 
        /// For example, IEnumerable&lt;TSource&gt;.ElementAt&lt;TSource&gt;(int index).
        /// </summary>
        InstanceMethod = 1,

        /// <summary>
        /// Displays the extension method as a static method. 
        /// For example, Enumerable.ElementAt&lt;TSource&gt;(this IEnumerable&lt;TSource&gt; source, int index).
        /// </summary>
        StaticMethod = 2
    }
}
