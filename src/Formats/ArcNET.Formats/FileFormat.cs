namespace ArcNET.Formats;

/// <summary>Known Arcanum file format types.</summary>
public enum FileFormat : byte
{
    /// <summary>Unknown or unsupported format.</summary>
    Unknown = 0,

    /// <summary>Sector file (.sec) — map tile and object data.</summary>
    Sector = 1,

    /// <summary>Prototype file (.pro) — game object template.</summary>
    Proto = 2,

    /// <summary>Message file (.mes) — localized text strings.</summary>
    Message = 3,

    /// <summary>Mobile file (.mob) — NPC/monster save state.</summary>
    Mob = 4,

    /// <summary>Art file (.art) — sprite animation data.</summary>
    Art = 5,

    /// <summary>Jump file (.jmp) — world map jump points.</summary>
    Jmp = 6,

    /// <summary>Script file (.scr) — compiled game script.</summary>
    Script = 7,

    /// <summary>Dialog file (.dlg) — conversation tree.</summary>
    Dialog = 8,

    /// <summary>Terrain definition file (.tdf).</summary>
    Terrain = 9,

    /// <summary>Map properties file (.prp).</summary>
    MapProperties = 10,

    /// <summary>FacadeWalk file (facwalk.*).</summary>
    FacadeWalk = 11,

    /// <summary>DAT archive file (.dat).</summary>
    DataArchive = 12,

    /// <summary>Save game info file (.gsi) — one per save slot.</summary>
    SaveInfo = 13,

    /// <summary>Save game archive index file (.tfai).</summary>
    SaveIndex = 14,

    /// <summary>Save game archive data blob file (.tfaf).</summary>
    SaveData = 15,

    /// <summary>Town map fog bit-array file (.tmf).</summary>
    TownMapFog = 16,
}
