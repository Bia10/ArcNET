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
            var nonZeroCounters = script.Counters.Select((c, i) => (c, i)).Where(p => p.c != 0).ToList();
            if (nonZeroCounters.Count > 0)
                vsb.AppendLine(
                    $"    Counters   : " + string.Join(", ", nonZeroCounters.Select(p => $"[{p.i}]=0x{p.c:X2}"))
                );
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
        if (blockedTiles > 0)
        {
            for (var row = 0; row < 64; row++)
            {
                var loWord = sector.BlockMask[row * 2];
                var hiWord = sector.BlockMask[row * 2 + 1];
                vsb.Append("    ");
                for (var col = 0; col < 32; col++)
                    vsb.Append((loWord & (1u << col)) != 0 ? '#' : '.');
                for (var col = 0; col < 32; col++)
                    vsb.Append((hiWord & (1u << col)) != 0 ? '#' : '.');
                vsb.AppendLine();
            }
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
