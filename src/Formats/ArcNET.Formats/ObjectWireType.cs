namespace ArcNET.Formats;

/// <summary>
/// Wire-type codes used when dispatching field reads in MOB / PRO files.
/// Corresponds to the <c>OD_TYPE_*</c> constants used for field dispatch.
/// </summary>
internal enum ObjectWireType : byte
{
    /// <summary>32-bit signed integer.</summary>
    Int32 = 0,

    /// <summary>64-bit signed integer.</summary>
    Int64 = 1,

    /// <summary>32-bit IEEE 754 float.</summary>
    Float = 2,

    /// <summary>Length-prefixed ASCII string (int32 byte-count followed by raw bytes).</summary>
    String = 3,

    /// <summary>SAR block of 32-bit signed integer elements (4 bytes each).</summary>
    Int32Array = 4,

    /// <summary>SAR block of 32-bit unsigned integer elements (4 bytes each).</summary>
    UInt32Array = 5,

    /// <summary>SAR block of 64-bit signed integer elements (8 bytes each).</summary>
    Int64Array = 6,

    /// <summary>SAR block of 24-byte ObjectID elements.</summary>
    HandleArray = 7,

    /// <summary>SAR block of 12-byte Script elements.</summary>
    ScriptArray = 8,

    /// <summary>SAR block of quest elements (element size determined from SAR header).</summary>
    QuestArray = 9,

    /// <summary>Three-byte RGB color payload.</summary>
    Rgb24 = 10,

    /// <summary>Single 24-byte ObjectID payload.</summary>
    ObjectId = 11,
}
