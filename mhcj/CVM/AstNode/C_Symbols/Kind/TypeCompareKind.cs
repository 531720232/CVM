﻿
using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the different kinds of comparison between types.
    /// </summary>
    [Flags]
    internal enum TypeCompareKind
    {
        ConsiderEverything = 0,
        IgnoreCustomModifiersAndArraySizesAndLowerBounds = 1,
        IgnoreDynamic = 2,
        IgnoreTupleNames = 4,
        IgnoreDynamicAndTupleNames = IgnoreDynamic | IgnoreTupleNames,

        IgnoreNullableModifiersForReferenceTypes = 8,
        UnknownNullableModifierMatchesAny = 16,

        /// <summary>
        /// Different flavors of Nullable are equivalent,
        /// different flavors of Not-Nullable are equivalent unless the type is possibly nullable reference type parameter.
        /// This option can cause cycles in binding if used too early!
        /// </summary>
        IgnoreInsignificantNullableModifiersDifference = 32,

        AllNullableIgnoreOptions = IgnoreNullableModifiersForReferenceTypes | UnknownNullableModifierMatchesAny | IgnoreInsignificantNullableModifiersDifference,
        AllIgnoreOptions = IgnoreCustomModifiersAndArraySizesAndLowerBounds | IgnoreDynamic | IgnoreTupleNames | AllNullableIgnoreOptions,
        AllIgnoreOptionsForVB = IgnoreCustomModifiersAndArraySizesAndLowerBounds | IgnoreTupleNames
    }
}
