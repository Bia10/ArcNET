using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Numerics;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;
using ArcNET.GameObjects.Classes;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="MobData"/> instance.
/// <br/>
/// Understanding the binary layout:
/// <list type="bullet">
///   <item><b>ObjectID (24 bytes)</b>: int16 type + int16 pad2 + int32 pad4 + TigGuid(16).</item>
///   <item><b>OD_TYPE_HANDLE_ARRAY</b>: SAR block — each element is a 24-byte ObjectID.</item>
///   <item><b>ContainerInventoryListIdx</b>: HANDLE_ARRAY of item ObjectIDs stored in the mob file.</item>
///   <item><b>ContainerInventorySource</b>: int32 — 1-based InvenSource.mes ID, 0 = empty.</item>
///   <item>
///     Setting <c>InvSource=0</c> alone causes an early return in <c>sub_463C60</c> so
///     the engine never clears existing items. You must also clear the inventory list.
///   </item>
/// </list>
/// </summary>
public static class MobDumper
{
    // ── Public dump API ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns a multi-line text dump of <paramref name="mob"/> for diagnostic purposes.
    /// Includes the header, all present field names, their byte sizes, and decoded values
    /// for known scalars. Array / inventory fields are expanded with per-element detail.
    /// </summary>
    public static string Dump(MobData mob)
    {
        Span<char> buf = stackalloc char[1024];
        var vsb = new ValueStringBuilder(buf);
        DumpHeader(ref vsb, mob.Header);
        DumpProperties(ref vsb, mob);
        return vsb.ToString();
    }

    /// <summary>
    /// Writes a dump of <paramref name="mob"/> to <paramref name="writer"/>.
    /// </summary>
    public static void Dump(MobData mob, TextWriter writer) => writer.Write(Dump(mob));

    // ── Private helpers ───────────────────────────────────────────────────────

    internal static void DumpHeader(ref ValueStringBuilder vsb, GameObjectHeader h, string label = "=== MOB HEADER ===")
    {
        vsb.AppendLine(label);

        // Object type + format version
        var fmtLabel = h.Version switch
        {
            0x08 => "Arcanum original (0x08)",
            0x77 => "Arcanum/ToEE extended (0x77)",
            _ => $"unknown (0x{h.Version:X2})",
        };
        var kindLabel = h.IsPrototype ? "prototype definition" : "instance";
        vsb.Append("  Object type  : ");
        vsb.Append(h.GameObjectType);
        vsb.Append("  (");
        vsb.Append(kindLabel);
        vsb.Append(", format ");
        vsb.Append(fmtLabel);
        vsb.AppendLine(")");

        // Proto ID — show proto number prominently for A-type refs
        var protoNum = h.ProtoId.GetProtoNumber();
        if (protoNum is not null)
        {
            vsb.Append("  Proto        : #");
            vsb.Append(protoNum.Value);
            vsb.Append("  (type ");
            vsb.Append(h.GameObjectType);
            vsb.AppendLine(")");
        }
        else
        {
            vsb.Append("  Proto ID     : ");
            vsb.AppendLine(h.ProtoId.ToString());
        }

        // Object GUID (only for instances)
        if (!h.IsPrototype)
        {
            vsb.Append("  Object GUID  : ");
            vsb.AppendLine(h.ObjectId.Id.ToString());
        }

        // Bitmap summary
        var setBits = new List<int>();
        for (var by = 0; by < h.Bitmap.Length; by++)
        {
            var word = (uint)h.Bitmap[by];
            while (word != 0)
            {
                var lsb = BitOperations.TrailingZeroCount(word);
                setBits.Add(by * 8 + lsb);
                word &= word - 1;
            }
        }
        vsb.Append("  Fields set   : ");
        vsb.Append(setBits.Count);
        vsb.Append('/');
        vsb.Append(h.Bitmap.Length * 8);
        vsb.Append("  (bits: [");
        vsb.AppendJoin(", ", setBits);
        vsb.AppendLine("])");
        vsb.AppendLine();
    }

    internal static void DumpProperties(ref ValueStringBuilder vsb, MobData mob)
    {
        vsb.AppendLine("=== PROPERTIES ===");
        if (mob.Properties.Count == 0)
        {
            vsb.AppendLine("  (none)");
            return;
        }

        var objectType = mob.Header.GameObjectType;
        foreach (var prop in mob.Properties)
        {
            var fieldName = ResolveFieldName(objectType, (int)prop.Field);
            var bytes = prop.RawBytes;
            if (prop.ParseNote is not null)
            {
                vsb.Append("  [");
                vsb.AppendPadded<int>((int)prop.Field, 3, leftAlign: false, padChar: ' ');
                vsb.Append("] ");
                vsb.AppendPadded(fieldName, 32);
                vsb.Append("  *** parse stopped: ");
                vsb.Append(prop.ParseNote);
                vsb.AppendLine(" ***");
                vsb.AppendLine("  (subsequent fields in bitmap were not read — wire type unknown)");
                break;
            }
            vsb.Append("  [");
            vsb.AppendPadded<int>((int)prop.Field, 3, leftAlign: false, padChar: ' ');
            vsb.Append("] ");
            vsb.AppendPadded(fieldName, 32);
            vsb.Append(" (");
            vsb.AppendPadded<int>(bytes.Length, 3, leftAlign: false, padChar: ' ');
            vsb.Append(" B)  ");
            AppendDecodedValue(ref vsb, prop, objectType);
            vsb.AppendLine();
        }
    }

    private static void AppendDecodedValue(ref ValueStringBuilder vsb, ObjectProperty prop, ObjectType objectType)
    {
        var bytes = prop.RawBytes;

        // ── Absent field (presence byte = 0) ──
        if (bytes.Length == 1 && bytes[0] == 0)
        {
            vsb.Append("(absent)");
            return;
        }

        if (bytes.Length == 4)
        {
            AppendInt32Value(ref vsb, prop, objectType);
            return;
        }
        if (bytes.Length == 9)
        {
            AppendInt64Value(ref vsb, prop, objectType);
            return;
        }
        if (bytes.Length >= 14 && bytes[0] != 0)
        {
            AppendSarValue(ref vsb, prop, objectType);
            return;
        }

        // ── String (1-byte presence + int32 length + (length+1) bytes) ──
        vsb.Append("= \"");
        vsb.Append(prop.GetString());
        vsb.Append('"');
    }

    /// <summary>Appends human-readable representation of a 4-byte (Int32) property value.</summary>
    private static void AppendInt32Value(ref ValueStringBuilder vsb, ObjectProperty prop, ObjectType objectType)
    {
        var bytes = prop.RawBytes;
        var fieldBit = (int)prop.Field;
        var i32 = BinaryPrimitives.ReadInt32LittleEndian(bytes);
        var u32 = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        var f32 = BinaryPrimitives.ReadSingleLittleEndian(bytes);

        // ── Common flag fields ──
        if (prop.Field == ObjectField.ObjFBlitFlags)
        {
            AppendAssignedHex32(ref vsb, u32, "  ");
            AppendFlagNames<ObjFBlitFlags>(ref vsb, u32);
            return;
        }
        if (prop.Field == ObjectField.ObjFFlags)
        {
            AppendAssignedHex32(ref vsb, u32, "  ");
            AppendFlagNames<ObjFFlags>(ref vsb, u32);
            return;
        }
        if (prop.Field == ObjectField.ObjFSpellFlags)
        {
            AppendAssignedHex32(ref vsb, u32, "  ");
            AppendFlagNames<ObjFSpellFlags>(ref vsb, u32);
            return;
        }

        // ── Float fields (rotation, speed, radius, height) ──
        if (
            prop.Field
            is ObjectField.ObjFPadIas1
                or ObjectField.ObjFSpeedRun
                or ObjectField.ObjFSpeedWalk
                or ObjectField.ObjFPadFloat1
                or ObjectField.ObjFRadius
                or ObjectField.ObjFHeight
        )
        {
            if (prop.Field == ObjectField.ObjFPadIas1) // ObjFRotation — stored as radians
            {
                vsb.Append("= ");
                vsb.Append(f32, "F4");
                vsb.Append(" rad (");
                vsb.Append(f32 * (180.0 / Math.PI), "F1");
                vsb.Append("\u00b0)");
            }
            else
            {
                vsb.Append("= ");
                vsb.Append(f32, "G6");
            }
            return;
        }

        // ── Type-specific flag fields (bit 64 = first type-specific slot) ──
        if (fieldBit == 64)
        {
            AppendAssignedHex32(ref vsb, u32, "  ");
            switch (objectType)
            {
                case ObjectType.Npc or ObjectType.Pc:
                    AppendFlagNames<ObjFCritterFlags>(ref vsb, u32);
                    break;
                case ObjectType.Container:
                    AppendFlagNames<ObjFContainerFlags>(ref vsb, u32);
                    break;
                case ObjectType.Portal:
                    AppendFlagNames<ObjFPortalFlags>(ref vsb, u32);
                    break;
                case ObjectType.Scenery:
                    AppendFlagNames<ObjFSceneryFlags>(ref vsb, u32);
                    break;
                case ObjectType.Weapon
                or ObjectType.Ammo
                or ObjectType.Armor
                or ObjectType.Gold
                or ObjectType.Food
                or ObjectType.Scroll
                or ObjectType.Key
                or ObjectType.KeyRing
                or ObjectType.Written
                or ObjectType.Generic:
                    AppendFlagNames<ObjFItemFlags>(ref vsb, u32);
                    break;
                default:
                    vsb.Append("(unknown type flags)");
                    break;
            }
            return;
        }
        if (prop.Field == ObjectField.ObjFCritterFlags2 && objectType is ObjectType.Npc or ObjectType.Pc)
        {
            AppendAssignedHex32(ref vsb, u32, "  ");
            AppendFlagNames<ObjFCritterFlags2>(ref vsb, u32);
            return;
        }

        // ── Weapon / armor type-specific flags (bit 96) ──
        if (fieldBit == 96)
        {
            switch (objectType)
            {
                case ObjectType.Weapon:
                    AppendAssignedHex32(ref vsb, u32, "  ");
                    AppendFlagNames<ObjFWeaponFlags>(ref vsb, u32);
                    break;
                case ObjectType.Armor:
                    AppendAssignedHex32(ref vsb, u32, "  ");
                    AppendFlagNames<ObjFArmorFlags>(ref vsb, u32);
                    break;
                case ObjectType.Key:
                    // ObjFKeyKeyId — not flags, it's the key identifier
                    AppendAssignedInt32(ref vsb, i32, "  (key ID — must match ObjFPortalKeyId / ObjFContainerKeyId)");
                    break;
                default:
                    AppendAssignedHex32(ref vsb, u32, "  (type-specific flags)");
                    break;
            }
            return;
        }

        if (prop.Field == ObjectField.ObjFNpcFlags && objectType is ObjectType.Npc)
        {
            AppendAssignedHex32(ref vsb, u32, "  ");
            AppendFlagNames<ObjFNpcFlags>(ref vsb, u32);
            return;
        }
        if (prop.Field == ObjectField.ObjFPcFlags && objectType is ObjectType.Pc)
        {
            AppendAssignedHex32(ref vsb, u32, "  (PC flags — no enum defined)");
            return;
        }

        // ── Inventory fields ──
        if (prop.Field is ObjectField.ObjFContainerInventoryNum or ObjectField.ObjFCritterInventoryNum)
        {
            AppendAssignedInt32(ref vsb, i32, "  (item count)");
            return;
        }
        if (prop.Field is ObjectField.ObjFContainerInventorySource or ObjectField.ObjFCritterInventorySource)
        {
            if (i32 == 0)
                vsb.Append("= 0  *** EMPTY (InvSource=0: engine skips fill) ***");
            else
                AppendAssignedInt32(ref vsb, i32, "  (InvenSource.mes ID)");
            return;
        }

        // ── Well-known common scalar fields ──
        var wellKnownLabel = prop.Field switch
        {
            ObjectField.ObjFAid => "art resource ID",
            ObjectField.ObjFDestroyedAid => "destroyed-art resource ID",
            ObjectField.ObjFAc => "armor class",
            ObjectField.ObjFHpPts => "max HP",
            ObjectField.ObjFHpAdj => "HP adjustment",
            ObjectField.ObjFHpDamage => "HP damage taken",
            ObjectField.ObjFSoundEffect => "sound effect ID",
            ObjectField.ObjFCategory => "object category",
            _ => null,
        };
        if (wellKnownLabel is not null)
        {
            AppendAssignedInt32(ref vsb, i32, "  (");
            vsb.Append(wellKnownLabel);
            vsb.Append(')');
            return;
        }

        vsb.Append("= ");
        vsb.Append(i32);
        vsb.Append("  (0x");
        vsb.AppendHex(u32);
        vsb.Append(')');
        if (!float.IsNaN(f32) && !float.IsInfinity(f32) && Math.Abs(f32) is > 0.00001f and < 1e7f)
        {
            vsb.Append("  [float=");
            vsb.Append(f32, "G6");
            vsb.Append(']');
        }
    }

    /// <summary>Appends human-readable representation of a 9-byte (Int64 with presence byte) property value.</summary>
    private static void AppendInt64Value(ref ValueStringBuilder vsb, ObjectProperty prop, ObjectType objectType)
    {
        var bytes = prop.RawBytes;
        var i64 = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(1));
        if (IsLocationField(prop.Field, objectType))
        {
            var x = (int)(i64 & 0xFFFFFFFF);
            var y = (int)((i64 >> 32) & 0xFFFFFFFF);
            vsb.Append("= tile (");
            vsb.Append(x);
            vsb.Append(", ");
            vsb.Append(y);
            vsb.Append(')');
            return;
        }

        vsb.AppendHex((ulong)i64, "= 0x".AsSpan());
    }

    /// <summary>Appends human-readable representation of a SAR (Sparse Array) property value.</summary>
    private static void AppendSarValue(ref ValueStringBuilder vsb, ObjectProperty prop, ObjectType objectType)
    {
        var bytes = prop.RawBytes;
        var fieldBit = (int)prop.Field;

        // SA header at offsets 1..12: { int32 size, int32 count, int32 bitset_id }
        var elementSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(1));
        var elementCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(5));
        vsb.Append("SAR[");
        vsb.Append(elementCount);
        vsb.Append(" × ");
        vsb.Append(elementSize);
        vsb.Append("B]");

        if (elementCount == 0)
        {
            vsb.Append("  (empty)");
            return;
        }

        if (elementSize == ObjectPropertyExtensions.ObjectIdWireSize)
        {
            // ObjectID (handle) array — expand full OidType + proto/guid inline
            vsb.AppendLine();
            var items = prop.GetObjectIdArrayFull();
            foreach (var (oidType, protoOrData1, guid) in items)
            {
                var oidLabel = oidType switch
                {
                    GameObjectGuid.OidTypeHandle => "HANDLE",
                    GameObjectGuid.OidTypeBlocked => "BLOCKED",
                    GameObjectGuid.OidTypeNull => "NULL",
                    GameObjectGuid.OidTypeA => "A(proto)",
                    GameObjectGuid.OidTypeGuid => "GUID",
                    GameObjectGuid.OidTypeP => "P",
                    _ => $"type{oidType}",
                };
                var extra = oidType == GameObjectGuid.OidTypeA ? $"proto={protoOrData1}" : $"d.a=0x{protoOrData1:X8}";
                vsb.Append("      [");
                vsb.Append(oidLabel);
                vsb.Append("] ");
                vsb.Append(extra);
                vsb.Append("  guid=");
                vsb.AppendLine(guid);
            }
            return;
        }

        if (elementSize == 4)
        {
            var vals = prop.GetInt32Array();

            // ── Critter base stats — index maps to BasicStatType ──
            if (fieldBit == (int)ObjectField.ObjFCritterStatBaseIdx && objectType is ObjectType.Npc or ObjectType.Pc)
            {
                vsb.AppendLine();
                for (var idx = 0; idx < vals.Length; idx++)
                {
                    var statLabel = Enum.IsDefined((BasicStatType)idx) ? ((BasicStatType)idx).ToString() : $"Stat{idx}";
                    vsb.Append("      [");
                    vsb.AppendPadded(statLabel, 20);
                    vsb.Append("] = ");
                    vsb.AppendLine(vals[idx]);
                }
                return;
            }

            vsb.Append("  [");
            vsb.AppendJoin(", ", vals, 8, " more".AsSpan());
            vsb.Append(']');
            return;
        }

        // Script slots: 12B per element — { uint hdrFlags, uint hdrCounters, int scriptId }
        // Element index maps to ScriptAttachmentPoint
        if (elementSize == 12)
        {
            var dataSpan = bytes.AsSpan(13, elementCount * 12);
            vsb.AppendLine();
            for (var idx = 0; idx < elementCount; idx++)
            {
                var elem = dataSpan.Slice(idx * 12, 12);
                var hdrFlags = BinaryPrimitives.ReadUInt32LittleEndian(elem);
                var hdrCounters = BinaryPrimitives.ReadUInt32LittleEndian(elem.Slice(4));
                var scriptId = BinaryPrimitives.ReadInt32LittleEndian(elem.Slice(8));
                if (scriptId == 0 && hdrFlags == 0 && hdrCounters == 0)
                    continue; // empty slot
                var apName = Enum.IsDefined((ScriptAttachmentPoint)idx)
                    ? ((ScriptAttachmentPoint)idx).ToString()
                    : $"Slot{idx}";
                vsb.Append("      [");
                vsb.Append(apName);
                vsb.Append("] scriptId=");
                vsb.Append(scriptId);
                vsb.Append("  hdrFlags=");
                vsb.AppendHex(hdrFlags, "0x".AsSpan());
                vsb.Append("  hdrCounters=");
                vsb.AppendHex(hdrCounters, "0x".AsSpan());
                vsb.AppendLine();
            }
            return;
        }

        // NPC waypoints: 8B per element — each is an Int64 tile location
        if (elementSize == 8 && objectType is ObjectType.Npc && prop.Field == ObjectField.ObjFNpcWaypointsIdx)
        {
            var dataSpan = bytes.AsSpan(13, elementCount * 8);
            vsb.AppendLine();
            for (var idx = 0; idx < elementCount; idx++)
            {
                var loc = BinaryPrimitives.ReadInt64LittleEndian(dataSpan.Slice(idx * 8));
                var tx = (int)(loc & 0xFFFFFFFF);
                var ty = (int)((loc >> 32) & 0xFFFFFFFF);
                vsb.Append("      [Waypoint ");
                vsb.Append(idx);
                vsb.Append("] tile (");
                vsb.Append(tx);
                vsb.Append(", ");
                vsb.Append(ty);
                vsb.AppendLine(")");
            }
            return;
        }

        vsb.Append("  [");
        vsb.Append(elementCount);
        vsb.Append(" elem(s)]");
    }

    private static void AppendAssignedHex32(ref ValueStringBuilder vsb, uint value) =>
        AppendAssignedHex32(ref vsb, value, default);

    private static void AppendAssignedHex32(ref ValueStringBuilder vsb, uint value, scoped ReadOnlySpan<char> suffix)
    {
        vsb.AppendHex(value, "= 0x".AsSpan());
        if (!suffix.IsEmpty)
            vsb.Append(suffix);
    }

    private static void AppendAssignedInt32(ref ValueStringBuilder vsb, int value) =>
        AppendAssignedInt32(ref vsb, value, default);

    private static void AppendAssignedInt32(ref ValueStringBuilder vsb, int value, scoped ReadOnlySpan<char> suffix)
    {
        vsb.Append("= ");
        vsb.Append(value);
        if (!suffix.IsEmpty)
            vsb.Append(suffix);
    }

    private static bool IsLocationField(ObjectField field, ObjectType objectType) =>
        field is ObjectField.ObjFLocation or ObjectField.ObjFCritterTeleportDest
        || (
            objectType is ObjectType.Npc
            && field is ObjectField.ObjFNpcStandpointDay or ObjectField.ObjFNpcStandpointNight
        );

    private static void AppendFlagNames<T>(ref ValueStringBuilder vsb, uint value)
        where T : struct, Enum
    {
        if (value == 0)
        {
            vsb.Append("(none)");
            return;
        }

        var first = true;
        foreach (var flag in Enum.GetValues<T>())
        {
            var flagVal = Convert.ToUInt32(flag);
            if (flagVal != 0 && (value & flagVal) == flagVal)
            {
                if (!first)
                    vsb.Append(" | ");
                vsb.Append(flag);
                first = false;
            }
        }

        if (first)
            vsb.Append("(unknown flags)");
    }

    // ── Type-aware field name resolution ──────────────────────────────────────

    /// <summary>
    /// Resolves the correct <see cref="ObjectField"/> name for a (type, bit) pair.
    /// Common fields (bits 0–63) have unique enum values so <c>ToString()</c> works.
    /// Type-specific fields (bits 64+) share values across object types, so we
    /// resolve by matching the enum member's name prefix to the object type.
    /// </summary>
    private static string ResolveFieldName(ObjectType objectType, int bit)
    {
        if (bit < 64)
            return ((ObjectField)bit).ToString();

        return s_typeFieldNames.GetValueOrDefault((objectType, bit)) ?? $"Field_{bit}";
    }

    private static readonly FrozenDictionary<(ObjectType Type, int Bit), string> s_typeFieldNames =
        BuildTypeFieldNames();

    private static FrozenDictionary<(ObjectType, int), string> BuildTypeFieldNames()
    {
        // Map name prefix → ObjectType(s) that use fields with that prefix.
        // Ordered longest-first so "ObjFKeyRing" is checked before "ObjFKey".
        ReadOnlySpan<(string Prefix, ObjectType[] Types)> prefixes =
        [
            ("ObjFProjectile", [ObjectType.Projectile]),
            ("ObjFContainer", [ObjectType.Container]),
            ("ObjFCritter", [ObjectType.Pc, ObjectType.Npc]),
            ("ObjFScenery", [ObjectType.Scenery]),
            ("ObjFKeyRing", [ObjectType.KeyRing]),
            ("ObjFWritten", [ObjectType.Written]),
            ("ObjFGeneric", [ObjectType.Generic]),
            ("ObjFPortal", [ObjectType.Portal]),
            ("ObjFWeapon", [ObjectType.Weapon]),
            ("ObjFScroll", [ObjectType.Scroll]),
            ("ObjFArmor", [ObjectType.Armor]),
            ("ObjFWall", [ObjectType.Wall]),
            ("ObjFTrap", [ObjectType.Trap]),
            ("ObjFAmmo", [ObjectType.Ammo]),
            ("ObjFFood", [ObjectType.Food]),
            ("ObjFGold", [ObjectType.Gold]),
            (
                "ObjFItem",
                [
                    ObjectType.Weapon,
                    ObjectType.Ammo,
                    ObjectType.Armor,
                    ObjectType.Gold,
                    ObjectType.Food,
                    ObjectType.Scroll,
                    ObjectType.Key,
                    ObjectType.KeyRing,
                    ObjectType.Written,
                    ObjectType.Generic,
                ]
            ),
            ("ObjFKey", [ObjectType.Key]),
            ("ObjFNpc", [ObjectType.Npc]),
            ("ObjFPc", [ObjectType.Pc]),
        ];

        var dict = new Dictionary<(ObjectType, int), string>();

        foreach (var name in Enum.GetNames<ObjectField>())
        {
            var value = (int)Enum.Parse<ObjectField>(name);
            if (value < 64)
                continue;

            foreach (var (prefix, types) in prefixes)
            {
                if (!name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                foreach (var type in types)
                    dict.TryAdd((type, value), name);

                break;
            }
        }

        return dict.ToFrozenDictionary();
    }
}
