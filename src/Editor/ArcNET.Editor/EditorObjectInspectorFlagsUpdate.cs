using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Staged flags-pane update for one object/proto inspector target.
/// Null properties preserve the current value; non-null properties replace the target group.
/// </summary>
public sealed class EditorObjectInspectorFlagsUpdate
{
    public ObjectFlags? ObjectFlags { get; init; }

    public SpellFlags? SpellFlags { get; init; }

    public int? WallFlags { get; init; }

    public PortalFlags? PortalFlags { get; init; }

    public ContainerFlags? ContainerFlags { get; init; }

    public SceneryFlags? SceneryFlags { get; init; }

    public int? ProjectileCombatFlags { get; init; }

    public ItemFlags? ItemFlags { get; init; }

    public WeaponFlags? WeaponFlags { get; init; }

    public int? AmmoFlags { get; init; }

    public ArmorFlags? ArmorFlags { get; init; }

    public int? GoldFlags { get; init; }

    public int? FoodFlags { get; init; }

    public int? ScrollFlags { get; init; }

    public int? KeyRingFlags { get; init; }

    public int? WrittenFlags { get; init; }

    public int? GenericFlags { get; init; }

    public CritterFlags? CritterFlags { get; init; }

    public CritterFlags2? CritterFlags2 { get; init; }

    public int? PcFlags { get; init; }

    public NpcFlags? NpcFlags { get; init; }

    public int? TrapFlags { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the update changes one or more flag groups.
    /// </summary>
    public bool HasChanges =>
        ObjectFlags.HasValue
        || SpellFlags.HasValue
        || WallFlags.HasValue
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
}
