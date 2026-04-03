namespace ArcNET.GameObjects.Classes;

/// <summary>Stat types used by NPC/creature templates.</summary>
public enum BasicStatType : byte
{
    /// <summary>Character's gender.</summary>
    Gender = 0,

    /// <summary>Character's race.</summary>
    Race = 1,

    /// <summary>Strength attribute.</summary>
    Strength = 2,

    /// <summary>Dexterity attribute.</summary>
    Dexterity = 3,

    /// <summary>Constitution attribute.</summary>
    Constitution = 4,

    /// <summary>Beauty attribute.</summary>
    Beauty = 5,

    /// <summary>Intelligence attribute.</summary>
    Intelligence = 6,

    /// <summary>Willpower attribute.</summary>
    Willpower = 7,

    /// <summary>Charisma attribute.</summary>
    Charisma = 8,

    /// <summary>Perception attribute.</summary>
    Perception = 9,

    /// <summary>Technological aptitude points.</summary>
    TechPoints = 10,

    /// <summary>Magical aptitude points.</summary>
    MagickPoints = 11,
}
