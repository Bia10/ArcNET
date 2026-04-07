using System.Buffers;
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
public sealed class MobileMdyFormat : IFormatReader<MobileMdyFile>, IFormatWriter<MobileMdyFile>
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
                var character = CharacterMdyRecord.Parse(remaining, out var consumed);
                reader.Skip(consumed);
                records.Add(MobileMdyRecord.FromCharacter(character));
                continue;
            }

            // Standard mob record: version must be 0x08 or 0x77.
            var nextVersion = reader.PeekInt32At(0);
            if (nextVersion != 0x08 && nextVersion != 0x77)
                break;

            var mob = MobFormat.Parse(ref reader);
            records.Add(MobileMdyRecord.FromMob(mob));
        }

        return new MobileMdyFile { Records = records };
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
