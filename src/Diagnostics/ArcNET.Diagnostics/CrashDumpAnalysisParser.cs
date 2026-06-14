namespace ArcNET.Diagnostics;

public static class CrashDumpAnalysisParser
{
    public static CrashDumpAnalysisParsedOutput Parse(string output)
    {
        var lines = output
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(static line => line.Trim())
            .Where(static line => line.Length != 0)
            .ToArray();
        if (lines.Length == 0)
            return new CrashDumpAnalysisParsedOutput(null, null, null, [], ["cdb.exe returned no output."]);

        var processName = TryReadPrefixedValue(lines, "PROCESS_NAME:");
        var exceptionCode = TryReadPrefixedValue(lines, "EXCEPTION_CODE:");
        var faultingInstruction = TryReadFaultingInstruction(lines);
        var stackPreview = ReadStackPreview(lines);

        List<string> highlights = [];
        if (!string.IsNullOrWhiteSpace(processName))
            highlights.Add($"PROCESS_NAME: {processName}");

        if (!string.IsNullOrWhiteSpace(exceptionCode))
            highlights.Add($"EXCEPTION_CODE: {exceptionCode}");

        if (!string.IsNullOrWhiteSpace(faultingInstruction))
            highlights.Add($"FAULTING_IP: {faultingInstruction}");

        if (stackPreview.Count > 0)
            highlights.AddRange(stackPreview.Take(3));

        if (highlights.Count == 0)
            highlights.AddRange(lines.Take(4));

        return new CrashDumpAnalysisParsedOutput(
            processName,
            exceptionCode,
            faultingInstruction,
            stackPreview,
            highlights
        );
    }

    private static string? TryReadPrefixedValue(IReadOnlyList<string> lines, string prefix)
    {
        var line = lines.FirstOrDefault(candidate => candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var value = line[prefix.Length..].Trim();
        return value.Length == 0 ? null : value;
    }

    private static string? TryReadFaultingInstruction(IReadOnlyList<string> lines)
    {
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            if (!line.StartsWith("FAULTING_IP:", StringComparison.OrdinalIgnoreCase))
                continue;

            var inlineValue = line["FAULTING_IP:".Length..].Trim();
            if (inlineValue.Length != 0)
                return inlineValue;

            var candidate = lines.Skip(lineIndex + 1).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(candidate) && !candidate.EndsWith(":", StringComparison.Ordinal))
                return candidate;
        }

        return null;
    }

    private static IReadOnlyList<string> ReadStackPreview(IReadOnlyList<string> lines)
    {
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            if (!lines[lineIndex].StartsWith("STACK_TEXT:", StringComparison.OrdinalIgnoreCase))
                continue;

            return
            [
                .. lines
                    .Skip(lineIndex + 1)
                    .TakeWhile(static line => !line.EndsWith(":", StringComparison.Ordinal))
                    .Take(3),
            ];
        }

        return [];
    }
}
