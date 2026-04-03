using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="ScrFile"/> instance.
/// </summary>
public static class ScriptDumper
{
    public static string Dump(ScrFile scr)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== SCRIPT FILE ===");
        sb.AppendLine($"  Description    : \"{scr.Description}\"");
        sb.AppendLine($"  HeaderFlags    : 0x{scr.HeaderFlags:X8}");
        sb.AppendLine($"  HeaderCounters : 0x{scr.HeaderCounters:X8}");
        sb.AppendLine($"  Flags          : {scr.Flags} (0x{(uint)scr.Flags:X8})");
        sb.AppendLine($"  Entries        : {scr.Entries.Count}");
        sb.AppendLine();

        for (var i = 0; i < scr.Entries.Count; i++)
        {
            var cond = scr.Entries[i];
            var condName = FormatEnum<ScriptConditionType>(cond.Type);
            sb.AppendLine($"  --- Condition [{i}] {condName} ---");
            DumpOperands(sb, "    Cond", cond.OpTypes, cond.OpValues);
            DumpAction(sb, "    Then", cond.Action);
            DumpAction(sb, "    Else", cond.Else);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static void Dump(ScrFile scr, TextWriter writer) => writer.Write(Dump(scr));

    private static void DumpAction(StringBuilder sb, string prefix, ScriptActionData action)
    {
        var actionName = FormatEnum<ScriptActionType>(action.Type);
        sb.AppendLine($"{prefix}: {actionName}");
        DumpOperands(sb, $"{prefix}  ", action.OpTypes, action.OpValues);
    }

    private static void DumpOperands(StringBuilder sb, string prefix, byte[] opTypes, int[] opValues)
    {
        for (var j = 0; j < 8; j++)
        {
            if (opTypes[j] == 0 && opValues[j] == 0)
                continue;

            var typeName = FormatOperandType(opTypes[j]);
            sb.AppendLine($"{prefix}  op[{j}] {typeName} = {opValues[j]} (0x{opValues[j]:X8})");
        }
    }

    private static string FormatEnum<T>(int value)
        where T : struct, Enum
    {
        return Enum.IsDefined((T)(object)value) ? ((T)(object)value).ToString() : $"Unknown({value})";
    }

    private static string FormatOperandType(byte opType)
    {
        // Operand types combine two concepts: SFO (object focus) for object operands,
        // SVT (value type) for value operands. The engine uses the same byte field for both.
        // SVT values are 0-6, SFO values are 0-23. Context determines interpretation.
        // We show both when the value is in the overlapping range, otherwise show SFO.
        if (Enum.IsDefined((ScriptValueType)opType))
            return ((ScriptValueType)opType).ToString();

        if (Enum.IsDefined((ScriptFocusObject)opType))
            return ((ScriptFocusObject)opType).ToString();

        return $"OpType({opType})";
    }
}
