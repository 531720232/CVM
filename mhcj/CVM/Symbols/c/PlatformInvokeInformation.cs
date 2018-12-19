// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information that describes how a method from the underlying Platform is to be invoked.
    /// </summary>
    public sealed class DllImportData : Cci.IPlatformInvokeInformation
    {
        private readonly string _moduleName;
        private readonly string _entryPointName;            // null if unspecified, the name of the target method should be used
        private  System.Runtime.InteropServices.CharSet _flags;

        internal DllImportData(string moduleName, string entryPointName, System.Runtime.InteropServices.CharSet flags)
        {
            _moduleName = moduleName;
            _entryPointName = entryPointName;
            _flags = flags;
        }

        /// <summary>
        /// Module name. Null if value specified in the attribute is not valid.
        /// </summary>
        public string ModuleName
        {
            get { return _moduleName; }
        }

        /// <summary>
        /// Name of the native entry point or null if not specified (the effective name is the same as the name of the target method).
        /// </summary>
        public string EntryPointName
        {
            get { return _entryPointName; }
        }

        System.Runtime.InteropServices.CharSet Cci.IPlatformInvokeInformation.Flags
        {
            get { return _flags; }
        }

        /// <summary>
        /// Controls whether the <see cref="CharacterSet"/> field causes the common language runtime 
        /// to search an unmanaged DLL for entry-point names other than the one specified.
        /// </summary>
        public bool ExactSpelling
        {
            get;set;
        }

        /// <summary>
        /// Indicates how to marshal string parameters and controls name mangling.
        /// </summary>
        public CharSet CharacterSet
        {
            get {

                return _flags;
            }
        }

        /// <summary>
        /// Indicates whether the callee calls the SetLastError Win32 API function before returning from the attributed method.
        /// </summary>
        public bool SetLastError
        {
            get; set;
        }

        /// <summary>
        /// Indicates the calling convention of an entry point.
        /// </summary>
        public CallingConvention CallingConvention
        {
            get; set;
        }

        /// <summary>
        /// Enables or disables best-fit mapping behavior when converting Unicode characters to ANSI characters.
        /// Null if not specified (the setting for the containing type or assembly should be used, <see cref="BestFitMappingAttribute"/>).
        /// </summary>
        public bool? BestFitMapping
        {
            get; set;
        }

        /// <summary>
        /// Enables or disables the throwing of an exception on an unmappable Unicode character that is converted to an ANSI "?" character.
        /// Null if not specified.
        /// </summary>
        public bool? ThrowOnUnmappableCharacter
        {
            get; set;
        }

        internal void  MakeFlags(bool exactSpelling, CharSet charSet, bool setLastError, CallingConvention callingConvention, bool? useBestFit, bool? throwOnUnmappable)
        {
            this.ExactSpelling = exactSpelling;
            _flags = charSet;
            SetLastError = setLastError;
            CallingConvention = callingConvention;

            BestFitMapping = useBestFit;
            ThrowOnUnmappableCharacter = throwOnUnmappable;
        }
    }
}
