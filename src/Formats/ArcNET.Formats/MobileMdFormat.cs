using System.Buffers.Binary;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects;
using Bia.ValueBuffers;

namespace ArcNET.Formats;

/// <summary>
/// A single record read from a <c>mobile.md</c> file.
/// Each record identifies a map object that was modified at run-time and stores its
/// complete updated state as a standard <see cref="MobData"/> block.
/// </summary>
public sealed class MobileMdRecord
{
    /// <summary>
    /// The 24-byte <c>ObjectID</c> that identifies which map object received this diff.
    /// This is the same identifier used in the base <c>.mob</c> files and in
    /// <c>CritterInventoryListIdx</c> / <c>ContainerInventoryListIdx</c> handle arrays.
    /// </summary>
    public required GameObjectGuid MapObjectId { get; init; }

    /// <summary>
    /// Object file version (0x08 or 0x77). Stored separately so that records with
    /// <see langword="null"/> <see cref="Data"/> can still be round-tripped verbatim.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Raw bytes of the mob body — the exact bytes found between the START and END sentinels,
    /// <em>not</em> including the 4-byte version prefix.
    /// Always populated; used verbatim during write when <see cref="Data"/> is
    /// <see langword="null"/>.
    /// </summary>
    public required byte[] RawMobBytes { get; init; }

    /// <summary>
    /// Fully-decoded object state, or <see langword="null"/> when the mob body could not be
    /// parsed (e.g. unknown object type or unsupported wire format). When null,
    /// <see cref="RawMobBytes"/> is written back verbatim to preserve round-trip fidelity.
    /// </summary>
    public MobData? Data { get; init; }

    /// <summary>
    /// When <see cref="Data"/> is not <see langword="null"/>, these are the bytes that remain
    /// in the mob body <em>after</em> the primary bitmap properties are consumed.  They are
    /// appended verbatim after the serialized header+properties during write so that the full
    /// record length is preserved.  <see langword="null"/> means the primary parse consumed
    /// all rawMob bytes exactly.
    /// </summary>
    public byte[]? TailBytes { get; init; }

    /// <summary>
    /// <see langword="true"/> when <see cref="Data"/> was decoded using the compact single-OID
    /// format (no separate ObjectId field in the body).  Controls which serialization path is
    /// used during write.
    /// </summary>
    public bool IsCompact { get; init; }

    /// <summary>
    /// When <see cref="Data"/> is <see langword="null"/>, contains the exception message that
    /// describes why the mob body could not be decoded. <see langword="null"/> when decode succeeded.
    /// </summary>
    public string? ParseNote { get; init; }
}

/// <summary>
/// The complete contents of a <c>mobile.md</c> save-game file.
/// <para>
/// <c>mobile.md</c> stores per-map runtime overrides for static world objects
/// (containers opened, portals unlocked, scenery destroyed, etc.).
/// Each record is a modified-object diff keyed by the object's <c>ObjectID</c>.
/// </para>
/// </summary>
public sealed class MobileMdFile
{
    /// <summary>All modified-object records, in the order they appear on disk.</summary>
    public required IReadOnlyList<MobileMdRecord> Records { get; init; }
}

/// <summary>
/// Parser and writer for Arcanum's <c>mobile.md</c> save-game format.
/// <para>
/// Binary layout (one record per modified object, repeated until EOF):
/// <code>
/// [24 bytes]  ObjectID   — identifies the map object that received the diff
/// [4  bytes]  version    — 0x08 (original) or 0x77 (arcanum-ce)
/// [4  bytes]  START      — sentinel 0x12344321
/// [N  bytes]  mob data   — GameObjectHeader + properties (identical to .mob file body from version onwards)
/// [4  bytes]  END        — sentinel 0x23455432
/// </code>
/// Note: the version dword is the first field of <c>GameObjectHeader</c>; it is placed
/// before the START sentinel so care is taken when serialising and deserialising.
/// </para>
/// </summary>
public sealed class MobileMdFormat : IFormatFileReader<MobileMdFile>, IFormatFileWriter<MobileMdFile>
{
    private const int OidSize = 24;
    private const uint StartMarker = 0x12344321u;
    private const uint EndMarker = 0x23455432u;

    /// <inheritdoc/>
    public static MobileMdFile Parse(scoped ref SpanReader reader)
    {
        // Work on the raw span with a manual pos index so that MobFormat.Parse can
        // consume its inner SpanReader and we know exactly how many bytes were consumed.
        var span = reader.ReadBytes(reader.Remaining);
        return ParseSpan(span);
    }

    /// <inheritdoc/>
    public static MobileMdFile ParseMemory(ReadOnlyMemory<byte> memory) =>
        FormatIo.ParseMemory<MobileMdFormat, MobileMdFile>(memory);

    private static MobileMdFile ParseSpan(ReadOnlySpan<byte> span)
    {
        var records = new List<MobileMdRecord>();
        var pos = 0;

        while (pos + OidSize + 8 <= span.Length)
        {
            // 24-byte file-level ObjectID.
            var oidReader = new SpanReader(span.Slice(pos, OidSize));
            var mapObjectId = GameObjectGuid.Read(ref oidReader);
            pos += OidSize;

            // 4-byte version (first field of GameObjectHeader).
            var version = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pos, 4));
            pos += 4;
            if (version != 0x08 && version != 0x77)
                throw new InvalidDataException(
                    $"mobile.md record {records.Count + 1}: unexpected version 0x{version:X2} (expected 0x08 or 0x77)"
                );

            // 4-byte START sentinel.
            var startMark = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(pos, 4));
            pos += 4;
            if (startMark != StartMarker)
                throw new InvalidDataException(
                    $"mobile.md record {records.Count + 1}: expected START 0x{StartMarker:X8} but got 0x{startMark:X8}"
                );

            // Locate the END sentinel using lookahead validation to skip false positives.
            // Property data can coincidentally contain the END marker value, so we verify
            // that what follows the candidate END is either EOF or a new record with both
            // a valid version (0x08/0x77) and the START sentinel (0x12344321) at expected
            // offsets. The dual-marker check makes false acceptance astronomically unlikely.
            var endPos = FindEndSentinel(span, pos, records.Count + 1);

            // Capture the raw mob body bytes (between START and END, not including version).
            var rawMobBytes = span[pos..endPos].ToArray();
            pos = endPos + 4; // advance past END

            // Attempt typed decode using the exact rawMobBytes. Failure is non-fatal —
            // raw bytes preserve full round-trip fidelity for records we cannot decode.
            MobData? data = null;
            byte[]? tailBytes = null;
            bool isCompact = false;
            string? parseNote = null;

            // Compact format is used exclusively for Pc (objectType=15) and Npc (objectType=16)
            // records. In compact layout rawMobBytes[24..27] is the objectType uint32 as a
            // clean little-endian value: low byte = 15 or 16, high three bytes = 0.
            // All other object types (Wall=0, Portal=1, Container=2, … Trap=17) appear only in
            // standard format even though their OidType low byte would also be ≤17 and could
            // otherwise look like a compact candidate. Restricting to {15,16} avoids
            // misidentifying standard records as compact.
            var isCompactCandidate =
                rawMobBytes.Length >= 28
                && (rawMobBytes[24] == 15 || rawMobBytes[24] == 16)
                && rawMobBytes[25] == 0
                && rawMobBytes[26] == 0
                && rawMobBytes[27] == 0;

            try
            {
#pragma warning disable CA2014 // stackalloc is 256 B; ValueByteBuffer grows via ArrayPool if needed, not via stack
                Span<byte> combinedInitial = stackalloc byte[256];
#pragma warning restore CA2014
                using var combinedBuf = new ValueByteBuffer(combinedInitial);
                combinedBuf.WriteInt32LittleEndian(version);
                combinedBuf.Write(rawMobBytes);
                var innerReader = new SpanReader(combinedBuf.WrittenSpan);

                if (isCompactCandidate)
                {
                    // Compact layout: [protoId 24B][objectType 4B][propCollItems 2B][bitmap N B][props]
                    var compactHeader = GameObjectHeader.ReadCompact(ref innerReader, mapObjectId);
                    var compactProps = ObjectPropertyIo.ReadProperties(ref innerReader, compactHeader);
                    data = new MobData { Header = compactHeader, Properties = compactProps };
                    tailBytes =
                        innerReader.Remaining > 0 ? combinedBuf.WrittenSpan[innerReader.Position..].ToArray() : null;
                    isCompact = true;
                }
                else
                {
                    data = MobFormat.Parse(ref innerReader);
                    if (innerReader.Remaining > 0)
                        tailBytes = combinedBuf.WrittenSpan[innerReader.Position..].ToArray();
                }
            }
            catch (Exception ex)
            {
                parseNote = $"{ex.GetType().Name}: {ex.Message}";
            }

            // Fallback: only attempt a second format for Pc/Npc compact candidates.
            // Non-compact-candidate records that fail standard parse remain data=null so
            // their raw bytes are written back verbatim — this guarantees round-trip fidelity
            // for all game objects we do not need to decode and modify.
            if (data is null && isCompactCandidate)
            {
                try
                {
                    // Standard fallback: compact candidate that compact-parse couldn't handle.
#pragma warning disable CA2014 // stackalloc is 256 B; ValueByteBuffer grows via ArrayPool if needed, not via stack
                    Span<byte> combinedFallbackInitial = stackalloc byte[256];
#pragma warning restore CA2014
                    using var combinedFallback = new ValueByteBuffer(combinedFallbackInitial);
                    combinedFallback.WriteInt32LittleEndian(version);
                    combinedFallback.Write(rawMobBytes);
                    var innerReader = new SpanReader(combinedFallback.WrittenSpan);
                    data = MobFormat.Parse(ref innerReader);
                    if (innerReader.Remaining > 0)
                        tailBytes = combinedFallback.WrittenSpan[innerReader.Position..].ToArray();
                    isCompact = false;
                    parseNote = null;
                }
                catch (Exception ex)
                {
                    data = null;
                    tailBytes = null;
                    isCompact = false;
                    parseNote =
                        (parseNote is null ? "" : parseNote + " | fallback: ") + $"{ex.GetType().Name}: {ex.Message}";
                }
            }

            records.Add(
                new MobileMdRecord
                {
                    MapObjectId = mapObjectId,
                    Version = version,
                    RawMobBytes = rawMobBytes,
                    Data = data,
                    TailBytes = tailBytes,
                    IsCompact = isCompact,
                    ParseNote = parseNote,
                }
            );
        }

        return new MobileMdFile { Records = records };
    }

    /// <summary>
    /// Locates the END sentinel (0x23455432) scanning byte-by-byte forward from
    /// <paramref name="searchFrom"/>. Uses lookahead to skip false positives: property
    /// data can coincidentally contain the sentinel value, so we verify that what follows
    /// a candidate position is either EOF or a new record with version 0x08 or 0x77.
    /// </summary>
    private static int FindEndSentinel(ReadOnlySpan<byte> span, int searchFrom, int recordIndex)
    {
        for (var i = searchFrom; i + 4 <= span.Length; i++)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(i, 4)) != EndMarker)
                continue;

            var afterEnd = i + 4;

            // Accept immediately if EOF or insufficient bytes remain for a full next-record
            // preamble (OID 24 B + version 4 B + START 4 B = 32 B).
            if (afterEnd >= span.Length || afterEnd + OidSize + 8 > span.Length)
                return i;

            // Strong two-sentinel validation: require both a valid version field and the
            // START marker at the precise offsets expected for the next record. The dual
            // check makes false acceptance astronomically unlikely even when property data
            // coincidentally contains the END sentinel value.
            var nextVersion = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(afterEnd + OidSize, 4));
            var nextStart = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(afterEnd + OidSize + 4, 4));
            if ((nextVersion == 0x08 || nextVersion == 0x77) && nextStart == StartMarker)
                return i;

            // False positive — END sentinel value appeared inside property data.
        }

        throw new InvalidDataException($"mobile.md record {recordIndex}: END sentinel 0x{EndMarker:X8} not found");
    }

    /// <inheritdoc/>
    public static MobileMdFile ParseFile(string path) => FormatIo.ParseFile<MobileMdFormat, MobileMdFile>(path);

    /// <inheritdoc/>
    public static void Write(in MobileMdFile value, ref SpanWriter writer)
    {
        foreach (var record in value.Records)
        {
            // 24-byte ObjectID.
            record.MapObjectId.Write(ref writer);

            // Use serialised decoded data ONLY for compact (Pc/Npc) records.  Standard-format
            // records always write their raw bytes verbatim because WriteToArray cannot
            // guarantee byte-identical output for every standard mobile.md record (some compact
            // records accidentally pass standard parse, and their standard re-serialisation
            // produces different bytes at the objectType position).  Compact records we
            // explicitly decoded (IsCompact=true) are safe to re-serialise via WriteCompactToArray.
            if (record.Data is { } data && record.IsCompact)
            {
                var primaryBytes = MobFormat.WriteCompactToArray(in data);

                // primary[0..3] = version; primary[4..] = header + props.
                writer.WriteBytes(primaryBytes.AsSpan(0, 4)); // version
                writer.WriteUInt32(StartMarker);
                writer.WriteBytes(primaryBytes.AsSpan(4)); // header + properties
                // Append any bytes that follow the primary property block verbatim.
                if (record.TailBytes is { } tail)
                    writer.WriteBytes(tail);
                writer.WriteUInt32(EndMarker);
            }
            else
            {
                // Round-trip verbatim — covers: decode failed (Data=null), standard-decoded
                // records we do not intend to re-serialise, and compact records that fell to
                // the standard-fallback path.
                writer.WriteInt32(record.Version);
                writer.WriteUInt32(StartMarker);
                writer.WriteBytes(record.RawMobBytes);
                writer.WriteUInt32(EndMarker);
            }
        }
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in MobileMdFile value) =>
        FormatIo.WriteToArray<MobileMdFormat, MobileMdFile>(in value);

    /// <inheritdoc/>
    public static void WriteToFile(in MobileMdFile value, string path) =>
        FormatIo.WriteToFile<MobileMdFormat, MobileMdFile>(in value, path);
}
