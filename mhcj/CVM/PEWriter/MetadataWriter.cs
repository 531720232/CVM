using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Cci
{
   internal class MetadataWriter
    {
        public static string GetMangledName(INamedTypeReference namedType)
        {
            string unmangledName = namedType.Name;

            return namedType.MangleName
                ? MetadataHelpers.ComposeAritySuffixedMetadataName(unmangledName, namedType.GenericParameterCount)
                : unmangledName;
        }

        /// <summary>
        /// Returns a reference to the unit that defines the given referenced type. If the referenced type is a structural type, such as a pointer or a generic type instance,
        /// then the result is null.
        /// </summary>
        public static IUnitReference GetDefiningUnitReference(ITypeReference typeReference, EmitContext context)
        {
            INestedTypeReference nestedTypeReference = typeReference.AsNestedTypeReference;
            while (nestedTypeReference != null)
            {
                if (nestedTypeReference.AsGenericTypeInstanceReference != null)
                {
                    return null;
                }

                typeReference = nestedTypeReference.GetContainingType(context);
                nestedTypeReference = typeReference.AsNestedTypeReference;
            }

            INamespaceTypeReference namespaceTypeReference = typeReference.AsNamespaceTypeReference;
            if (namespaceTypeReference == null)
            {
                return null;
            }

            Debug.Assert(namespaceTypeReference.AsGenericTypeInstanceReference == null);

            return namespaceTypeReference.GetUnit(context);
        }
    }
}
