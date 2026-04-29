using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

internal static class ObjectCommonFieldsCodec
{
    public static void Read(ObjectCommon obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        ReadVisualFields(obj, ref reader, bitmap, isPrototype);
        ReadLightingFields(obj, ref reader, bitmap, isPrototype);
        ReadStateFields(obj, ref reader, bitmap, isPrototype);
    }

    public static void Write(ObjectCommon obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteVisualFields(obj, ref writer, bitmap, isPrototype);
        WriteLightingFields(obj, ref writer, bitmap, isPrototype);
        WriteStateFields(obj, ref writer, bitmap, isPrototype);
    }

    private static void ReadVisualFields(ObjectCommon obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFCurrentAid))
            obj.CurrentAid = reader.ReadArtId();
        if (Bit(ObjectField.ObjFLocation))
            obj.Location = reader.ReadLocation();
        if (Bit(ObjectField.ObjFOffsetX))
            obj.OffsetX = reader.ReadInt32();
        if (Bit(ObjectField.ObjFOffsetY))
            obj.OffsetY = reader.ReadInt32();
        if (Bit(ObjectField.ObjFShadow))
            obj.Shadow = reader.ReadArtId();
        if (Bit(ObjectField.ObjFOverlayFore))
            obj.OverlayFore = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFOverlayBack))
            obj.OverlayBack = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFUnderlay))
            obj.Underlay = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFBlitFlags))
            obj.BlitFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFBlitColor))
            obj.BlitColor = Color.Read(ref reader);
        if (Bit(ObjectField.ObjFBlitAlpha))
            obj.BlitAlpha = reader.ReadInt32();
        if (Bit(ObjectField.ObjFBlitScale))
            obj.BlitScale = reader.ReadInt32();
    }

    private static void ReadLightingFields(ObjectCommon obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFLightFlags))
            obj.LightFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFLightAid))
            obj.LightAid = reader.ReadArtId();
        if (Bit(ObjectField.ObjFLightColor))
            obj.LightColor = Color.Read(ref reader);
        if (Bit(ObjectField.ObjFOverlayLightFlags))
            obj.OverlayLightFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFOverlayLightAid))
            obj.OverlayLightAid = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFOverlayLightColor))
            obj.OverlayLightColor = reader.ReadInt32();
    }

    private static void ReadStateFields(ObjectCommon obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFFlags))
            obj.ObjectFlags = unchecked((ObjFFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ObjFSpellFlags))
            obj.SpellFlags = unchecked((ObjFSpellFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ObjFBlockingMask))
            obj.BlockingMask = reader.ReadInt32();
        if (Bit(ObjectField.ObjFName))
            obj.Name = reader.ReadInt32();
        if (Bit(ObjectField.ObjFDescription))
            obj.Description = reader.ReadInt32();
        if (Bit(ObjectField.ObjFAid))
            obj.Aid = reader.ReadArtId();
        if (Bit(ObjectField.ObjFDestroyedAid))
            obj.DestroyedAid = reader.ReadArtId();

        ReadCombatFields(obj, ref reader, bitmap, isPrototype);

        if (Bit(ObjectField.ObjFMaterial))
            obj.Material = reader.ReadInt32();
        if (Bit(ObjectField.ObjFResistanceIdx))
            obj.ResistanceIdx = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFScriptsIdx))
            obj.ScriptsIdx = ObjectCommon.ReadScripts(ref reader);
        if (Bit(ObjectField.ObjFSoundEffect))
            obj.SoundEffect = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCategory))
            obj.Category = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPadIas1))
            obj.CommonPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPadI64As1))
            obj.CommonPadI64As1Reserved = reader.ReadInt64();
    }

    private static void ReadCombatFields(ObjectCommon obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFAc))
            obj.Ac = reader.ReadInt32();
        if (Bit(ObjectField.ObjFHpPts))
            obj.HpPts = reader.ReadInt32();
        if (Bit(ObjectField.ObjFHpAdj))
            obj.HpAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFHpDamage))
            obj.HpDamage = reader.ReadInt32();
    }

    private static void WriteVisualFields(ObjectCommon obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFCurrentAid))
            obj.CurrentAid.Write(ref writer);
        if (Bit(ObjectField.ObjFLocation))
            obj.Location!.Value.Write(ref writer);
        if (Bit(ObjectField.ObjFOffsetX))
            writer.WriteInt32(obj.OffsetX);
        if (Bit(ObjectField.ObjFOffsetY))
            writer.WriteInt32(obj.OffsetY);
        if (Bit(ObjectField.ObjFShadow))
            obj.Shadow.Write(ref writer);
        if (Bit(ObjectField.ObjFOverlayFore))
            ObjectCommon.WriteIndexedInts(ref writer, obj.OverlayFore);
        if (Bit(ObjectField.ObjFOverlayBack))
            ObjectCommon.WriteIndexedInts(ref writer, obj.OverlayBack);
        if (Bit(ObjectField.ObjFUnderlay))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Underlay);
        if (Bit(ObjectField.ObjFBlitFlags))
            writer.WriteInt32(obj.BlitFlags);
        if (Bit(ObjectField.ObjFBlitColor))
            obj.BlitColor.Write(ref writer);
        if (Bit(ObjectField.ObjFBlitAlpha))
            writer.WriteInt32(obj.BlitAlpha);
        if (Bit(ObjectField.ObjFBlitScale))
            writer.WriteInt32(obj.BlitScale);
    }

    private static void WriteLightingFields(ObjectCommon obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFLightFlags))
            writer.WriteInt32(obj.LightFlags);
        if (Bit(ObjectField.ObjFLightAid))
            obj.LightAid.Write(ref writer);
        if (Bit(ObjectField.ObjFLightColor))
            obj.LightColor.Write(ref writer);
        if (Bit(ObjectField.ObjFOverlayLightFlags))
            writer.WriteInt32(obj.OverlayLightFlags);
        if (Bit(ObjectField.ObjFOverlayLightAid))
            ObjectCommon.WriteIndexedInts(ref writer, obj.OverlayLightAid);
        if (Bit(ObjectField.ObjFOverlayLightColor))
            writer.WriteInt32(obj.OverlayLightColor);
    }

    private static void WriteStateFields(ObjectCommon obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFFlags))
            writer.WriteInt32(unchecked((int)obj.ObjectFlags));
        if (Bit(ObjectField.ObjFSpellFlags))
            writer.WriteInt32(unchecked((int)obj.SpellFlags));
        if (Bit(ObjectField.ObjFBlockingMask))
            writer.WriteInt32(obj.BlockingMask);
        if (Bit(ObjectField.ObjFName))
            writer.WriteInt32(obj.Name);
        if (Bit(ObjectField.ObjFDescription))
            writer.WriteInt32(obj.Description);
        if (Bit(ObjectField.ObjFAid))
            obj.Aid.Write(ref writer);
        if (Bit(ObjectField.ObjFDestroyedAid))
            obj.DestroyedAid.Write(ref writer);

        WriteCombatFields(obj, ref writer, bitmap, isPrototype);

        if (Bit(ObjectField.ObjFMaterial))
            writer.WriteInt32(obj.Material);
        if (Bit(ObjectField.ObjFResistanceIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.ResistanceIdx);
        if (Bit(ObjectField.ObjFScriptsIdx))
            ObjectCommon.WriteScripts(ref writer, obj.ScriptsIdx);
        if (Bit(ObjectField.ObjFSoundEffect))
            writer.WriteInt32(obj.SoundEffect);
        if (Bit(ObjectField.ObjFCategory))
            writer.WriteInt32(obj.Category);
        if (Bit(ObjectField.ObjFPadIas1))
            writer.WriteInt32(obj.CommonPadIas1Reserved);
        if (Bit(ObjectField.ObjFPadI64As1))
            writer.WriteInt64(obj.CommonPadI64As1Reserved);
    }

    private static void WriteCombatFields(ObjectCommon obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFAc))
            writer.WriteInt32(obj.Ac);
        if (Bit(ObjectField.ObjFHpPts))
            writer.WriteInt32(obj.HpPts);
        if (Bit(ObjectField.ObjFHpAdj))
            writer.WriteInt32(obj.HpAdj);
        if (Bit(ObjectField.ObjFHpDamage))
            writer.WriteInt32(obj.HpDamage);
    }
}
