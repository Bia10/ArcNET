using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="Sector"/> instance.
/// </summary>
public static class SectorDumper
{
    public static string Dump(Sector sector)
    {
        Span<char> buf = stackalloc char[1024];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== SECTOR ===");
        vsb.AppendLine(
            $"  Contains {sector.Objects.Count} object(s), {sector.Lights.Count} light source(s), and {sector.TileScripts.Count} tile script(s)."
        );
        vsb.AppendLine(
            $"  Roofs: {(sector.HasRoofs ? "present" : "none")}  "
                + $"Light scheme: {(sector.LightSchemeIdx < 0 ? "none" : sector.LightSchemeIdx.ToString())}  "
                + $"Townmap cache: {(sector.TownmapInfo != 0 ? "yes" : "no")}  "
                + $"Encounter adjustment: {sector.AptitudeAdjustment:+#;-#;0}"
        );
        vsb.AppendLine();

        // Sound
        var music = sector.SoundList.MusicSchemeIdx < 0 ? "none" : sector.SoundList.MusicSchemeIdx.ToString();
        var ambient = sector.SoundList.AmbientSchemeIdx < 0 ? "none" : sector.SoundList.AmbientSchemeIdx.ToString();
        vsb.AppendLine($"  Sound — music scheme: {music}, ambient scheme: {ambient}");
        if (sector.SoundList.Flags != 0)
            vsb.AppendLine($"    (runtime flags: 0x{sector.SoundList.Flags:X8} — not meaningful for editor tools)");
        vsb.AppendLine();

        // Sector script
        if (sector.SectorScript is { } script)
        {
            vsb.AppendLine("  --- Sector Script ---");
            vsb.AppendLine($"    Script ID  : {script.ScriptId}");
            vsb.AppendLine($"    Flags      : {script.Flags}  (0x{(uint)script.Flags:X8})");
            // Counters is uint (4 LE bytes); extract individual bytes for display.
            var counters = script.Counters;
            var hasNonZeroCounter = counters != 0;
            if (hasNonZeroCounter)
            {
                Span<char> cpBuf = stackalloc char[64];
                var counterParts = new ValueStringBuilder(cpBuf);
                for (var ci = 0; ci < 4; ci++)
                {
                    var b = (byte)(counters >> (ci * 8));
                    if (b != 0)
                    {
                        if (counterParts.Length > 0)
                            counterParts.Append(", ");
                        counterParts.Append('[');
                        counterParts.Append(ci);
                        counterParts.Append("]=0x");
                        counterParts.Append(b, "X2");
                    }
                }
                vsb.Append("    Counters   : ");
                vsb.Append(counterParts.WrittenSpan);
                vsb.AppendLine();
                counterParts.Dispose();
            }
            else
                vsb.AppendLine("    Counters   : (all zero)");
            vsb.AppendLine();
        }

        // Lights
        if (sector.Lights.Count > 0)
        {
            vsb.AppendLine($"  --- Lights ({sector.Lights.Count}) ---");
            for (var i = 0; i < sector.Lights.Count; i++)
            {
                var l = sector.Lights[i];
                var flagLabel = l.Flags == SectorLightFlags.None ? "active" : l.Flags.ToString();
                var attached = l.ObjHandle == -1L ? "standalone" : $"attached to obj 0x{l.ObjHandle:X16}";
                vsb.AppendLine(
                    $"    [{i, 3}] tile=({l.TileX},{l.TileY})  offset=({l.OffsetX},{l.OffsetY})  "
                        + $"color=RGB({l.R},{l.G},{l.B})  art={l.ArtId}  status={flagLabel}  {attached}"
                );
            }
            vsb.AppendLine();
        }

        // Tile art ID distribution (summary, not all 4096)
        var distinctTiles = sector.Tiles.Distinct().Count();
        vsb.AppendLine($"  --- Tile Art ({distinctTiles} distinct ground tile art IDs across 4096 tiles) ---");
        var tileGroups = sector.Tiles.GroupBy(t => t).OrderByDescending(g => g.Count()).Take(10);
        foreach (var g in tileGroups)
            vsb.AppendLine($"    art {g.Key, 5}  ×{g.Count()}");
        if (distinctTiles > 10)
            vsb.AppendLine($"    ... and {distinctTiles - 10} more");
        vsb.AppendLine();

        // Roofs
        if (sector.HasRoofs && sector.Roofs is not null)
        {
            var distinctRoofs = sector.Roofs.Distinct().Count();
            vsb.AppendLine($"  --- Roof Art ({distinctRoofs} distinct art IDs across 256 roof tiles) ---");
            foreach (var g in sector.Roofs.GroupBy(r => r).OrderByDescending(g => g.Count()).Take(5))
                vsb.AppendLine($"    art {g.Key, 5}  ×{g.Count()}");
            vsb.AppendLine();
        }

        // Tile scripts
        if (sector.TileScripts.Count > 0)
        {
            vsb.AppendLine($"  --- Tile Scripts ({sector.TileScripts.Count}) ---");
            vsb.AppendLine(
                "    (ScriptFlags and ScriptCounters are runtime state — only ScriptNum and NodeFlags are meaningful here)"
            );
            foreach (var ts in sector.TileScripts)
            {
                var nodeLabel = (ts.NodeFlags & 0x1) != 0 ? "modified" : "clean";
                var runtimeNote =
                    ts.ScriptFlags != 0 || ts.ScriptCounters != 0
                        ? $"  [runtime: flags=0x{ts.ScriptFlags:X8} counters=0x{ts.ScriptCounters:X8}]"
                        : "";
                vsb.AppendLine($"    tile {ts.TileId, 4}: script {ts.ScriptNum}  node={nodeLabel}{runtimeNote}");
            }
            vsb.AppendLine();
        }

        // Block mask
        var blockedTiles = 0;
        foreach (var mask in sector.BlockMask)
            blockedTiles += int.PopCount((int)mask);
        vsb.AppendLine($"  --- Walkability ({4096 - blockedTiles}/4096 walkable, {blockedTiles}/4096 blocked) ---");
        if (blockedTiles > 0 && blockedTiles < 4096)
        {
            // Compact: list blocked tile indices grouped into runs, up to 20 runs shown.
            var runs = new List<(int Start, int End)>();
            var inRun = false;
            var runStart = 0;
            for (var tile = 0; tile < 4096; tile++)
            {
                var wordIdx = tile / 32;
                var bitIdx = tile % 32;
                var blocked = (sector.BlockMask[wordIdx] & (1u << bitIdx)) != 0;
                if (blocked && !inRun)
                {
                    runStart = tile;
                    inRun = true;
                }
                else if (!blocked && inRun)
                {
                    runs.Add((runStart, tile - 1));
                    inRun = false;
                }
            }
            if (inRun)
                runs.Add((runStart, 4095));

            var shown = Math.Min(runs.Count, 20);
            for (var ri = 0; ri < shown; ri++)
            {
                var (s, e) = runs[ri];
                var tileDesc = s == e ? $"tile {s}" : $"tiles {s}–{e} ({e - s + 1})";
                vsb.AppendLine($"    blocked: {tileDesc}");
            }
            if (runs.Count > 20)
                vsb.AppendLine($"    ... and {runs.Count - 20} more blocked run(s)");
        }
        else if (blockedTiles == 4096)
        {
            vsb.AppendLine("    (all tiles blocked)");
        }

        vsb.AppendLine();

        // Objects
        if (sector.Objects.Count > 0)
        {
            vsb.AppendLine($"  --- Objects ({sector.Objects.Count}) ---");
            for (var i = 0; i < sector.Objects.Count; i++)
            {
                var mob = sector.Objects[i];
                vsb.AppendLine($"    ┌─ Object [{i}] ─────────────────");
                var mobDump = MobDumper.Dump(mob);
                foreach (var line in mobDump.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    vsb.Append("    │ ");
                    vsb.AppendLine(line.TrimEnd('\r'));
                }
                vsb.AppendLine("    └──────────────────────────────");
            }
        }

        return vsb.ToString();
    }

    public static void Dump(Sector sector, TextWriter writer) => writer.Write(Dump(sector));
}
