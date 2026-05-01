using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One typed script operand used to populate a condition or action operand slot.
/// </summary>
public readonly record struct ScriptOperand(byte Type, int Value)
{
    /// <summary>
    /// Creates an operand tagged with one of the value-oriented <see cref="ScriptValueType"/> enums.
    /// </summary>
    public static ScriptOperand FromValueType(ScriptValueType type, int value) => new((byte)type, value);

    /// <summary>
    /// Creates an operand tagged with one of the object-focus <see cref="ScriptFocusObject"/> enums.
    /// </summary>
    public static ScriptOperand FromFocusObject(ScriptFocusObject focusObject, int value = 0) =>
        new((byte)focusObject, value);

    /// <summary>
    /// Creates an operand from the raw wire-level type byte when the enum surface is incomplete.
    /// </summary>
    public static ScriptOperand FromRaw(byte type, int value) => new(type, value);
}
