namespace ArcNET.GameObjects.Classes;

/// <summary>
/// Data model for an NPC or creature template loaded from TDF text data.
/// Parsing of the text format is the responsibility of <c>ArcNET.Formats.TextDataFormat</c>.
/// </summary>
public class Entity
{
    public (int Id, string Text) Description { get; init; }
    public int InternalName { get; init; }
    public int Level { get; init; }
    public (int ArtNumber, int Palette) ArtNumberAndPalette { get; init; }
    public int Scale { get; init; }
    public int Alignment { get; init; }

    public List<ObjFFlags> ObjectFlags { get; init; } = [];
    public List<ObjFCritterFlags> CritterFlags { get; init; } = [];
    public List<ObjFCritterFlags2> CritterFlags2 { get; init; } = [];
    public List<ObjFNpcFlags> NpcFlags { get; init; } = [];
    public List<ObjFBlitFlags> BlitFlags { get; init; } = [];
    public List<ObjFSpellFlags> SpellFlags { get; init; } = [];

    public int HitChart { get; init; }
    public List<(BasicStatType Stat, int Value)> BasicStats { get; init; } = [];
    public List<string> Spells { get; init; } = [];
    public List<(int A, int B, int C, int D, int E, int F)> Scripts { get; init; } = [];

    public int Faction { get; init; }
    public int AIPacket { get; init; }
    public int Material { get; init; }
    public int HitPoints { get; init; }
    public int Fatigue { get; init; }

    public List<(ResistanceType Type, int Value)> Resistances { get; init; } = [];
    public List<(DamageType Type, int Min, int Max)> Damages { get; init; } = [];

    public int SoundBank { get; init; }
    public int Category { get; init; }
    public int AutoLevelScheme { get; init; }
    public int InventorySource { get; init; }
}
