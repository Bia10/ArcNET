using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

/// <summary>
/// Common fields shared by every game object type.  Field presence is controlled by the
/// <see cref="GameObjectHeader.Bitmap"/>; absent fields retain their default (zero/null) value.
/// </summary>
public class ObjectCommon
{
    // ── Visual ───────────────────────────────────────────────────────────────
    public ArtId CurrentAid { get; set; }
    public Location? Location { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public ArtId Shadow { get; set; }

    // Overlay lists — count + data pairs; exact format TBD during reverse engineering
    public int[] OverlayFore { get; set; } = [];
    public int[] OverlayBack { get; set; } = [];
    public int[] Underlay { get; set; } = [];

    // ── Rendering ────────────────────────────────────────────────────────────
    public int BlitFlags { get; set; }
    public Color BlitColor { get; set; }
    public int BlitAlpha { get; set; }
    public int BlitScale { get; set; }

    // ── Lighting ─────────────────────────────────────────────────────────────
    public int LightFlags { get; set; }
    public ArtId LightAid { get; set; }
    public Color LightColor { get; set; }
    public int OverlayLightFlags { get; set; }
    public int[] OverlayLightAid { get; set; } = [];
    public int OverlayLightColor { get; set; }

    // ── State & stats ─────────────────────────────────────────────────────────
    public int Flags { get; set; }
    public int SpellFlags { get; set; }
    public int BlockingMask { get; set; }
    public int Name { get; set; }
    public int Description { get; set; }
    public ArtId Aid { get; set; }
    public ArtId DestroyedAid { get; set; }
    public int Ac { get; set; }
    public int HpPts { get; set; }
    public int HpAdj { get; set; }
    public int HpDamage { get; set; }
    public int Material { get; set; }

    // ── Indexed arrays ────────────────────────────────────────────────────────
    public int[] ResistanceIdx { get; set; } = [];
    public GameObjectScript[] ScriptsIdx { get; set; } = [];

    // ── Misc ──────────────────────────────────────────────────────────────────
    public int SoundEffect { get; set; }
    public int Category { get; set; }

    // ── Padding (reserved by engine) ─────────────────────────────────────────
    public int PadIas1 { get; set; }
    public long PadI64As1 { get; set; }

    protected void ReadCommonFields(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;

        if (Bit(ObjectField.ObjFCurrentAid))
            CurrentAid = reader.ReadArtId();
        if (Bit(ObjectField.ObjFLocation))
            Location = reader.ReadLocation();
        if (Bit(ObjectField.ObjFOffsetX))
            OffsetX = reader.ReadInt32();
        if (Bit(ObjectField.ObjFOffsetY))
            OffsetY = reader.ReadInt32();
        if (Bit(ObjectField.ObjFShadow))
            Shadow = reader.ReadArtId();

        // Indexed array fields — binary layout not yet fully reversed
        if (Bit(ObjectField.ObjFOverlayFore))
            OverlayFore = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFOverlayBack))
            OverlayBack = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFUnderlay))
            Underlay = ReadIndexedInts(ref reader);

        if (Bit(ObjectField.ObjFBlitFlags))
            BlitFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFBlitColor))
            BlitColor = Color.Read(ref reader);
        if (Bit(ObjectField.ObjFBlitAlpha))
            BlitAlpha = reader.ReadInt32();
        if (Bit(ObjectField.ObjFBlitScale))
            BlitScale = reader.ReadInt32();
        if (Bit(ObjectField.ObjFLightFlags))
            LightFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFLightAid))
            LightAid = reader.ReadArtId();
        if (Bit(ObjectField.ObjFLightColor))
            LightColor = Color.Read(ref reader);
        if (Bit(ObjectField.ObjFOverlayLightFlags))
            OverlayLightFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFOverlayLightAid))
            OverlayLightAid = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFOverlayLightColor))
            OverlayLightColor = reader.ReadInt32();
        if (Bit(ObjectField.ObjFFlags))
            Flags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFSpellFlags))
            SpellFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFBlockingMask))
            BlockingMask = reader.ReadInt32();
        if (Bit(ObjectField.ObjFName))
            Name = reader.ReadInt32();
        if (Bit(ObjectField.ObjFDescription))
            Description = reader.ReadInt32();
        if (Bit(ObjectField.ObjFAid))
            Aid = reader.ReadArtId();
        if (Bit(ObjectField.ObjFDestroyedAid))
            DestroyedAid = reader.ReadArtId();
        if (Bit(ObjectField.ObjFAc))
            Ac = reader.ReadInt32();
        if (Bit(ObjectField.ObjFHpPts))
            HpPts = reader.ReadInt32();
        if (Bit(ObjectField.ObjFHpAdj))
            HpAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFHpDamage))
            HpDamage = reader.ReadInt32();
        if (Bit(ObjectField.ObjFMaterial))
            Material = reader.ReadInt32();
        if (Bit(ObjectField.ObjFResistanceIdx))
            ResistanceIdx = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFScriptsIdx))
            ScriptsIdx = ReadScripts(ref reader);
        if (Bit(ObjectField.ObjFSoundEffect))
            SoundEffect = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCategory))
            Category = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPadIas1))
            PadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPadI64As1))
            PadI64As1 = reader.ReadInt64();
    }

    // Indexed int array: 4-byte count followed by count × 4-byte values.
    internal static int[] ReadIndexedInts(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        if (count == 0)
            return [];
        var result = new int[count];
        reader.ReadInt32Array(result);
        return result;
    }

    internal static GameObjectScript[] ReadScripts(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        if (count == 0)
            return [];
        var result = new GameObjectScript[count];
        for (var i = 0; i < count; i++)
            result[i] = GameObjectScript.Read(ref reader);
        return result;
    }

    protected void WriteCommonFields(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;

        if (Bit(ObjectField.ObjFCurrentAid))
            CurrentAid.Write(ref writer);
        if (Bit(ObjectField.ObjFLocation))
            Location!.Value.Write(ref writer);
        if (Bit(ObjectField.ObjFOffsetX))
            writer.WriteInt32(OffsetX);
        if (Bit(ObjectField.ObjFOffsetY))
            writer.WriteInt32(OffsetY);
        if (Bit(ObjectField.ObjFShadow))
            Shadow.Write(ref writer);
        if (Bit(ObjectField.ObjFOverlayFore))
            WriteIndexedInts(ref writer, OverlayFore);
        if (Bit(ObjectField.ObjFOverlayBack))
            WriteIndexedInts(ref writer, OverlayBack);
        if (Bit(ObjectField.ObjFUnderlay))
            WriteIndexedInts(ref writer, Underlay);
        if (Bit(ObjectField.ObjFBlitFlags))
            writer.WriteInt32(BlitFlags);
        if (Bit(ObjectField.ObjFBlitColor))
            BlitColor.Write(ref writer);
        if (Bit(ObjectField.ObjFBlitAlpha))
            writer.WriteInt32(BlitAlpha);
        if (Bit(ObjectField.ObjFBlitScale))
            writer.WriteInt32(BlitScale);
        if (Bit(ObjectField.ObjFLightFlags))
            writer.WriteInt32(LightFlags);
        if (Bit(ObjectField.ObjFLightAid))
            LightAid.Write(ref writer);
        if (Bit(ObjectField.ObjFLightColor))
            LightColor.Write(ref writer);
        if (Bit(ObjectField.ObjFOverlayLightFlags))
            writer.WriteInt32(OverlayLightFlags);
        if (Bit(ObjectField.ObjFOverlayLightAid))
            WriteIndexedInts(ref writer, OverlayLightAid);
        if (Bit(ObjectField.ObjFOverlayLightColor))
            writer.WriteInt32(OverlayLightColor);
        if (Bit(ObjectField.ObjFFlags))
            writer.WriteInt32(Flags);
        if (Bit(ObjectField.ObjFSpellFlags))
            writer.WriteInt32(SpellFlags);
        if (Bit(ObjectField.ObjFBlockingMask))
            writer.WriteInt32(BlockingMask);
        if (Bit(ObjectField.ObjFName))
            writer.WriteInt32(Name);
        if (Bit(ObjectField.ObjFDescription))
            writer.WriteInt32(Description);
        if (Bit(ObjectField.ObjFAid))
            Aid.Write(ref writer);
        if (Bit(ObjectField.ObjFDestroyedAid))
            DestroyedAid.Write(ref writer);
        if (Bit(ObjectField.ObjFAc))
            writer.WriteInt32(Ac);
        if (Bit(ObjectField.ObjFHpPts))
            writer.WriteInt32(HpPts);
        if (Bit(ObjectField.ObjFHpAdj))
            writer.WriteInt32(HpAdj);
        if (Bit(ObjectField.ObjFHpDamage))
            writer.WriteInt32(HpDamage);
        if (Bit(ObjectField.ObjFMaterial))
            writer.WriteInt32(Material);
        if (Bit(ObjectField.ObjFResistanceIdx))
            WriteIndexedInts(ref writer, ResistanceIdx);
        if (Bit(ObjectField.ObjFScriptsIdx))
            WriteScripts(ref writer, ScriptsIdx);
        if (Bit(ObjectField.ObjFSoundEffect))
            writer.WriteInt32(SoundEffect);
        if (Bit(ObjectField.ObjFCategory))
            writer.WriteInt32(Category);
        if (Bit(ObjectField.ObjFPadIas1))
            writer.WriteInt32(PadIas1);
        if (Bit(ObjectField.ObjFPadI64As1))
            writer.WriteInt64(PadI64As1);
    }

    internal static void WriteIndexedInts(ref SpanWriter writer, int[] values)
    {
        writer.WriteInt32(values.Length);
        foreach (var v in values)
            writer.WriteInt32(v);
    }

    internal static void WriteScripts(ref SpanWriter writer, GameObjectScript[] scripts)
    {
        writer.WriteInt32(scripts.Length);
        foreach (var s in scripts)
            s.Write(ref writer);
    }
}
