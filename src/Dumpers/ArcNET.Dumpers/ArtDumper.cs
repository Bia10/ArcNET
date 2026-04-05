using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="ArtFile"/> instance.
/// Pixel data is summarised (not dumped byte-by-byte).
/// </summary>
public static class ArtDumper
{
    public static string Dump(ArtFile art)
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== ART FILE ===");

        var typeLabel = art.Flags switch
        {
            ArtFlags.Static => "static sprite (1 direction)",
            ArtFlags.Critter => "critter animation (8 directions)",
            ArtFlags.Font => "font glyph sheet",
            _ when art.Flags == ArtFlags.None => "unknown type (no flags set)",
            _ => $"{art.Flags}  (0x{(uint)art.Flags:X8})",
        };
        vsb.AppendLine($"  Type           : {typeLabel}");
        vsb.AppendLine(
            $"  Animation      : {art.FrameCount} frame(s) at {art.FrameRate} fps"
                + (art.ActionFrame > 0 ? $", key frame at index {art.ActionFrame}" : "")
        );
        vsb.AppendLine($"  Rotations      : {art.EffectiveRotationCount}");
        if (art.PaletteData1.Any(v => v != 0))
            vsb.AppendLine($"  Palette data 1 : [{string.Join(", ", art.PaletteData1.Select(v => $"0x{v:X8}"))}]");
        if (art.PaletteData2.Any(v => v != 0))
            vsb.AppendLine($"  Palette data 2 : [{string.Join(", ", art.PaletteData2.Select(v => $"0x{v:X8}"))}]");
        vsb.AppendLine();

        // Palettes
        for (var slot = 0; slot < 4; slot++)
        {
            var pal = art.Palettes[slot];
            if (pal is null)
            {
                if (art.PaletteIds[slot] != 0)
                    vsb.AppendLine($"  Palette {slot}      : absent  (id={art.PaletteIds[slot]})");
            }
            else
            {
                vsb.AppendLine($"  Palette {slot}      : {pal.Length} entries  (id={art.PaletteIds[slot]})");
                vsb.AppendLine($"    [ 0] BGR=({pal[0].Blue},{pal[0].Green},{pal[0].Red})  (transparency)");
                if (pal.Length > 1)
                    vsb.AppendLine($"    [{pal.Length - 1, 3}] BGR=({pal[^1].Blue},{pal[^1].Green},{pal[^1].Red})");
            }
        }

        vsb.AppendLine();

        // Frames
        for (var r = 0; r < art.EffectiveRotationCount; r++)
        {
            vsb.AppendLine(
                art.EffectiveRotationCount > 1 ? $"  --- Rotation {r} (direction {r * 45}°) ---" : "  --- Frames ---"
            );
            for (var f = 0; f < (int)art.FrameCount; f++)
            {
                var frame = art.Frames[r][f];
                var h = frame.Header;
                var compressed = h.DataSize < h.Width * h.Height;
                vsb.AppendLine(
                    $"    [{f, 3}] {h.Width}×{h.Height}  center=({h.CenterX},{h.CenterY})  delta=({h.DeltaX},{h.DeltaY})"
                        + $"  {h.DataSize}B {(compressed ? "RLE" : "raw")}  pixels={frame.Pixels.Length}B"
                );
            }
        }

        return vsb.ToString();
    }

    public static void Dump(ArtFile art, TextWriter writer) => writer.Write(Dump(art));
}
