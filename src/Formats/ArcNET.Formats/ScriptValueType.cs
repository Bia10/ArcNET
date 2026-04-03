namespace ArcNET.Formats;

/// <summary>
/// Script operand value-type tags (SVT_* enum).
/// Determines how to interpret an operand's <c>op_value</c> slot.
/// </summary>
public enum ScriptValueType : byte
{
    /// <summary>Value is a counter reference (SVT_COUNTER).</summary>
    Counter = 0,

    /// <summary>Value is a global variable index (SVT_GLOBAL_VAR).</summary>
    GlobalVar = 1,

    /// <summary>Value is a local variable index (SVT_LOCAL_VAR).</summary>
    LocalVar = 2,

    /// <summary>Value is a literal number (SVT_NUMBER).</summary>
    Number = 3,

    /// <summary>Value is a global flag index (SVT_GLOBAL_FLAG).</summary>
    GlobalFlag = 4,

    /// <summary>Value is a PC variable index (SVT_PC_VAR).</summary>
    PcVar = 5,

    /// <summary>Value is a PC flag index (SVT_PC_FLAG).</summary>
    PcFlag = 6,
}
