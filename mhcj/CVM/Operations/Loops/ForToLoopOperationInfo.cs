// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Operations
{
    internal class ForToLoopOperationUserDefinedInfo
    {
        public readonly Lazy1<IBinaryOperation> Addition;
        public readonly Lazy1<IBinaryOperation> Subtraction;
        public readonly Lazy1<IOperation> LessThanOrEqual;
        public readonly Lazy1<IOperation> GreaterThanOrEqual;

        public ForToLoopOperationUserDefinedInfo(Lazy1<IBinaryOperation> addition, Lazy1<IBinaryOperation> subtraction, Lazy1<IOperation> lessThanOrEqual, Lazy1<IOperation> greaterThanOrEqual)
        {
            Addition = addition;
            Subtraction = subtraction;
            LessThanOrEqual = lessThanOrEqual;
            GreaterThanOrEqual = greaterThanOrEqual;
        }
    }
}
