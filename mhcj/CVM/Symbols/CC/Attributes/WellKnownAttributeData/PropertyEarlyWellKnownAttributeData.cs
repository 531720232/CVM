﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from early well-known custom attributes applied on a property.
    /// </summary>
    internal sealed class PropertyEarlyWellKnownAttributeData : CommonPropertyEarlyWellKnownAttributeData
    {
        #region IndexerNameAttribute

        private string _indexerName;
        public string IndexerName
        {
            get
            {
                VerifySealed(expected: true);
                return _indexerName;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value != null);

                // This can be false if there are duplicate IndexerNameAttributes.
                // Just ignore the second one and let a later pass report an
                // appropriate diagnostic.
                if (_indexerName == null)
                {
                    _indexerName = value;
                    SetDataStored();
                }
            }
        }

        #endregion
    }
}
