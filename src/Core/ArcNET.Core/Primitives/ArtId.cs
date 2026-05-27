using System.Text.Unicode;

namespace ArcNET.Core.Primitives;

/// <summary>An opaque 32-bit identifier for an art resource.</summary>
public readonly record struct ArtId(uint Value)
    : IBinarySerializable<ArtId, SpanReader>,
        ISpanFormattable,
        IUtf8SpanFormattable
{
    /// <summary>Art type discriminator decoded from the upper nibble of the AID.</summary>
    public enum TypeCode : uint
    {
        Tile = 0,
        None = Tile,
        Wall = 1,
        Critter = 2,
        Portal = 3,
        Scenery = 4,
        Interface = 5,
        Item = 6,
        Container = 7,
        Misc = 8,
        Light = 9,
        Roof = 10,
        Facade = 11,
        Monster = 12,
        UniqueNpc = 13,
        EyeCandy = 14,
    }

    private const int ArtTypeShift = 28;
    private const uint ArtTypeMask = 0xFu << ArtTypeShift;
    private const int GenericFrameShift = 14;
    private const uint GenericFrameMask = 0x1Fu;
    private const int PaletteShift = 4;
    private const uint PaletteMask = 0x3u;
    private const int InterfaceFrameShift = 8;
    private const uint InterfaceFrameMask = 0xFFu;
    private const int FacadeNumberLowShift = 17;
    private const int FacadeNumberHighShift = 27;
    private const int FacadeFrameShift = 1;
    private const uint FacadeFrameMask = 0x3FFu;
    private const int FacadeTypeShift = 25;
    private const uint TileTypeMask = 0x1u;
    private const int TileTypeShift = 8;
    private const int LightFrameShift = 12;
    private const uint LightFrameMask = 0x7Fu;
    private const int RoofFadeShift = 12;
    private const int RoofFillShift = 13;
    private const int EyeCandyFrameShift = 12;
    private const uint EyeCandyFrameMask = 0x7Fu;
    private const int EyeCandyTranslucencyShift = 8;

    /// <summary>Decoded art type.</summary>
    public TypeCode Type => (TypeCode)((Value & ArtTypeMask) >> ArtTypeShift);

    /// <summary>The base numeric identifier of the art asset (corresponds to CE's tig_art_num_get).</summary>
    public int ArtNum =>
        Type switch
        {
            TypeCode.Tile or TypeCode.Wall or TypeCode.Critter or TypeCode.Monster => 0,
            TypeCode.UniqueNpc => checked((int)((Value >> 12) & 0x7Fu)),
            TypeCode.Item => checked((int)((Value >> 17) & 0x7FFu)),
            TypeCode.Interface => checked((int)((Value >> 16) & 0xFFu)),
            _ => checked((int)((Value >> 19) & 0x1FFu)),
        };

    /// <summary>Facade number extracted from this AID.</summary>
    public int FacadeNumber =>
        Type is not TypeCode.Facade
            ? 0
            : checked(
                (int)(((Value >> FacadeNumberLowShift) & 0xFFu) + (((Value >> FacadeNumberHighShift) & 0x1u) << 8))
            );

    /// <summary>Decoded frame index using the CE bit layout for the current art type.</summary>
    public int FrameIndex =>
        Type switch
        {
            TypeCode.Tile or TypeCode.Wall or TypeCode.Item => 0,
            TypeCode.Interface or TypeCode.Misc => checked((int)((Value >> InterfaceFrameShift) & InterfaceFrameMask)),
            TypeCode.Facade => checked((int)((Value >> FacadeFrameShift) & FacadeFrameMask)),
            TypeCode.Light => checked((int)((Value >> LightFrameShift) & LightFrameMask)),
            TypeCode.EyeCandy => checked((int)((Value >> EyeCandyFrameShift) & EyeCandyFrameMask)),
            _ => checked((int)((Value >> GenericFrameShift) & GenericFrameMask)),
        };

    /// <summary>Decoded palette slot using the CE layout for palette-aware art ids.</summary>
    public int PaletteIndex =>
        Type switch
        {
            TypeCode.Tile or TypeCode.Light or TypeCode.Facade => 0,
            _ => checked((int)((Value >> PaletteShift) & PaletteMask)),
        };

    /// <summary>Returns <see langword="true"/> when this AID is one roof fill piece.</summary>
    public bool IsRoofFill => Type is TypeCode.Roof && ((Value >> RoofFillShift) & 0x1u) != 0;

    /// <summary>Returns <see langword="true"/> when this AID is one faded roof piece.</summary>
    public bool IsRoofFaded => Type is TypeCode.Roof && ((Value >> RoofFadeShift) & 0x1u) != 0;

    /// <summary>Returns <see langword="true"/> when this AID is one horizontally mirrored roof piece.</summary>
    public bool IsRoofMirrored => Type is TypeCode.Roof && (Value & 0x1u) != 0;

    /// <summary>Returns the CE roof piece index, including mirrored variants, or -1 for non-roof AIDs.</summary>
    public int RoofPieceIndex => Type is not TypeCode.Roof ? -1 : FrameIndex + (((Value & 0x1u) != 0) ? 9 : 0);

    /// <summary>Returns the CE tile type bit for tile and facade AIDs.</summary>
    public int TileType =>
        Type switch
        {
            TypeCode.Tile => checked((int)((Value >> TileTypeShift) & TileTypeMask)),
            TypeCode.Facade => checked((int)((Value >> FacadeTypeShift) & TileTypeMask)),
            _ => 0,
        };

    /// <summary>Returns <see langword="true"/> when this eye-candy AID uses CE translucency.</summary>
    public bool IsEyeCandyTranslucent =>
        Type is TypeCode.EyeCandy && ((Value >> EyeCandyTranslucencyShift) & 0x1u) != 0;

    /// <summary>Returns a copy of this AID with the specified frame index encoded using the CE layout for its type.</summary>
    public ArtId WithFrameIndex(int frameIndex)
    {
        return Type switch
        {
            TypeCode.Tile or TypeCode.Wall or TypeCode.Item => this,
            TypeCode.Interface or TypeCode.Misc => ReplaceBits(
                InterfaceFrameShift,
                InterfaceFrameMask,
                checked((uint)frameIndex)
            ),
            TypeCode.Facade => ReplaceBits(FacadeFrameShift, FacadeFrameMask, checked((uint)frameIndex)),
            TypeCode.Light => ReplaceBits(LightFrameShift, LightFrameMask, checked((uint)frameIndex)),
            TypeCode.EyeCandy => ReplaceBits(EyeCandyFrameShift, EyeCandyFrameMask, checked((uint)frameIndex)),
            _ => ReplaceBits(GenericFrameShift, GenericFrameMask, checked((uint)frameIndex)),
        };
    }

    private ArtId ReplaceBits(int shift, uint mask, uint value)
    {
        var cleared = Value & ~(mask << shift);
        return new ArtId(cleared | ((value & mask) << shift));
    }

    /// <inheritdoc/>
    public static ArtId Read(ref SpanReader reader) => new(reader.ReadUInt32());

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer) => writer.WriteUInt32(Value);

    /// <inheritdoc/>
    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        dest.TryWrite($"0x{Value:X8}", out written);

    /// <inheritdoc/>
    public bool TryFormat(Span<byte> utf8Dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        Utf8.TryWrite(utf8Dest, $"0x{Value:X8}", out written);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? provider) => $"0x{Value:X8}";

    /// <inheritdoc/>
    public override string ToString() => $"0x{Value:X8}";
}
