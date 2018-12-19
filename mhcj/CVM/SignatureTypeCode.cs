using System;

namespace CVM
{
    //
    // 摘要:
    //     Specifies additional flags that can be applied to method signatures. The underlying
    //     values of the fields in this type correspond to the representation in the leading
    //     signature byte represented by a System.Reflection.Metadata.SignatureHeader structure.
    [Flags]
    public enum SignatureAttributes : byte
    {
        //
        // 摘要:
        //     No flags.
        None = 0,
        //
        // 摘要:
        //     A generic method.
        Generic = 16,
        //
        // 摘要:
        //     An instance method.
        Instance = 32,
        //
        // 摘要:
        //     Indicates the first explicitly declared parameter that represents the instance
        //     pointer.
        ExplicitThis = 64
    }
    //
    // 摘要:
    //     Specifies constants that define type codes used in signature encoding.
    public enum SignatureTypeCode : byte
    {
        //
        // 摘要:
        //     Represents an invalid or uninitialized type code. It will not appear in valid
        //     signatures.
        Invalid = 0,
        //
        // 摘要:
        //     Represents System.Void in signatures.
        Void = 1,
        //
        // 摘要:
        //     Represents a System.Boolean in signatures.
        Boolean = 2,
        //
        // 摘要:
        //     Represents a System.Char in signatures.
        Char = 3,
        //
        // 摘要:
        //     Represents an System.SByte in signatures.
        SByte = 4,
        //
        // 摘要:
        //     Represents a System.Byte in signatures.
        Byte = 5,
        //
        // 摘要:
        //     Represents an System.Int16 in signatures.
        Int16 = 6,
        //
        // 摘要:
        //     Represents a System.UInt16 in signatures.
        UInt16 = 7,
        //
        // 摘要:
        //     Represents an System.Int32 in signatures.
        Int32 = 8,
        //
        // 摘要:
        //     Represents a System.UInt32 in signatures.
        UInt32 = 9,
        //
        // 摘要:
        //     Represents an System.Int64 in signatures.
        Int64 = 10,
        //
        // 摘要:
        //     Represents a System.UInt64 in signatures.
        UInt64 = 11,
        //
        // 摘要:
        //     Represents a System.Single in signatures.
        Single = 12,
        //
        // 摘要:
        //     Represents a System.Double in signatures.
        Double = 13,
        //
        // 摘要:
        //     Represents a System.String in signatures.
        String = 14,
        //
        // 摘要:
        //     Represents an unmanaged pointer in signatures. It is followed in the blob by
        //     the signature encoding of the underlying type.
        Pointer = 15,
        //
        // 摘要:
        //     Represents managed pointers (byref return values and parameters) in signatures.
        //     It is followed in the blob by the signature encoding of the underlying type.
        ByReference = 16,
        //
        // 摘要:
        //     Represents a generic type parameter used within a signature.
        GenericTypeParameter = 19,
        //
        // 摘要:
        //     Represents a generalized System.Array in signatures.
        Array = 20,
        //
        // 摘要:
        //     Represents the instantiation of a generic type in signatures.
        GenericTypeInstance = 21,
        //
        // 摘要:
        //     Represents a typed reference in signatures.
        TypedReference = 22,
        //
        // 摘要:
        //     Represents an System.IntPtr in signatures.
        IntPtr = 24,
        //
        // 摘要:
        //     Represents a System.UIntPtr in signatures.
        UIntPtr = 25,
        //
        // 摘要:
        //     Represents function pointer types in signatures.
        FunctionPointer = 27,
        //
        // 摘要:
        //     Represents an System.Object in signatures.
        Object = 28,
        //
        // 摘要:
        //     Represents a single dimensional System.Array with a lower bound of 0.
        SZArray = 29,
        //
        // 摘要:
        //     Represents a generic method parameter used within a signature.
        GenericMethodParameter = 30,
        //
        // 摘要:
        //     Represents a custom modifier applied to a type within a signature that the caller
        //     must understand.
        RequiredModifier = 31,
        //
        // 摘要:
        //     Represents a custom modifier applied to a type within a signature that the caller
        //     can ignore.
        OptionalModifier = 32,
        //
        // 摘要:
        //     Precedes a type EntityHandle in signatures.
        TypeHandle = 64,
        //
        // 摘要:
        //     Represents a marker to indicate the end of fixed arguments and the beginning
        //     of variable arguments.
        Sentinel = 65,
        //
        // 摘要:
        //     Represents a local variable that is pinned by garbage collector.
        Pinned = 69
    }

    //
    // 摘要:
    //     Specifies type codes used to encode the types of values in a System.Reflection.Metadata.CustomAttributeValue`1
    //     blob.
    public enum SerializationTypeCode : byte
    {
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.Invalid.
        Invalid = 0,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.Boolean.
        Boolean = 2,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.Char.
        Char = 3,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.SByte.
        SByte = 4,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.Byte.
        Byte = 5,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.Int16.
        Int16 = 6,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.UInt16.
        UInt16 = 7,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.Int32.
        Int32 = 8,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.UInt32.
        UInt32 = 9,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.Int64.
        Int64 = 10,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.UInt64.
        UInt64 = 11,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.Single.
        Single = 12,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.Double.
        Double = 13,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.String.
        String = 14,
        //
        // 摘要:
        //     A value equivalent to SignatureTypeCode.SZArray.
        SZArray = 29,
        //
        // 摘要:
        //     The attribute argument is a System.Type instance.
        Type = 80,
        //
        // 摘要:
        //     The attribute argument is &quot;boxed&quot; (passed to a parameter, field, or
        //     property of type object) and carries type information in the attribute blob.
        TaggedObject = 81,
        //
        // 摘要:
        //     The attribute argument is an Enum instance.
        Enum = 85
    }
}
