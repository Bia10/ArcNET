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
        sb.AppendLine($"  Description : \"{scr.Description}\"");
        sb.AppendLine($"  Behaviour   : {scr.Flags}  (0x{(uint)scr.Flags:X4})");
        // Count active (non-empty) slots for the summary line
        var activeSlots = scr.Entries.Count(e =>
            !IsEmptyCondition(e) || !IsEmptyAction(e.Action) || !IsEmptyAction(e.Else)
        );
        var emptySlots = scr.Entries.Count - activeSlots;
        sb.Append($"  Entries     : {scr.Entries.Count} attachment slot(s)");
        if (activeSlots < scr.Entries.Count)
            sb.Append($"  ({activeSlots} active, {emptySlots} unused/empty)");
        sb.AppendLine();
        // HeaderFlags and HeaderCounters are runtime state only (not meaningful off-disk)
        if (scr.HeaderFlags != 0 || scr.HeaderCounters != 0)
            sb.AppendLine($"  Runtime state  : flags=0x{scr.HeaderFlags:X8}  counters=0x{scr.HeaderCounters:X8}");
        sb.AppendLine();

        for (var i = 0; i < scr.Entries.Count; i++)
        {
            var cond = scr.Entries[i];
            // Skip completely empty slots — condition=True with no operands + DoNothing action
            if (IsEmptyCondition(cond) && IsEmptyAction(cond.Action) && IsEmptyAction(cond.Else))
                continue;

            var condName = FormatEnum<ScriptConditionType>(cond.Type);
            var apName = Enum.IsDefined((ScriptAttachmentPoint)i) ? ((ScriptAttachmentPoint)i).ToString() : $"Slot{i}";
            sb.AppendLine($"  [{apName}]  when {condName}");
            DumpOperands(sb, "    condition", cond.OpTypes, cond.OpValues);
            DumpAction(sb, "      then", cond.Action);
            if (!IsEmptyAction(cond.Else))
                DumpAction(sb, "      else", cond.Else);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static void Dump(ScrFile scr, TextWriter writer) => writer.Write(Dump(scr));

    private static bool IsEmptyCondition(ScriptConditionData cond) =>
        cond.Type == (int)ScriptConditionType.True && cond.OpTypes.All(t => t == 0) && cond.OpValues.All(v => v == 0);

    private static bool IsEmptyAction(ScriptActionData action) =>
        action.Type == (int)ScriptActionType.DoNothing
        && action.OpTypes.All(t => t == 0)
        && action.OpValues.All(v => v == 0);

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
            sb.AppendLine($"{prefix}  [{j}] {typeName} = {opValues[j]}  (0x{opValues[j]:X8})");
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
