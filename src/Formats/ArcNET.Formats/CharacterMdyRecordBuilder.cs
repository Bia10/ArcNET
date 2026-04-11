using System.Buffers.Binary;
using Bia.ValueBuffers;

namespace ArcNET.Formats;

/// <summary>
/// Factory for constructing <see cref="CharacterMdyRecord"/> instances from scratch,
/// without needing to start from an existing save file.
/// <para>
/// Use this class when building a new save (see <see cref="SaveGameBuilder"/>) or when
/// importing a character from an external source.  The resulting record is byte-identical
/// in structure to one produced by the game engine for a freshly-created character.
/// </para>
/// </summary>
/// <remarks>
/// The produced record contains the four mandatory SAR arrays (stats, basic skills,
/// tech skills, spell/tech disciplines) followed by an optional gold SAR, portrait SAR,
/// and PC name field.  Pre-stat SARs (position/AI, HP, fatigue) are omitted; the game
/// engine initialises those the first time the character enters a map.
/// </remarks>
public static class CharacterMdyRecordBuilder
{
    // bsId constants — must match the values expected by CharacterMdyRecord.Parse()
    // and the Arcanum engine for the extended-scan recognised SARs.
    private const int GoldBsId = 0x4B13;
    private const int PortraitBsId = 0x4DA4;

    /// <summary>
    /// Validates argument lengths for <see cref="Create"/>.
    /// </summary>
    private static void ValidateArrays(int[] stats, int[] basicSkills, int[] techSkills, int[] spellTech)
    {
        if (stats.Length != 28)
            throw new ArgumentException("Must have exactly 28 elements.", nameof(stats));
        if (basicSkills.Length != 12)
            throw new ArgumentException("Must have exactly 12 elements.", nameof(basicSkills));
        if (techSkills.Length != 4)
            throw new ArgumentException("Must have exactly 4 elements.", nameof(techSkills));
        if (spellTech.Length != 25)
            throw new ArgumentException("Must have exactly 25 elements.", nameof(spellTech));
    }

    /// <summary>
    /// Creates a new <see cref="CharacterMdyRecord"/> from the supplied character data.
    /// </summary>
    /// <param name="stats">28-element stats array (strength … race), as stored in the save (base values).</param>
    /// <param name="basicSkills">12-element basic skills array (bow … persuasion).</param>
    /// <param name="techSkills">4-element technological skills array (repair … disarm traps).</param>
    /// <param name="spellTech">25-element spell/tech disciplines array (conveyance … therapeutics).</param>
    /// <param name="gold">Starting gold amount (default 0).</param>
    /// <param name="name">Character name; must contain only printable ASCII characters (default "Hero").</param>
    /// <param name="portraitIndex">Portrait art index shown in the character sheet (default 0).</param>
    /// <param name="maxFollowers">Computed max-followers value stored in the portrait SAR (default 5).</param>
    /// <returns>A fully-parsed <see cref="CharacterMdyRecord"/> whose <c>RawBytes</c> are ready-to-write.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when any array has the wrong length, <paramref name="name"/> contains
    /// non-printable ASCII, or <paramref name="name"/> is empty.
    /// </exception>
    public static CharacterMdyRecord Create(
        int[] stats,
        int[] basicSkills,
        int[] techSkills,
        int[] spellTech,
        int gold = 0,
        string name = "Hero",
        int portraitIndex = 0,
        int maxFollowers = 5
    )
    {
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentNullException.ThrowIfNull(basicSkills);
        ArgumentNullException.ThrowIfNull(techSkills);
        ArgumentNullException.ThrowIfNull(spellTech);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ValidateArrays(stats, basicSkills, techSkills, spellTech);
        ValidateName(name);

        var bytes = BuildRecordBytes(
            stats,
            basicSkills,
            techSkills,
            spellTech,
            gold,
            name,
            portraitIndex,
            maxFollowers
        );
        return CharacterMdyRecord.Parse(bytes, out _);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static byte[] BuildRecordBytes(
        int[] stats,
        int[] basicSkills,
        int[] techSkills,
        int[] spellTech,
        int gold,
        string name,
        int portraitIndex,
        int maxFollowers
    )
    {
        // V2 magic header — identifies this as a v2 PC/NPC record.
        ReadOnlySpan<byte> magic = CharacterMdyRecord.V2Magic;

        Span<byte> initial = stackalloc byte[512];
        var buf = new ValueByteBuffer(initial);

        buf.Write(magic);
        AppendInt32Sar(ref buf, 0, stats);
        AppendInt32Sar(ref buf, 0, basicSkills);
        AppendInt32Sar(ref buf, 0, techSkills);
        AppendInt32Sar(ref buf, 0, spellTech);
        AppendInt32Sar(ref buf, GoldBsId, [gold]);
        AppendInt32Sar(ref buf, PortraitBsId, [maxFollowers, portraitIndex, 0]);
        AppendNameField(ref buf, name);

        var result = buf.ToArray();
        buf.Dispose();
        return result;
    }

    /// <summary>Appends a SAR packet for an array of int32 values with the given bsId directly into <paramref name="buf"/>.</summary>
    private static void AppendInt32Sar(ref ValueByteBuffer buf, int bsId, ReadOnlySpan<int> values)
    {
        Span<byte> elemInitial = stackalloc byte[256];
        using var elemBuf = new ValueByteBuffer(elemInitial);
        elemBuf.WriteInt32LittleEndianAll(values);
        buf.Write(SarEncoding.BuildSarBytes(4, values.Length, bsId, elemBuf.WrittenSpan));
    }

    /// <summary>Appends the non-SAR PC name field: <c>0x01 + uint32_len + ascii_chars</c> directly into <paramref name="buf"/>.</summary>
    private static void AppendNameField(ref ValueByteBuffer buf, string name)
    {
        buf.Write(0x01);
        buf.WriteInt32LittleEndian(name.Length); // ASCII: 1 byte per char
        buf.WriteAsciiEncoded(name.AsSpan());
    }

    /// <summary>Validates that <paramref name="name"/> contains only printable ASCII (0x20–0x7E).</summary>
    private static void ValidateName(string name)
    {
        foreach (var c in name)
        {
            if (c < 0x20 || c > 0x7E)
                throw new ArgumentException(
                    $"Name must contain only printable ASCII characters (0x20–0x7E). Offending char: '{c}' (U+{(int)c:X4}).",
                    nameof(name)
                );
        }
    }
}
