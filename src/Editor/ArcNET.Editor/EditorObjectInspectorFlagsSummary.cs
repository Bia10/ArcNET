using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Typed flags-pane contract for one object/proto inspector target.
/// Type-specific groups remain <see langword="null"/> when they do not apply to the current target.
/// </summary>
public sealed class EditorObjectInspectorFlagsSummary
{
    /// <summary>
    /// Top-level inspector summary that owns this flags contract.
    /// </summary>
    public required EditorObjectInspectorSummary Inspector { get; init; }

    /// <summary>
    /// Common object flags from <see cref="ObjectField.ObjectFlags"/>.
    /// </summary>
    public ObjectFlags ObjectFlags { get; init; }

    /// <summary>
    /// Common spell flags from <see cref="ObjectField.SpellFlags"/>.
    /// </summary>
    public SpellFlags SpellFlags { get; init; }

    /// <summary>
    /// Wall flags when the current target is one wall object.
    /// </summary>
    public int? WallFlags { get; init; }

    /// <summary>
    /// Portal flags when the current target is one portal object.
    /// </summary>
    public PortalFlags? PortalFlags { get; init; }

    /// <summary>
    /// Container flags when the current target is one container object.
    /// </summary>
    public ContainerFlags? ContainerFlags { get; init; }

    /// <summary>
    /// Scenery flags when the current target is one scenery object.
    /// </summary>
    public SceneryFlags? SceneryFlags { get; init; }

    /// <summary>
    /// Projectile combat flags when the current target is one projectile object.
    /// </summary>
    public int? ProjectileCombatFlags { get; init; }

    /// <summary>
    /// Base item flags when the current target is one item-derived object.
    /// </summary>
    public ItemFlags? ItemFlags { get; init; }

    /// <summary>
    /// Weapon flags when the current target is one weapon object.
    /// </summary>
    public WeaponFlags? WeaponFlags { get; init; }

    /// <summary>
    /// Ammo flags when the current target is one ammunition object.
    /// </summary>
    public int? AmmoFlags { get; init; }

    /// <summary>
    /// Armor flags when the current target is one armor object.
    /// </summary>
    public ArmorFlags? ArmorFlags { get; init; }

    /// <summary>
    /// Gold flags when the current target is one gold object.
    /// </summary>
    public int? GoldFlags { get; init; }

    /// <summary>
    /// Food flags when the current target is one food object.
    /// </summary>
    public int? FoodFlags { get; init; }

    /// <summary>
    /// Scroll flags when the current target is one scroll object.
    /// </summary>
    public int? ScrollFlags { get; init; }

    /// <summary>
    /// Key ring flags when the current target is one key-ring object.
    /// </summary>
    public int? KeyRingFlags { get; init; }

    /// <summary>
    /// Written-item flags when the current target is one written object.
    /// </summary>
    public int? WrittenFlags { get; init; }

    /// <summary>
    /// Generic-item flags when the current target is one generic item.
    /// </summary>
    public int? GenericFlags { get; init; }

    /// <summary>
    /// Critter flags when the current target is one Pc or Npc.
    /// </summary>
    public CritterFlags? CritterFlags { get; init; }

    /// <summary>
    /// Secondary critter flags when the current target is one Pc or Npc.
    /// </summary>
    public CritterFlags2? CritterFlags2 { get; init; }

    /// <summary>
    /// Raw PC flags when the current target is one Pc.
    /// </summary>
    public int? PcFlags { get; init; }

    /// <summary>
    /// NPC flags when the current target is one Npc.
    /// </summary>
    public NpcFlags? NpcFlags { get; init; }

    /// <summary>
    /// Trap flags when the current target is one trap object.
    /// </summary>
    public int? TrapFlags { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when one or more type-specific flag groups apply to the current target.
    /// </summary>
    public bool HasTypeSpecificGroups =>
        WallFlags.HasValue
        || PortalFlags.HasValue
        || ContainerFlags.HasValue
        || SceneryFlags.HasValue
        || ProjectileCombatFlags.HasValue
        || ItemFlags.HasValue
        || WeaponFlags.HasValue
        || AmmoFlags.HasValue
        || ArmorFlags.HasValue
        || GoldFlags.HasValue
        || FoodFlags.HasValue
        || ScrollFlags.HasValue
        || KeyRingFlags.HasValue
        || WrittenFlags.HasValue
        || GenericFlags.HasValue
        || CritterFlags.HasValue
        || CritterFlags2.HasValue
        || PcFlags.HasValue
        || NpcFlags.HasValue
        || TrapFlags.HasValue;

    internal static EditorObjectInspectorFlagsSummary Create(
        EditorObjectInspectorSummary inspector,
        IReadOnlyList<ObjectProperty> properties
    )
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(properties);

        var objectType = inspector.TargetObjectType;
        var isItemLike = IsItemLike(objectType);
        var isCritter = objectType is ObjectType.Pc or ObjectType.Npc;

        return new EditorObjectInspectorFlagsSummary
        {
            Inspector = inspector,
            ObjectFlags = ReadEnum<ObjectFlags>(properties, ObjectField.ObjectFlags),
            SpellFlags = ReadEnum<SpellFlags>(properties, ObjectField.SpellFlags),
            WallFlags = objectType is ObjectType.Wall ? ReadInt32(properties, ObjectField.WallFlags) : null,
            PortalFlags =
                objectType is ObjectType.Portal ? ReadEnum<PortalFlags>(properties, ObjectField.PortalFlags) : null,
            ContainerFlags =
                objectType is ObjectType.Container
                    ? ReadEnum<ContainerFlags>(properties, ObjectField.ContainerFlags)
                    : null,
            SceneryFlags =
                objectType is ObjectType.Scenery ? ReadEnum<SceneryFlags>(properties, ObjectField.SceneryFlags) : null,
            ProjectileCombatFlags =
                objectType is ObjectType.Projectile ? ReadInt32(properties, ObjectField.ProjectileFlagsCombat) : null,
            ItemFlags = isItemLike ? ReadEnum<ItemFlags>(properties, ObjectField.ItemFlags) : null,
            WeaponFlags =
                objectType is ObjectType.Weapon ? ReadEnum<WeaponFlags>(properties, ObjectField.WeaponFlags) : null,
            AmmoFlags = objectType is ObjectType.Ammo ? ReadInt32(properties, ObjectField.AmmoFlags) : null,
            ArmorFlags =
                objectType is ObjectType.Armor ? ReadEnum<ArmorFlags>(properties, ObjectField.ArmorFlags) : null,
            GoldFlags = objectType is ObjectType.Gold ? ReadInt32(properties, ObjectField.GoldFlags) : null,
            FoodFlags = objectType is ObjectType.Food ? ReadInt32(properties, ObjectField.FoodFlags) : null,
            ScrollFlags = objectType is ObjectType.Scroll ? ReadInt32(properties, ObjectField.ScrollFlags) : null,
            KeyRingFlags = objectType is ObjectType.KeyRing ? ReadInt32(properties, ObjectField.KeyRingFlags) : null,
            WrittenFlags = objectType is ObjectType.Written ? ReadInt32(properties, ObjectField.WrittenFlags) : null,
            GenericFlags = objectType is ObjectType.Generic ? ReadInt32(properties, ObjectField.GenericFlags) : null,
            CritterFlags = isCritter ? ReadEnum<CritterFlags>(properties, ObjectField.CritterFlags) : null,
            CritterFlags2 = isCritter ? ReadEnum<CritterFlags2>(properties, ObjectField.CritterFlags2) : null,
            PcFlags = objectType is ObjectType.Pc ? ReadInt32(properties, ObjectField.PcFlags) : null,
            NpcFlags = objectType is ObjectType.Npc ? ReadEnum<NpcFlags>(properties, ObjectField.NpcFlags) : null,
            TrapFlags = objectType is ObjectType.Trap ? ReadInt32(properties, ObjectField.TrapFlags) : null,
        };
    }

    private static bool IsItemLike(ObjectType? objectType) =>
        objectType
            is ObjectType.Weapon
                or ObjectType.Ammo
                or ObjectType.Armor
                or ObjectType.Gold
                or ObjectType.Food
                or ObjectType.Scroll
                or ObjectType.Key
                or ObjectType.KeyRing
                or ObjectType.Written
                or ObjectType.Generic;

    private static int ReadInt32(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field == field)
                return property.GetInt32();
        }

        return 0;
    }

    private static TEnum ReadEnum<TEnum>(IReadOnlyList<ObjectProperty> properties, ObjectField field)
        where TEnum : struct, Enum =>
        (TEnum)Enum.ToObject(typeof(TEnum), unchecked((uint)ReadInt32(properties, field)));
}
