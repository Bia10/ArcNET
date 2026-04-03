namespace ArcNET.GameObjects.Classes;

/// <summary>
/// Data model for an NPC or creature template loaded from TDF text data.
/// Parsing of the text format is the responsibility of <c>ArcNET.Formats.TextDataFormat</c>.
/// </summary>
public class Entity
{
    public (int Id, string Text) Description { get; set; }
    public int InternalName { get; set; }
    public int Level { get; set; }
    public (int ArtNumber, int Palette) ArtNumberAndPalette { get; set; }
    public int Scale { get; set; }
    public int Alignment { get; set; }

    public List<ObjFFlags> ObjectFlags { get; set; } = [];
    public List<ObjFCritterFlags> CritterFlags { get; set; } = [];
    public List<ObjFCritterFlags2> CritterFlags2 { get; set; } = [];
    public List<ObjFNpcFlags> NpcFlags { get; set; } = [];
    public List<ObjFBlitFlags> BlitFlags { get; set; } = [];
    public List<ObjFSpellFlags> SpellFlags { get; set; } = [];

    public int HitChart { get; set; }
    public List<(BasicStatType Stat, int Value)> BasicStats { get; set; } = [];
    public List<string> Spells { get; set; } = [];
    public List<(int A, int B, int C, int D, int E, int F)> Scripts { get; set; } = [];

    public int Faction { get; set; }
    public int AIPacket { get; set; }
    public int Material { get; set; }
    public int HitPoints { get; set; }
    public int Fatigue { get; set; }

    public List<(ResistanceType Type, int Value)> Resistances { get; set; } = [];
    public List<(DamageType Type, int Min, int Max)> Damages { get; set; } = [];

    public int SoundBank { get; set; }
    public int Category { get; set; }
    public int AutoLevelScheme { get; set; }
    public int InventorySource { get; set; }
}
