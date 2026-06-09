namespace ArcNET.Diagnostics;

public static class QuestStateFormatter
{
    public static string Format(int state) =>
        state switch
        {
            0 => "known",
            1 => "active",
            2 => "completed(primary)",
            4 => "completed(secondary)",
            _ => FormatBits(state),
        };

    private static string FormatBits(int state)
    {
        List<string> parts = [];
        if ((state & 0x001) != 0)
            parts.Add("active");
        if ((state & 0x002) != 0)
            parts.Add("completed(primary)");
        if ((state & 0x004) != 0)
            parts.Add("completed(secondary)");
        if ((state & 0x100) != 0)
            parts.Add("bit8?");

        var unknownMask = state & ~0x107;
        if (unknownMask != 0)
            parts.Add($"0x{unknownMask:X}");

        if (parts.Count == 0)
            return $"0x{state:X3}";

        return $"{string.Join('|', parts)} [0x{state:X3}]";
    }
}
