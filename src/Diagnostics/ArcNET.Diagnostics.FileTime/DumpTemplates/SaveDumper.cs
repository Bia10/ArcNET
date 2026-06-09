using System.Buffers.Binary;
using System.Collections.Frozen;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Diagnostics;
using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Diagnostics;

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

        // ── Parse all data up-front ──────────────────────────────────────────
        var save = SaveSlotLoadService.LoadFiles(gsiPath, tfaiPath, tfafPath);
        var payloads = save.Files;
        var audit = SaveFileAuditService.Create(new SaveFileAuditRequest(save));
        var structure = SaveStructureAnalysisService.Create(save);

        // ── Section 1: Narrative summary ────────────────────────────────────
        vsb.AppendLine(DumpNarrative(structure));

        // ── Section 2: File-time diagnostics summary ────────────────────────
        vsb.AppendLine(DumpDiagnostics(audit));

        // ── Section 3: Save metadata (detailed) ─────────────────────────────
        vsb.AppendLine(SaveInfoDumper.Dump(save.Info));

        // ── Section 4: Archive structure (index tree) ────────────────────────
        vsb.AppendLine(SaveIndexDumper.Dump(save.Index));

        // ── Section 5: Extracted file contents ──────────────────────────────
        vsb.AppendLine("=== SAVE FILE CONTENTS ===");
        vsb.Append("  ");
        vsb.Append(structure.TotalFileCount);
        vsb.Append(" embedded file(s):");
        foreach (var extension in structure.Extensions)
        {
            vsb.Append("  ");
            vsb.Append(extension.Count);
            vsb.Append(' ');
            vsb.Append(extension.DisplayExtension);
            vsb.Append(" (");
            vsb.Append(extension.TotalBytes, "N0");
            vsb.Append(" B)");
        }
        vsb.AppendLine();
        vsb.AppendLine();

        // Group by directory for readability
        var grouped = payloads
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .GroupBy(kvp => GetDirectory(kvp.Key), StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            var dirLabel = string.IsNullOrEmpty(group.Key) ? "(root)" : group.Key + "/";
            vsb.Append("  ┌── ");
            vsb.AppendLine(dirLabel);

            foreach (var kvp in group)
            {
                var virtualPath = kvp.Key;
                var data = kvp.Value;
                var fileName = Path.GetFileName(virtualPath);
                var ext = Path.GetExtension(fileName).ToLowerInvariant();

                vsb.Append("  │  [");
                vsb.Append(fileName);
                vsb.Append("]  (");
                vsb.Append(data.Length, "N0");
                vsb.AppendLine(" bytes)");

                try
                {
                    var parsed = ParseAndDump(fileName, ext, data);
                    if (parsed is not null)
                    {
                        foreach (var line in parsed.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        {
                            vsb.Append("  │    ");
                            vsb.AppendLine(line.AsSpan().TrimEnd('\r'));
                        }
                    }
                    else
                    {
                        vsb.AppendLine("  │    (binary / unrecognised format)");
                    }
                }
                catch (Exception ex)
                {
                    vsb.Append("  │    (parse error: ");
                    vsb.Append(ex.Message);
                    vsb.AppendLine(")");
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

    /// <summary>
    /// Produces a high-level, plain-English narrative summary of the save so the
    /// reader can understand the save at a glance — without parsing every file.
    /// </summary>
    private static string DumpNarrative(SaveStructureAnalysisSnapshot structure)
    {
        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);

        sb.AppendLine('═', 72);
        sb.Append("  ARCANUM SAVE: \"");
        sb.Append(structure.DisplayName);
        sb.AppendLine("\"");
        sb.AppendLine('═', 72);
        sb.AppendLine();

        sb.Append("  Character   : ");
        sb.AppendLine(structure.LeaderName);
        sb.Append("  Level       : ");
        sb.Append(structure.LeaderLevel);
        sb.Append("   Portrait: #");
        sb.AppendLine(structure.LeaderPortraitId);
        sb.Append("  Campaign    : ");
        sb.AppendLine(structure.ModuleName);
        sb.Append("  Map         : map ");
        sb.Append(structure.MapId);
        sb.Append(", tile (");
        sb.Append(structure.LeaderTileX);
        sb.Append(", ");
        sb.Append(structure.LeaderTileY);
        sb.AppendLine(")");
        sb.Append("  Game time   : Day ");
        sb.Append(structure.GameTime.DayNumber);
        sb.Append(", ");
        sb.Append(structure.GameTime.Hours, "D2");
        sb.Append(':');
        sb.Append(structure.GameTime.Minutes, "D2");
        sb.Append(':');
        sb.AppendLine(structure.GameTime.Seconds, "D2");
        sb.AppendLine();

        if (structure.ExploredAreas.Count > 0)
        {
            sb.AppendLine("  ─── Explored areas ──────────────────────────────────────────────");
            foreach (var area in structure.ExploredAreas)
            {
                var marker =
                    area.CoveragePercent >= 99.9 ? "✓"
                    : area.RevealedTiles > 0 ? "~"
                    : " ";
                sb.Append("  ");
                sb.Append(marker);
                sb.Append(' ');
                sb.AppendPadded(area.Area, 30);
                sb.Append(' ');
                sb.AppendPadded(area.CoveragePercent, 5, leftAlign: false, format: "F1");
                sb.AppendLine("% revealed");
            }
            sb.AppendLine();
        }

        if (structure.Maps.Count > 0)
        {
            sb.AppendLine("  ─── World-state changes by map ─────────────────────────────────");
            foreach (var map in structure.Maps)
            {
                sb.Append("  Map: ");
                sb.Append(map.MapName);

                if (map.DestroyedObjectCount > 0)
                {
                    sb.Append("  |  ");
                    sb.Append(map.DestroyedObjectCount);
                    sb.Append(" object(s) destroyed");
                }

                if (map.ModifiedObjectCount > 0)
                {
                    sb.Append("  |  ");
                    sb.Append(map.ModifiedObjectCount);
                    sb.Append(" object(s) modified");
                }

                if (map.DynamicMobileCount > 0)
                {
                    sb.Append("  |  ");
                    sb.Append(map.DynamicMobileCount);
                    sb.Append(" dynamic mobile(s)");
                }

                if (map.ObjectDiffCount > 0)
                {
                    sb.Append("  |  ");
                    sb.Append(map.ObjectDiffCount);
                    sb.Append(" object diff(s)");
                }

                sb.AppendLine();
            }
            sb.AppendLine();
        }

        sb.AppendLine('\u2550', 72);
        return sb.ToString();
    }

    private static string DumpDiagnostics(SaveFileAuditSnapshot audit)
    {
        Span<char> sbBuf = stackalloc char[1024];
        var sb = new ValueStringBuilder(sbBuf);

        sb.AppendLine("=== FILE-TIME DIAGNOSTICS ===");
        sb.Append("  Validation : issues=");
        sb.Append(audit.Validation.IssueCount);
        sb.Append("  errors=");
        sb.Append(audit.Validation.ErrorCount);
        sb.Append("  warnings=");
        sb.Append(audit.Validation.WarningCount);
        sb.Append("  info=");
        sb.Append(audit.Validation.InfoCount);
        sb.Append("  files=");
        sb.AppendLine(audit.Validation.FileCountWithIssues);

        sb.Append("  Assets     : files=");
        sb.Append(audit.Assets.TotalFileCount);
        sb.Append("  raw=");
        sb.Append(audit.Assets.RawFileCount);
        sb.Append("  parseErrors=");
        sb.Append(audit.Assets.ParseErrorCount);
        sb.Append("  mobiles=");
        sb.Append(audit.Assets.MobCount);
        sb.Append("  mobileMd=");
        sb.Append(audit.Assets.MobileMdCount);
        sb.Append("  mobileMdy=");
        sb.AppendLine(audit.Assets.MobileMdyCount);

        sb.Append("  Objects    : objects=");
        sb.Append(audit.Objects.ObjectCount);
        sb.Append("  props=");
        sb.Append(audit.Objects.TotalPropertyCount);
        sb.Append("  distinctFields=");
        sb.Append(audit.Objects.DistinctFieldCount);
        sb.Append("  parseNotes=");
        sb.AppendLine(audit.Objects.ParseNoteCount);

        if (audit.Objects.TopFields.Count > 0)
        {
            sb.Append("  Top fields : ");
            AppendFieldList(ref sb, audit.Objects.TopFields, 5);
            sb.AppendLine();
        }

        if (audit.Objects.LinkFields.Count > 0)
        {
            sb.Append("  Link fields: ");
            AppendFieldList(ref sb, audit.Objects.LinkFields, 5);
            sb.AppendLine();
        }

        if (audit.PlayerCharacter is { } player)
        {
            sb.Append("  Player     : ");
            sb.Append(player.Name ?? "(none)");
            sb.Append("  lv=");
            sb.Append(player.Level);
            sb.Append("  gold=");
            sb.Append(player.Gold);
            sb.Append("  quests=");
            sb.Append(player.QuestCount);
            sb.Append("  rumors=");
            sb.Append(player.RumorsCount);
            sb.Append("  bless=");
            sb.Append(player.BlessingCount);
            sb.Append("  curse=");
            sb.Append(player.CurseCount);
            sb.Append("  schematics=");
            sb.Append(player.SchematicsCount);
            sb.Append("  sars=");
            sb.AppendLine(player.Sars.Count);
        }

        if (audit.ParseErrors.Count > 0)
        {
            foreach (var parseError in audit.ParseErrors.Take(3))
            {
                sb.Append("  ParseError : ");
                sb.Append(parseError.FilePath);
                sb.Append("  ");
                sb.AppendLine(Truncate(parseError.Message, 100));
            }
        }

        if (audit.ValidationIssues.Count > 0)
        {
            foreach (var issue in audit.ValidationIssues.Take(3))
            {
                sb.Append("  Finding    : [");
                sb.Append(issue.Severity.ToString());
                sb.Append("] ");
                sb.Append(issue.FilePath ?? "(save)");
                sb.Append("  ");
                sb.AppendLine(Truncate(issue.Message, 100));
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }

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

    // ── Extension → dump handler registry (OCP: register new formats here, no changes elsewhere) ──
    // Handler signature: (fileName, rawBytes) → string?   null = no text representation
    private static readonly FrozenDictionary<string, Func<string, byte[], string?>> s_handlers = new Dictionary<
        string,
        Func<string, byte[], string?>
    >(StringComparer.OrdinalIgnoreCase)
    {
        // ── Formats shared with base-map files ────────────────────────────────────────────────
        [".mob"] = static (_, d) => MobDumper.Dump(MobFormat.ParseMemory(d)),
        [".pro"] = static (_, d) => ProtoDumper.Dump(ProtoFormat.ParseMemory(d)),
        [".sec"] = static (_, d) => SectorDumper.Dump(SectorFormat.ParseMemory(d)),
        [".jmp"] = static (_, d) => JmpDumper.Dump(JmpFormat.ParseMemory(d)),
        [".prp"] = static (_, d) => MapPropertiesDumper.Dump(MapPropertiesFormat.ParseMemory(d)),
        [".scr"] = static (_, d) => ScriptDumper.Dump(ScriptFormat.ParseMemory(d)),
        [".mes"] = static (_, d) => MessageDumper.Dump(MessageFormat.ParseMemory(d)),
        [".tdf"] = static (_, d) => TerrainDumper.Dump(TerrainFormat.ParseMemory(d)),
        [".dlg"] = static (_, d) => DialogDumper.Dump(DialogFormat.ParseMemory(d)),
        [".art"] = static (_, d) => ArtDumper.Dump(ArtFormat.ParseMemory(d)),

        // ── Save-specific formats ────────────────────────────────────────────────────────────
        // .dif  — solitary object diff file.
        //         Large files: full obj_write format (same as .mob).
        //         Small files: compact obj_dif_write format (byte[8-11] == 0x80000001).
        [".dif"] = static (_, d) =>
            SaveEmbeddedFileAnalysisService.IsCompactDifFormat(d)
                ? DumpCompactDif(d)
                : MobDumper.Dump(MobFormat.ParseMemory(d)),

        // .mdy  — sequence of dynamic mobile objects (same obj_write per object)
        [".mdy"] = static (_, d) => DumpMultipleMobs(d),

        // .des  — list of ObjectIDs of destroyed/extinct objects
        [".des"] = static (_, d) => DumpDestroyedObjects(d),

        // .md   — modified-object diffs: [ObjectID][obj_dif_write block] pairs
        [".md"] = static (_, d) => DumpModifiedObjects(d),

        // .dat  — TimeEvent.dat (time-scheduled events); other .dat files have no known text representation
        [".dat"] = static (n, d) =>
            n.Equals("TimeEvent.dat", StringComparison.OrdinalIgnoreCase) ? DumpTimeEvents(d) : null,

        // .tmf  — town-map fog-of-war bitmask (1 bit per map tile)
        [".tmf"] = static (_, d) => DumpTownMapFog(d),

        // .tmn  — town-map player notes (format not yet reversed)
        [".tmn"] = static (_, d) => $"Town map notes: {d.Length} bytes  (binary — format not yet reversed)",

        // .sav  — per-slot global state (quests, flags; format not yet reversed)
        [".sav"] = static (_, d) => $"Global save data: {d.Length} bytes  (binary — format not yet reversed)",

        // .sbf  — unknown save binary (format not yet reversed)
        [".sbf"] = static (_, d) => $"Save binary: {d.Length} bytes  (binary — format not yet reversed)",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Attempts to parse and dump <paramref name="data"/> according to its file name / extension.
    /// Returns <see langword="null"/> for formats that do not have a text representation.
    /// To add support for a new format, register its handler in <see cref="s_handlers"/>.
    /// </summary>
    private static string? ParseAndDump(string fileName, string ext, byte[] data)
    {
        if (SaveEmbeddedFileAnalysisService.TryAnalyze(fileName, data) is { } detail)
            return DumpEmbeddedFileDetail(detail);

        return s_handlers.TryGetValue(ext, out var handler) ? handler(fileName, data) : null;
    }

    private static string DumpEmbeddedFileDetail(SaveEmbeddedFileDetailSnapshot detail) =>
        detail switch
        {
            SaveDynamicMobileFileDetailSnapshot dynamicMobiles => DumpDynamicMobileAnalysis(dynamicMobiles.Analysis),
            SaveCompactDifFileDetailSnapshot compactDif => DumpCompactDifAnalysis(compactDif.Analysis),
            SaveDestroyedObjectsFileDetailSnapshot destroyedObjects => DumpDestroyedObjectsAnalysis(
                destroyedObjects.Analysis
            ),
            SaveModifiedObjectsFileDetailSnapshot modifiedObjects => DumpModifiedObjectsAnalysis(
                modifiedObjects.Analysis
            ),
            SaveTimeEventFileDetailSnapshot timeEvents => DumpTimeEventAnalysis(timeEvents.Analysis),
            SaveTownMapFogFileDetailSnapshot townMapFog => DumpTownMapFogAnalysis(townMapFog.Analysis),
            _ => throw new NotSupportedException($"Unsupported embedded file detail kind '{detail.Kind}'."),
        };

    private static string DumpDynamicMobileAnalysis(SaveDynamicMobileAnalysisSnapshot analysis)
    {
        if (analysis.ObjectCount == 0 && analysis.SkippedSentinelCount == 0)
            return "No dynamic mobile objects";

        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);
        foreach (var entry in analysis.Entries)
        {
            sb.Append("--- object ");
            sb.Append(entry.Index);
            if (entry.ParseError is null)
            {
                sb.AppendLine(" ---");
                sb.Append(MobDumper.Dump(entry.Mob!));
                continue;
            }

            sb.Append(" parse failed at byte ");
            sb.Append(entry.Offset);
            sb.Append(": ");
            sb.Append(entry.ParseError);
            sb.AppendLine(" (stopping) ---");
            break;
        }

        var suffix =
            analysis.SkippedSentinelCount > 0
                ? $" ({analysis.SkippedSentinelCount} sentinel dword(s) skipped)"
                : string.Empty;
        return $"Dynamic mobile objects: {analysis.ObjectCount}{suffix}\n{sb.ToString()}";
    }

    private static string DumpCompactDifAnalysis(SaveCompactDifAnalysisSnapshot analysis)
    {
        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);

        sb.Append("obj_dif_write compact format  (magic=0x");
        sb.Append(analysis.Magic, "X2");
        sb.AppendLine(")");

        foreach (var record in analysis.Records)
        {
            sb.Append("  [record ");
            sb.Append(record.Index);
            sb.Append("]  ");
            if (record.B is { } b)
            {
                sb.Append("B=");
                sb.Append(b);
                sb.Append("  ");
            }

            sb.Append("data=");
            sb.Append(record.DataLength);
            sb.AppendLine(" bytes");
        }

        if (analysis.MissingStartSentinel)
            sb.AppendLine("  (no records found — START sentinel 0x12344321 not present)");

        if (analysis.TrailingValue is { } trailing)
        {
            sb.AppendHex((uint)trailing, "  trailing=0x".AsSpan());
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string DumpDestroyedObjectsAnalysis(SaveDestroyedObjectsAnalysisSnapshot analysis)
    {
        if (analysis.ByteLength == 0)
            return "No destroyed objects";

        if (analysis.HasAlignmentWarning)
            return $"Destroyed objects: warning — size {analysis.ByteLength} is not a multiple of 24 (ObjectID size)";

        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);
        sb.Append("Destroyed/extinct objects: ");
        sb.AppendLine(analysis.ObjectIds.Count);

        for (var index = 0; index < analysis.ObjectIds.Count; index++)
        {
            sb.Append("  [");
            sb.Append(index + 1);
            sb.Append("] ");
            sb.AppendLine(analysis.ObjectIds[index]);
        }

        return sb.ToString();
    }

    private static string DumpModifiedObjectsAnalysis(SaveModifiedObjectsAnalysisSnapshot analysis)
    {
        if (analysis.Entries.Count == 0)
            return "No modified objects";

        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);
        foreach (var entry in analysis.Entries)
        {
            if (!string.IsNullOrEmpty(entry.Warning))
                sb.AppendLine(entry.Warning);

            if (entry.Mob is { } mob)
            {
                sb.Append("  [");
                sb.Append(entry.Index);
                sb.Append("] ");
                sb.Append(entry.FileObjectId);
                sb.Append("  →  ");
                sb.Append(mob.Header.GameObjectType.ToString());
                sb.Append(" (proto ");
                sb.Append(mob.Header.ProtoId.ToString());
                sb.AppendLine(")");
                sb.Append(MobDumper.Dump(mob));
                continue;
            }

            sb.Append("  [");
            sb.Append(entry.Index);
            sb.Append("] ");
            sb.Append(entry.FileObjectId);
            sb.Append("  (parse failed: ");
            sb.Append(entry.ParseError);
            sb.AppendLine(")");
        }

        if (!string.IsNullOrEmpty(analysis.TerminalWarning))
            sb.AppendLine(analysis.TerminalWarning);

        return $"Modified objects: {analysis.Entries.Count}\n{sb.ToString()}";
    }

    private static string DumpTimeEventAnalysis(SaveTimeEventAnalysisSnapshot analysis)
    {
        if (analysis.IsTooShort)
            return "TimeEvent.dat: too short to contain event count";

        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);
        sb.Append("TimeEvent.dat — ");
        sb.Append(analysis.DeclaredCount);
        sb.Append(" scheduled event(s)  (");
        sb.Append(analysis.ByteLength);
        sb.AppendLine(" bytes total)");

        foreach (var entry in analysis.Entries)
        {
            sb.Append("  [");
            sb.Append(entry.Index);
            sb.Append("] Day ");
            sb.Append(entry.Days);
            sb.Append(", +");
            sb.Append(entry.Milliseconds);
            sb.Append(" ms, type=");
            sb.AppendLine(entry.Type);
        }

        if (analysis.HasMoreEntries)
        {
            sb.Append("  ... (+");
            sb.Append(analysis.DeclaredCount - analysis.Entries.Count);
            sb.AppendLine(" more — param layout requires TimeEventTypeInfo table)");
        }

        return sb.ToString();
    }

    private static string DumpTownMapFogAnalysis(SaveTownMapFogFileAnalysisSnapshot analysis) =>
        $"Town map fog: {analysis.RevealedTiles}/{analysis.TotalTiles} tiles revealed ({analysis.CoveragePercent:F1}%)  ({analysis.ByteLength} bytes)";

    // ── Save-specific format parsers ──────────────────────────────────────────

    /// <summary>
    /// Parses a <c>.mdy</c> file (dynamic mobile objects) as a sequence of
    /// back-to-back <c>obj_write</c> records — identical binary layout to <c>.mob</c>.
    /// Some sectors prefix or separate records with a 4-byte sentinel (0xFFFFFFFF).
    /// </summary>
    private static string DumpMultipleMobs(ReadOnlyMemory<byte> mem) =>
        DumpDynamicMobileAnalysis(SaveEmbeddedFileAnalysisService.AnalyzeDynamicMobiles(mem));

    /// <summary>
    /// Parses a compact <c>obj_dif_write</c> block sequence stored in a solitary <c>.dif</c> file.
    /// <para>
    /// Three format variants are supported (all detected by <see cref="SaveEmbeddedFileAnalysisService.IsCompactDifFormat(byte[])"/>):
    /// <list type="bullet">
    ///   <item><b>Variant A</b> — magic=0x08, 4-byte preamble, records: B+C+D+START+data+END.</item>
    ///   <item><b>Variant B</b> — magic=0x08, 4-byte preamble, records: C+D+START+data+END (no B).</item>
    ///   <item><b>Variant C</b> — magic=0x18, variable preamble, records scanned via START/END sentinels.</item>
    /// </list>
    /// </para>
    /// </summary>
    private static string DumpCompactDif(byte[] data) =>
        DumpCompactDifAnalysis(SaveEmbeddedFileAnalysisService.AnalyzeCompactDif(data));

    /// <summary>
    /// Parses a <c>mobile.des</c> file: a flat array of 24-byte <c>ObjectID</c> structs
    /// identifying extinct or destroyed non-dynamic non-static objects.
    /// </summary>
    private static string DumpDestroyedObjects(ReadOnlyMemory<byte> mem) =>
        DumpDestroyedObjectsAnalysis(SaveEmbeddedFileAnalysisService.AnalyzeDestroyedObjects(mem));

    /// <summary>
    /// Parses a <c>mobile.md</c> file: alternating [<c>ObjectID</c>][<c>obj_dif_write</c> block] records.
    /// <para>
    /// Each block is an obj_write payload (ProtoId + ObjectId + type + bitmap + properties) preceded
    /// by the version and sentinels.  The payload is parsed directly so we never have to scan for the
    /// end-sentinel (which would wrongly stop at coincidental matches inside property data).
    /// </para>
    /// </summary>
    private static string DumpModifiedObjects(ReadOnlyMemory<byte> mem) =>
        DumpModifiedObjectsAnalysis(SaveEmbeddedFileAnalysisService.AnalyzeModifiedObjects(mem));

    /// <summary>
    /// Parses a <c>TimeEvent.dat</c> file.
    /// <para>
    /// Format: <c>count (int32)</c> followed by <c>count</c> event nodes.
    /// Each node begins with <c>datetime {days, milliseconds}</c> (8 bytes) and
    /// <c>type (int32)</c>.  The per-type parameter layout requires the
    /// <c>TimeEventTypeInfo</c> flags table which is not available at parse time,
    /// so only the count and the first event header are shown.
    /// </para>
    /// </summary>
    private static string DumpTimeEvents(ReadOnlyMemory<byte> mem) =>
        DumpTimeEventAnalysis(SaveEmbeddedFileAnalysisService.AnalyzeTimeEvents(mem));

    /// <summary>
    /// Parses a <c>.tmf</c> (Town Map Fog) file: a raw bit-array where each bit
    /// represents one map tile.  A set bit means the tile has been revealed.
    /// </summary>
    private static string DumpTownMapFog(ReadOnlyMemory<byte> mem) =>
        DumpTownMapFogAnalysis(SaveEmbeddedFileAnalysisService.AnalyzeTownMapFog(mem));

    private static string GetDirectory(string virtualPath)
    {
        var slash = virtualPath.LastIndexOf('/');
        return slash < 0 ? string.Empty : virtualPath[..slash];
    }

    private static void AppendFieldList(
        ref ValueStringBuilder sb,
        IReadOnlyList<ObjectFieldUsageSnapshot> fields,
        int maxCount
    )
    {
        for (var index = 0; index < fields.Count && index < maxCount; index++)
        {
            if (index > 0)
                sb.Append(", ");

            sb.Append(fields[index].Field);
            sb.Append('=');
            sb.Append(fields[index].Count);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
