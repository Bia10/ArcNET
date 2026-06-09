using System.Buffers.Binary;
using ArcNET.Archive;
using ArcNET.Core;
using ArcNET.Diagnostics;
using ArcNET.Formats;
using ArcNET.GameObjects;
using Bia.ValueBuffers;

namespace ArcNET.Diagnostics;

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
        vsb.AppendLine($"=== ITEM: {name} ===");
        vsb.AppendLine($"  Proto    : {protoId}");
        vsb.AppendLine($"  ObjectId : {mob.Header.ObjectId}");
        vsb.AppendLine($"  Type     : {mob.Header.GameObjectType}");
        vsb.AppendLine();
        var analysis = MobItemAnalysisService.Analyze(mob);
        AppendItemBase(ref vsb, analysis);
        AppendTypeSpecific(ref vsb, analysis);
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
            vsb.AppendLine($"  Name : {name}");

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

        var invNumProp = container.Properties.FirstOrDefault(p => p.Field == ObjectField.ContainerInventoryNum);
        var invSrcProp = container.Properties.FirstOrDefault(p => p.Field == ObjectField.ContainerInventorySource);
        var invListProp = container.Properties.FirstOrDefault(p => p.Field == ObjectField.ContainerInventoryListIdx);

        var invNum = invNumProp?.GetInt32() ?? 0;
        var invSrc = invSrcProp?.GetInt32() ?? 0;

        vsb.AppendLine($"  InvNum    = {invNum}");
        vsb.AppendLine($"  InvSource = {invSrc}");

        if (invListProp is null || invNum == 0)
        {
            vsb.AppendLine("  (empty inventory)");
            return vsb.ToString();
        }

        var items = invListProp.GetObjectIdArrayFull();
        vsb.AppendLine($"  Items: {items.Length}");
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
                vsb.AppendLine($"  [{i + 1}] {guid} — could not load: {ex.Message}");
                continue;
            }

            var protoId = BinaryPrimitives.ReadInt32LittleEndian(itemMob.Header.ProtoId.Id.ToByteArray());
            vsb.AppendLine($"  [{i + 1}]");
            vsb.Append(DumpItem(itemMob, protoId, nameLookup, installation));
        }

        var totalGold = MobGoldResolver.ResolveContainerGoldQuantity(
            container,
            handle =>
            {
                var guidStr = handle.Id.ToString("N").ToUpperInvariant();
                var fileName =
                    $"G_{guidStr[..8]}_{guidStr[8..12]}_{guidStr[12..16]}_{guidStr[16..20]}_{guidStr[20..32]}.mob";
                var itemDatPath = mapDirPrefix + fileName;

                try
                {
                    return MobFormat.ParseMemory(archiveForItems.GetEntryData(itemDatPath));
                }
                catch
                {
                    return null;
                }
            }
        );

        if (totalGold is not null)
        {
            vsb.AppendLine($"  Gold Total: {totalGold.Value}");
            vsb.AppendLine();
        }

        return vsb.ToString();
    }

    private static void AppendItemBase(ref ValueStringBuilder vsb, MobItemAnalysisSnapshot analysis)
    {
        var hasContent =
            (analysis.Weight is > 0)
            || (analysis.Worth is > 0)
            || analysis.Discipline is > 0
            || analysis.Complexity is > 0
            || analysis.SpellEffects.Count > 0;
        if (hasContent)
        {
            vsb.AppendLine("  --- Item Base ---");
            if (analysis.Weight is > 0)
            {
                vsb.Append("  Weight       : ");
                vsb.Append(analysis.Weight.Value / 10.0, "F1");
                vsb.AppendLine(" lbs");
            }
            if (analysis.Worth is > 0)
            {
                vsb.Append("  Worth        : ");
                vsb.Append(analysis.Worth.Value);
                vsb.AppendLine(" gp");
            }
            if (analysis.ItemFlags is > 0)
            {
                vsb.Append("  Item flags   : ");
                vsb.AppendHex((uint)analysis.ItemFlags.Value, "0x".AsSpan());
                vsb.Append("  ");
                vsb.AppendLine(
                    analysis.ItemFlagNames.Count > 0 ? string.Join(" | ", analysis.ItemFlagNames) : "(unknown flags)"
                );
            }
            if (analysis.Discipline is > 0)
            {
                vsb.Append("  Discipline   : ");
                vsb.AppendLine(analysis.DisciplineLabel ?? analysis.Discipline.Value.ToString());
            }
            if (analysis.Complexity is > 0)
            {
                vsb.Append("  Tech complexity : ");
                vsb.Append(analysis.Complexity.Value);
                vsb.AppendLine("  (schematic difficulty)");
            }
            foreach (var effect in analysis.SpellEffects)
            {
                vsb.Append("  Spell effect ");
                vsb.Append(effect.Slot);
                vsb.Append(" : ID ");
                vsb.Append(effect.SpellId);
                vsb.AppendLine("  (see spell.mes)");
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

    private static void AppendTypeSpecific(ref ValueStringBuilder vsb, MobItemAnalysisSnapshot analysis)
    {
        switch (analysis.Specific)
        {
            case WeaponItemAnalysisSnapshot weapon:
                AppendWeapon(ref vsb, weapon);
                break;
            case ArmorItemAnalysisSnapshot armor:
                AppendArmor(ref vsb, armor);
                break;
            case GoldItemAnalysisSnapshot gold:
                AppendGold(ref vsb, gold);
                break;
            case FoodItemAnalysisSnapshot food:
                AppendFood(ref vsb, food);
                break;
            case ScrollItemAnalysisSnapshot scroll:
                AppendScroll(ref vsb, scroll);
                break;
            case AmmoItemAnalysisSnapshot ammo:
                AppendAmmo(ref vsb, ammo);
                break;
            case KeyItemAnalysisSnapshot key:
                AppendKey(ref vsb, key);
                break;
            case WrittenItemAnalysisSnapshot written:
                AppendWritten(ref vsb, written);
                break;
            case GenericItemAnalysisSnapshot generic:
                AppendGeneric(ref vsb, generic);
                break;
        }
    }

    private static void AppendWeapon(ref ValueStringBuilder vsb, WeaponItemAnalysisSnapshot weapon)
    {
        if (
            (weapon.DamageLower is > 0)
            || (weapon.DamageUpper is > 0)
            || (weapon.Speed is > 0)
            || (weapon.Range is > 0)
        )
        {
            vsb.AppendLine("  --- Weapon ---");
            if ((weapon.DamageLower is > 0) || (weapon.DamageUpper is > 0))
            {
                vsb.Append("  Damage  : ");
                vsb.Append(weapon.DamageLower ?? 0);
                vsb.Append('\u2013');
                vsb.Append(weapon.DamageUpper ?? 0);
                if (weapon.MagicDamageBonus is > 0)
                {
                    vsb.Append(" (+");
                    vsb.Append(weapon.MagicDamageBonus.Value);
                    vsb.Append(" magic)");
                }
                vsb.AppendLine();
            }
            if (weapon.Speed is > 0)
            {
                vsb.Append("  Speed   : ");
                vsb.Append(weapon.Speed.Value);
                if (weapon.MagicSpeedBonus is > 0)
                {
                    vsb.Append(" (+");
                    vsb.Append(weapon.MagicSpeedBonus.Value);
                    vsb.Append(')');
                }
                vsb.AppendLine();
            }
            if (weapon.Range is > 0)
            {
                vsb.Append("  Range   : ");
                vsb.AppendLine(weapon.Range.Value);
            }
            if ((weapon.BonusToHit is > 0) || (weapon.MagicHitBonus is > 0))
            {
                vsb.Append("  To-Hit  : ");
                vsb.Append(weapon.BonusToHit ?? 0);
                if (weapon.MagicHitBonus is > 0)
                {
                    vsb.Append(" (+");
                    vsb.Append(weapon.MagicHitBonus.Value);
                    vsb.Append(" magic)");
                }
                vsb.AppendLine();
            }
            if (weapon.MinStrength is > 0)
            {
                vsb.Append("  Min STR : ");
                vsb.AppendLine(weapon.MinStrength.Value);
            }
            vsb.AppendLine();
        }
    }

    private static void AppendArmor(ref ValueStringBuilder vsb, ArmorItemAnalysisSnapshot armor)
    {
        if (armor.ArmorClassAdjustment is not null || armor.MagicArmorClassAdjustment is not null)
        {
            vsb.AppendLine("  --- Armor ---");
            if (armor.ArmorClassAdjustment.HasValue)
            {
                vsb.Append("  AC Adj      : ");
                vsb.Append(armor.ArmorClassAdjustment.Value);
                if (armor.MagicArmorClassAdjustment is > 0)
                {
                    vsb.Append(" (+");
                    vsb.Append(armor.MagicArmorClassAdjustment.Value);
                    vsb.Append(" magic)");
                }
                vsb.AppendLine();
            }
            if (armor.SilentMoveAdjustment is > 0)
            {
                vsb.Append("  Silent Move : ");
                vsb.AppendLine(armor.SilentMoveAdjustment.Value);
            }
            vsb.AppendLine();
        }
    }

    private static void AppendGold(ref ValueStringBuilder vsb, GoldItemAnalysisSnapshot gold)
    {
        if (gold.Quantity is > 0)
        {
            vsb.AppendLine("  --- Gold ---");
            vsb.Append("  Quantity : ");
            vsb.AppendLine(gold.Quantity.Value);
            vsb.AppendLine();
        }
    }

    private static void AppendFood(ref ValueStringBuilder vsb, FoodItemAnalysisSnapshot food)
    {
        if (food.Flags is not null)
        {
            vsb.AppendLine("  --- Food ---");
            if (food.Flags.Value != 0)
            {
                vsb.Append("  Flags : ");
                vsb.AppendHex((uint)food.Flags.Value, "0x".AsSpan());
                vsb.AppendLine("  (food type flags)");
            }
            vsb.AppendLine();
        }
    }

    private static void AppendScroll(ref ValueStringBuilder vsb, ScrollItemAnalysisSnapshot scroll)
    {
        if (scroll.Flags is not null)
        {
            vsb.AppendLine("  --- Scroll ---");
            if (scroll.Flags.Value != 0)
            {
                vsb.Append("  Flags : ");
                vsb.AppendHex((uint)scroll.Flags.Value, "0x".AsSpan());
                vsb.AppendLine("  (scroll type flags)");
            }
            vsb.AppendLine();
        }
    }

    private static void AppendAmmo(ref ValueStringBuilder vsb, AmmoItemAnalysisSnapshot ammo)
    {
        if (ammo.Quantity is not null || ammo.AmmoType is not null)
        {
            vsb.AppendLine("  --- Ammo ---");
            if (ammo.Quantity.HasValue)
            {
                vsb.Append("  Quantity : ");
                vsb.AppendLine(ammo.Quantity.Value);
            }
            if (ammo.AmmoType.HasValue)
            {
                vsb.Append("  Type     : ");
                vsb.Append(ammo.AmmoType.Value);
                vsb.AppendLine("  (ammo type — matches WeaponAmmoType)");
            }
            vsb.AppendLine();
        }
    }

    private static void AppendKey(ref ValueStringBuilder vsb, KeyItemAnalysisSnapshot key)
    {
        if (key.KeyId is not null)
        {
            vsb.AppendLine("  --- Key ---");
            vsb.Append("  Key ID : ");
            vsb.Append(key.KeyId.Value);
            vsb.AppendLine("  (must match PortalKeyId or ContainerKeyId)");
            vsb.AppendLine();
        }
    }

    private static void AppendWritten(ref ValueStringBuilder vsb, WrittenItemAnalysisSnapshot written)
    {
        if (written.Subtype is not null || written.StartLine is not null)
        {
            vsb.AppendLine("  --- Written Item ---");
            if (written.Subtype.HasValue)
            {
                vsb.Append("  Subtype    : ");
                vsb.AppendLine(written.SubtypeLabel ?? written.Subtype.Value.ToString());
            }
            if (written.StartLine is not null || written.EndLine is not null)
            {
                vsb.Append("  Text lines : ");
                vsb.Append(written.StartLine ?? 0);
                vsb.Append("..");
                vsb.Append(written.EndLine ?? 0);
                vsb.AppendLine("  (line indices into the text MES file)");
            }
            vsb.AppendLine();
        }
    }

    private static void AppendGeneric(ref ValueStringBuilder vsb, GenericItemAnalysisSnapshot generic)
    {
        if (generic.UsageBonus is > 0 || generic.UsesRemaining is not null)
        {
            vsb.AppendLine("  --- Generic Item ---");
            if (generic.UsageBonus is > 0)
            {
                vsb.Append("  Usage bonus     : +");
                vsb.AppendLine(generic.UsageBonus.Value);
            }
            if (generic.UsesRemaining is not null)
            {
                vsb.Append("  Uses remaining  : ");
                vsb.AppendLine(generic.UsesRemaining.Value);
            }
            vsb.AppendLine();
        }
    }

    private static int? GetPropInt32(MobData mob, ObjectField field)
    {
        var prop = mob.Properties.FirstOrDefault(p => p.Field == field);
        return prop?.GetInt32();
    }

    private static float? GetPropFloat(MobData mob, ObjectField field)
    {
        var prop = mob.Properties.FirstOrDefault(p => p.Field == field);
        return prop?.GetFloat();
    }
}
