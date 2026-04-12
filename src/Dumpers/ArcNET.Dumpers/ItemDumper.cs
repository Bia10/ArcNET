using System.Buffers.Binary;
using ArcNET.Archive;
using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameObjects;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces human-readable text dumps of item mob data and resolves item identities
/// from DAT archives or the on-disk proto directory by proto ID or display name.
/// </summary>
public static class ItemDumper
{
    // ── Name resolution ────────────────────────────────────────────────────────

    /// <summary>
    /// Loads item display names from description.mes and oname.mes inside every .dat
    /// archive found directly in <paramref name="gameDir"/>.
    /// Keys are MES entry IDs (proto IDs); values are display names.
    /// </summary>
    public static Dictionary<int, string> LoadProtoNameLookup(string gameDir)
    {
        var result = new Dictionary<int, string>();
        var datFiles = Directory.GetFiles(gameDir, "*.dat");

        foreach (var datFile in datFiles)
        {
            DatArchive archive;
            try
            {
                archive = DatArchive.Open(datFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[warning] Could not open archive '{datFile}': {ex.Message}");
                continue;
            }

            using (archive)
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.IsDirectory || !entry.Path.EndsWith(".mes", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (
                        !entry.Path.Equals("mes\\description.mes", StringComparison.OrdinalIgnoreCase)
                        && !entry.Path.Equals("oemes\\oname.mes", StringComparison.OrdinalIgnoreCase)
                    )
                        continue;

                    try
                    {
                        var mesFile = MessageFormat.ParseMemory(archive.ReadEntry(entry));
                        foreach (var msg in mesFile.Entries)
                            result.TryAdd(msg.Index, msg.Text);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(
                            $"[warning] Could not parse '{entry.Path}' in '{datFile}': {ex.Message}"
                        );
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves the display name for an item mob using <paramref name="installation"/> to
    /// translate the proto ID into the vanilla key used by <c>description.mes</c>:
    /// <list type="number">
    ///   <item>Vanilla-translated proto ID → description.mes lookup.</item>
    ///   <item>Raw proto ID fallback (for UAP-only protos 1–20 with no vanilla equivalent).</item>
    ///   <item>Object type name as last resort.</item>
    /// </list>
    /// </summary>
    public static string ResolveItemName(
        MobData mob,
        int protoId,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation
    )
    {
        var vanillaId = ArcanumInstallation.ToVanillaProtoId(protoId, installation);
        if (nameLookup.TryGetValue(vanillaId, out var name))
            return name;

        // Fallback for UAP-only IDs (1–20) that have no vanilla equivalent.
        if (nameLookup.TryGetValue(protoId, out var altName))
            return altName;

        return mob.Header.GameObjectType.ToString();
    }

    // ── Single-item text dump ──────────────────────────────────────────────────

    /// <summary>
    /// Produces a human-readable text dump of a single item mob's meaningful properties
    /// (identity, weight/worth, and type-specific stats).
    /// </summary>
    public static string DumpItem(
        MobData mob,
        int protoId,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation
    )
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        var name = ResolveItemName(mob, protoId, nameLookup, installation);
        vsb.Append("=== ITEM: ");
        vsb.Append(name);
        vsb.AppendLine(" ===");
        vsb.Append("  Proto    : ");
        vsb.AppendLine(protoId);
        vsb.Append("  ObjectId : ");
        vsb.AppendLine(mob.Header.ObjectId.ToString());
        vsb.Append("  Type     : ");
        vsb.AppendLine(mob.Header.GameObjectType);
        vsb.AppendLine();
        AppendItemBase(ref vsb, mob);
        AppendTypeSpecific(ref vsb, mob);
        return vsb.ToString();
    }

    /// <inheritdoc cref="DumpItem(MobData, int, Dictionary{int,string}, ArcanumInstallationType)"/>
    public static void DumpItem(
        MobData mob,
        int protoId,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation,
        TextWriter writer
    ) => writer.Write(DumpItem(mob, protoId, nameLookup, installation));

    // ── Archive-aware proto lookup ─────────────────────────────────────────────

    /// <summary>
    /// Finds and dumps a proto file from the on-disk proto directory by its numeric ID.
    /// Proto files are expected at <c>{gameDir}/data/proto/{protoId:D6}*.pro</c>.
    /// Returns <see langword="null"/> if no matching file is found.
    /// </summary>
    public static string? DumpProtoById(
        string gameDir,
        int protoId,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation
    )
    {
        var protoDir = Path.Combine(gameDir, "data", "proto");
        if (!Directory.Exists(protoDir))
            return null;

        var match = Directory
            .EnumerateFiles(protoDir, $"{protoId:D6}*.pro", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        if (match is null)
            return null;

        var proto = ProtoFormat.ParseFile(match);
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);

        var vanillaId = ArcanumInstallation.ToVanillaProtoId(protoId, installation);
        if (nameLookup.TryGetValue(vanillaId, out var name) || nameLookup.TryGetValue(protoId, out name))
        {
            vsb.Append("  Name : ");
            vsb.AppendLine(name);
        }

        vsb.Append(ProtoDumper.Dump(proto));
        return vsb.ToString();
    }

    /// <summary>
    /// Finds and dumps a proto file from the on-disk proto directory whose display name
    /// matches <paramref name="itemName"/> (case-insensitive).
    /// Returns <see langword="null"/> if no matching proto is found.
    /// </summary>
    public static string? DumpProtoByName(
        string gameDir,
        string itemName,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation
    )
    {
        int? protoId = null;
        foreach (var kvp in nameLookup)
        {
            if (kvp.Value.Equals(itemName, StringComparison.OrdinalIgnoreCase))
            {
                protoId = kvp.Key;
                break;
            }
        }

        return protoId is null ? null : DumpProtoById(gameDir, protoId.Value, nameLookup, installation);
    }

    // ── Container inventory dump ───────────────────────────────────────────────

    /// <summary>
    /// Loads the container mob from <paramref name="archive"/> at <paramref name="containerMobPath"/>,
    /// then dumps all inventory items resolving each child mob from the same archive.
    /// </summary>
    /// <param name="archive">The open DAT archive.</param>
    /// <param name="containerMobPath">DAT entry path of the container mob (e.g. <c>maps\Cave\G_GUID.mob</c>).</param>
    /// <param name="mapDirPrefix">Directory prefix used to locate child item mobs (e.g. <c>maps\Cave of the Bangellian Scourge\</c>).</param>
    /// <param name="nameLookup">Proto → name lookup built by <see cref="LoadProtoNameLookup"/>.</param>
    /// <param name="installation">Installation variant used to translate proto IDs for name resolution.</param>
    public static string DumpContainerItems(
        DatArchive archive,
        string containerMobPath,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation = ArcanumInstallationType.Vanilla
    )
    {
        ReadOnlyMemory<byte> containerData;
        try
        {
            containerData = archive.GetEntryData(containerMobPath);
        }
        catch (KeyNotFoundException)
        {
            return $"Container not found in archive: {containerMobPath}\n";
        }

        var container = MobFormat.ParseMemory(containerData);
        return DumpContainerItemsCore(container, archive, mapDirPrefix, nameLookup, installation);
    }

    /// <summary>
    /// Dumps all inventory items from a pre-parsed container mob, resolving each child
    /// mob from <paramref name="archiveForItems"/>.
    /// </summary>
    public static string DumpContainerItems(
        MobData container,
        DatArchive archiveForItems,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation = ArcanumInstallationType.Vanilla
    ) => DumpContainerItemsCore(container, archiveForItems, mapDirPrefix, nameLookup, installation);

    /// <inheritdoc cref="DumpContainerItems(DatArchive, string, string, Dictionary{int,string}, ArcanumInstallationType)"/>
    public static void DumpContainerItems(
        DatArchive archive,
        string containerMobPath,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup,
        TextWriter writer,
        ArcanumInstallationType installation = ArcanumInstallationType.Vanilla
    ) => writer.Write(DumpContainerItems(archive, containerMobPath, mapDirPrefix, nameLookup, installation));

    /// <inheritdoc cref="DumpContainerItems(MobData, DatArchive, string, Dictionary{int,string}, ArcanumInstallationType)"/>
    public static void DumpContainerItems(
        MobData container,
        DatArchive archiveForItems,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup,
        TextWriter writer,
        ArcanumInstallationType installation = ArcanumInstallationType.Vanilla
    ) => writer.Write(DumpContainerItemsCore(container, archiveForItems, mapDirPrefix, nameLookup, installation));

    // ── Private helpers ────────────────────────────────────────────────────────

    private static string DumpContainerItemsCore(
        MobData container,
        DatArchive archiveForItems,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation
    )
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);

        var invNumProp = container.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFContainerInventoryNum);
        var invSrcProp = container.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFContainerInventorySource);
        var invListProp = container.Properties.FirstOrDefault(p =>
            p.Field == ObjectField.ObjFContainerInventoryListIdx
        );

        var invNum = invNumProp?.GetInt32() ?? 0;
        var invSrc = invSrcProp?.GetInt32() ?? 0;

        vsb.Append("  InvNum    = ");
        vsb.AppendLine(invNum);
        vsb.Append("  InvSource = ");
        vsb.AppendLine(invSrc);

        if (invListProp is null || invNum == 0)
        {
            vsb.AppendLine("  (empty inventory)");
            return vsb.ToString();
        }

        var items = invListProp.GetObjectIdArrayFull();
        vsb.Append("  Items: ");
        vsb.AppendLine(items.Length);
        vsb.AppendLine();

        for (var i = 0; i < items.Length; i++)
        {
            var (_, _, guid) = items[i];
            var guidStr = guid.ToString("N").ToUpperInvariant();
            var fileName =
                $"G_{guidStr[..8]}_{guidStr[8..12]}_{guidStr[12..16]}_{guidStr[16..20]}_{guidStr[20..32]}.mob";
            var itemDatPath = mapDirPrefix + fileName;

            MobData itemMob;
            try
            {
                var itemData = archiveForItems.GetEntryData(itemDatPath);
                itemMob = MobFormat.ParseMemory(itemData);
            }
            catch (Exception ex)
            {
                vsb.Append("  [");
                vsb.Append(i + 1);
                vsb.Append("] ");
                vsb.Append(guid);
                vsb.Append(" — could not load: ");
                vsb.AppendLine(ex.Message);
                continue;
            }

            var protoId = BinaryPrimitives.ReadInt32LittleEndian(itemMob.Header.ProtoId.Id.ToByteArray());
            vsb.Append("  [");
            vsb.Append(i + 1);
            vsb.AppendLine("]");
            vsb.Append(DumpItem(itemMob, protoId, nameLookup, installation));
        }

        return vsb.ToString();
    }

    private static void AppendItemBase(ref ValueStringBuilder vsb, MobData mob)
    {
        var weight = GetPropInt32(mob, ObjectField.ObjFItemWeight);
        var worth = GetPropInt32(mob, ObjectField.ObjFItemWorth);
        var flags = GetPropInt32(mob, ObjectField.ObjFItemFlags);
        var discipline = GetPropInt32(mob, ObjectField.ObjFItemDiscipline);
        var complexity = GetPropInt32(mob, ObjectField.ObjFItemMagicTechComplexity);
        int?[] spells =
        [
            GetPropInt32(mob, ObjectField.ObjFItemSpell1),
            GetPropInt32(mob, ObjectField.ObjFItemSpell2),
            GetPropInt32(mob, ObjectField.ObjFItemSpell3),
            GetPropInt32(mob, ObjectField.ObjFItemSpell4),
            GetPropInt32(mob, ObjectField.ObjFItemSpell5),
        ];

        var hasContent =
            (weight is > 0) || (worth is > 0) || discipline is > 0 || complexity is > 0 || spells.Any(s => s is > 0);
        if (hasContent)
        {
            vsb.AppendLine("  --- Item Base ---");
            if (weight is > 0)
            {
                vsb.Append("  Weight       : ");
                vsb.Append(weight.Value / 10.0, "F1");
                vsb.AppendLine(" lbs");
            }
            if (worth is > 0)
            {
                vsb.Append("  Worth        : ");
                vsb.Append(worth.Value);
                vsb.AppendLine(" gp");
            }
            if (flags is > 0)
            {
                var flagNames = AppendFlagSummary<ObjFItemFlags>((uint)flags.Value);
                vsb.Append("  Item flags   : ");
                vsb.AppendHex((uint)flags.Value, "0x".AsSpan());
                vsb.Append("  ");
                vsb.AppendLine(flagNames);
            }
            if (discipline is > 0)
            {
                // 0=magic/tech-neutral, 1=magical, 2=technological
                var discLabel = discipline.Value switch
                {
                    1 => "magical",
                    2 => "technological",
                    _ => discipline.Value.ToString(),
                };
                vsb.Append("  Discipline   : ");
                vsb.AppendLine(discLabel);
            }
            if (complexity is > 0)
            {
                vsb.Append("  Tech complexity : ");
                vsb.Append(complexity.Value);
                vsb.AppendLine("  (schematic difficulty)");
            }
            for (var s = 0; s < spells.Length; s++)
            {
                if (spells[s] is int spellId and > 0)
                {
                    vsb.Append("  Spell effect ");
                    vsb.Append(s + 1);
                    vsb.Append(" : ID ");
                    vsb.Append(spellId);
                    vsb.AppendLine("  (see spell.mes)");
                }
            }
            vsb.AppendLine();
        }
    }

    private static string AppendFlagSummary<T>(uint value)
        where T : struct, Enum
    {
        if (value == 0)
            return "(none)";
        var vsb = new ValueStringBuilder(stackalloc char[256]);
        foreach (var flag in Enum.GetValues<T>())
        {
            var fv = Convert.ToUInt32(flag);
            if (fv != 0 && (value & fv) == fv)
            {
                if (vsb.Length > 0)
                    vsb.Append(" | ");
                vsb.Append(flag);
            }
        }
        return vsb.Length > 0 ? vsb.ToString() : "(unknown flags)";
    }

    private static void AppendTypeSpecific(ref ValueStringBuilder vsb, MobData mob)
    {
        switch (mob.Header.GameObjectType)
        {
            case ObjectType.Weapon:
                AppendWeapon(ref vsb, mob);
                break;
            case ObjectType.Armor:
                AppendArmor(ref vsb, mob);
                break;
            case ObjectType.Gold:
                AppendGold(ref vsb, mob);
                break;
            case ObjectType.Food:
                AppendFood(ref vsb, mob);
                break;
            case ObjectType.Scroll:
                AppendScroll(ref vsb, mob);
                break;
            case ObjectType.Ammo:
                AppendAmmo(ref vsb, mob);
                break;
            case ObjectType.Key:
                AppendKey(ref vsb, mob);
                break;
            case ObjectType.Written:
                AppendWritten(ref vsb, mob);
                break;
            case ObjectType.Generic:
                AppendGeneric(ref vsb, mob);
                break;
        }
    }

    private static void AppendWeapon(ref ValueStringBuilder vsb, MobData mob)
    {
        var dmgLo = GetPropInt32(mob, ObjectField.ObjFWeaponDamageLowerIdx);
        var dmgHi = GetPropInt32(mob, ObjectField.ObjFWeaponDamageUpperIdx);
        var magDmg = GetPropInt32(mob, ObjectField.ObjFWeaponMagicDamageAdjIdx);
        var speed = GetPropInt32(mob, ObjectField.ObjFWeaponSpeedFactor);
        var magSpd = GetPropInt32(mob, ObjectField.ObjFWeaponMagicSpeedAdj);
        var range = GetPropInt32(mob, ObjectField.ObjFWeaponRange);
        var toHit = GetPropInt32(mob, ObjectField.ObjFWeaponBonusToHit);
        var magHit = GetPropInt32(mob, ObjectField.ObjFWeaponMagicHitAdj);
        var minStr = GetPropInt32(mob, ObjectField.ObjFWeaponMinStrength);

        if ((dmgLo is > 0) || (dmgHi is > 0) || (speed is > 0) || (range is > 0))
        {
            vsb.AppendLine("  --- Weapon ---");
            if ((dmgLo is > 0) || (dmgHi is > 0))
            {
                vsb.Append("  Damage  : ");
                vsb.Append(dmgLo ?? 0);
                vsb.Append('\u2013');
                vsb.Append(dmgHi ?? 0);
                if (magDmg is > 0)
                {
                    vsb.Append(" (+");
                    vsb.Append(magDmg.Value);
                    vsb.Append(" magic)");
                }
                vsb.AppendLine();
            }
            if (speed is > 0)
            {
                vsb.Append("  Speed   : ");
                vsb.Append(speed.Value);
                if (magSpd is > 0)
                {
                    vsb.Append(" (+");
                    vsb.Append(magSpd.Value);
                    vsb.Append(')');
                }
                vsb.AppendLine();
            }
            if (range is > 0)
            {
                vsb.Append("  Range   : ");
                vsb.AppendLine(range.Value);
            }
            if ((toHit is > 0) || (magHit is > 0))
            {
                vsb.Append("  To-Hit  : ");
                vsb.Append(toHit ?? 0);
                if (magHit is > 0)
                {
                    vsb.Append(" (+");
                    vsb.Append(magHit.Value);
                    vsb.Append(" magic)");
                }
                vsb.AppendLine();
            }
            if (minStr is > 0)
            {
                vsb.Append("  Min STR : ");
                vsb.AppendLine(minStr.Value);
            }
            vsb.AppendLine();
        }
    }

    private static void AppendArmor(ref ValueStringBuilder vsb, MobData mob)
    {
        var ac = GetPropInt32(mob, ObjectField.ObjFArmorAcAdj);
        var magAc = GetPropInt32(mob, ObjectField.ObjFArmorMagicAcAdj);
        var silent = GetPropInt32(mob, ObjectField.ObjFArmorSilentMoveAdj);

        if (ac is not null || magAc is not null)
        {
            vsb.AppendLine("  --- Armor ---");
            if (ac.HasValue)
            {
                vsb.Append("  AC Adj      : ");
                vsb.Append(ac.Value);
                if (magAc is > 0)
                {
                    vsb.Append(" (+");
                    vsb.Append(magAc.Value);
                    vsb.Append(" magic)");
                }
                vsb.AppendLine();
            }
            if (silent is > 0)
            {
                vsb.Append("  Silent Move : ");
                vsb.AppendLine(silent.Value);
            }
            vsb.AppendLine();
        }
    }

    private static void AppendGold(ref ValueStringBuilder vsb, MobData mob)
    {
        var qty = GetPropInt32(mob, ObjectField.ObjFGoldQuantity);
        if (qty is > 0)
        {
            vsb.AppendLine("  --- Gold ---");
            vsb.Append("  Quantity : ");
            vsb.AppendLine(qty.Value);
            vsb.AppendLine();
        }
    }

    private static void AppendFood(ref ValueStringBuilder vsb, MobData mob)
    {
        var flags = GetPropInt32(mob, ObjectField.ObjFFoodFlags);
        // Food type flags bits are undocumented; we show the raw value only when non-zero
        if (flags is not null)
        {
            vsb.AppendLine("  --- Food ---");
            if (flags.Value != 0)
            {
                vsb.Append("  Flags : ");
                vsb.AppendHex((uint)flags.Value, "0x".AsSpan());
                vsb.AppendLine("  (food type flags)");
            }
            vsb.AppendLine();
        }
    }

    private static void AppendScroll(ref ValueStringBuilder vsb, MobData mob)
    {
        var flags = GetPropInt32(mob, ObjectField.ObjFScrollFlags);
        if (flags is not null)
        {
            vsb.AppendLine("  --- Scroll ---");
            if (flags.Value != 0)
            {
                vsb.Append("  Flags : ");
                vsb.AppendHex((uint)flags.Value, "0x".AsSpan());
                vsb.AppendLine("  (scroll type flags)");
            }
            vsb.AppendLine();
        }
    }

    private static void AppendAmmo(ref ValueStringBuilder vsb, MobData mob)
    {
        var qty = GetPropInt32(mob, ObjectField.ObjFAmmoQuantity);
        var type = GetPropInt32(mob, ObjectField.ObjFAmmoType);
        if (qty is not null || type is not null)
        {
            vsb.AppendLine("  --- Ammo ---");
            if (qty.HasValue)
            {
                vsb.Append("  Quantity : ");
                vsb.AppendLine(qty.Value);
            }
            if (type.HasValue)
            {
                vsb.Append("  Type     : ");
                vsb.Append(type.Value);
                vsb.AppendLine("  (ammo type — matches ObjFWeaponAmmoType)");
            }
            vsb.AppendLine();
        }
    }

    private static void AppendKey(ref ValueStringBuilder vsb, MobData mob)
    {
        var keyId = GetPropInt32(mob, ObjectField.ObjFKeyKeyId);
        if (keyId is not null)
        {
            vsb.AppendLine("  --- Key ---");
            vsb.Append("  Key ID : ");
            vsb.Append(keyId.Value);
            vsb.AppendLine("  (must match ObjFPortalKeyId or ObjFContainerKeyId)");
            vsb.AppendLine();
        }
    }

    private static void AppendWritten(ref ValueStringBuilder vsb, MobData mob)
    {
        var subtype = GetPropInt32(mob, ObjectField.ObjFWrittenSubtype);
        var startLine = GetPropInt32(mob, ObjectField.ObjFWrittenTextStartLine);
        var endLine = GetPropInt32(mob, ObjectField.ObjFWrittenTextEndLine);
        if (subtype is not null || startLine is not null)
        {
            vsb.AppendLine("  --- Written Item ---");
            if (subtype.HasValue)
            {
                // 0=book, 1=note, 2=letter, 3=manual
                var subtypeLabel = subtype.Value switch
                {
                    0 => "book",
                    1 => "note",
                    2 => "letter",
                    3 => "manual",
                    _ => subtype.Value.ToString(),
                };
                vsb.Append("  Subtype    : ");
                vsb.AppendLine(subtypeLabel);
            }
            if (startLine is not null || endLine is not null)
            {
                vsb.Append("  Text lines : ");
                vsb.Append(startLine ?? 0);
                vsb.Append("..");
                vsb.Append(endLine ?? 0);
                vsb.AppendLine("  (line indices into the text MES file)");
            }
            vsb.AppendLine();
        }
    }

    private static void AppendGeneric(ref ValueStringBuilder vsb, MobData mob)
    {
        var bonus = GetPropInt32(mob, ObjectField.ObjFGenericUsageBonus);
        var count = GetPropInt32(mob, ObjectField.ObjFGenericUsageCountRemaining);
        if (bonus is > 0 || count is not null)
        {
            vsb.AppendLine("  --- Generic Item ---");
            if (bonus is > 0)
            {
                vsb.Append("  Usage bonus     : +");
                vsb.AppendLine(bonus.Value);
            }
            if (count is not null)
            {
                vsb.Append("  Uses remaining  : ");
                vsb.AppendLine(count.Value);
            }
            vsb.AppendLine();
        }
    }

    private static int? GetPropInt32(MobData mob, ObjectField field)
    {
        var prop = mob.Properties.FirstOrDefault(p => p.Field == field);
        return prop?.GetInt32();
    }
}
