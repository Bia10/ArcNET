using System.Buffers.Binary;

namespace ArcNET.Formats;

/// <summary>
/// Represents a v2 PC/NPC character record as it appears inside a <c>mobile.mdy</c> file.
/// <para>
/// A v2 record starts with a 12-byte magic header
/// <c>[02 00 00 00 0F 00 00 00 00 00 00 00]</c> and is followed by one to four
/// SAR (Sparse Array Record) packets that encode the character's stats, basic skills,
/// tech skills, and spell / tech discipline ranks.
/// </para>
/// <para>
/// <see cref="RawBytes"/> always contains the exact bytes as they appear on disk.
/// The four decoded arrays are provided for convenient reading; use the
/// <c>With*</c> methods to obtain a new record with patched bytes —
/// all other content is preserved verbatim.
/// </para>
/// </summary>
public sealed record CharacterMdyRecord
{
    // ── Wire signatures ───────────────────────────────────────────────────────

    /// <summary>12-byte magic that identifies a v2 character record.</summary>
    internal static ReadOnlySpan<byte> V2Magic =>
        [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    // presence(1B)=0x01 + elemSz(4B) + elemCnt(4B) — SAR array signatures.
    private static ReadOnlySpan<byte> StatSig => [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00]; // elemCnt=28
    private static ReadOnlySpan<byte> BasicSkillSig => [0x04, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00]; // elemCnt=12
    private static ReadOnlySpan<byte> TechSkillSig => [0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00]; // elemCnt=4
    private static ReadOnlySpan<byte> SpellTechSig => [0x04, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00, 0x00]; // elemCnt=25

    // presence(1B) + elemSz(4B) + elemCnt(4B) + bitsetId(4B)
    private const int SarHeaderSize = 13;
    private const int MaxScanDistance = 4096;

    // ── Public state ──────────────────────────────────────────────────────────

    /// <summary>
    /// The exact bytes of this record as read from disk, from the first byte of
    /// <see cref="V2Magic"/> through the end of the last SAR packet found.
    /// Written back verbatim when no changes are applied.
    /// </summary>
    public required byte[] RawBytes { get; init; }

    /// <summary>Stats array — 28 elements (strength … race).</summary>
    public required int[] Stats { get; init; }

    /// <summary>Basic skills array — 12 elements (bow … persuasion).</summary>
    public required int[] BasicSkills { get; init; }

    /// <summary>Tech skills array — 4 elements (repair … disarm traps).</summary>
    public required int[] TechSkills { get; init; }

    /// <summary>Spell / tech disciplines array — 25 elements (conveyance … therapeutics).</summary>
    public required int[] SpellTech { get; init; }

    /// <summary>
    /// <see langword="true"/> when all four SAR arrays were found in the record.
    /// PC records always have all four; NPCs may only have the stat array.
    /// </summary>
    public required bool HasCompleteData { get; init; }

    // ── Byte offsets for in-place patching ────────────────────────────────────
    // Offsets point to the first int data byte within RawBytes (-1 = absent).
    // Not `required` — only CharacterMdyRecord.Parse constructs instances.

    internal int StatsDataOffset { get; init; }
    internal int BasicSkillsDataOffset { get; init; }
    internal int TechSkillsDataOffset { get; init; }
    internal int SpellTechDataOffset { get; init; }

    // ── Parsing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a v2 character record starting at the beginning of <paramref name="span"/>.
    /// <paramref name="span"/> must start at the first byte of <see cref="V2Magic"/>.
    /// </summary>
    /// <param name="span">Bytes starting at the v2 magic.</param>
    /// <param name="consumed">Number of bytes consumed from <paramref name="span"/>.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when the mandatory stats SAR cannot be located within
    /// <see cref="MaxScanDistance"/> bytes of the magic header.
    /// </exception>
    public static CharacterMdyRecord Parse(ReadOnlySpan<byte> span, out int consumed)
    {
        // Mandatory: stats SAR (28 × int32), searched within the first 12 + MaxScanDistance bytes.
        var statOff = FindSar(span, 12, span.Length, StatSig);
        if (statOff < 0)
            throw new InvalidDataException("v2 character record: stats SAR not found within scan range");

        var statsDataOff = statOff + SarHeaderSize;
        if (statsDataOff + 28 * 4 > span.Length)
            throw new InvalidDataException("v2 character record: stats SAR data extends beyond available bytes");

        var stats = ReadInts(span, statsDataOff, 28);
        var end = SarEnd(span, statOff, 28);

        // Optional: basic skills, tech skills, spell/tech — each must follow the previous SAR.
        int[] basicSkills = new int[12];
        int[] techSkills = new int[4];
        int[] spellTech = new int[25];
        bool hasAll = false;

        int basicDataOff = -1,
            techDataOff = -1,
            spellDataOff = -1;

        var basicOff = FindSar(span, end, span.Length, BasicSkillSig);
        if (basicOff >= 0)
        {
            basicDataOff = basicOff + SarHeaderSize;
            basicSkills = ReadInts(span, basicDataOff, 12);
            end = SarEnd(span, basicOff, 12);

            var techOff = FindSar(span, end, span.Length, TechSkillSig);
            if (techOff >= 0)
            {
                techDataOff = techOff + SarHeaderSize;
                techSkills = ReadInts(span, techDataOff, 4);
                end = SarEnd(span, techOff, 4);

                var spellOff = FindSar(span, end, span.Length, SpellTechSig);
                if (spellOff >= 0)
                {
                    spellDataOff = spellOff + SarHeaderSize;
                    spellTech = ReadInts(span, spellDataOff, 25);
                    end = SarEnd(span, spellOff, 25);
                    hasAll = true;
                }
            }
        }

        consumed = end;
        return new CharacterMdyRecord
        {
            RawBytes = span[..consumed].ToArray(),
            Stats = stats,
            BasicSkills = basicSkills,
            TechSkills = techSkills,
            SpellTech = spellTech,
            HasCompleteData = hasAll,
            StatsDataOffset = statsDataOff,
            BasicSkillsDataOffset = basicDataOff,
            TechSkillsDataOffset = techDataOff,
            SpellTechDataOffset = spellDataOff,
        };
    }

    // ── Patch methods ─────────────────────────────────────────────────────────

    /// <summary>Returns a new record with the stats array replaced by <paramref name="stats"/>.</summary>
    public CharacterMdyRecord WithStats(int[] stats)
    {
        var raw = PatchInts(RawBytes, StatsDataOffset, stats);
        return this with { RawBytes = raw, Stats = stats };
    }

    /// <summary>Returns a new record with the basic skills array replaced by <paramref name="basicSkills"/>.</summary>
    public CharacterMdyRecord WithBasicSkills(int[] basicSkills)
    {
        if (BasicSkillsDataOffset < 0)
            return this;
        var raw = PatchInts(RawBytes, BasicSkillsDataOffset, basicSkills);
        return this with { RawBytes = raw, BasicSkills = basicSkills };
    }

    /// <summary>Returns a new record with the tech skills array replaced by <paramref name="techSkills"/>.</summary>
    public CharacterMdyRecord WithTechSkills(int[] techSkills)
    {
        if (TechSkillsDataOffset < 0)
            return this;
        var raw = PatchInts(RawBytes, TechSkillsDataOffset, techSkills);
        return this with { RawBytes = raw, TechSkills = techSkills };
    }

    /// <summary>Returns a new record with the spell / tech array replaced by <paramref name="spellTech"/>.</summary>
    public CharacterMdyRecord WithSpellTech(int[] spellTech)
    {
        if (SpellTechDataOffset < 0)
            return this;
        var raw = PatchInts(RawBytes, SpellTechDataOffset, spellTech);
        return this with { RawBytes = raw, SpellTech = spellTech };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Scans forward from <paramref name="from"/> looking for a byte with value 0x01
    /// (presence flag) immediately followed by <paramref name="sig"/>
    /// (elemSz + elemCnt as 8 LE bytes).
    /// </summary>
    private static int FindSar(ReadOnlySpan<byte> data, int from, int limit, ReadOnlySpan<byte> sig)
    {
        var end = Math.Min(limit, from + MaxScanDistance);
        for (var i = from; i + SarHeaderSize <= end; i++)
        {
            if (data[i] != 0x01)
                continue;
            if (i + 1 + sig.Length > data.Length)
                break;
            if (data.Slice(i + 1, sig.Length).SequenceEqual(sig))
                return i;
        }
        return -1;
    }

    /// <summary>Returns the byte offset immediately after the end of a SAR packet.</summary>
    private static int SarEnd(ReadOnlySpan<byte> data, int sarOff, int elemCount)
    {
        var bcOff = sarOff + SarHeaderSize + elemCount * 4;
        if (bcOff + 4 > data.Length)
            return bcOff;
        var bc = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(bcOff, 4));
        if (bc is < 0 or > 256)
            bc = 0;
        return bcOff + 4 + bc * 4;
    }

    private static int[] ReadInts(ReadOnlySpan<byte> data, int off, int count)
    {
        var arr = new int[count];
        for (var i = 0; i < count && off + i * 4 + 4 <= data.Length; i++)
            arr[i] = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(off + i * 4, 4));
        return arr;
    }

    private static byte[] PatchInts(byte[] source, int off, int[] values)
    {
        var raw = (byte[])source.Clone();
        for (var i = 0; i < values.Length && off + i * 4 + 4 <= raw.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(off + i * 4, 4), values[i]);
        return raw;
    }
}
