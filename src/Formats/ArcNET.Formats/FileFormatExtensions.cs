using System.Collections.Frozen;

namespace ArcNET.Formats;

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
        [".tmf"] = FileFormat.TownMapFog,
        [".dat"] = FileFormat.DataArchive,
        [".gsi"] = FileFormat.SaveInfo,
        [".tfai"] = FileFormat.SaveIndex,
        [".tfaf"] = FileFormat.SaveData,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the <see cref="FileFormat"/> for the given file extension (including leading dot).</summary>
    public static FileFormat FromExtension(string extension) =>
        s_byExtension.TryGetValue(extension, out var fmt) ? fmt : FileFormat.Unknown;

    /// <summary>Infers the format from a file path.</summary>
    public static FileFormat FromPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return FromExtension(Path.GetExtension(path));
    }
}
