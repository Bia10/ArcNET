using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// In-memory representation of a fully loaded Arcanum save slot.
/// A save slot consists of three files: a <c>.gsi</c> info file, a <c>.tfai</c> index,
/// and a <c>.tfaf</c> data blob. All embedded files are extracted and stored here;
/// mobile (<c>.mob</c>) files are additionally pre-parsed into <see cref="Mobiles"/>.
/// </summary>
public sealed class SaveGame
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
    /// Pre-parsed mobile objects keyed by the same virtual path as <see cref="Files"/>.
    /// Only entries whose path ends with <c>.mob</c> (case-insensitive) appear here.
    /// </summary>
    public required IReadOnlyDictionary<string, MobData> Mobiles { get; init; }
}
