using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Text;
using ArcNET.Formats;
using ArcNET.GameObjects;
using ArcNET.GameObjects.Classes;

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
        var sb = new StringBuilder();
        DumpHeader(sb, mob.Header);
        DumpProperties(sb, mob);
        return sb.ToString();
    }

    /// <summary>
    /// Writes a dump of <paramref name="mob"/> to <paramref name="writer"/>.
    /// </summary>
    public static void Dump(MobData mob, TextWriter writer) => writer.Write(Dump(mob));

    // ── Private helpers ───────────────────────────────────────────────────────

    internal static void DumpHeader(StringBuilder sb, GameObjectHeader h, string label = "=== MOB HEADER ===")
    {
        sb.AppendLine(label);
        sb.AppendLine(
            $"  Format       : 0x{h.Version:X2}  ({(h.Version == 0x77 ? "Object File Format — standard Arcanum/ToEE" : "unknown version")})"
        );
        sb.AppendLine(
            $"  Type         : {h.GameObjectType}  ({(h.IsPrototype ? "prototype definition" : "object instance")})"
        );
        sb.AppendLine(
            $"  Proto ID     : {h.ProtoId}  [{(h.ProtoId.IsProto ? "is a .PRO file itself" : "references prototype")}]"
        );
        if (!h.IsPrototype)
            sb.AppendLine($"  Object ID    : {h.ObjectId}");
        var setBits = Enumerable.Range(0, h.Bitmap.Length).Where(i => h.Bitmap[i]).ToList();
        sb.AppendLine(
            $"  Properties   : {setBits.Count}/{h.Bitmap.Length} present  (bits: [{string.Join(", ", setBits)}])"
        );
        sb.AppendLine();
    }

    internal static void DumpProperties(StringBuilder sb, MobData mob)
    {
        sb.AppendLine("=== PROPERTIES ===");
        if (mob.Properties.Count == 0)
        {
            sb.AppendLine("  (none)");
            return;
        }

        var objectType = mob.Header.GameObjectType;
        foreach (var prop in mob.Properties)
        {
            var fieldName = ResolveFieldName(objectType, (int)prop.Field);
            var bytes = prop.RawBytes;
            sb.Append($"  [{(int)prop.Field, 3}] {fieldName, -32} ({bytes.Length, 3} B)  ");
            AppendDecodedValue(sb, prop, objectType);
            sb.AppendLine();
        }
    }

    private static void AppendDecodedValue(StringBuilder sb, ObjectProperty prop, ObjectType objectType)
    {
        var bytes = prop.RawBytes;
        var fieldBit = (int)prop.Field;

        // ── Absent field (presence byte = 0) ──
        if (bytes.Length == 1 && bytes[0] == 0)
        {
            sb.Append("(absent)");
            return;
        }

        // ── Int32 scalar (no presence byte, always 4 bytes) ──
        if (bytes.Length == 4)
        {
            var i32 = BinaryPrimitives.ReadInt32LittleEndian(bytes);
            var u32 = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            var f32 = BinaryPrimitives.ReadSingleLittleEndian(bytes);

            // ── Common flag fields ──
            if (fieldBit == 8) // ObjFBlitFlags
            {
                sb.Append($"= 0x{u32:X8}  ");
                AppendFlagNames<ObjFBlitFlags>(sb, u32);
                return;
            }
            if (fieldBit == 18) // ObjFFlags
            {
                sb.Append($"= 0x{u32:X8}  ");
                AppendFlagNames<ObjFFlags>(sb, u32);
                return;
            }
            if (fieldBit == 19) // ObjFSpellFlags
            {
                sb.Append($"= 0x{u32:X8}  ");
                AppendFlagNames<ObjFSpellFlags>(sb, u32);
                return;
            }

            // ── Float fields (rotation, speed, radius, height) ──
            if (fieldBit is 34 or 36 or 37 or 38 or 39 or 40)
            {
                if (fieldBit == 34) // rotation
                    sb.Append($"= {f32:F4} rad ({f32 * (180.0 / Math.PI):F1}\u00b0)");
                else
                    sb.Append($"= {f32:G6}");
                return;
            }

            // ── Type-specific flag fields (bit 64 = first type-specific slot) ──
            if (fieldBit == 64)
            {
                sb.Append($"= 0x{u32:X8}  ");
                switch (objectType)
                {
                    case ObjectType.Npc or ObjectType.Pc:
                        AppendFlagNames<ObjFCritterFlags>(sb, u32);
                        break;
                    case ObjectType.Container:
                        AppendFlagNames<ObjFContainerFlags>(sb, u32);
                        break;
                    case ObjectType.Portal:
                        AppendFlagNames<ObjFPortalFlags>(sb, u32);
                        break;
                    case ObjectType.Scenery:
                        AppendFlagNames<ObjFSceneryFlags>(sb, u32);
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
                        AppendFlagNames<ObjFItemFlags>(sb, u32);
                        break;
                    default:
                        sb.Append("(unknown type flags)");
                        break;
                }
                return;
            }
            if (fieldBit == 65 && objectType is ObjectType.Npc or ObjectType.Pc)
            {
                sb.Append($"= 0x{u32:X8}  ");
                AppendFlagNames<ObjFCritterFlags2>(sb, u32);
                return;
            }

            // ── Weapon / armor type-specific flags (bit 96) ──
            if (fieldBit == 96)
            {
                switch (objectType)
                {
                    case ObjectType.Weapon:
                        sb.Append($"= 0x{u32:X8}  ");
                        AppendFlagNames<ObjFWeaponFlags>(sb, u32);
                        break;
                    case ObjectType.Armor:
                        sb.Append($"= 0x{u32:X8}  ");
                        AppendFlagNames<ObjFArmorFlags>(sb, u32);
                        break;
                    case ObjectType.Key:
                        // ObjFKeyKeyId — not flags, it's the key identifier
                        sb.Append($"= {i32}  (key ID — must match ObjFPortalKeyId / ObjFContainerKeyId)");
                        break;
                    default:
                        sb.Append($"= 0x{u32:X8}  (type-specific flags)");
                        break;
                }
                return;
            }

            if (fieldBit == 128 && objectType is ObjectType.Npc)
            {
                sb.Append($"= 0x{u32:X8}  ");
                AppendFlagNames<ObjFNpcFlags>(sb, u32);
                return;
            }
            if (fieldBit == 128 && objectType is ObjectType.Pc)
            {
                sb.Append($"= 0x{u32:X8}  (PC flags — no enum defined)");
                return;
            }

            // ── Inventory fields ──
            if (prop.Field is ObjectField.ObjFContainerInventoryNum or ObjectField.ObjFCritterInventoryNum)
            {
                sb.Append($"= {i32}  (item count)");
                return;
            }
            if (prop.Field is ObjectField.ObjFContainerInventorySource or ObjectField.ObjFCritterInventorySource)
            {
                sb.Append(
                    i32 == 0 ? "= 0  *** EMPTY (InvSource=0: engine skips fill) ***" : $"= {i32}  (InvenSource.mes ID)"
                );
                return;
            }

            // ── Well-known common scalar fields ──
            var wellKnownLabel = fieldBit switch
            {
                23 => "art resource ID",
                24 => "destroyed-art resource ID",
                25 => "armor class",
                26 => "max HP",
                27 => "HP adjustment",
                28 => "HP damage taken",
                32 => "sound effect ID",
                33 => "object category",
                _ => null,
            };
            if (wellKnownLabel is not null)
            {
                sb.Append($"= {i32}  ({wellKnownLabel})");
                return;
            }

            sb.Append($"= {i32}  (0x{u32:X8})");
            if (!float.IsNaN(f32) && !float.IsInfinity(f32) && Math.Abs(f32) is > 0.00001f and < 1e7f)
                sb.Append($"  [float={f32:G6}]");
            return;
        }

        // ── Int64 scalar (1-byte presence + 8-byte value) ──
        if (bytes.Length == 9)
        {
            var i64 = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(1));
            if (IsLocationField(prop.Field, objectType))
            {
                var x = (int)(i64 & 0xFFFFFFFF);
                var y = (int)((i64 >> 32) & 0xFFFFFFFF);
                sb.Append($"= tile ({x}, {y})");
                return;
            }

            sb.Append($"= 0x{(ulong)i64:X16}");
            return;
        }

        // ── SAR arrays (1-byte presence + 12-byte SA header + data + bitset) ──
        if (bytes.Length >= 14 && bytes[0] != 0)
        {
            // SA header at offsets 1..12: { int32 size, int32 count, int32 bitset_id }
            var elementSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(1));
            var elementCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(5));
            sb.Append($"SAR[{elementCount} × {elementSize}B]");

            if (elementCount == 0)
            {
                sb.Append("  (empty)");
                return;
            }

            if (elementSize == ObjectPropertyExtensions.ObjectIdWireSize)
            {
                // ObjectID (handle) array — expand full OidType + proto/guid inline
                sb.AppendLine();
                var items = prop.GetObjectIdArrayFull();
                foreach (var (oidType, protoOrData1, guid) in items)
                {
                    var oidLabel = oidType switch
                    {
                        -2 => "HANDLE",
                        -1 => "BLOCKED",
                        0 => "NULL",
                        1 => "A(proto)",
                        2 => "GUID",
                        3 => "P",
                        _ => $"type{oidType}",
                    };
                    var extra = oidType == 1 ? $"proto={protoOrData1}" : $"d.a=0x{protoOrData1:X8}";
                    sb.AppendLine($"      [{oidLabel}] {extra}  guid={guid}");
                }
                return;
            }

            if (elementSize == 4)
            {
                var vals = prop.GetInt32Array();

                // ── Critter base stats — index maps to BasicStatType ──
                if (
                    fieldBit == (int)ObjectField.ObjFCritterStatBaseIdx
                    && objectType is ObjectType.Npc or ObjectType.Pc
                )
                {
                    sb.AppendLine();
                    for (var idx = 0; idx < vals.Length; idx++)
                    {
                        var statLabel = Enum.IsDefined((BasicStatType)idx)
                            ? ((BasicStatType)idx).ToString()
                            : $"Stat{idx}";
                        sb.AppendLine($"      [{statLabel, -20}] = {vals[idx]}");
                    }
                    return;
                }

                sb.Append("  [");
                sb.Append(string.Join(", ", vals.Take(8).Select(v => v.ToString())));
                if (vals.Length > 8)
                    sb.Append($", +{vals.Length - 8} more");
                sb.Append(']');
                return;
            }

            // Script slots: 12B per element — { uint hdrFlags, uint hdrCounters, int scriptId }
            // Element index maps to ScriptAttachmentPoint
            if (elementSize == 12)
            {
                var dataSpan = bytes.AsSpan(13, elementCount * 12);
                sb.AppendLine();
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
                    sb.AppendLine(
                        $"      [{apName}] scriptId={scriptId}  hdrFlags=0x{hdrFlags:X8}  hdrCounters=0x{hdrCounters:X8}"
                    );
                }
                return;
            }

            // NPC waypoints: 8B per element — each is an Int64 tile location
            if (elementSize == 8 && fieldBit == 135) // ObjFNpcWaypointsIdx
            {
                var dataSpan = bytes.AsSpan(13, elementCount * 8);
                sb.AppendLine();
                for (var idx = 0; idx < elementCount; idx++)
                {
                    var loc = BinaryPrimitives.ReadInt64LittleEndian(dataSpan.Slice(idx * 8));
                    var tx = (int)(loc & 0xFFFFFFFF);
                    var ty = (int)((loc >> 32) & 0xFFFFFFFF);
                    sb.AppendLine($"      [Waypoint {idx}] tile ({tx}, {ty})");
                }
                return;
            }

            sb.Append($"  [{elementCount} elem(s)]");
            return;
        }

        // ── String (1-byte presence + int32 length + (length+1) bytes) ──
        sb.Append($"= \"{prop.GetString()}\"");
    }

    private static bool IsLocationField(ObjectField field, ObjectType objectType) =>
        field is ObjectField.ObjFLocation or ObjectField.ObjFCritterTeleportDest
        || (objectType is ObjectType.Npc && (int)field is 137 or 138); // ObjFNpcStandpointDay/Night

    private static void AppendFlagNames<T>(StringBuilder sb, uint value)
        where T : struct, Enum
    {
        if (value == 0)
        {
            sb.Append("(none)");
            return;
        }

        var names = new List<string>();
        foreach (var flag in Enum.GetValues<T>())
        {
            var flagVal = Convert.ToUInt32(flag);
            if (flagVal != 0 && (value & flagVal) == flagVal)
                names.Add(flag.ToString());
        }

        sb.Append(names.Count > 0 ? string.Join(" | ", names) : "(unknown flags)");
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
