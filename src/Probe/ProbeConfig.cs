namespace Probe;

internal static class ProbeConfig
{
    private const string SaveDirArg = "--save-dir";
    private const string StdoutArg = "--stdout";
    private const string OutputPathArg = "--out";
    private const string SlotStemPrefix = "Slot";

    public static string ResolveSaveDir(string[] args)
    {
        var index = Array.IndexOf(args, SaveDirArg);
        if (index >= 0 && index + 1 < args.Length)
            return args[index + 1];

        var env = Environment.GetEnvironmentVariable("ARCNET_SAVE_DIR");
        if (!string.IsNullOrEmpty(env))
            return env;

        return @"C:\Games\Arcanum\ArcanumCleanUAPnohighres - Copy\modules\Arcanum\Save";
    }

    public static ProbeOutputOptions ResolveOutputOptions(string[] args)
    {
        var forceStdout = Array.IndexOf(args, StdoutArg) >= 0;
        var outputPathIndex = Array.IndexOf(args, OutputPathArg);
        var outputPath = outputPathIndex >= 0 && outputPathIndex + 1 < args.Length ? args[outputPathIndex + 1] : null;

        return new ProbeOutputOptions(forceStdout, outputPath);
    }

    public static string[] StripGlobalArgs(string[] args)
    {
        if (args.Length == 0)
            return args;

        var stripped = new List<string>(args.Length);
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg == StdoutArg)
                continue;

            if (arg == SaveDirArg || arg == OutputPathArg)
            {
                if (index + 1 < args.Length)
                    index++;

                continue;
            }

            stripped.Add(arg);
        }

        return [.. stripped];
    }

    public static (int FirstSlot, int LastSlot) ResolveRecentSlotRange(string saveDir, int width, int fallbackSlot = 13)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        var slots = Directory
            .GetFiles(saveDir, SlotStemPrefix + "*.gsi")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(TryParseSlotStem)
            .Where(static slot => slot.HasValue)
            .Select(static slot => slot!.Value)
            .Distinct()
            .OrderBy(static slot => slot)
            .ToArray();

        if (slots.Length == 0)
            return (fallbackSlot, fallbackSlot);

        var startIndex = Math.Max(0, slots.Length - width);
        return (slots[startIndex], slots[^1]);
    }

    private static int? TryParseSlotStem(string? fileStem)
    {
        if (
            string.IsNullOrWhiteSpace(fileStem)
            || !fileStem.StartsWith(SlotStemPrefix, StringComparison.OrdinalIgnoreCase)
        )
            return null;

        if (fileStem.Length < SlotStemPrefix.Length + 4)
            return null;

        return int.TryParse(fileStem.AsSpan(SlotStemPrefix.Length, 4), out var slot) ? slot : null;
    }
}
