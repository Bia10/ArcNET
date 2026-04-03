using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="Sector"/> instance.
/// </summary>
public static class SectorDumper
{
    public static string Dump(Sector sector)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== SECTOR ===");
        sb.AppendLine($"  Lights           : {sector.Lights.Count}");
        sb.AppendLine($"  TownmapInfo      : {sector.TownmapInfo}");
        sb.AppendLine($"  AptitudeAdj      : {sector.AptitudeAdjustment}");
        sb.AppendLine($"  LightSchemeIdx   : {sector.LightSchemeIdx}");
        sb.AppendLine($"  HasRoofs         : {sector.HasRoofs}");
        sb.AppendLine($"  TileScripts      : {sector.TileScripts.Count}");
        sb.AppendLine($"  Objects          : {sector.Objects.Count}");
        sb.AppendLine();

        // Sound
        sb.AppendLine("  --- Sound ---");
        sb.AppendLine($"    MusicSchemeIdx   : {sector.SoundList.MusicSchemeIdx}");
        sb.AppendLine($"    AmbientSchemeIdx : {sector.SoundList.AmbientSchemeIdx}");
        sb.AppendLine($"    Flags            : 0x{sector.SoundList.Flags:X8}");
        sb.AppendLine();

        // Sector script
        if (sector.SectorScript is { } script)
        {
            sb.AppendLine("  --- Sector Script ---");
            sb.AppendLine($"    ScriptId   : {script.ScriptId}");
            sb.AppendLine($"    Flags      : {script.Flags}");
            sb.AppendLine($"    Counters   : [{string.Join(", ", script.Counters.Select(c => $"0x{c:X2}"))}]");
            sb.AppendLine();
        }

        // Lights
        if (sector.Lights.Count > 0)
        {
            sb.AppendLine("  --- Lights ---");
            for (var i = 0; i < sector.Lights.Count; i++)
            {
                var l = sector.Lights[i];
                sb.AppendLine(
                    $"    [{i, 3}] tile=({l.TileX},{l.TileY})  offset=({l.OffsetX},{l.OffsetY})  flags=0x{l.Flags:X8}  art={l.ArtId}  RGB=({l.R},{l.G},{l.B})  tint=0x{l.TintColor:X8}  obj=0x{l.ObjHandle:X16}"
                );
            }
            sb.AppendLine();
        }

        // Tile art ID distribution (summary, not all 4096)
        var distinctTiles = sector.Tiles.Distinct().Count();
        sb.AppendLine($"  --- Tiles (4096 total, {distinctTiles} distinct art IDs) ---");
        var tileGroups = sector.Tiles.GroupBy(t => t).OrderByDescending(g => g.Count()).Take(10);
        foreach (var g in tileGroups)
        {
            sb.AppendLine($"    art={g.Key, 5}  count={g.Count()}");
        }

        if (distinctTiles > 10)
            sb.AppendLine($"    ... and {distinctTiles - 10} more");
        sb.AppendLine();

        // Roofs
        if (sector.HasRoofs && sector.Roofs is not null)
        {
            var distinctRoofs = sector.Roofs.Distinct().Count();
            sb.AppendLine($"  --- Roofs (256 total, {distinctRoofs} distinct art IDs) ---");
            var roofGroups = sector.Roofs.GroupBy(r => r).OrderByDescending(g => g.Count()).Take(5);
            foreach (var g in roofGroups)
            {
                sb.AppendLine($"    art={g.Key, 5}  count={g.Count()}");
            }

            sb.AppendLine();
        }

        // Tile scripts
        if (sector.TileScripts.Count > 0)
        {
            sb.AppendLine("  --- Tile Scripts ---");
            foreach (var ts in sector.TileScripts)
            {
                sb.AppendLine(
                    $"    tile={ts.TileId, 4}  script={ts.ScriptNum}  flags=0x{ts.ScriptFlags:X8}  counters=0x{ts.ScriptCounters:X8}  nodeFlags=0x{ts.NodeFlags:X8}"
                );
            }
            sb.AppendLine();
        }

        // Block mask summary
        var blockedTiles = 0;
        foreach (var mask in sector.BlockMask)
            blockedTiles += int.PopCount((int)mask);
        sb.AppendLine($"  --- Block Mask ({blockedTiles} blocked tiles) ---");
        sb.AppendLine();

        // Objects
        if (sector.Objects.Count > 0)
        {
            sb.AppendLine("  --- Objects ---");
            for (var i = 0; i < sector.Objects.Count; i++)
            {
                var mob = sector.Objects[i];
                sb.AppendLine($"    ┌─ Object [{i}] ─────────────────");
                var mobDump = MobDumper.Dump(mob);
                // Indent each mob dump line
                foreach (var line in mobDump.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    sb.Append("    │ ");
                    sb.AppendLine(line.TrimEnd('\r'));
                }
                sb.AppendLine("    └──────────────────────────────");
            }
        }

        return sb.ToString();
    }

    public static void Dump(Sector sector, TextWriter writer) => writer.Write(Dump(sector));
}
