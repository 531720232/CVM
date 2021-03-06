﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    internal class StringTextWriter : SourceTextWriter
    {
        private readonly StringBuilder _builder;
        private readonly Encoding _encoding;
        private readonly SourceHashAlgorithm _checksumAlgorithm;

        public StringTextWriter(Encoding encoding, SourceHashAlgorithm checksumAlgorithm, int capacity)
        {
            _builder = new StringBuilder(capacity);
            _encoding = encoding;
            _checksumAlgorithm = checksumAlgorithm;
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        public override SourceText ToSourceText()
        {
            return new StringText(_builder.ToString(), _encoding, checksumAlgorithm: _checksumAlgorithm);
        }

        public override void Write(char value)
        {
            _builder.Append(value);
        }

        public override void Write(string value)
        {
            _builder.Append(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _builder.Append(buffer, index, count);
        }
    }
}
