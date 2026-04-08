using System.Buffers;
using System.Buffers.Binary;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// The complete contents of a <c>mobile.mdy</c> save-game file.
/// <para>
/// <c>mobile.mdy</c> stores dynamically spawned (non-static) mobile objects for a map —
/// NPCs, monsters, items dropped at runtime, etc.  Most entries are standard game objects
/// identical in binary layout to a standalone <c>.mob</c> file; the player-character record
/// on their current map is stored in the v2 format (<see cref="CharacterMdyRecord"/>).
/// </para>
/// </summary>
public sealed class MobileMdyFile
{
    /// <summary>All records in the order they appear on disk.</summary>
    public required IReadOnlyList<MobileMdyRecord> Records { get; init; }

    /// <summary>Convenience view: all standard mob records, in original order.</summary>
    public IEnumerable<MobData> Mobs => Records.Where(r => r.IsMob).Select(r => r.Mob!);

    /// <summary>Convenience view: all v2 character records, in original order.</summary>
    public IEnumerable<CharacterMdyRecord> Characters => Records.Where(r => r.IsCharacter).Select(r => r.Character!);
}

/// <summary>
/// Parser and writer for Arcanum's <c>mobile.mdy</c> save-game format.
/// <para>
/// Binary layout: a back-to-back sequence of records. Standard records are identical to
/// the binary body of a <c>.mob</c> file (version dword followed by
/// <see cref="GameObjects.GameObjectHeader"/> and property bytes).
/// A v2 PC character record starts with a 12-byte magic header and is followed by
/// up to four SAR (Sparse Array Record) packets; see <see cref="CharacterMdyRecord"/>.
/// Some saves prefix or separate records with a 4-byte sentinel dword
/// <c>0xFFFFFFFF</c>; these are silently skipped when reading.
/// </para>
/// </summary>
public sealed class MobileMdyFormat : IFormatFileReader<MobileMdyFile>, IFormatFileWriter<MobileMdyFile>
{
    private const uint Sentinel = 0xFFFFFFFF;

    /// <inheritdoc/>
    public static MobileMdyFile Parse(scoped ref SpanReader reader)
    {
        var records = new List<MobileMdyRecord>();

        while (reader.Remaining >= 4)
        {
            // Skip inter-record sentinel dwords.
            if (unchecked((uint)reader.PeekInt32At(0)) == Sentinel)
            {
                reader.Skip(4);
                continue;
            }

            // Detect v2 character record by its 12-byte magic.
            if (
                reader.Remaining >= CharacterMdyRecord.V2Magic.Length
                && reader.PeekSpan(CharacterMdyRecord.V2Magic.Length).SequenceEqual(CharacterMdyRecord.V2Magic)
            )
            {
                var remaining = reader.RemainingSpan;
                try
                {
                    var character = CharacterMdyRecord.Parse(remaining, out var consumed);
                    reader.Skip(consumed);
                    records.Add(MobileMdyRecord.FromCharacter(character));
                }
                catch (Exception ex)
                {
                    // False-positive magic hit (the pattern appeared inside mob data).
                    // Advance one byte and resync.
                    System.Diagnostics.Debug.WriteLine(
                        $"[MobileMdyFormat] V2 magic false-positive at offset {reader.Position}: {ex.Message}"
                    );
                    int skip = FindResyncOffset(reader.RemainingSpan, skipFirst: 1);
                    reader.Skip(skip);
                }
                continue;
            }

            // Standard mob record: version must be 0x08 or 0x77.
            var nextVersion = reader.PeekInt32At(0);
            if (nextVersion != 0x08 && nextVersion != 0x77)
            {
                // Unknown dword — not a sentinel, v2 magic, or standard record version.
                // Advance 1 byte and keep scanning so we don't miss records that follow.
                reader.Skip(1);
                continue;
            }

            try
            {
                var mob = MobFormat.Parse(ref reader);
                records.Add(MobileMdyRecord.FromMob(mob));
            }
            catch (Exception ex)
            {
                // Mob parse failed mid-record. Scan forward (past current position)
                // for the next valid record start: sentinel, v2 magic, or version word.
                System.Diagnostics.Debug.WriteLine(
                    $"[MobileMdyFormat] Mob parse failed at offset {reader.Position}: {ex.Message}"
                );
                int skip = FindResyncOffset(reader.RemainingSpan, skipFirst: 1);
                reader.Skip(skip);
            }
        }

        return new MobileMdyFile { Records = records };
    }

    // ── Resync helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Scans <paramref name="data"/> for the first position ≥ <paramref name="skipFirst"/>
    /// that looks like a valid record boundary: a 0xFFFFFFFF sentinel, the v2 magic,
    /// or a version dword of 0x00000008 or 0x00000077.
    /// Returns <paramref name="data"/>.Length (skip to end) if nothing is found.
    /// </summary>
    private static int FindResyncOffset(ReadOnlySpan<byte> data, int skipFirst = 0)
    {
        for (var i = skipFirst; i + 4 <= data.Length; i++)
        {
            var dword = BinaryPrimitives.ReadUInt32LittleEndian(data[i..]);
            if (dword == Sentinel)
                return i;
            if (
                i + CharacterMdyRecord.V2Magic.Length <= data.Length
                && data.Slice(i, CharacterMdyRecord.V2Magic.Length).SequenceEqual(CharacterMdyRecord.V2Magic)
            )
                return i;
            if (dword is 0x00000008 or 0x00000077)
                return i;
        }
        return data.Length;
    }

    /// <inheritdoc/>
    public static MobileMdyFile ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static MobileMdyFile ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in MobileMdyFile value, ref SpanWriter writer)
    {
        foreach (var record in value.Records)
        {
            if (record.IsCharacter)
                // V2 records are written back verbatim — element counts are fixed and
                // With* methods patch in-place, so RawBytes is always byte-identical
                // to the original or a correctly patched version.
                writer.WriteBytes(record.Character.RawBytes);
            else
            {
                var mob = record.Mob!;
                MobFormat.Write(in mob, ref writer);
            }
        }
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in MobileMdyFile value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in MobileMdyFile value, string path) =>
        File.WriteAllBytes(path, WriteToArray(in value));
}
