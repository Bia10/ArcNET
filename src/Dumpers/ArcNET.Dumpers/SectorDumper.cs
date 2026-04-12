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
        vsb.Append("  Contains ");
        vsb.Append(sector.Objects.Count);
        vsb.Append(" object(s), ");
        vsb.Append(sector.Lights.Count);
        vsb.Append(" light source(s), and ");
        vsb.Append(sector.TileScripts.Count);
        vsb.AppendLine(" tile script(s).");
        vsb.Append("  Roofs: ");
        vsb.Append(sector.HasRoofs ? "present" : "none");
        vsb.Append("  Light scheme: ");
        if (sector.LightSchemeIdx < 0)
            vsb.Append("none");
        else
            vsb.Append(sector.LightSchemeIdx);
        vsb.Append("  Townmap cache: ");
        vsb.Append(sector.TownmapInfo != 0 ? "yes" : "no");
        vsb.Append("  Encounter adjustment: ");
        vsb.AppendLine(sector.AptitudeAdjustment, "+#;-#;0");
        vsb.AppendLine();

        // Sound
        var music = sector.SoundList.MusicSchemeIdx < 0 ? "none" : sector.SoundList.MusicSchemeIdx.ToString();
        var ambient = sector.SoundList.AmbientSchemeIdx < 0 ? "none" : sector.SoundList.AmbientSchemeIdx.ToString();
        vsb.AppendLine($"  Sound \u2014 music scheme: {music}, ambient scheme: {ambient}");
        if (sector.SoundList.Flags != 0)
        {
            vsb.Append("    (runtime flags: ");
            vsb.AppendHex((uint)sector.SoundList.Flags, "0x".AsSpan());
            vsb.AppendLine(" — not meaningful for editor tools)");
        }
        vsb.AppendLine();

        // Sector script
        if (sector.SectorScript is { } script)
        {
            vsb.AppendLine("  --- Sector Script ---");
            vsb.Append("    Script ID  : ");
            vsb.AppendLine(script.ScriptId);
            vsb.Append("    Flags      : ");
            vsb.Append(script.Flags);
            vsb.Append("  (");
            vsb.AppendHex((uint)script.Flags, "0x".AsSpan());
            vsb.AppendLine(")");
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
                        counterParts.Append("]=");
                        counterParts.AppendHex(b, "0x".AsSpan());
                    }
                }
                vsb.Append("    Counters   : ");
                vsb.AppendLine(counterParts.WrittenSpan);
                counterParts.Dispose();
            }
            else
                vsb.AppendLine("    Counters   : (all zero)");
            vsb.AppendLine();
        }

        // Lights
        if (sector.Lights.Count > 0)
        {
            vsb.Append("  --- Lights (");
            vsb.Append(sector.Lights.Count);
            vsb.AppendLine(") ---");
            for (var i = 0; i < sector.Lights.Count; i++)
            {
                var l = sector.Lights[i];
                var flagLabel = l.Flags == SectorLightFlags.None ? "active" : l.Flags.ToString();
                vsb.Append("    [");
                vsb.AppendPadded<int>(i, 3, leftAlign: false);
                vsb.Append("] tile=(");
                vsb.Append(l.TileX);
                vsb.Append(',');
                vsb.Append(l.TileY);
                vsb.Append(")  offset=(");
                vsb.Append(l.OffsetX);
                vsb.Append(',');
                vsb.Append(l.OffsetY);
                vsb.Append(")  color=RGB(");
                vsb.Append(l.R);
                vsb.Append(',');
                vsb.Append(l.G);
                vsb.Append(',');
                vsb.Append(l.B);
                vsb.Append(")  art=");
                vsb.Append(l.ArtId);
                vsb.Append("  status=");
                vsb.Append(flagLabel);
                vsb.Append("  ");
                if (l.ObjHandle == -1L)
                    vsb.Append("standalone");
                else
                {
                    vsb.Append("attached to obj ");
                    vsb.AppendHex((ulong)l.ObjHandle, "0x".AsSpan());
                }
                vsb.AppendLine();
            }
            vsb.AppendLine();
        }

        // Tile art ID distribution (summary, not all 4096)
        var distinctTiles = sector.Tiles.Distinct().Count();
        vsb.Append("  --- Tile Art (");
        vsb.Append(distinctTiles);
        vsb.AppendLine(" distinct ground tile art IDs across 4096 tiles) ---");
        var tileGroups = sector.Tiles.GroupBy(t => t).OrderByDescending(g => g.Count()).Take(10);
        foreach (var g in tileGroups)
        {
            vsb.Append("    art ");
            vsb.AppendPadded<uint>(g.Key, 5, leftAlign: false);
            vsb.Append("  ×");
            vsb.AppendLine(g.Count());
        }
        if (distinctTiles > 10)
        {
            vsb.Append("    ... and ");
            vsb.Append(distinctTiles - 10);
            vsb.AppendLine(" more");
        }
        vsb.AppendLine();

        // Roofs
        if (sector.HasRoofs && sector.Roofs is not null)
        {
            var distinctRoofs = sector.Roofs.Distinct().Count();
            vsb.Append("  --- Roof Art (");
            vsb.Append(distinctRoofs);
            vsb.AppendLine(" distinct art IDs across 256 roof tiles) ---");
            foreach (var g in sector.Roofs.GroupBy(r => r).OrderByDescending(g => g.Count()).Take(5))
            {
                vsb.Append("    art ");
                vsb.AppendPadded<uint>(g.Key, 5, leftAlign: false);
                vsb.Append("  ×");
                vsb.AppendLine(g.Count());
            }
            vsb.AppendLine();
        }

        // Tile scripts
        if (sector.TileScripts.Count > 0)
        {
            vsb.Append("  --- Tile Scripts (");
            vsb.Append(sector.TileScripts.Count);
            vsb.AppendLine(") ---");
            vsb.AppendLine(
                "    (ScriptFlags and ScriptCounters are runtime state — only ScriptNum and NodeFlags are meaningful here)"
            );
            foreach (var ts in sector.TileScripts)
            {
                var nodeLabel = (ts.NodeFlags & 0x1) != 0 ? "modified" : "clean";
                vsb.Append("    tile ");
                vsb.AppendPadded<uint>(ts.TileId, 4, leftAlign: false);
                vsb.Append(": script ");
                vsb.Append(ts.ScriptNum);
                vsb.Append("  node=");
                vsb.Append(nodeLabel);
                if (ts.ScriptFlags != 0 || ts.ScriptCounters != 0)
                {
                    vsb.Append("  [runtime: flags=");
                    vsb.AppendHex(ts.ScriptFlags, "0x".AsSpan());
                    vsb.Append(" counters=");
                    vsb.AppendHex(ts.ScriptCounters, "0x".AsSpan());
                    vsb.Append(']');
                }
                vsb.AppendLine();
            }
            vsb.AppendLine();
        }

        // Block mask
        var blockedTiles = 0;
        foreach (var mask in sector.BlockMask)
            blockedTiles += int.PopCount((int)mask);
        vsb.Append("  --- Walkability (");
        vsb.Append(4096 - blockedTiles);
        vsb.Append("/4096 walkable, ");
        vsb.Append(blockedTiles);
        vsb.AppendLine("/4096 blocked) ---");
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
                vsb.Append("    blocked: ");
                if (s == e)
                {
                    vsb.Append("tile ");
                    vsb.Append(s);
                }
                else
                {
                    vsb.Append("tiles ");
                    vsb.Append(s);
                    vsb.Append('\u2013');
                    vsb.Append(e);
                    vsb.Append(" (");
                    vsb.Append(e - s + 1);
                    vsb.Append(')');
                }
                vsb.AppendLine();
            }
            if (runs.Count > 20)
            {
                vsb.Append("    ... and ");
                vsb.Append(runs.Count - 20);
                vsb.AppendLine(" more blocked run(s)");
            }
        }
        else if (blockedTiles == 4096)
        {
            vsb.AppendLine("    (all tiles blocked)");
        }

        vsb.AppendLine();

        // Objects
        if (sector.Objects.Count > 0)
        {
            vsb.Append("  --- Objects (");
            vsb.Append(sector.Objects.Count);
            vsb.AppendLine(") ---");
            for (var i = 0; i < sector.Objects.Count; i++)
            {
                var mob = sector.Objects[i];
                vsb.Append("    ┌─ Object [");
                vsb.Append(i);
                vsb.AppendLine("] ─────────────────");
                var mobDump = MobDumper.Dump(mob);
                foreach (var line in mobDump.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    vsb.Append("    │ ");
                    vsb.AppendLine(line.AsSpan().TrimEnd('\r'));
                }
                vsb.AppendLine("    └──────────────────────────────");
            }
        }

        return vsb.ToString();
    }

    public static void Dump(Sector sector, TextWriter writer) => writer.Write(Dump(sector));
}
