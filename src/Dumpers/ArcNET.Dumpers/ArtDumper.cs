using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="ArtFile"/> instance.
/// Pixel data is summarised (not dumped byte-by-byte).
/// </summary>
public static class ArtDumper
{
    public static string Dump(ArtFile art)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ART FILE ===");

        var typeLabel = art.Flags switch
        {
            ArtFlags.Static => "static sprite (1 direction)",
            ArtFlags.Critter => "critter animation (8 directions)",
            ArtFlags.Font => "font glyph sheet",
            _ when art.Flags == ArtFlags.None => "unknown type (no flags set)",
            _ => $"{art.Flags}  (0x{(uint)art.Flags:X8})",
        };
        sb.AppendLine($"  Type           : {typeLabel}");
        sb.AppendLine(
            $"  Animation      : {art.FrameCount} frame(s) at {art.FrameRate} fps"
                + (art.ActionFrame > 0 ? $", key frame at index {art.ActionFrame}" : "")
        );
        sb.AppendLine($"  Rotations      : {art.EffectiveRotationCount}");
        if (art.Unknown0.Any(v => v != 0))
            sb.AppendLine($"  Service data 0 : [{string.Join(", ", art.Unknown0.Select(v => $"0x{v:X8}"))}]");
        if (art.Unknown2.Any(v => v != 0))
            sb.AppendLine($"  Service data 2 : [{string.Join(", ", art.Unknown2.Select(v => $"0x{v:X8}"))}]");
        sb.AppendLine();

        // Palettes
        for (var slot = 0; slot < 4; slot++)
        {
            var pal = art.Palettes[slot];
            if (pal is null)
            {
                if (art.PaletteIds[slot] != 0)
                    sb.AppendLine($"  Palette {slot}      : absent  (id={art.PaletteIds[slot]})");
            }
            else
            {
                sb.AppendLine($"  Palette {slot}      : {pal.Length} entries  (id={art.PaletteIds[slot]})");
                sb.AppendLine($"    [ 0] BGR=({pal[0].Blue},{pal[0].Green},{pal[0].Red})  (transparency)");
                if (pal.Length > 1)
                    sb.AppendLine($"    [{pal.Length - 1, 3}] BGR=({pal[^1].Blue},{pal[^1].Green},{pal[^1].Red})");
            }
        }

        sb.AppendLine();

        // Frames
        for (var r = 0; r < art.EffectiveRotationCount; r++)
        {
            sb.AppendLine(
                art.EffectiveRotationCount > 1 ? $"  --- Rotation {r} (direction {r * 45}°) ---" : "  --- Frames ---"
            );
            for (var f = 0; f < (int)art.FrameCount; f++)
            {
                var frame = art.Frames[r][f];
                var h = frame.Header;
                var compressed = h.DataSize < h.Width * h.Height;
                sb.AppendLine(
                    $"    [{f, 3}] {h.Width}×{h.Height}  center=({h.CenterX},{h.CenterY})  delta=({h.DeltaX},{h.DeltaY})"
                        + $"  {h.DataSize}B {(compressed ? "RLE" : "raw")}  pixels={frame.Pixels.Length}B"
                );
            }
        }

        return sb.ToString();
    }

    public static void Dump(ArtFile art, TextWriter writer) => writer.Write(Dump(art));
}
