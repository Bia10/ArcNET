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
        sb.AppendLine($"  Flags          : 0x{art.Flags:X8}");
        sb.AppendLine($"  FrameRate      : {art.FrameRate}");
        sb.AppendLine($"  ActionFrame    : {art.ActionFrame}");
        sb.AppendLine($"  FrameCount     : {art.FrameCount}");
        sb.AppendLine($"  Rotations      : {art.EffectiveRotationCount}");
        sb.AppendLine($"  DataSizes      : [{string.Join(", ", art.DataSizes)}]");
        sb.AppendLine();

        // Palettes
        for (var slot = 0; slot < 4; slot++)
        {
            var pal = art.Palettes[slot];
            if (pal is null)
            {
                sb.AppendLine($"  Palette[{slot}]    : (absent, id={art.PaletteIds[slot]})");
            }
            else
            {
                sb.AppendLine($"  Palette[{slot}]    : {pal.Length} entries (id={art.PaletteIds[slot]})");
                // Show first/last entry for quick identification
                sb.AppendLine($"    [  0] B={pal[0].Blue, 3} G={pal[0].Green, 3} R={pal[0].Red, 3}  (transparency)");
                if (pal.Length > 1)
                    sb.AppendLine(
                        $"    [{pal.Length - 1, 3}] B={pal[^1].Blue, 3} G={pal[^1].Green, 3} R={pal[^1].Red, 3}"
                    );
            }
        }

        sb.AppendLine();

        // Frames
        for (var r = 0; r < art.EffectiveRotationCount; r++)
        {
            sb.AppendLine($"  --- Rotation {r} ---");
            for (var f = 0; f < (int)art.FrameCount; f++)
            {
                var frame = art.Frames[r][f];
                var h = frame.Header;
                sb.AppendLine(
                    $"    Frame[{f, 3}] {h.Width}x{h.Height}  center=({h.CenterX},{h.CenterY})  delta=({h.DeltaX},{h.DeltaY})  data={h.DataSize}B  pixels={frame.Pixels.Length}B"
                );
            }
        }

        return sb.ToString();
    }

    public static void Dump(ArtFile art, TextWriter writer) => writer.Write(Dump(art));
}
