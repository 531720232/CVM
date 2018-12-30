// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents compilation emit options.
    /// </summary>
    public sealed class EmitOptions : IEquatable<EmitOptions>
    {
        internal static readonly EmitOptions Default =              new EmitOptions()
          ;

        /// <summary>
        /// True to emit an assembly excluding executable code such as method bodies.
        /// </summary>
        public bool EmitMetadataOnly { get; private set; }

        /// <summary>
        /// Tolerate errors, producing a PE stream and a success result even in the presence of (some) errors. 
        /// </summary>
        public bool TolerateErrors { get; private set; }

        /// <summary>
        /// Unless set (private) members that don't affect the language semantics of the resulting assembly will be excluded
        /// when emitting metadata-only assemblies as primary output (with <see cref="EmitMetadataOnly"/> on).
        /// If emitting a secondary output, this flag is required to be false.
        /// </summary>
        public bool IncludePrivateMembers { get; private set; }

        /// <summary>
        /// Type of instrumentation that should be added to the output binary.
        /// </summary>

        /// <summary>
        /// Subsystem version
        /// </summary>

        /// <summary>
        /// Specifies the size of sections in the output file. 
        /// </summary>
        /// <remarks>
        /// Valid values are 0, 512, 1024, 2048, 4096 and 8192.
        /// If the value is 0 the file alignment is determined based upon the value of <see cref="Platform"/>.
        /// </remarks>
        public int FileAlignment { get; private set; }

        /// <summary>
        /// True to enable high entropy virtual address space for the output binary.
        /// </summary>
        public bool HighEntropyVirtualAddressSpace { get; private set; }

        /// <summary>
        /// Specifies the preferred base address at which to load the output DLL.
        /// </summary>
        public ulong BaseAddress { get; private set; }

        /// <summary>
        /// Debug information format.
        /// </summary>

        /// <summary>
        /// Assembly name override - file name and extension. If not specified the compilation name is used.
        /// </summary>
        /// <remarks>
        /// By default the name of the output assembly is <see cref="Compilation.AssemblyName"/>. Only in rare cases it is necessary
        /// to override the name.
        /// 
        /// CAUTION: If this is set to a (non-null) value other than the existing compilation output name, then internals-visible-to
        /// and assembly references may not work as expected.  In particular, things that were visible at bind time, based on the 
        /// name of the compilation, may not be visible at runtime and vice-versa.
        /// </remarks>
        public string OutputNameOverride { get; private set; }

        /// <summary>
        /// The name of the PDB file to be embedded in the PE image, or null to use the default.
        /// </summary>
        /// <remarks>
        /// If not specified the file name of the source module with an extension changed to "pdb" is used.
        /// </remarks>
        public string PdbFilePath { get; private set; }

        /// <summary>
        /// A crypto hash algorithm used to calculate PDB Checksum stored in the PE/COFF File.
        /// If not specified (the value is <c>default(HashAlgorithmName)</c>) the checksum is not calculated.
        /// </summary>

        /// <summary>
        /// Runtime metadata version. 
        /// </summary>
        public string RuntimeMetadataVersion { get; private set; }

        // 1.2 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        //public EmitOptions(
        //    bool metadataOnly,
        //    string pdbFilePath,
        //    string outputNameOverride,
        //    int fileAlignment,
        //    ulong baseAddress,
        //    bool highEntropyVirtualAddressSpace,
        //    string runtimeMetadataVersion,
        //    bool tolerateErrors,
        //    bool includePrivateMembers)
        //    : this(
        //          metadataOnly,
        //          pdbFilePath,
        //          outputNameOverride,
        //          fileAlignment,
        //          baseAddress,
        //          highEntropyVirtualAddressSpace,
                
        //          runtimeMetadataVersion,
        //          tolerateErrors,
        //          includePrivateMembers
        //        )
        //{
        //}

        // 2.7 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        //public EmitOptions(
        //    bool metadataOnly,
           
        //    string pdbFilePath,
        //    string outputNameOverride,
        //    int fileAlignment,
        //    ulong baseAddress,
        //    bool highEntropyVirtualAddressSpace,
          
        //    string runtimeMetadataVersion,
        //    bool tolerateErrors,
        //    bool includePrivateMembers
        //   )
        //    : this(
        //          metadataOnly,
        //          pdbFilePath,
        //          outputNameOverride,
        //          fileAlignment,
        //          baseAddress,
        //          highEntropyVirtualAddressSpace,
        //          runtimeMetadataVersion,
        //          tolerateErrors,
        //          includePrivateMembers
        //          )
        //{
        //}

        public EmitOptions(
            bool metadataOnly = false,
            string pdbFilePath = null,
            string outputNameOverride = null,
            int fileAlignment = 0,
            ulong baseAddress = 0,
            bool highEntropyVirtualAddressSpace = false,
            string runtimeMetadataVersion = null,
            bool tolerateErrors = false,
            bool includePrivateMembers = true
           )
        {
            EmitMetadataOnly = metadataOnly;
            PdbFilePath = pdbFilePath;
            OutputNameOverride = outputNameOverride;
            FileAlignment = fileAlignment;
            BaseAddress = baseAddress;
            HighEntropyVirtualAddressSpace = highEntropyVirtualAddressSpace;
       
            RuntimeMetadataVersion = runtimeMetadataVersion;
            TolerateErrors = tolerateErrors;
            IncludePrivateMembers = includePrivateMembers;
          
        }

        private EmitOptions(EmitOptions other) : this(
            other.EmitMetadataOnly,
            other.PdbFilePath,
            other.OutputNameOverride,
            other.FileAlignment,
            other.BaseAddress,
            other.HighEntropyVirtualAddressSpace,
            other.RuntimeMetadataVersion,
            other.TolerateErrors,
            other.IncludePrivateMembers
         )
        {
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EmitOptions);
        }

        public bool Equals(EmitOptions other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return
                EmitMetadataOnly == other.EmitMetadataOnly &&
                BaseAddress == other.BaseAddress &&
                FileAlignment == other.FileAlignment &&
                HighEntropyVirtualAddressSpace == other.HighEntropyVirtualAddressSpace &&
                PdbFilePath == other.PdbFilePath &&
                OutputNameOverride == other.OutputNameOverride &&
                RuntimeMetadataVersion == other.RuntimeMetadataVersion &&
                TolerateErrors == other.TolerateErrors &&
                IncludePrivateMembers == other.IncludePrivateMembers; 
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(EmitOptions left, EmitOptions right)
        {
            return object.Equals(left, right);
        }

        public static bool operator !=(EmitOptions left, EmitOptions right)
        {
            return !object.Equals(left, right);
        }

        internal void ValidateOptions(DiagnosticBag diagnostics, CommonMessageProvider messageProvider, bool isDeterministic)
        {
        
       

            if (OutputNameOverride != null)
            {
                MetadataHelpers.CheckAssemblyOrModuleName(OutputNameOverride, messageProvider, messageProvider.ERR_InvalidOutputName, diagnostics);
            }

            if (FileAlignment != 0 && !IsValidFileAlignment(FileAlignment))
            {
                diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_InvalidFileAlignment, Location.None, FileAlignment));
            }

            else if (isDeterministic)
            {
                diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_InvalidHashAlgorithmName, Location.None, ""));
            }

          
        }


        internal static bool IsValidFileAlignment(int value)
        {
            switch (value)
            {
                case 512:
                case 1024:
                case 2048:
                case 4096:
                case 8192:
                    return true;

                default:
                    return false;
            }
        }

        public EmitOptions WithEmitMetadataOnly(bool value)
        {
            if (EmitMetadataOnly == value)
            {
                return this;
            }

            return new EmitOptions(this) { EmitMetadataOnly = value };
        }

        public EmitOptions WithPdbFilePath(string path)
        {
            if (PdbFilePath == path)
            {
                return this;
            }

            return new EmitOptions(this) { PdbFilePath = path };
        }

 

        public EmitOptions WithOutputNameOverride(string outputName)
        {
            if (OutputNameOverride == outputName)
            {
                return this;
            }

            return new EmitOptions(this) { OutputNameOverride = outputName };
        }

      

        /// <summary>
        /// Sets the byte alignment for portable executable file sections.
        /// </summary>
        /// <param name="value">Can be one of the following values: 0, 512, 1024, 2048, 4096, 8192</param>
        public EmitOptions WithFileAlignment(int value)
        {
            if (FileAlignment == value)
            {
                return this;
            }

            return new EmitOptions(this) { FileAlignment = value };
        }

        public EmitOptions WithBaseAddress(ulong value)
        {
            if (BaseAddress == value)
            {
                return this;
            }

            return new EmitOptions(this) { BaseAddress = value };
        }

        public EmitOptions WithHighEntropyVirtualAddressSpace(bool value)
        {
            if (HighEntropyVirtualAddressSpace == value)
            {
                return this;
            }

            return new EmitOptions(this) { HighEntropyVirtualAddressSpace = value };
        }

   

        public EmitOptions WithRuntimeMetadataVersion(string version)
        {
            if (RuntimeMetadataVersion == version)
            {
                return this;
            }

            return new EmitOptions(this) { RuntimeMetadataVersion = version };
        }

        public EmitOptions WithTolerateErrors(bool value)
        {
            if (TolerateErrors == value)
            {
                return this;
            }

            return new EmitOptions(this) { TolerateErrors = value };
        }

        public EmitOptions WithIncludePrivateMembers(bool value)
        {
            if (IncludePrivateMembers == value)
            {
                return this;
            }

            return new EmitOptions(this) { IncludePrivateMembers = value };
        }

    
    }
}
