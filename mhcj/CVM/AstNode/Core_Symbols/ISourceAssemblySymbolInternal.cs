﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Mono.Cecil;
using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    internal interface ISourceAssemblySymbolInternal : ISourceAssemblySymbol
    {
        AssemblyAttributes AssemblyFlags { get; }

        /// <summary>
        /// The contents of the AssemblySignatureKeyAttribute
        /// </summary>
        string SignatureKey { get; }

        AssemblyHashAlgorithm HashAlgorithm { get; }

        Version AssemblyVersionPattern { get; }

        bool InternalsAreVisible { get; }
    }
}
