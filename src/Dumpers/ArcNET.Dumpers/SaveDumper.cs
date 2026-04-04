using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable dump of a complete Arcanum save game.
/// <para>
/// An Arcanum save slot consists of three co-located files that share the same base name:
/// <list type="bullet">
///   <item><c>.gsi</c> — save metadata (leader, map, time, campaign).</item>
///   <item><c>.tfai</c> — archive index listing all embedded files with sizes.</item>
///   <item><c>.tfaf</c> — raw payload blob (files concatenated in TFAI depth-first order).</item>
/// </list>
/// </para>
/// </summary>
public static class SaveDumper
{
    // ── Public API ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Locates the <c>.gsi</c>, <c>.tfai</c>, and <c>.tfaf</c> files inside
    /// <paramref name="saveDir"/> and produces a full human-readable dump.
    /// </summary>
    /// <param name="saveDir">Directory containing the three save files.</param>
    /// <exception cref="FileNotFoundException">
    /// Thrown when any of the three required files cannot be found in <paramref name="saveDir"/>.
    /// </exception>
    public static string Dump(string saveDir)
    {
        var (gsiPath, tfaiPath, tfafPath) = LocateSaveFiles(saveDir);
        return Dump(gsiPath, tfaiPath, tfafPath);
    }

    /// <inheritdoc cref="Dump(string)"/>
    public static void Dump(string saveDir, TextWriter writer) => writer.Write(Dump(saveDir));

    /// <summary>
    /// Produces a full human-readable dump from explicitly provided file paths.
    /// </summary>
    public static string Dump(string gsiPath, string tfaiPath, string tfafPath)
    {
        var sb = new StringBuilder();

        // ── Section 1: Save metadata ─────────────────────────────────────────
        var saveInfo = SaveInfoFormat.ParseFile(gsiPath);
        sb.AppendLine(SaveInfoDumper.Dump(saveInfo));

        // ── Section 2: Archive structure (index tree) ────────────────────────
        var index = SaveIndexFormat.ParseFile(tfaiPath);
        sb.AppendLine(SaveIndexDumper.Dump(index));

        // ── Section 3: Extracted file contents ──────────────────────────────
        var tfafData = File.ReadAllBytes(tfafPath);
        var payloads = TfafFormat.ExtractAll(index, tfafData);

        sb.AppendLine("=== SAVE FILE CONTENTS ===");
        sb.AppendLine();

        // Group by directory for readability
        var grouped = payloads
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .GroupBy(kvp => GetDirectory(kvp.Key), StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            if (!string.IsNullOrEmpty(group.Key))
            {
                sb.AppendLine($"  ┌── {group.Key}/");
            }

            foreach (var kvp in group)
            {
                var virtualPath = kvp.Key;
                var data = kvp.Value;
                var fileName = Path.GetFileName(virtualPath);
                var ext = Path.GetExtension(fileName).ToLowerInvariant();

                sb.AppendLine($"  │  [{fileName}]  ({data.Length:N0} bytes)");

                try
                {
                    var parsed = ParseAndDump(ext, data);
                    if (parsed is not null)
                    {
                        foreach (var line in parsed.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        {
                            sb.Append("  │    ");
                            sb.AppendLine(line.TrimEnd('\r'));
                        }
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  │    (parse error: {ex.Message})");
                }

                sb.AppendLine("  │");
            }

            if (!string.IsNullOrEmpty(group.Key))
                sb.AppendLine("  └──");

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <inheritdoc cref="Dump(string, string, string)"/>
    public static void Dump(string gsiPath, string tfaiPath, string tfafPath, TextWriter writer) =>
        writer.Write(Dump(gsiPath, tfaiPath, tfafPath));

    // ── Private helpers ───────────────────────────────────────────────────────

    private static (string Gsi, string Tfai, string Tfaf) LocateSaveFiles(string saveDir)
    {
        var gsi = FindSingle(saveDir, "*.gsi");
        var tfai = FindSingle(saveDir, "*.tfai");
        var tfaf = FindSingle(saveDir, "*.tfaf");
        return (gsi, tfai, tfaf);
    }

    private static string FindSingle(string dir, string pattern)
    {
        var matches = Directory.GetFiles(dir, pattern);
        if (matches.Length == 0)
            throw new FileNotFoundException(
                $"No {pattern} file found in save directory: {dir}. "
                    + "Expected a directory containing .gsi, .tfai, and .tfaf files."
            );

        // Prefer the file whose base name matches a sibling .gsi if multiple exist
        return matches[0];
    }

    /// <summary>
    /// Attempts to parse and dump <paramref name="data"/> according to its file extension.
    /// Returns <see langword="null"/> for formats that do not have a text representation.
    /// </summary>
    private static string? ParseAndDump(string ext, byte[] data)
    {
        var mem = (ReadOnlyMemory<byte>)data;

        return ext switch
        {
            ".mob" => MobDumper.Dump(MobFormat.ParseMemory(mem)),
            ".pro" => ProtoDumper.Dump(ProtoFormat.ParseMemory(mem)),
            ".sec" => SectorDumper.Dump(SectorFormat.ParseMemory(mem)),
            ".jmp" => JmpDumper.Dump(JmpFormat.ParseMemory(mem)),
            ".prp" => MapPropertiesDumper.Dump(MapPropertiesFormat.ParseMemory(mem)),
            ".scr" => ScriptDumper.Dump(ScriptFormat.ParseMemory(mem)),
            ".mes" => MessageDumper.Dump(MessageFormat.ParseMemory(mem)),
            ".tdf" => TerrainDumper.Dump(TerrainFormat.ParseMemory(mem)),
            ".dlg" => DialogDumper.Dump(DialogFormat.ParseMemory(mem)),
            ".art" => ArtDumper.Dump(ArtFormat.ParseMemory(mem)),
            _ => null, // Binary or unknown — size already shown above
        };
    }

    private static string GetDirectory(string virtualPath)
    {
        var slash = virtualPath.LastIndexOf('/');
        return slash < 0 ? string.Empty : virtualPath[..slash];
    }
}
