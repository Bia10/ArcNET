using ArcNET.Formats;
using Bia.ValueBuffers;

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
        Span<char> buf = stackalloc char[2048];
        var vsb = new ValueStringBuilder(buf);

        // ── Section 1: Save metadata ─────────────────────────────────────────
        var saveInfo = SaveInfoFormat.ParseFile(gsiPath);
        vsb.AppendLine(SaveInfoDumper.Dump(saveInfo));

        // ── Section 2: Archive structure (index tree) ────────────────────────
        var index = SaveIndexFormat.ParseFile(tfaiPath);
        vsb.AppendLine(SaveIndexDumper.Dump(index));

        // ── Section 3: Extracted file contents ──────────────────────────────
        var tfafData = File.ReadAllBytes(tfafPath);
        var payloads = TfafFormat.ExtractAll(index, tfafData);

        // Build content summary (count by extension)
        var byExt = payloads
            .GroupBy(kvp => Path.GetExtension(kvp.Key).ToLowerInvariant())
            .OrderBy(g => g.Key)
            .Select(g => (Ext: g.Key.Length > 0 ? g.Key : "(no ext)", Count: g.Count(), TotalBytes: g.Sum(x => (long)x.Value.Length)))
            .ToList();

        vsb.AppendLine("=== SAVE FILE CONTENTS ===");
        vsb.Append($"  {payloads.Count} embedded file(s):");
        foreach (var (ext, count, totalBytes) in byExt)
            vsb.Append($"  {count} {ext} ({totalBytes:N0} B)");
        vsb.AppendLine();
        vsb.AppendLine();

        // Group by directory for readability
        var grouped = payloads
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .GroupBy(kvp => GetDirectory(kvp.Key), StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            var dirLabel = string.IsNullOrEmpty(group.Key) ? "(root)" : group.Key + "/";
            vsb.AppendLine($"  ┌── {dirLabel}");

            foreach (var kvp in group)
            {
                var virtualPath = kvp.Key;
                var data = kvp.Value;
                var fileName = Path.GetFileName(virtualPath);
                var ext = Path.GetExtension(fileName).ToLowerInvariant();

                vsb.AppendLine($"  │  [{fileName}]  ({data.Length:N0} bytes)");

                try
                {
                    var parsed = ParseAndDump(ext, data);
                    if (parsed is not null)
                    {
                        foreach (var line in parsed.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        {
                            vsb.Append("  │    ");
                            vsb.AppendLine(line.TrimEnd('\r'));
                        }
                    }
                    else
                    {
                        vsb.AppendLine($"  │    (binary / unrecognised format)");
                    }
                }
                catch (Exception ex)
                {
                    vsb.AppendLine($"  │    (parse error: {ex.Message})");
                }

                vsb.AppendLine("  │");
            }

            vsb.AppendLine("  └──");
            vsb.AppendLine();
        }

        return vsb.ToString();
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
            _ => null,
        };
    }

    private static string GetDirectory(string virtualPath)
    {
        var slash = virtualPath.LastIndexOf('/');
        return slash < 0 ? string.Empty : virtualPath[..slash];
    }
}
