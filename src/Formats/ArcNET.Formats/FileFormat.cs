using System.Collections.Frozen;

namespace ArcNET.Formats;

/// <summary>Known Arcanum file format types.</summary>
public enum FileFormat
{
    /// <summary>Unknown or unsupported format.</summary>
    Unknown,

    /// <summary>Sector file (.sec) — map tile and object data.</summary>
    Sector,

    /// <summary>Prototype file (.pro) — game object template.</summary>
    Proto,

    /// <summary>Message file (.mes) — localized text strings.</summary>
    Message,

    /// <summary>Mobile file (.mob) — NPC/monster save state.</summary>
    Mob,

    /// <summary>Art file (.art) — sprite animation data.</summary>
    Art,

    /// <summary>Jump file (.jmp) — world map jump points.</summary>
    Jmp,

    /// <summary>Script file (.scr) — compiled game script.</summary>
    Script,

    /// <summary>Dialog file (.dlg) — conversation tree.</summary>
    Dialog,

    /// <summary>Terrain definition file (.tdf).</summary>
    Terrain,

    /// <summary>Map properties file (.prp).</summary>
    MapProperties,

    /// <summary>FacadeWalk file (facwalk.*).</summary>
    FacadeWalk,

    /// <summary>DAT archive file (.dat).</summary>
    DataArchive,

    /// <summary>Save game info file (.gsi) — one per save slot.</summary>
    SaveInfo,

    /// <summary>Save game archive index file (.tfai).</summary>
    SaveIndex,

    /// <summary>Save game archive data blob file (.tfaf).</summary>
    SaveData,
}

/// <summary>Extension-based format lookup utilities.</summary>
public static class FileFormatExtensions
{
    private static readonly FrozenDictionary<string, FileFormat> s_byExtension = new Dictionary<string, FileFormat>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        [".sec"] = FileFormat.Sector,
        [".pro"] = FileFormat.Proto,
        [".mes"] = FileFormat.Message,
        [".mob"] = FileFormat.Mob,
        [".art"] = FileFormat.Art,
        [".jmp"] = FileFormat.Jmp,
        [".scr"] = FileFormat.Script,
        [".dlg"] = FileFormat.Dialog,
        [".tdf"] = FileFormat.Terrain,
        [".prp"] = FileFormat.MapProperties,
        [".dat"] = FileFormat.DataArchive,
        [".gsi"] = FileFormat.SaveInfo,
        [".tfai"] = FileFormat.SaveIndex,
        [".tfaf"] = FileFormat.SaveData,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the <see cref="FileFormat"/> for the given file extension (including leading dot).</summary>
    public static FileFormat FromExtension(string extension) =>
        s_byExtension.TryGetValue(extension, out var fmt) ? fmt : FileFormat.Unknown;

    /// <summary>Infers the format from a file path.</summary>
    public static FileFormat FromPath(string path) => FromExtension(Path.GetExtension(path));
}
