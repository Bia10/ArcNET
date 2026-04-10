using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Bundles optional per-type replacements passed to <see cref="SaveGameWriter.Save(LoadedSave, string, string, SaveGameUpdates?)"/>.
/// Only supply the dictionaries whose contents have changed; <see langword="null"/> keeps originals.
/// </summary>
public sealed record SaveGameUpdates
{
    /// <summary>
    /// Replacement <see cref="SaveInfo"/>; pass <see langword="null"/> to keep the original.
    /// </summary>
    public SaveInfo? UpdatedInfo { get; init; }

    /// <summary>
    /// Map of virtual path → updated <see cref="MobData"/> to replace in the save.
    /// Only paths already present in <see cref="LoadedSave.Files"/> are replaced.
    /// </summary>
    public IReadOnlyDictionary<string, MobData>? UpdatedMobiles { get; init; }

    /// <summary>
    /// Map of virtual path → updated <see cref="Sector"/> to replace in the save.
    /// </summary>
    public IReadOnlyDictionary<string, Sector>? UpdatedSectors { get; init; }

    /// <summary>
    /// Map of virtual path → updated <see cref="JmpFile"/> to replace in the save.
    /// </summary>
    public IReadOnlyDictionary<string, JmpFile>? UpdatedJumpFiles { get; init; }

    /// <summary>
    /// Map of virtual path → updated <see cref="MapProperties"/> to replace in the save.
    /// </summary>
    public IReadOnlyDictionary<string, MapProperties>? UpdatedMapProperties { get; init; }

    /// <summary>
    /// Map of virtual path → updated <see cref="ScrFile"/> to replace in the save.
    /// </summary>
    public IReadOnlyDictionary<string, ScrFile>? UpdatedScripts { get; init; }

    /// <summary>
    /// Map of virtual path → updated <see cref="DlgFile"/> to replace in the save.
    /// </summary>
    public IReadOnlyDictionary<string, DlgFile>? UpdatedDialogs { get; init; }

    /// <summary>
    /// Map of virtual path → updated <see cref="MobileMdFile"/> to replace in the save.
    /// Keys must be paths to <c>mobile.md</c> files already present in <see cref="LoadedSave.Files"/>.
    /// </summary>
    public IReadOnlyDictionary<string, MobileMdFile>? UpdatedMobileMds { get; init; }

    /// <summary>
    /// Map of virtual path → updated <see cref="MobileMdyFile"/> to replace in the save.
    /// Keys must be paths to <c>mobile.mdy</c> files already present in <see cref="LoadedSave.Files"/>.
    /// </summary>
    public IReadOnlyDictionary<string, MobileMdyFile>? UpdatedMobileMdys { get; init; }

    /// <summary>
    /// Map of virtual path → raw byte replacement applied after all typed updates.
    /// Use this for files that cannot be round-tripped through the typed parsers.
    /// </summary>
    public IReadOnlyDictionary<string, byte[]>? RawFileUpdates { get; init; }
}
