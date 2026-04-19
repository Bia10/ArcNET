using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// In-memory representation of a fully loaded Arcanum save slot.
/// A save slot consists of three files: a <c>.gsi</c> info file, a <c>.tfai</c> index,
/// and a <c>.tfaf</c> data blob. All embedded files are extracted and stored here;
/// known binary formats are additionally pre-parsed into their respective typed dictionaries.
/// </summary>
public sealed class LoadedSave
{
    /// <summary>Save-slot metadata (leader name, map ID, game time, etc.).</summary>
    public required SaveInfo Info { get; init; }

    /// <summary>
    /// Parsed TFAI archive index describing the directory tree and file sizes.
    /// Used by <see cref="SaveGameWriter"/> to rebuild the index when file sizes change.
    /// </summary>
    public required SaveIndex Index { get; init; }

    /// <summary>
    /// All embedded file payloads keyed by virtual path
    /// (forward-slash separator, e.g. <c>"maps/map01/mobile/G_abc.mob"</c>).
    /// This dictionary contains every file extracted from the TFAF blob and is the
    /// authoritative source for <see cref="SaveGameWriter"/>.
    /// </summary>
    public required IReadOnlyDictionary<string, byte[]> Files { get; init; }

    /// <summary>
    /// Embedded files that do not currently have a successful typed editor surface.
    /// This is a subset of <see cref="Files"/> and includes unresolved save-global blobs
    /// beyond the structural <c>data.sav</c> / partial typed <c>data2.sav</c> surfaces,
    /// unknown files, and any typed-format
    /// file that failed to parse during load.
    /// </summary>
    public required IReadOnlyDictionary<string, byte[]> RawFiles { get; init; }

    /// <summary>
    /// Pre-parsed mobile objects keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose path ends with <c>.mob</c> (case-insensitive) appear here.
    /// </summary>
    public required IReadOnlyDictionary<string, MobData> Mobiles { get; init; }

    /// <summary>
    /// Pre-parsed sector data keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose path ends with <c>.sec</c> (case-insensitive) appear here.
    /// Each sector covers a 64×64 tile grid and includes tile art, lights, blocking masks, and placed objects.
    /// </summary>
    public required IReadOnlyDictionary<string, Sector> Sectors { get; init; }

    /// <summary>
    /// Pre-parsed jump-point files keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose path ends with <c>.jmp</c> (case-insensitive) appear here.
    /// Jump files define map-to-map transition points (e.g. <c>maps/map01/map01.jmp</c>).
    /// </summary>
    public required IReadOnlyDictionary<string, JmpFile> JumpFiles { get; init; }

    /// <summary>
    /// Pre-parsed map-properties files keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose path ends with <c>.prp</c> (case-insensitive) appear here.
    /// Map properties store the base terrain art ID and tile-grid dimensions.
    /// </summary>
    public required IReadOnlyDictionary<string, MapProperties> MapPropertiesList { get; init; }

    /// <summary>
    /// Pre-parsed message files keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose path ends with <c>.mes</c> (case-insensitive) appear here.
    /// Save slots usually contain module-level message overrides under
    /// <c>modules/&lt;module&gt;/&lt;module&gt;.mes</c>.
    /// </summary>
    public required IReadOnlyDictionary<string, MesFile> Messages { get; init; }

    /// <summary>
    /// Pre-parsed town-map fog files keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose path ends with <c>.tmf</c> (case-insensitive) appear here.
    /// Each file is a raw bit-array where one bit represents one revealed town-map tile.
    /// </summary>
    public required IReadOnlyDictionary<string, TownMapFog> TownMapFogs { get; init; }

    /// <summary>
    /// Parsed <c>data.sav</c> files keyed by the same virtual path as <see cref="Files"/>.
    /// The typed surface is structural rather than semantic: it exposes the verified 8-byte
    /// header and aligned INT32[4] row framing while preserving the raw bytes verbatim.
    /// </summary>
    public required IReadOnlyDictionary<string, DataSavFile> DataSavFiles { get; init; }

    /// <summary>
    /// Parsed <c>data2.sav</c> files keyed by the same virtual path as <see cref="Files"/>.
    /// The typed surface currently exposes the verified <c>50000+</c> ID pair table while
    /// preserving the unresolved prefix/suffix bytes for safe round-trip edits.
    /// </summary>
    public required IReadOnlyDictionary<string, Data2SavFile> Data2SavFiles { get; init; }

    /// <summary>
    /// Pre-parsed compiled script files keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose path ends with <c>.scr</c> (case-insensitive) appear here.
    /// Scripts define condition/action trees that drive NPC and environment behaviour.
    /// </summary>
    public required IReadOnlyDictionary<string, ScrFile> Scripts { get; init; }

    /// <summary>
    /// Pre-parsed dialogue files keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose path ends with <c>.dlg</c> (case-insensitive) appear here.
    /// Dialogue files define NPC conversation trees with condition/action expressions.
    /// </summary>
    public required IReadOnlyDictionary<string, DlgFile> Dialogs { get; init; }

    /// <summary>
    /// Pre-parsed <c>mobile.md</c> files keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose file name is <c>mobile.md</c> (case-insensitive) appear here.
    /// Each entry lists static world objects whose state was modified at run-time
    /// (containers looted, portals unlocked, scenery altered, etc.).
    /// </summary>
    public required IReadOnlyDictionary<string, MobileMdFile> MobileMds { get; init; }

    /// <summary>
    /// Pre-parsed <c>mobile.mdy</c> files keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose file name is <c>mobile.mdy</c> (case-insensitive) appear here.
    /// Each entry lists dynamically spawned mobile objects (NPCs, monsters, dropped items)
    /// present in the map at save time — including the player character on their current map.
    /// </summary>
    public required IReadOnlyDictionary<string, MobileMdyFile> MobileMdys { get; init; }

    /// <summary>
    /// Per-file parse errors collected during load, keyed by virtual path.
    /// A file that appears here failed to parse into its typed representation;
    /// its raw bytes remain accessible via <see cref="Files"/> for round-trip fidelity.
    /// Empty when all files loaded successfully.
    /// </summary>
    public required IReadOnlyDictionary<string, string> ParseErrors { get; init; }
}
