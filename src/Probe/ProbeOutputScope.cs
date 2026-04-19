using System.Text;

namespace Probe;

internal readonly record struct ProbeOutputOptions(bool ForceStdout, string? OutputPath);

internal sealed class ProbeOutputScope : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly StreamWriter? _redirectWriter;

    private ProbeOutputScope(TextWriter originalOut, StreamWriter? redirectWriter, string? outputPath)
    {
        _originalOut = originalOut;
        _redirectWriter = redirectWriter;
        OutputPath = outputPath;
    }

    public string? OutputPath { get; }

    public static ProbeOutputScope Create(string modeArg, ProbeOutputOptions options)
    {
        if (options.ForceStdout || !ShouldCaptureToFile(modeArg))
            return new ProbeOutputScope(Console.Out, null, null);

        var outputPath = ResolveOutputPath(modeArg, options.OutputPath);
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        var originalOut = Console.Out;
        var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
        Console.SetOut(writer);
        Console.Error.WriteLine($"[probe] High-volume output redirected to {outputPath}");
        Console.Error.WriteLine(
            "[probe] Pass --stdout to force terminal output, or --out <path> to choose the capture file."
        );
        return new ProbeOutputScope(originalOut, writer, outputPath);
    }

    public void Dispose()
    {
        if (_redirectWriter is null)
            return;

        try
        {
            Console.Out.Flush();
        }
        finally
        {
            Console.SetOut(_originalOut);
            _redirectWriter.Dispose();
        }

        Console.Error.WriteLine($"[probe] Full output saved to {OutputPath}");
    }

    private static string ResolveOutputPath(string modeArg, string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);

        var modeSegment = SanitizeSegment(modeArg);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(
            Directory.GetCurrentDirectory(),
            "artifacts",
            "probe",
            $"probe-{modeSegment}-{timestamp}.txt"
        );
    }

    private static bool ShouldCaptureToFile(string modeArg) =>
        modeArg
            is "7"
                or "sar-dump"
                or "9"
                or "sar-diff"
                or "10"
                or "full-sar-dump"
                or "11"
                or "binary-diff"
                or "12"
                or "diagnostics"
                or "13"
                or "field-evolution"
                or "14"
                or "quest-book"
                or "15"
                or "npc-scan"
                or "17"
                or "pc-data";

    private static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "probe";

        Span<char> buffer = stackalloc char[value.Length];
        var written = 0;
        foreach (var ch in value)
        {
            buffer[written++] = char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_';
        }

        return new string(buffer[..written]);
    }
}
