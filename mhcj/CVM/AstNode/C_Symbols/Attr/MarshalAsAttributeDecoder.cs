// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    internal static class MarshalAsAttributeDecoder<TWellKnownAttributeData, TAttributeSyntax, TAttributeData, TAttributeLocation>
        where TWellKnownAttributeData : WellKnownAttributeData, IMarshalAsAttributeTarget, new()
        where TAttributeSyntax : SyntaxNode
        where TAttributeData : AttributeData
    {
        internal static void Decode(ref DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation> arguments, AttributeTargets target, CommonMessageProvider messageProvider)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            UnmanagedType unmanagedType = DecodeMarshalAsType(arguments.Attribute);

            switch (unmanagedType)
            {
                //case Cci.Constants.UnmanagedType_CustomMarshaler:
                //    DecodeMarshalAsCustom(ref arguments, messageProvider);
                //    break;

                case UnmanagedType.Interface:
                case UnmanagedType.IUnknown:
                    DecodeMarshalAsComInterface(ref arguments, unmanagedType, messageProvider);
                    break;

                case UnmanagedType.LPArray:
               //     DecodeMarshalAsArray(ref arguments, messageProvider, isFixed: false);
                    break;

                case UnmanagedType.ByValArray:
                    if (target != AttributeTargets.Field)
                    {
                  //      messageProvider.ReportMarshalUnmanagedTypeOnlyValidForFields(arguments.Diagnostics, arguments.AttributeSyntaxOpt, 0, "ByValArray", arguments.Attribute);
                    }
                    else
                    {
                //        DecodeMarshalAsArray(ref arguments, messageProvider, isFixed: true);
                    }

                    break;

               

                case UnmanagedType.ByValTStr:
                    if (target != AttributeTargets.Field)
                    {
                   //     messageProvider.ReportMarshalUnmanagedTypeOnlyValidForFields(arguments.Diagnostics, arguments.AttributeSyntaxOpt, 0, "ByValTStr", arguments.Attribute);
                    }
                    else
                    {
                   //  ..   DecodeMarshalAsFixedString(ref arguments, messageProvider);
                    }

                    break;

               
                default:
                    if ((int)unmanagedType < 0 || (int)unmanagedType > MarshalPseudoCustomAttributeData.MaxMarshalInteger)
                    {
                        // Dev10 reports CS0647: "Error emitting attribute ..."
                //  ..      messageProvider.ReportInvalidAttributeArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, 0, arguments.Attribute);
                    }
                    else
                    {
                        // named parameters ignored with no error
                        arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData().SetMarshalAsSimpleType(unmanagedType);
                    }

                    break;
            }
        }

        private static UnmanagedType DecodeMarshalAsType(AttributeData attribute)
        {
            UnmanagedType unmanagedType;
            if (attribute.AttributeConstructor.Parameters[0].Type.SpecialType == SpecialType.System_Int16)
            {
                unmanagedType = (UnmanagedType)attribute.CommonConstructorArguments[0].DecodeValue<short>(SpecialType.System_Int16);
            }
            else
            {
                unmanagedType = attribute.CommonConstructorArguments[0].DecodeValue<UnmanagedType>(SpecialType.System_Enum);
            }

            return unmanagedType;
        }

        private static void DecodeMarshalAsCustom(ref DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation> arguments, CommonMessageProvider messageProvider)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            ITypeSymbol typeSymbol = null;
            string typeName = null;
            string cookie = null;
            bool hasTypeName = false;
            bool hasTypeSymbol = false;
            bool hasErrors = false;

            int position = 1;
            foreach (var namedArg in arguments.Attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "MarshalType":
                        typeName = namedArg.Value.DecodeValue<string>(SpecialType.System_String);
                        //if (!MetadataHelpers.IsValidUnicodeString(typeName))
                        //{
                        //    messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                        //    hasErrors = true;
                        //}

                        hasTypeName = true; // even if MarshalType == null
                        break;

                    case "MarshalTypeRef":
                        typeSymbol = namedArg.Value.DecodeValue<ITypeSymbol>(SpecialType.None);
                        hasTypeSymbol = true; // even if MarshalTypeRef == null
                        break;

                    case "MarshalCookie":
                        cookie = namedArg.Value.DecodeValue<string>(SpecialType.System_String);
                        //if (!MetadataHelpers.IsValidUnicodeString(cookie))
                        //{
                        //    messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                        //    hasErrors = true;
                        //}

                        break;
                        // other parameters ignored with no error
                }

                position++;
            }

            if (!hasTypeName && !hasTypeSymbol)
            {
                // MarshalType or MarshalTypeRef must be specified:
                messageProvider.ReportAttributeParameterRequired(arguments.Diagnostics, arguments.AttributeSyntaxOpt, "MarshalType", "MarshalTypeRef");
                hasErrors = true;
            }

            if (!hasErrors)
            {
                arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData().SetMarshalAsCustom(hasTypeName ? (object)typeName : typeSymbol, cookie);
            }
        }

        private static void DecodeMarshalAsComInterface(ref DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation> arguments, UnmanagedType unmanagedType, CommonMessageProvider messageProvider)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            int? parameterIndex = null;
            int position = 1;
            bool hasErrors = false;

            foreach (var namedArg in arguments.Attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "IidParameterIndex":
                        parameterIndex = namedArg.Value.DecodeValue<int>(SpecialType.System_Int32);
                        if (parameterIndex < 0 || parameterIndex > MarshalPseudoCustomAttributeData.MaxMarshalInteger)
                        {
                       //     messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, arguments.Attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;
                        // other parameters ignored with no error
                }

                position++;
            }

            if (!hasErrors)
            {
                arguments.GetOrCreateData<TWellKnownAttributeData>().GetOrCreateData().SetMarshalAsComInterface(unmanagedType, parameterIndex);
            }
        }



    }
}
