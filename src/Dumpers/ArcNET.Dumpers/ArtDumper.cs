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
        vsb.Append(typeLabel);
        vsb.AppendLine();
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
        vsb.Append(art.EffectiveRotationCount);
        vsb.AppendLine();
        if (art.PaletteData1.Any(v => v != 0))
        {
            vsb.Append("  Palette data 1 : [");
            for (var i = 0; i < art.PaletteData1.Length; i++)
            {
                if (i > 0)
                    vsb.Append(", ");
                AppendHexUInt32(ref vsb, art.PaletteData1[i]);
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
                AppendHexUInt32(ref vsb, art.PaletteData2[i]);
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
                {
                    vsb.Append("  Palette ");
                    vsb.Append(slot);
                    vsb.Append("      : absent  (id=");
                    vsb.Append(art.PaletteIds[slot]);
                    vsb.Append(')');
                    vsb.AppendLine();
                }
            }
            else
            {
                vsb.Append("  Palette ");
                vsb.Append(slot);
                vsb.Append("      : ");
                vsb.Append(pal.Length);
                vsb.Append(" entries  (id=");
                vsb.Append(art.PaletteIds[slot]);
                vsb.Append(')');
                vsb.AppendLine();
                vsb.Append("    [ 0] BGR=(");
                vsb.Append(pal[0].Blue);
                vsb.Append(',');
                vsb.Append(pal[0].Green);
                vsb.Append(',');
                vsb.Append(pal[0].Red);
                vsb.Append(")  (transparency)");
                vsb.AppendLine();
                if (pal.Length > 1)
                {
                    vsb.Append("    [");
                    AppendLeftPaddedInt(ref vsb, pal.Length - 1, 3);
                    vsb.Append("] BGR=(");
                    vsb.Append(pal[^1].Blue);
                    vsb.Append(',');
                    vsb.Append(pal[^1].Green);
                    vsb.Append(',');
                    vsb.Append(pal[^1].Red);
                    vsb.Append(')');
                    vsb.AppendLine();
                }
            }
        }

        vsb.AppendLine();

        // Frames
        for (var r = 0; r < art.EffectiveRotationCount; r++)
        {
            if (art.EffectiveRotationCount > 1)
            {
                vsb.Append("  --- Rotation ");
                vsb.Append(r);
                vsb.Append(" (direction ");
                vsb.Append(r * 45);
                vsb.Append("°) ---");
                vsb.AppendLine();
            }
            else
            {
                vsb.AppendLine("  --- Frames ---");
            }
            for (var f = 0; f < (int)art.FrameCount; f++)
            {
                var frame = art.Frames[r][f];
                var h = frame.Header;
                var compressed = h.DataSize < h.Width * h.Height;
                // TODO: replace this remaining composite line once ValueStringBuilder gains an interpolated-string handler.
                vsb.AppendLine(
                    $"    [{f, 3}] {h.Width}×{h.Height}  center=({h.CenterX},{h.CenterY})  delta=({h.DeltaX},{h.DeltaY})"
                        + $"  {h.DataSize}B {(compressed ? "RLE" : "raw")}  pixels={frame.Pixels.Length}B"
                );
            }
        }

        return vsb.ToString();
    }

    private static void AppendHexUInt32(ref ValueStringBuilder vsb, uint value)
    {
        vsb.Append("0x");
        vsb.AppendHex(value);
    }

    private static void AppendLeftPaddedInt(ref ValueStringBuilder vsb, int value, int width)
    {
        Span<char> buffer = stackalloc char[16];
        _ = value.TryFormat(buffer, out var written);
        for (var index = written; index < width; index++)
            vsb.Append(' ');
        vsb.Append(value);
    }

    public static void Dump(ArtFile art, TextWriter writer) => writer.Write(Dump(art));
}
