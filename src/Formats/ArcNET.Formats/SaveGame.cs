namespace ArcNET.Formats;

/// <summary>
/// Identifies the Arcanum engine build that produced a save slot.
/// Detected from object-file version fields found inside the save's TFAF data.
/// </summary>
public enum SaveEngineVersion
{
    /// <summary>
    /// Original Arcanum engine (vanilla or UAP). Object-file version <c>0x08</c>.
    /// All vanilla and UAP saves use this engine version.
    /// </summary>
    Vanilla = 0x08,

    /// <summary>
    /// arcanum-CE engine. Object-file version <c>0x77</c>.
    /// Enables additional common-extension property bits 41–63 and PC/NPC bits above 152.
    /// </summary>
    ArcanumCE = 0x77,
}

/// <summary>
/// Complete parsed state of one Arcanum save slot.
/// <para>
/// A save slot on disk is a pair of binary files sharing a stem name:
/// <list type="bullet">
///   <item><description><c>&lt;slot&gt;.tfai</c> — TFAI index (typed, tree-structured entry list)</description></item>
///   <item><description><c>&lt;slot&gt;.tfaf</c> — TFAF data blob (raw concatenation of all file payloads in DFS order)</description></item>
///   <item><description><c>&lt;slot&gt;.gsi</c>  — GSI metadata (display name, leader stats, map ID, game time)</description></item>
/// </list>
/// </para>
/// <para>
/// In addition to per-map files under <c>modules/&lt;module&gt;/maps/&lt;map&gt;</c>,
/// the TFAF may also contain top-level save-global files such as <c>data.sav</c>,
/// <c>data2.sav</c>, and town-map fog <c>.tmf</c> files. Known save-global formats are
/// exposed through <see cref="TownMapFogs"/>, <see cref="DataSavFiles"/>, and
/// <see cref="Data2SavFiles"/>, while unresolved files remain preserved through
/// <see cref="RawFiles"/>.
/// </para>
/// <para>
/// Use <see cref="SaveGameReader"/> to load a slot from disk or from bytes, and
/// <see cref="SaveGameWriter"/> to write it back.
/// </para>
/// </summary>
public sealed class SaveGame
{
    /// <summary>Save-slot metadata read from the companion <c>.gsi</c> file.</summary>
    public required SaveInfo Info { get; init; }

    /// <summary>
    /// Engine version that produced this save, derived from object-file version fields
    /// found inside the TFAF data. <see cref="SaveEngineVersion.Vanilla"/> when no
    /// arcanum-CE objects are detected.
    /// </summary>
    public SaveEngineVersion EngineVersion { get; init; } = SaveEngineVersion.Vanilla;

    /// <summary>
    /// Parsed state of every map folder found inside the TFAF data blob, one entry per
    /// unique <c>modules/&lt;module&gt;/maps/&lt;map&gt;</c> path.
    /// </summary>
    public required IReadOnlyList<SaveMapState> Maps { get; init; }

    /// <summary>
    /// Module-level message-file overrides present at the
    /// <c>modules/&lt;module&gt;/&lt;module&gt;.mes</c> level inside the TFAF blob.
    /// The key is the archive-relative virtual path (e.g. <c>modules/Arcanum/Arcanum.mes</c>);
    /// the value is the raw message-file bytes.
    /// May be empty when no per-save message overrides exist.
    /// </summary>
    public IReadOnlyList<(string VirtualPath, byte[] Data)> MessageFiles { get; init; } = [];

    /// <summary>
    /// Top-level town-map fog files present in the save.
    /// The key is the archive-relative virtual path (for example <c>Tsen Ang.tmf</c>).
    /// May be empty when the save contains no town-map fog files.
    /// </summary>
    public IReadOnlyList<(string VirtualPath, TownMapFog Data)> TownMapFogs { get; init; } = [];

    /// <summary>
    /// Parsed <c>data.sav</c> save-global files present in the save.
    /// The current typed surface is structural rather than semantic: it exposes the verified
    /// header + aligned-row framing while preserving all bytes verbatim for write-safe edits.
    /// </summary>
    public IReadOnlyList<(string VirtualPath, DataSavFile Data)> DataSavFiles { get; init; } = [];

    /// <summary>
    /// Parsed <c>data2.sav</c> save-global files present in the save.
    /// The current typed surface covers the verified <c>50000+</c> ID pair table while
    /// preserving all unresolved bytes verbatim for write-safe round-trip edits.
    /// </summary>
    public IReadOnlyList<(string VirtualPath, Data2SavFile Data)> Data2SavFiles { get; init; } = [];

    /// <summary>
    /// Raw non-map save files that do not yet have a typed format model.
    /// This currently captures unresolved save-global blobs beyond the structural
    /// <c>data.sav</c> / partial typed <c>data2.sav</c> surfaces and unknown future files,
    /// while preserving their original virtual paths and bytes.
    /// </summary>
    public IReadOnlyList<(string VirtualPath, byte[] Data)> RawFiles { get; init; } = [];
}
