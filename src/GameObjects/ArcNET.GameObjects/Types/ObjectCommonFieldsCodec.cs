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

        if (Bit(ObjectField.CurrentAid))
            obj.CurrentAid = reader.ReadArtId();
        if (Bit(ObjectField.Location))
            obj.Location = ObjectSerializationHelpers.ReadLocation(ref reader);
        if (Bit(ObjectField.OffsetX))
            obj.OffsetX = reader.ReadInt32();
        if (Bit(ObjectField.OffsetY))
            obj.OffsetY = reader.ReadInt32();
        if (Bit(ObjectField.Shadow))
            obj.Shadow = reader.ReadArtId();
        if (Bit(ObjectField.OverlayFore))
            obj.OverlayFore = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.OverlayBack))
            obj.OverlayBack = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.Underlay))
            obj.Underlay = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.BlitFlags))
            obj.BlitFlags = reader.ReadInt32();
        if (Bit(ObjectField.BlitColor))
            obj.BlitColor = Color.Read(ref reader);
        if (Bit(ObjectField.BlitAlpha))
            obj.BlitAlpha = reader.ReadInt32();
        if (Bit(ObjectField.BlitScale))
            obj.BlitScale = reader.ReadInt32();
    }

    private static void ReadLightingFields(ObjectCommon obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.LightFlags))
            obj.LightFlags = reader.ReadInt32();
        if (Bit(ObjectField.LightAid))
            obj.LightAid = reader.ReadArtId();
        if (Bit(ObjectField.LightColor))
            obj.LightColor = Color.Read(ref reader);
        if (Bit(ObjectField.OverlayLightFlags))
            obj.OverlayLightFlags = reader.ReadInt32();
        if (Bit(ObjectField.OverlayLightAid))
            obj.OverlayLightAid = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.OverlayLightColor))
            obj.OverlayLightColor = reader.ReadInt32();
    }

    private static void ReadStateFields(ObjectCommon obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjectFlags))
            obj.ObjectFlags = unchecked((ObjectFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.SpellFlags))
            obj.SpellFlags = unchecked((SpellFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.BlockingMask))
            obj.BlockingMask = reader.ReadInt32();
        if (Bit(ObjectField.Name))
            obj.Name = reader.ReadInt32();
        if (Bit(ObjectField.Description))
            obj.Description = reader.ReadInt32();
        if (Bit(ObjectField.Aid))
            obj.Aid = reader.ReadArtId();
        if (Bit(ObjectField.DestroyedAid))
            obj.DestroyedAid = reader.ReadArtId();

        ReadCombatFields(obj, ref reader, bitmap, isPrototype);

        if (Bit(ObjectField.Material))
            obj.Material = reader.ReadInt32();
        if (Bit(ObjectField.ResistanceIdx))
            obj.ResistanceIdx = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ScriptsIdx))
            obj.ScriptsIdx = ObjectCommon.ReadScripts(ref reader);
        if (Bit(ObjectField.SoundEffect))
            obj.SoundEffect = reader.ReadInt32();
        if (Bit(ObjectField.Category))
            obj.Category = reader.ReadInt32();
        if (Bit(ObjectField.PadIas1))
            obj.CommonPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.PadI64As1))
            obj.CommonPadI64As1Reserved = ObjectSerializationHelpers.ReadPresencePrefixedInt64(ref reader);
    }

    private static void ReadCombatFields(ObjectCommon obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.Ac))
            obj.Ac = reader.ReadInt32();
        if (Bit(ObjectField.HpPts))
            obj.HpPts = reader.ReadInt32();
        if (Bit(ObjectField.HpAdj))
            obj.HpAdj = reader.ReadInt32();
        if (Bit(ObjectField.HpDamage))
            obj.HpDamage = reader.ReadInt32();
    }

    private static void WriteVisualFields(ObjectCommon obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.CurrentAid))
            obj.CurrentAid.Write(ref writer);
        if (Bit(ObjectField.Location))
            ObjectSerializationHelpers.WriteLocation(ref writer, obj.Location ?? default);
        if (Bit(ObjectField.OffsetX))
            writer.WriteInt32(obj.OffsetX);
        if (Bit(ObjectField.OffsetY))
            writer.WriteInt32(obj.OffsetY);
        if (Bit(ObjectField.Shadow))
            obj.Shadow.Write(ref writer);
        if (Bit(ObjectField.OverlayFore))
            ObjectCommon.WriteIndexedInts(ref writer, obj.OverlayFore);
        if (Bit(ObjectField.OverlayBack))
            ObjectCommon.WriteIndexedInts(ref writer, obj.OverlayBack);
        if (Bit(ObjectField.Underlay))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Underlay);
        if (Bit(ObjectField.BlitFlags))
            writer.WriteInt32(obj.BlitFlags);
        if (Bit(ObjectField.BlitColor))
            obj.BlitColor.Write(ref writer);
        if (Bit(ObjectField.BlitAlpha))
            writer.WriteInt32(obj.BlitAlpha);
        if (Bit(ObjectField.BlitScale))
            writer.WriteInt32(obj.BlitScale);
    }

    private static void WriteLightingFields(ObjectCommon obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.LightFlags))
            writer.WriteInt32(obj.LightFlags);
        if (Bit(ObjectField.LightAid))
            obj.LightAid.Write(ref writer);
        if (Bit(ObjectField.LightColor))
            obj.LightColor.Write(ref writer);
        if (Bit(ObjectField.OverlayLightFlags))
            writer.WriteInt32(obj.OverlayLightFlags);
        if (Bit(ObjectField.OverlayLightAid))
            ObjectCommon.WriteIndexedInts(ref writer, obj.OverlayLightAid);
        if (Bit(ObjectField.OverlayLightColor))
            writer.WriteInt32(obj.OverlayLightColor);
    }

    private static void WriteStateFields(ObjectCommon obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjectFlags))
            writer.WriteInt32(unchecked((int)obj.ObjectFlags));
        if (Bit(ObjectField.SpellFlags))
            writer.WriteInt32(unchecked((int)obj.SpellFlags));
        if (Bit(ObjectField.BlockingMask))
            writer.WriteInt32(obj.BlockingMask);
        if (Bit(ObjectField.Name))
            writer.WriteInt32(obj.Name);
        if (Bit(ObjectField.Description))
            writer.WriteInt32(obj.Description);
        if (Bit(ObjectField.Aid))
            obj.Aid.Write(ref writer);
        if (Bit(ObjectField.DestroyedAid))
            obj.DestroyedAid.Write(ref writer);

        WriteCombatFields(obj, ref writer, bitmap, isPrototype);

        if (Bit(ObjectField.Material))
            writer.WriteInt32(obj.Material);
        if (Bit(ObjectField.ResistanceIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.ResistanceIdx);
        if (Bit(ObjectField.ScriptsIdx))
            ObjectCommon.WriteScripts(ref writer, obj.ScriptsIdx);
        if (Bit(ObjectField.SoundEffect))
            writer.WriteInt32(obj.SoundEffect);
        if (Bit(ObjectField.Category))
            writer.WriteInt32(obj.Category);
        if (Bit(ObjectField.PadIas1))
            writer.WriteInt32(obj.CommonPadIas1Reserved);
        if (Bit(ObjectField.PadI64As1))
            ObjectSerializationHelpers.WritePresencePrefixedInt64(ref writer, obj.CommonPadI64As1Reserved);
    }

    private static void WriteCombatFields(ObjectCommon obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.Ac))
            writer.WriteInt32(obj.Ac);
        if (Bit(ObjectField.HpPts))
            writer.WriteInt32(obj.HpPts);
        if (Bit(ObjectField.HpAdj))
            writer.WriteInt32(obj.HpAdj);
        if (Bit(ObjectField.HpDamage))
            writer.WriteInt32(obj.HpDamage);
    }
}
