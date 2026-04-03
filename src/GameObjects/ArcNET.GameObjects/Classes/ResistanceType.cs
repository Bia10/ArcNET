namespace ArcNET.GameObjects.Classes;

/// <summary>Resistance categories.</summary>
public enum ResistanceType : byte
{
    /// <summary>Resistance to physical damage.</summary>
    Damage = 0,

    /// <summary>Resistance to fire damage.</summary>
    Fire = 1,

    /// <summary>Resistance to electrical damage.</summary>
    Electrical = 2,

    /// <summary>Resistance to poison damage.</summary>
    Poison = 3,

    /// <summary>Resistance to magical effects.</summary>
    Magic = 4,
}
