namespace ArcNET.GameObjects.Classes;

/// <summary>Damage categories.</summary>
public enum DamageType : byte
{
    /// <summary>Normal physical damage.</summary>
    Normal = 0,

    /// <summary>Fatigue-based damage.</summary>
    Fatigue = 1,

    /// <summary>Poison damage over time.</summary>
    Poison = 2,

    /// <summary>Electrical damage.</summary>
    Electrical = 3,

    /// <summary>Fire damage.</summary>
    Fire = 4,
}
