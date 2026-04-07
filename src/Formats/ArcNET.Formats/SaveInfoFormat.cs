using System.Buffers;
using System.Text;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// Metadata stored in an Arcanum save-game info (.gsi) file — one per save slot.
/// </summary>
public sealed class SaveInfo
{
    /// <summary>
    /// GSI format version read from the file. Preserved during round-trips.
    /// Vanilla Arcanum uses 0; UAP/patched installations use 25.
    /// Defaults to 0 for newly constructed instances.
    /// </summary>
    public int Version { get; init; } = 0;

    /// <summary>Name of the module (campaign) associated with this save.</summary>
    public required string ModuleName { get; init; }

    /// <summary>Name of the party leader character.</summary>
    public required string LeaderName { get; init; }

    /// <summary>Human-readable save slot display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Map identifier where the game was saved.</summary>
    public required int MapId { get; init; }

    /// <summary>In-game elapsed days at save time.</summary>
    public required int GameTimeDays { get; init; }

    /// <summary>In-game elapsed milliseconds within the current day at save time.</summary>
    public required int GameTimeMs { get; init; }

    /// <summary>Art resource identifier for the leader's portrait.</summary>
    public required int LeaderPortraitId { get; init; }

    /// <summary>Current level of the party leader.</summary>
    public required int LeaderLevel { get; init; }

    /// <summary>Leader's tile X coordinate (unpacked from <c>LeaderLoc</c>).</summary>
    public required int LeaderTileX { get; init; }

    /// <summary>Leader's tile Y coordinate (unpacked from <c>LeaderLoc</c>).</summary>
    public required int LeaderTileY { get; init; }

    /// <summary>Story progress state (unused in most saves; always 0).</summary>
    public required int StoryState { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum save-game info (.gsi) files.
/// All strings are length-prefixed with a little-endian <see cref="int"/> followed by ASCII bytes.
/// Binary layout (exact field order):
/// <c>int32 version(0) | PrefixedString ModuleName | PrefixedString LeaderName |
/// int32 MapId | int32 GameTimeDays | int32 GameTimeMs | int32 LeaderPortraitId |
/// int32 LeaderLevel | int64 LeaderLoc | int32 StoryState | PrefixedString DisplayName</c>.
/// </summary>
public sealed class SaveInfoFormat : IFormatReader<SaveInfo>, IFormatWriter<SaveInfo>
{
    private const int MinSupportedVersion = 0;
    private const int MaxSupportedVersion = 25;

    /// <inheritdoc/>
    public static SaveInfo Parse(scoped ref SpanReader reader)
    {
        var version = reader.ReadInt32();
        if (version is < MinSupportedVersion or > MaxSupportedVersion)
            throw new InvalidDataException(
                $"Unsupported GSI version {version}; expected {MinSupportedVersion}–{MaxSupportedVersion}."
            );

        var moduleName = ReadPrefixed(ref reader);
        var leaderName = ReadPrefixed(ref reader);
        var mapId = reader.ReadInt32();
        var timeDays = reader.ReadInt32();
        var timeMs = reader.ReadInt32();
        var portraitId = reader.ReadInt32();
        var level = reader.ReadInt32();
        var loc = reader.ReadInt64();
        var storyState = reader.ReadInt32();
        var displayName = ReadPrefixed(ref reader);

        return new SaveInfo
        {
            Version = version,
            ModuleName = moduleName,
            LeaderName = leaderName,
            DisplayName = displayName,
            MapId = mapId,
            GameTimeDays = timeDays,
            GameTimeMs = timeMs,
            LeaderPortraitId = portraitId,
            LeaderLevel = level,
            LeaderTileX = (int)(loc & 0xFFFFFFFF),
            LeaderTileY = (int)((loc >> 32) & 0xFFFFFFFF),
            StoryState = storyState,
        };
    }

    private static string ReadPrefixed(ref SpanReader reader)
    {
        var length = reader.ReadInt32();
        if (length <= 0)
            return string.Empty;

        return Encoding.ASCII.GetString(reader.ReadBytes(length));
    }

    /// <inheritdoc/>
    public static SaveInfo ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static SaveInfo ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in SaveInfo value, ref SpanWriter writer)
    {
        writer.WriteInt32(value.Version);

        WritePrefixed(value.ModuleName, ref writer);
        WritePrefixed(value.LeaderName, ref writer);
        writer.WriteInt32(value.MapId);
        writer.WriteInt32(value.GameTimeDays);
        writer.WriteInt32(value.GameTimeMs);
        writer.WriteInt32(value.LeaderPortraitId);
        writer.WriteInt32(value.LeaderLevel);

        var loc = (long)(uint)value.LeaderTileX | ((long)(uint)value.LeaderTileY << 32);
        writer.WriteInt64(loc);

        writer.WriteInt32(value.StoryState);
        WritePrefixed(value.DisplayName, ref writer);
    }

    private static void WritePrefixed(string value, ref SpanWriter writer)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        writer.WriteInt32(bytes.Length);
        writer.WriteBytes(bytes);
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in SaveInfo value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in SaveInfo value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
