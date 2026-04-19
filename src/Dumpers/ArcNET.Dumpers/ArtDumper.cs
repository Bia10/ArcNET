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
        vsb.Append("  Type           : ");
        vsb.AppendLine(typeLabel);
        vsb.Append("  Animation      : ");
        vsb.Append(art.FrameCount);
        vsb.Append(" frame(s) at ");
        vsb.Append(art.FrameRate);
        vsb.Append(" fps");
        if (art.ActionFrame > 0)
        {
            vsb.Append(", key frame at index ");
            vsb.Append(art.ActionFrame);
        }
        vsb.AppendLine();
        vsb.Append("  Rotations      : ");
        vsb.AppendLine(art.EffectiveRotationCount);
        if (art.PaletteData1.Any(v => v != 0))
        {
            vsb.Append("  Palette data 1 : [");
            for (var i = 0; i < art.PaletteData1.Length; i++)
            {
                if (i > 0)
                    vsb.Append(", ");
                vsb.AppendHex(art.PaletteData1[i], "0x".AsSpan());
            }
            vsb.AppendLine("]");
        }
        if (art.PaletteData2.Any(v => v != 0))
        {
            vsb.Append("  Palette data 2 : [");
            for (var i = 0; i < art.PaletteData2.Length; i++)
            {
                if (i > 0)
                    vsb.Append(", ");
                vsb.AppendHex(art.PaletteData2[i], "0x".AsSpan());
            }
            vsb.AppendLine("]");
        }
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
            if (art.EffectiveRotationCount > 1)
                vsb.AppendLine($"  --- Rotation {r} (direction {r * 45}\u00B0) ---");
            else
                vsb.AppendLine("  --- Frames ---");

            for (var f = 0; f < (int)art.FrameCount; f++)
            {
                var frame = art.Frames[r][f];
                var h = frame.Header;
                var compressed = h.DataSize < h.Width * h.Height;
                vsb.AppendLine(
                    $"    [{f, 3}] {h.Width}\u00D7{h.Height}  center=({h.CenterX},{h.CenterY})  delta=({h.DeltaX},{h.DeltaY})  {h.DataSize}B {(compressed ? "RLE" : "raw")}  pixels={frame.Pixels.Length}B"
                );
            }
        }

        return vsb.ToString();
    }

    public static void Dump(ArtFile art, TextWriter writer) => writer.Write(Dump(art));
}
