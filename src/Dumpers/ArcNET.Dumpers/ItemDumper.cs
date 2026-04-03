using System.Buffers.Binary;
using System.Text;
using ArcNET.Archive;
using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameObjects;

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
            catch
            {
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
                    catch
                    {
                        // Skip corrupt files
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
        var sb = new StringBuilder();
        var name = ResolveItemName(mob, protoId, nameLookup, installation);
        sb.AppendLine($"=== ITEM: {name} ===");
        sb.AppendLine($"  Proto    : {protoId}");
        sb.AppendLine($"  ObjectId : {mob.Header.ObjectId}");
        sb.AppendLine($"  Type     : {mob.Header.GameObjectType}");
        sb.AppendLine();
        AppendItemBase(sb, mob);
        AppendTypeSpecific(sb, mob);
        return sb.ToString();
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
        var sb = new StringBuilder();

        var vanillaId = ArcanumInstallation.ToVanillaProtoId(protoId, installation);
        if (nameLookup.TryGetValue(vanillaId, out var name) || nameLookup.TryGetValue(protoId, out name))
            sb.AppendLine($"  Name : {name}");

        sb.Append(ProtoDumper.Dump(proto));
        return sb.ToString();
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
    public static string DumpContainerItems(
        DatArchive archive,
        string containerMobPath,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup
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
        return DumpContainerItemsCore(container, archive, mapDirPrefix, nameLookup);
    }

    /// <summary>
    /// Dumps all inventory items from a pre-parsed container mob, resolving each child
    /// mob from <paramref name="archiveForItems"/>.
    /// </summary>
    public static string DumpContainerItems(
        MobData container,
        DatArchive archiveForItems,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup
    ) => DumpContainerItemsCore(container, archiveForItems, mapDirPrefix, nameLookup);

    /// <inheritdoc cref="DumpContainerItems(DatArchive, string, string, Dictionary{int,string})"/>
    public static void DumpContainerItems(
        DatArchive archive,
        string containerMobPath,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup,
        TextWriter writer
    ) => writer.Write(DumpContainerItems(archive, containerMobPath, mapDirPrefix, nameLookup));

    /// <inheritdoc cref="DumpContainerItems(MobData, DatArchive, string, Dictionary{int,string})"/>
    public static void DumpContainerItems(
        MobData container,
        DatArchive archiveForItems,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup,
        TextWriter writer
    ) => writer.Write(DumpContainerItemsCore(container, archiveForItems, mapDirPrefix, nameLookup));

    // ── Private helpers ────────────────────────────────────────────────────────

    private static string DumpContainerItemsCore(
        MobData container,
        DatArchive archiveForItems,
        string mapDirPrefix,
        Dictionary<int, string> nameLookup
    )
    {
        var sb = new StringBuilder();

        var invNumProp = container.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFContainerInventoryNum);
        var invSrcProp = container.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFContainerInventorySource);
        var invListProp = container.Properties.FirstOrDefault(p =>
            p.Field == ObjectField.ObjFContainerInventoryListIdx
        );

        var invNum = invNumProp?.GetInt32() ?? 0;
        var invSrc = invSrcProp?.GetInt32() ?? 0;

        sb.AppendLine($"  InvNum    = {invNum}");
        sb.AppendLine($"  InvSource = {invSrc}");

        if (invListProp is null || invNum == 0)
        {
            sb.AppendLine("  (empty inventory)");
            return sb.ToString();
        }

        var items = invListProp.GetObjectIdArrayFull();
        sb.AppendLine($"  Items: {items.Length}");
        sb.AppendLine();

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
            catch
            {
                sb.AppendLine($"  [{i + 1}] {guid} — could not load");
                continue;
            }

            var protoId = BinaryPrimitives.ReadInt32LittleEndian(itemMob.Header.ProtoId.Id.ToByteArray());
            sb.AppendLine($"  [{i + 1}]");
            sb.Append(DumpItem(itemMob, protoId, nameLookup, ArcanumInstallationType.Vanilla));
        }

        return sb.ToString();
    }

    private static void AppendItemBase(StringBuilder sb, MobData mob)
    {
        var weight = GetPropInt32(mob, ObjectField.ObjFItemWeight);
        var worth = GetPropInt32(mob, ObjectField.ObjFItemWorth);
        var flags = GetPropInt32(mob, ObjectField.ObjFItemFlags);
        var spell1 = GetPropInt32(mob, ObjectField.ObjFItemSpell1);
        var spell2 = GetPropInt32(mob, ObjectField.ObjFItemSpell2);

        if ((weight is > 0) || (worth is > 0))
        {
            sb.AppendLine("  --- Item Base ---");
            if (weight is > 0)
                sb.AppendLine($"  Weight : {weight.Value / 10.0:F1} lbs");
            if (worth is > 0)
                sb.AppendLine($"  Worth  : {worth.Value} gp");
            if (flags is > 0)
                sb.AppendLine($"  Flags  : 0x{flags.Value:X8}");
            if (spell1 is > 0)
                sb.AppendLine($"  Spell1 : {spell1.Value}");
            if (spell2 is > 0)
                sb.AppendLine($"  Spell2 : {spell2.Value}");
            sb.AppendLine();
        }
    }

    private static void AppendTypeSpecific(StringBuilder sb, MobData mob)
    {
        switch (mob.Header.GameObjectType)
        {
            case ObjectType.Weapon:
                AppendWeapon(sb, mob);
                break;
            case ObjectType.Armor:
                AppendArmor(sb, mob);
                break;
            case ObjectType.Gold:
                AppendGold(sb, mob);
                break;
            case ObjectType.Food:
                AppendFood(sb, mob);
                break;
            case ObjectType.Scroll:
                AppendScroll(sb, mob);
                break;
        }
    }

    private static void AppendWeapon(StringBuilder sb, MobData mob)
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
            sb.AppendLine("  --- Weapon ---");
            if ((dmgLo is > 0) || (dmgHi is > 0))
                sb.AppendLine(
                    $"  Damage  : {dmgLo ?? 0}\u2013{dmgHi ?? 0}{(magDmg is > 0 ? $" (+{magDmg} magic)" : "")}"
                );
            if (speed is > 0)
                sb.AppendLine($"  Speed   : {speed.Value}{(magSpd is > 0 ? $" (+{magSpd})" : "")}");
            if (range is > 0)
                sb.AppendLine($"  Range   : {range.Value}");
            if ((toHit is > 0) || (magHit is > 0))
                sb.AppendLine($"  To-Hit  : {toHit ?? 0}{(magHit is > 0 ? $" (+{magHit} magic)" : "")}");
            if (minStr is > 0)
                sb.AppendLine($"  Min STR : {minStr.Value}");
            sb.AppendLine();
        }
    }

    private static void AppendArmor(StringBuilder sb, MobData mob)
    {
        var ac = GetPropInt32(mob, ObjectField.ObjFArmorAcAdj);
        var magAc = GetPropInt32(mob, ObjectField.ObjFArmorMagicAcAdj);
        var silent = GetPropInt32(mob, ObjectField.ObjFArmorSilentMoveAdj);

        if (ac is not null || magAc is not null)
        {
            sb.AppendLine("  --- Armor ---");
            if (ac.HasValue)
                sb.AppendLine($"  AC Adj      : {ac.Value}{(magAc is > 0 ? $" (+{magAc} magic)" : "")}");
            if (silent is > 0)
                sb.AppendLine($"  Silent Move : {silent.Value}");
            sb.AppendLine();
        }
    }

    private static void AppendGold(StringBuilder sb, MobData mob)
    {
        var qty = GetPropInt32(mob, ObjectField.ObjFGoldQuantity);
        if (qty is > 0)
        {
            sb.AppendLine("  --- Gold ---");
            sb.AppendLine($"  Quantity : {qty.Value}");
            sb.AppendLine();
        }
    }

    private static void AppendFood(StringBuilder sb, MobData mob)
    {
        var flags = GetPropInt32(mob, ObjectField.ObjFFoodFlags);
        if (flags is > 0)
        {
            sb.AppendLine("  --- Food ---");
            sb.AppendLine($"  Flags : 0x{flags.Value:X8}");
            sb.AppendLine();
        }
    }

    private static void AppendScroll(StringBuilder sb, MobData mob)
    {
        var flags = GetPropInt32(mob, ObjectField.ObjFScrollFlags);
        if (flags is > 0)
        {
            sb.AppendLine("  --- Scroll ---");
            sb.AppendLine($"  Flags : 0x{flags.Value:X8}");
            sb.AppendLine();
        }
    }

    private static int? GetPropInt32(MobData mob, ObjectField field)
    {
        var prop = mob.Properties.FirstOrDefault(p => p.Field == field);
        return prop?.GetInt32();
    }
}
