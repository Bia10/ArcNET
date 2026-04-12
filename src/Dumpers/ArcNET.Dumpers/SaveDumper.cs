using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Numerics;
using ArcNET.Core;
using ArcNET.Core.Primitives;
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

        // ── Parse all data up-front ──────────────────────────────────────────
        var saveInfo = SaveInfoFormat.ParseFile(gsiPath);
        var index = SaveIndexFormat.ParseFile(tfaiPath);
        var tfafData = File.ReadAllBytes(tfafPath);
        var payloads = TfafFormat.ExtractAll(index, tfafData);

        // ── Section 1: Narrative summary ────────────────────────────────────
        vsb.AppendLine(DumpNarrative(saveInfo, payloads));

        // ── Section 2: Save metadata (detailed) ─────────────────────────────
        vsb.AppendLine(SaveInfoDumper.Dump(saveInfo));

        // ── Section 3: Archive structure (index tree) ────────────────────────
        vsb.AppendLine(SaveIndexDumper.Dump(index));

        // ── Section 4: Extracted file contents ──────────────────────────────
        var byExt = payloads
            .GroupBy(kvp => Path.GetExtension(kvp.Key).ToLowerInvariant())
            .OrderBy(g => g.Key)
            .Select(g =>
                (
                    Ext: g.Key.Length > 0 ? g.Key : "(no ext)",
                    Count: g.Count(),
                    TotalBytes: g.Sum(x => (long)x.Value.Length)
                )
            )
            .ToList();

        vsb.AppendLine("=== SAVE FILE CONTENTS ===");
        vsb.Append("  ");
        vsb.Append(payloads.Count);
        vsb.Append(" embedded file(s):");
        foreach (var (ext, count, totalBytes) in byExt)
        {
            vsb.Append("  ");
            vsb.Append(count);
            vsb.Append(' ');
            vsb.Append(ext);
            vsb.Append(" (");
            vsb.Append(totalBytes, "N0");
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
    private static string DumpNarrative(SaveInfo info, IReadOnlyDictionary<string, byte[]> payloads)
    {
        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);

        sb.AppendLine('═', 72);
        sb.Append("  ARCANUM SAVE: \"");
        sb.Append(info.DisplayName);
        sb.AppendLine("\"");
        sb.AppendLine('═', 72);
        sb.AppendLine();

        // Character
        var totalMs = (long)info.GameTimeDays * 86_400_000L + info.GameTimeMs;
        var hours = (int)(totalMs / 3_600_000L % 24);
        var minutes = (int)(totalMs / 60_000L % 60);
        var seconds = (int)(totalMs / 1_000L % 60);

        sb.Append("  Character   : ");
        sb.AppendLine(info.LeaderName);
        sb.Append("  Level       : ");
        sb.Append(info.LeaderLevel);
        sb.Append("   Portrait: #");
        sb.AppendLine(info.LeaderPortraitId);
        sb.Append("  Campaign    : ");
        sb.AppendLine(info.ModuleName);
        sb.Append("  Map         : map ");
        sb.Append(info.MapId);
        sb.Append(", tile (");
        sb.Append(info.LeaderTileX);
        sb.Append(", ");
        sb.Append(info.LeaderTileY);
        sb.AppendLine(")");
        sb.Append("  Game time   : Day ");
        sb.Append(info.GameTimeDays + 1);
        sb.Append(", ");
        sb.Append(hours, "D2");
        sb.Append(':');
        sb.Append(minutes, "D2");
        sb.Append(':');
        sb.AppendLine(seconds, "D2");
        sb.AppendLine();

        // Town-map fog coverage
        var tmfFiles = payloads
            .Where(kvp => Path.GetExtension(kvp.Key).Equals(".tmf", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kvp => Path.GetFileNameWithoutExtension(kvp.Key), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tmfFiles.Count > 0)
        {
            sb.AppendLine("  ─── Explored areas ──────────────────────────────────────────────");
            foreach (var kvp in tmfFiles)
            {
                var area = Path.GetFileNameWithoutExtension(kvp.Key);
                var data = kvp.Value;
                var totalBits = data.Length * 8;
                var revealedBits = 0;
                foreach (var b in data)
                    revealedBits += int.PopCount(b);
                var pct = totalBits > 0 ? 100.0 * revealedBits / totalBits : 0.0;
                var marker =
                    pct >= 99.9 ? "✓"
                    : pct > 0 ? "~"
                    : " ";
                sb.Append("  ");
                sb.Append(marker);
                sb.Append(' ');
                sb.AppendPadded(area, 30);
                sb.Append(' ');
                sb.AppendPadded(pct, 5, leftAlign: false, format: "F1");
                sb.AppendLine("% revealed");
            }
            sb.AppendLine();
        }

        // Per-map world-state summary
        var mapDirs = payloads
            .Where(kvp => kvp.Key.StartsWith("maps/", StringComparison.OrdinalIgnoreCase))
            .GroupBy(
                kvp =>
                {
                    var rel = kvp.Key["maps/".Length..];
                    var slash = rel.IndexOf('/');
                    return slash >= 0 ? rel[..slash] : rel;
                },
                StringComparer.OrdinalIgnoreCase
            )
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (mapDirs.Count > 0)
        {
            sb.AppendLine("  ─── World-state changes by map ─────────────────────────────────");
            foreach (var mapGrp in mapDirs)
            {
                var mapName = mapGrp.Key;
                sb.Append("  Map: ");
                sb.Append(mapName);

                // Count objects destroyed (.des)
                var desFile = mapGrp.FirstOrDefault(kvp =>
                    Path.GetFileName(kvp.Key).Equals("mobile.des", StringComparison.OrdinalIgnoreCase)
                );
                if (desFile.Value is { } desData && desData.Length >= 4)
                {
                    const int oidSize = 24;
                    var desCount = desData.Length / oidSize;
                    if (desData.Length % oidSize == 0 && desCount > 0)
                    {
                        sb.Append("  |  ");
                        sb.Append(desCount);
                        sb.Append(" object(s) destroyed");
                    }
                }

                // Count modified objects (.md) — walk the proper framing
                var mdFile = mapGrp.FirstOrDefault(kvp =>
                    Path.GetFileName(kvp.Key).Equals("mobile.md", StringComparison.OrdinalIgnoreCase)
                );
                if (mdFile.Value is { } mdData && mdData.Length > 0)
                {
                    var mdCount = CountMdObjects(mdData);
                    if (mdCount > 0)
                    {
                        sb.Append("  |  ");
                        sb.Append(mdCount);
                        sb.Append(" object(s) modified");
                    }
                }

                // Count dynamic mobiles (.mdy)
                var mdyFile = mapGrp.FirstOrDefault(kvp =>
                    Path.GetFileName(kvp.Key).Equals("mobile.mdy", StringComparison.OrdinalIgnoreCase)
                );
                if (mdyFile.Value is { } mdyData && mdyData.Length > 0)
                {
                    var mdyCount = CountStartMarkers(mdyData);
                    if (mdyCount > 0)
                    {
                        sb.Append("  |  ");
                        sb.Append(mdyCount);
                        sb.Append(" dynamic mobile(s)");
                    }
                }

                // Count .dif files
                var difCount = mapGrp.Count(kvp =>
                    Path.GetExtension(kvp.Key).Equals(".dif", StringComparison.OrdinalIgnoreCase)
                );
                if (difCount > 0)
                {
                    sb.Append("  |  ");
                    sb.Append(difCount);
                    sb.Append(" object diff(s)");
                }

                sb.AppendLine();
            }
            sb.AppendLine();
        }

        sb.AppendLine('\u2550', 72);
        return sb.ToString();
    }

    /// <summary>Counts how many START sentinel dwords (0x12344321) exist in <paramref name="data"/>,
    /// stepping by 4 bytes (aligned scan only).</summary>
    private static int CountStartMarkers(byte[] data)
    {
        const uint StartMarker = 0x12344321u;
        var count = 0;
        for (var i = 0; i + 4 <= data.Length; i += 4)
            if (BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(i, 4)) == StartMarker)
                count++;
        return count;
    }

    /// <summary>
    /// Counts modified objects in a <c>mobile.md</c> blob by walking its
    /// [24-byte OID][4-byte version][4-byte START][object data][END] framing,
    /// using <see cref="MobFormat.Parse"/> to correctly advance past each object.
    /// </summary>
    private static int CountMdObjects(byte[] data)
    {
        const int OidSize = 24;
        const uint StartMarker = 0x12344321u;
        const uint EndMarker = 0x23455432u;
        var span = data.AsSpan();
        var count = 0;
        var pos = 0;
        while (pos + OidSize + 8 <= data.Length)
        {
            pos += OidSize; // skip OID
            var version = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pos, 4));
            pos += 4;
            if (version != 0x08 && version != 0x77)
                break;
            var start = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(pos, 4));
            pos += 4;
            if (start != StartMarker)
                break;
            count++;

            // Use MobFormat.Parse (mirroring DumpModifiedObjects) to advance exactly past the object.
            var remaining = span.Slice(pos);
            // Prepend the 4-byte version header that MobFormat.Parse expects.
#pragma warning disable CA2014 // 256-B seed; ValueByteBuffer grows via ArrayPool (not stack) when the record is larger
            Span<byte> combinedInitial = stackalloc byte[256];
#pragma warning restore CA2014
            using var combinedBuf = new ValueByteBuffer(combinedInitial);
            combinedBuf.WriteInt32LittleEndian(version);
            combinedBuf.Write(remaining);
            try
            {
                var reader = new SpanReader(combinedBuf.WrittenSpan);
                MobFormat.Parse(ref reader);
                var consumed = reader.Position - 4;
                pos += consumed;
                if (pos + 4 <= data.Length && BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(pos, 4)) == EndMarker)
                    pos += 4;
            }
            catch (Exception)
            {
                // Parse failed — scan byte-by-byte for END marker so we can continue counting.
                var found = false;
                for (var i = 0; i <= remaining.Length - 4; i++)
                {
                    if (BinaryPrimitives.ReadUInt32LittleEndian(remaining.Slice(i, 4)) == EndMarker)
                    {
                        pos += i + 4;
                        found = true;
                        break;
                    }
                }
                if (!found)
                    break;
            }
        }
        return count;
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
            IsCompactDifFormat(d) ? DumpCompactDif(d) : MobDumper.Dump(MobFormat.ParseMemory(d)),

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
    private static string? ParseAndDump(string fileName, string ext, byte[] data) =>
        s_handlers.TryGetValue(ext, out var handler) ? handler(fileName, data) : null;

    // ── Save-specific format parsers ──────────────────────────────────────────

    /// <summary>
    /// Parses a <c>.mdy</c> file (dynamic mobile objects) as a sequence of
    /// back-to-back <c>obj_write</c> records — identical binary layout to <c>.mob</c>.
    /// Some sectors prefix or separate records with a 4-byte sentinel (0xFFFFFFFF).
    /// </summary>
    private static string DumpMultipleMobs(ReadOnlyMemory<byte> mem)
    {
        const uint Sentinel = 0xFFFFFFFF;
        var span = mem.Span;
        var reader = new SpanReader(span);
        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);
        var count = 0;
        var skipped = 0;

        while (reader.Remaining >= 4)
        {
            // Skip sentinel dwords (0xFFFFFFFF) that appear before, between, or after objects.
            if (unchecked((uint)reader.PeekInt32At(0)) == Sentinel)
            {
                reader.Skip(4);
                skipped++;
                continue;
            }

            // If next 4 bytes aren't a valid version, the stream is exhausted or corrupted.
            var nextVersion = reader.PeekInt32At(0);
            if (nextVersion != 0x08 && nextVersion != 0x77)
                break;

            var posBeforeParse = reader.Position;
            try
            {
                var mob = MobFormat.Parse(ref reader);
                count++;
                sb.Append("--- object ");
                sb.Append(count);
                sb.AppendLine(" ---");
                sb.Append(MobDumper.Dump(mob));
            }
            catch (Exception ex)
            {
                sb.Append("--- object ");
                sb.Append(count + 1);
                sb.Append(" parse failed at byte ");
                sb.Append(posBeforeParse);
                sb.Append(": ");
                sb.Append(ex.Message);
                sb.AppendLine(" (stopping) ---");
                break;
            }
        }

        if (count == 0 && skipped == 0)
        {
            sb.Dispose();
            return "No dynamic mobile objects";
        }

        var suffix = skipped > 0 ? $" ({skipped} sentinel dword(s) skipped)" : "";
        return $"Dynamic mobile objects: {count}{suffix}\n{sb.ToString()}";
    }

    private static bool IsCompactDifFormat(byte[] data)
    {
        // Compact obj_dif_write .dif files come in three variants:
        //
        // Variant A — magic=0x08, 4-byte preamble, records: B+C+D+START+data+END
        //   D=0x77 at [12-15], START=0x12344321 at [16-19]
        //
        // Variant B — magic=0x08, 4-byte preamble, records: C+D+START+data+END (no B field)
        //   D=0x77 at [8-11], START=0x12344321 at [12-15]
        //
        // Variant C — magic=0x18, variable preamble.  0x18 is not a valid mob version
        //   (MobFormat only accepts 0x08 and 0x77), so any .dif with this magic must be
        //   a compact dif.  We accept it unconditionally and scan for START sentinels in
        //   DumpCompactDif rather than relying on hard-coded preamble offsets.
        if (data.Length < 8)
            return false;

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0));

        if (magic == 8u)
        {
            // Variant A
            if (
                data.Length >= 20
                && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12)) == 0x00000077u
                && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(16)) == 0x12344321u
            )
                return true;

            // Variant B
            if (
                BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(8)) == 0x00000077u
                && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12)) == 0x12344321u
            )
                return true;

            return false;
        }

        // Variant C: 0x18 is not a valid mob object version → definitely a compact dif.
        return magic == 0x18u;
    }

    /// <summary>
    /// Parses a compact <c>obj_dif_write</c> block sequence stored in a solitary <c>.dif</c> file.
    /// <para>
    /// Three format variants are supported (all detected by <see cref="IsCompactDifFormat"/>):
    /// <list type="bullet">
    ///   <item><b>Variant A</b> — magic=0x08, 4-byte preamble, records: B+C+D+START+data+END.</item>
    ///   <item><b>Variant B</b> — magic=0x08, 4-byte preamble, records: C+D+START+data+END (no B).</item>
    ///   <item><b>Variant C</b> — magic=0x18, variable preamble, records scanned via START/END sentinels.</item>
    /// </list>
    /// </para>
    /// </summary>
    private static string DumpCompactDif(byte[] data)
    {
        const uint StartMarker = 0x12344321u;
        const uint EndMarker = 0x23455432u;
        var span = data.AsSpan();
        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);

        var magic = BinaryPrimitives.ReadInt32LittleEndian(span);
        sb.Append("obj_dif_write compact format  (magic=0x");
        sb.Append(magic, "X2");
        sb.AppendLine(")");

        if (magic == 0x18)
        {
            // Variant C: preamble layout is not fully reversed.
            // Scan for START/END sentinel pairs directly rather than assuming fixed offsets.
            var pos = 4;
            var recIdx = 0;
            while (pos + 4 <= data.Length)
            {
                var word = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
                if (word == StartMarker)
                {
                    pos += 4; // skip START
                    var dataStart = pos;
                    while (pos + 4 <= data.Length)
                    {
                        if (BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]) == EndMarker)
                            break;
                        pos += 4;
                    }
                    var dataLen = pos - dataStart;
                    pos += 4; // skip END
                    recIdx++;
                    sb.Append("  [record ");
                    sb.Append(recIdx);
                    sb.Append("]  data=");
                    sb.Append(dataLen);
                    sb.AppendLine(" bytes");
                }
                else
                {
                    pos += 4;
                }
            }
            if (recIdx == 0)
                sb.AppendLine("  (no records found — START sentinel 0x12344321 not present)");
            return sb.ToString();
        }

        // magic == 0x08: Variant A has D at file-offset 12 (record has B+C+D+START);
        // Variant B has D at file-offset 8 (record has C+D+START, no B).
        var preambleSize = 4;
        var hasB = data.Length >= 16 && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12)) == 0x00000077u;

        var scanPos = preambleSize;
        var minHeader = hasB ? 16 : 12; // bytes consumed per record header
        var recordIdx = 0;

        while (scanPos + minHeader <= data.Length)
        {
            var b = 0;
            if (hasB)
            {
                b = BinaryPrimitives.ReadInt32LittleEndian(span[scanPos..]);
                scanPos += 4;
            }

            var c = BinaryPrimitives.ReadUInt32LittleEndian(span[scanPos..]);
            scanPos += 4;
            var d = BinaryPrimitives.ReadUInt32LittleEndian(span[scanPos..]);
            scanPos += 4;

            if ((c & 0x80000000u) == 0 || d != 0x00000077u)
                break; // C must have high bit set; D must be 0x77

            var startMark = BinaryPrimitives.ReadUInt32LittleEndian(span[scanPos..]);
            scanPos += 4;
            if (startMark != StartMarker)
                break;

            // Scan forward for end marker.
            var dataStart = scanPos;
            while (scanPos + 4 <= data.Length)
            {
                var word = BinaryPrimitives.ReadUInt32LittleEndian(span[scanPos..]);
                if (word == EndMarker)
                    break;
                scanPos += 4;
            }

            var dataLen = scanPos - dataStart;
            scanPos += 4; // skip END marker
            recordIdx++;

            if (hasB)
            {
                sb.Append("  [record ");
                sb.Append(recordIdx);
                sb.Append("]  B=");
                sb.Append(b);
                sb.Append("  data=");
                sb.Append(dataLen);
                sb.AppendLine(" bytes");
            }
            else
            {
                sb.Append("  [record ");
                sb.Append(recordIdx);
                sb.Append("]  data=");
                sb.Append(dataLen);
                sb.AppendLine(" bytes");
            }
        }

        if (scanPos < data.Length)
        {
            var trailing = BinaryPrimitives.ReadInt32LittleEndian(span[scanPos..]);
            sb.AppendHex((uint)trailing, "  trailing=0x".AsSpan());
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses a <c>mobile.des</c> file: a flat array of 24-byte <c>ObjectID</c> structs
    /// identifying extinct or destroyed non-dynamic non-static objects.
    /// </summary>
    private static string DumpDestroyedObjects(ReadOnlyMemory<byte> mem)
    {
        const int OidSize = 24; // sizeof(ObjectID)
        var span = mem.Span;

        if (span.Length == 0)
            return "No destroyed objects";

        if (span.Length % OidSize != 0)
            return $"Destroyed objects: warning — size {span.Length} is not a multiple of 24 (ObjectID size)";

        var count = span.Length / OidSize;
        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);
        sb.Append("Destroyed/extinct objects: ");
        sb.AppendLine(count);

        var reader = new SpanReader(span);
        for (var i = 0; i < count; i++)
        {
            var oid = GameObjectGuid.Read(ref reader);
            sb.Append("  [");
            sb.Append(i + 1);
            sb.Append("] ");
            sb.AppendLine(oid.ToString());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses a <c>mobile.md</c> file: alternating [<c>ObjectID</c>][<c>obj_dif_write</c> block] records.
    /// <para>
    /// Each block is an obj_write payload (ProtoId + ObjectId + type + bitmap + properties) preceded
    /// by the version and sentinels.  The payload is parsed directly so we never have to scan for the
    /// end-sentinel (which would wrongly stop at coincidental matches inside property data).
    /// </para>
    /// </summary>
    private static string DumpModifiedObjects(ReadOnlyMemory<byte> mem)
    {
        const int OidSize = 24;
        const int Version8 = 0x08;
        const int Version77 = 0x77;
        const uint StartMarker = 0x12344321u;
        const uint EndMarker = 0x23455432u;

        var span = mem.Span;
        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);
        var count = 0;
        var pos = 0;

        while (pos + OidSize + 8 <= span.Length)
        {
            // Read file-level ObjectID (identifies which map object received the diff).
            var oidReader = new SpanReader(span.Slice(pos, OidSize));
            var fileOid = GameObjectGuid.Read(ref oidReader);
            pos += OidSize;

            // Read version integer (expected: 0x77 = 119, or 0x08 = 8).
            if (pos + 4 > span.Length)
                break;
            var version = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pos, 4));
            pos += 4;

            if (version != Version8 && version != Version77)
            {
                sb.Append("  (unexpected version ");
                sb.Append(version);
                sb.Append(" at byte ");
                sb.Append(pos - 4);
                sb.AppendLine(" — stopping)");
                break;
            }

            // Read start-of-block sentinel.
            if (pos + 4 > span.Length)
                break;
            var startMark = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(pos, 4));
            pos += 4;

            if (startMark != StartMarker)
            {
                sb.AppendHex(startMark, "  (start marker mismatch 0x".AsSpan());
                sb.Append(" at byte ");
                sb.Append(pos - 4);
                sb.AppendLine(" — stopping)");
                break;
            }

            count++;

            // Build a temporary stream: [version bytes][rest of span from pos].
            // This lets MobFormat.Parse determine the exact object size without
            // any endMarker scanning (scanning causes false matches in property data).
            var remaining = span.Slice(pos);
#pragma warning disable CA2014 // 256-B seed; ValueByteBuffer grows via ArrayPool (not stack) when the record is larger
            Span<byte> combinedInitial = stackalloc byte[256];
#pragma warning restore CA2014
            using var combinedBuf = new ValueByteBuffer(combinedInitial);
            combinedBuf.WriteInt32LittleEndian(version);
            combinedBuf.Write(remaining);

            try
            {
                var combinedReader = new SpanReader(combinedBuf.WrittenSpan);
                var mob = MobFormat.Parse(ref combinedReader);

                // combinedReader consumed (4 prepended + object bytes); subtract prepended offset.
                var consumedInOriginal = combinedReader.Position - 4;

                // Expect end marker immediately after the parsed object.
                if (consumedInOriginal + 4 <= remaining.Length)
                {
                    var endMark = BinaryPrimitives.ReadUInt32LittleEndian(remaining.Slice(consumedInOriginal, 4));
                    if (endMark != EndMarker)
                    {
                        sb.Append("  WARNING: end marker missing after object ");
                        sb.AppendLine(count);
                    }
                    pos += consumedInOriginal + 4; // advance past object + end marker
                }
                else
                {
                    pos += consumedInOriginal;
                }

                sb.Append("  [");
                sb.Append(count);
                sb.Append("] ");
                sb.Append(fileOid.ToString());
                sb.Append("  →  ");
                sb.Append(mob.Header.GameObjectType.ToString());
                sb.Append(" (proto ");
                sb.Append(mob.Header.ProtoId.ToString());
                sb.AppendLine(")");
                sb.Append(MobDumper.Dump(mob));
            }
            catch (Exception ex)
            {
                // Cannot parse the object (unknown bits, truncated, wrong format).
                // Fall back to scanning for the endMarker so we can report the GUID and continue.
                var foundEnd = false;
                for (var i = 0; i <= remaining.Length - 4; i++)
                {
                    if (BinaryPrimitives.ReadUInt32LittleEndian(remaining.Slice(i, 4)) == EndMarker)
                    {
                        pos += i + 4;
                        foundEnd = true;
                        break;
                    }
                }

                if (!foundEnd)
                {
                    sb.Append("  [");
                    sb.Append(count);
                    sb.Append("] ");
                    sb.Append(fileOid.ToString());
                    sb.Append("  (parse failed: ");
                    sb.Append(ex.Message);
                    sb.AppendLine("; end marker not found — stopping)");
                    break;
                }

                sb.Append("  [");
                sb.Append(count);
                sb.Append("] ");
                sb.Append(fileOid.ToString());
                sb.Append("  (parse failed: ");
                sb.Append(ex.Message);
                sb.AppendLine(")");
            }
        }

        return count == 0 ? "No modified objects" : $"Modified objects: {count}\n{sb.ToString()}";
    }

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
    private static string DumpTimeEvents(ReadOnlyMemory<byte> mem)
    {
        var span = mem.Span;
        if (span.Length < 4)
            return "TimeEvent.dat: too short to contain event count";

        var count = BinaryPrimitives.ReadInt32LittleEndian(span);
        Span<char> sbBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(sbBuf);
        sb.Append("TimeEvent.dat — ");
        sb.Append(count);
        sb.Append(" scheduled event(s)  (");
        sb.Append(mem.Length);
        sb.AppendLine(" bytes total)");

        // Show as many event headers as we can parse (8 + 4 = 12 bytes per header minimum).
        var reader = new SpanReader(span.Slice(4)); // skip count
        for (var i = 0; i < count && reader.Remaining >= 12; i++)
        {
            var days = reader.ReadInt32();
            var ms = reader.ReadInt32();
            var type = reader.ReadInt32();
            sb.Append("  [");
            sb.Append(i + 1);
            sb.Append("] Day ");
            sb.Append(days);
            sb.Append(", +");
            sb.Append(ms);
            sb.Append(" ms, type=");
            sb.AppendLine(type);

            // Cannot safely consume param data without TimeEventTypeInfo.flags —
            // stop after first event to avoid mis-parsing the rest.
            if (i == 0 && count > 1)
            {
                sb.Append("  ... (+");
                sb.Append(count - 1);
                sb.AppendLine(" more — param layout requires TimeEventTypeInfo table)");
                break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses a <c>.tmf</c> (Town Map Fog) file: a raw bit-array where each bit
    /// represents one map tile.  A set bit means the tile has been revealed.
    /// </summary>
    private static string DumpTownMapFog(ReadOnlyMemory<byte> mem)
    {
        var span = mem.Span;
        var knownBits = 0;
        for (var i = 0; i < span.Length; i++)
            knownBits += BitOperations.PopCount(span[i]);

        var totalBits = span.Length * 8;
        var coverage = totalBits > 0 ? 100.0 * knownBits / totalBits : 0.0;
        return $"Town map fog: {knownBits}/{totalBits} tiles revealed ({coverage:F1}%)  ({span.Length} bytes)";
    }

    private static string GetDirectory(string virtualPath)
    {
        var slash = virtualPath.LastIndexOf('/');
        return slash < 0 ? string.Empty : virtualPath[..slash];
    }
}
