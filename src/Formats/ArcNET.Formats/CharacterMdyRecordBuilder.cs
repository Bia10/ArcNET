using System.Buffers.Binary;
using System.Text;

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

        // Primary SAR arrays — bsId=0 is fine; the parser finds these by elemSz+elemCnt signature.
        var statsSar = BuildInt32Sar(0, stats);
        var basicSar = BuildInt32Sar(0, basicSkills);
        var techSar = BuildInt32Sar(0, techSkills);
        var spellSar = BuildInt32Sar(0, spellTech);

        // Gold SAR — bsId=0x4B13 required by the extended scan in CharacterMdyRecord.Parse.
        var goldSar = BuildInt32Sar(GoldBsId, [gold]);

        // Portrait SAR — bsId=0x4DA4 required; layout: [MaxFollowers, PortraitIndex, 0].
        var portraitSar = BuildInt32Sar(PortraitBsId, [maxFollowers, portraitIndex, 0]);

        // PC name field — non-SAR encoding: 0x01 + uint32_len + ascii_chars.
        var nameField = BuildNameField(name);

        // Concatenate all segments.
        var totalLen =
            magic.Length
            + statsSar.Length
            + basicSar.Length
            + techSar.Length
            + spellSar.Length
            + goldSar.Length
            + portraitSar.Length
            + nameField.Length;
        var buf = new byte[totalLen];
        var off = 0;
        off += Write(buf, off, magic);
        off += Write(buf, off, statsSar);
        off += Write(buf, off, basicSar);
        off += Write(buf, off, techSar);
        off += Write(buf, off, spellSar);
        off += Write(buf, off, goldSar);
        off += Write(buf, off, portraitSar);
        Write(buf, off, nameField);
        return buf;
    }

    /// <summary>Builds a SAR packet for an array of int32 values with the given bsId.</summary>
    private static byte[] BuildInt32Sar(int bsId, ReadOnlySpan<int> values)
    {
        var elemData = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(elemData.AsSpan(i * 4, 4), values[i]);
        return SarEncoding.BuildSarBytes(4, values.Length, bsId, elemData);
    }

    /// <summary>Builds the non-SAR PC name field: <c>0x01 + uint32_len + ascii_chars</c>.</summary>
    private static byte[] BuildNameField(string name)
    {
        var ascii = Encoding.ASCII.GetBytes(name);
        var field = new byte[1 + 4 + ascii.Length];
        field[0] = 0x01;
        BinaryPrimitives.WriteInt32LittleEndian(field.AsSpan(1, 4), ascii.Length);
        ascii.CopyTo(field, 5);
        return field;
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

    private static int Write(byte[] buf, int offset, ReadOnlySpan<byte> src)
    {
        src.CopyTo(buf.AsSpan(offset));
        return src.Length;
    }
}
