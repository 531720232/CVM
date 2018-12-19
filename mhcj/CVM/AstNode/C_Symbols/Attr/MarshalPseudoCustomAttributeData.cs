// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from <see cref="MarshalAsAttribute"/>.
    /// </summary>
    internal sealed class MarshalPseudoCustomAttributeData
    {
        private UnmanagedType _marshalType;
        private int _marshalArrayElementType;      // safe array: VarEnum; array: UnmanagedType
        private int _marshalArrayElementCount;     // number of elements in an array, length of a string, or Unspecified
        private int _marshalParameterIndex;        // index of parameter that specifies array size (short) or IID (int), or Unspecified
        private object _marshalTypeNameOrSymbol;   // custom marshaller: string or ITypeSymbol; safe array: element type symbol
        private string _marshalCookie;

        internal const int Invalid = -1;
        private const UnmanagedType InvalidUnmanagedType = (UnmanagedType)Invalid;
        internal const int MaxMarshalInteger = 0x1fffffff;

        #region Initialization

        public MarshalPseudoCustomAttributeData()
        {
        }

        internal void SetMarshalAsCustom(object typeSymbolOrName, string cookie)
        {
            _marshalTypeNameOrSymbol = typeSymbolOrName;
            _marshalCookie = cookie;
        }

        internal void SetMarshalAsComInterface(UnmanagedType unmanagedType, int? parameterIndex)
        {
            Debug.Assert(parameterIndex == null || parameterIndex >= 0 && parameterIndex <= MaxMarshalInteger);

            _marshalType = unmanagedType;
            _marshalParameterIndex = parameterIndex ?? Invalid;
        }

        internal void SetMarshalAsArray(UnmanagedType? elementType, int? elementCount, short? parameterIndex)
        {
            Debug.Assert(elementCount == null || elementCount >= 0 && elementCount <= MaxMarshalInteger);
            Debug.Assert(parameterIndex == null || parameterIndex >= 0);

            _marshalType = UnmanagedType.LPArray;
            _marshalArrayElementType = (int)(elementType ?? (UnmanagedType)0x50);
            _marshalArrayElementCount = elementCount ?? Invalid;
            _marshalParameterIndex = parameterIndex ?? Invalid;
        }

        internal void SetMarshalAsFixedArray(UnmanagedType? elementType, int? elementCount)
        {
            Debug.Assert(elementCount == null || elementCount >= 0 && elementCount <= MaxMarshalInteger);
            Debug.Assert(elementType == null || elementType >= 0 && (int)elementType <= MaxMarshalInteger);

            _marshalType = UnmanagedType.ByValArray;
            _marshalArrayElementType = (int)(elementType ?? InvalidUnmanagedType);
            _marshalArrayElementCount = elementCount ?? Invalid;
        }

   
        internal void SetMarshalAsFixedString(int elementCount)
        {
            Debug.Assert(elementCount >= 0 && elementCount <= MaxMarshalInteger);

            _marshalType = UnmanagedType.ByValTStr;
            _marshalArrayElementCount = elementCount;
        }

        internal void SetMarshalAsSimpleType(UnmanagedType type)
        {
            Debug.Assert(type >= 0 && (int)type <= MaxMarshalInteger);
            _marshalType = type;
        }

        #endregion

        public UnmanagedType UnmanagedType
        {
            get { return _marshalType; }
        }

    
    

        /// <summary>
        /// Returns an instance of <see cref="MarshalPseudoCustomAttributeData"/> with all types replaced by types returned by specified translator.
        /// Returns this instance if it doesn't hold on any types.
        /// </summary>
        internal MarshalPseudoCustomAttributeData WithTranslatedTypes<TTypeSymbol, TArg>(
            Func<TTypeSymbol, TArg, TTypeSymbol> translator, TArg arg)
            where TTypeSymbol : ITypeSymbol
        {
            if ( _marshalTypeNameOrSymbol == null)
            {
                return this;
            }

            var translatedType = translator((TTypeSymbol)_marshalTypeNameOrSymbol, arg);
            if ((object)translatedType == (object)_marshalTypeNameOrSymbol)
            {
                return this;
            }

            var result = new MarshalPseudoCustomAttributeData();
            return result;
        }

        // testing only
        internal ITypeSymbol TryGetSafeArrayElementUserDefinedSubtype()
        {
            return _marshalTypeNameOrSymbol as ITypeSymbol;
        }
    }
}
