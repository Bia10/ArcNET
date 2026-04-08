namespace ArcNET.Formats;

/// <summary>
/// Parsed state for a single map inside a save-game TFAF archive.
/// <para>
/// Each map occupies a <c>modules/&lt;module&gt;/maps/&lt;mapname&gt;</c> directory in the
/// TFAF blob and may contain any combination of the following file types:
/// <c>.prp</c>, <c>.jmp</c>, <c>mobile/*.mob</c>, <c>mobile.md</c>,
/// <c>mobile.mdy</c>, and one or more <c>sector_*.sec</c> files.
/// </para>
/// </summary>
public sealed class SaveMapState
{
    /// <summary>
    /// Archive-relative path of this map's directory inside the TFAF blob,
    /// e.g. <c>modules/Arcanum/maps/Map01</c>.
    /// Used as the path prefix when re-serialising this map back into a TFAF archive.
    /// </summary>
    public required string MapPath { get; init; }

    /// <summary>
    /// Map terrain and tile-limit properties from the companion <c>map.prp</c> file.
    /// <see langword="null"/> when no <c>map.prp</c> file is present for this map.
    /// </summary>
    public MapProperties? Properties { get; init; }

    /// <summary>
    /// Jump-point transition table from the companion <c>map.jmp</c> file.
    /// <see langword="null"/> when no <c>map.jmp</c> file is present for this map.
    /// </summary>
    public JmpFile? JumpPoints { get; init; }

    /// <summary>
    /// All sector files present in this map's directory.
    /// Each tuple pairs the original file name (e.g. <c>sector_000000.sec</c>) with
    /// its parsed <see cref="Sector"/> contents.
    /// The name is used verbatim when the save is written back to disk.
    /// </summary>
    public required IReadOnlyList<(string FileName, Sector Data)> Sectors { get; init; }

    /// <summary>
    /// Static world objects from the <c>mobile/</c> sub-directory.
    /// Each tuple pairs the original file name (e.g. <c>G_00001234.mob</c>) with
    /// its parsed <see cref="MobData"/> body.
    /// The name is used verbatim when the save is written back to disk.
    /// </summary>
    public required IReadOnlyList<(string FileName, MobData Data)> StaticObjects { get; init; }

    /// <summary>
    /// Runtime diffs for static world objects from the companion <c>mobile.md</c> file.
    /// <see langword="null"/> when no <c>mobile.md</c> file is present for this map.
    /// </summary>
    public MobileMdFile? StaticDiffs { get; init; }

    /// <summary>
    /// Dynamically spawned objects (NPCs, dropped items, PC character record) from the
    /// companion <c>mobile.mdy</c> file.
    /// <see langword="null"/> when no <c>mobile.mdy</c> file is present for this map.
    /// </summary>
    public MobileMdyFile? DynamicObjects { get; init; }
}
