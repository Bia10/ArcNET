using System.Text.Json;
using System.Text.Json.Serialization;
using ArcNET.Formats;

namespace ArcNET.App;

/// <summary>
/// Produces structured JSON output for machine/AI consumers of the CLI.
/// Every dump command that accepts <c>--json</c> routes through this class.
/// <para>
/// Convention: each method returns an envelope with three guaranteed fields:
/// <list type="bullet">
///   <item><c>format</c> — the format name (e.g. <c>"mob"</c>).</item>
///   <item><c>source</c> — path to the file or directory that was parsed.</item>
///   <item><c>data</c> — format-specific structured payload.</item>
/// </list>
/// Parse errors are written to <see cref="Console.Error"/> as
/// <c>{ "error": "...", "source": "..." }</c> and the process exits with code 1.
/// </para>
/// </summary>
internal static partial class AgentOutput
{
    private static readonly JsonSerializerOptions s_opts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Emit helpers ─────────────────────────────────────────────────────────

    internal static void Write(string format, string source, object data) =>
        Console.WriteLine(
            JsonSerializer.Serialize(
                new
                {
                    format,
                    source,
                    data,
                },
                s_opts
            )
        );

    internal static void WriteError(string source, Exception ex)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new { error = ex.Message, source }, s_opts));
    }
}
