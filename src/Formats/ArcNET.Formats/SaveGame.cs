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
}
