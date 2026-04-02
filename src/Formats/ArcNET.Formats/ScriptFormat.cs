using System.Buffers;
using System.Text;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// Behaviour flags on a compiled script file, matching the <c>SF_*</c> constants
/// from <c>arcanum-ce/src/game/script.h</c>.
/// </summary>
[Flags]
public enum ScriptFlags : uint
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>Script defines a non-magical trap.</summary>
    NonmagicalTrap = 0x0001,

    /// <summary>Script defines a magical trap.</summary>
    MagicalTrap = 0x0002,

    /// <summary>Trap is auto-removed after triggering.</summary>
    AutoRemoving = 0x0004,

    /// <summary>NPC has a death speech.</summary>
    DeathSpeech = 0x0008,

    /// <summary>NPC has a surrender speech.</summary>
    SurrenderSpeech = 0x0010,

    /// <summary>Trigger radius is 2 tiles.</summary>
    RadiusTwo = 0x0020,

    /// <summary>Trigger radius is 3 tiles.</summary>
    RadiusThree = 0x0040,

    /// <summary>Trigger radius is 5 tiles.</summary>
    RadiusFive = 0x0080,

    /// <summary>Script is a teleport trigger.</summary>
    TeleportTrigger = 0x0100,
}

/// <summary>
/// A single script action (the "then" or "else" branch of a condition).
/// Corresponds to <c>ScriptAction</c> (44 bytes / 0x2C) in <c>arcanum-ce/src/game/script.h</c>.
/// </summary>
public readonly record struct ScriptActionData(
    int Type,
    byte[] OpTypes, // [8] operand type tags
    int[] OpValues // [8] operand values
);

/// <summary>
/// A condition/action node in a compiled script.
/// Corresponds to <c>ScriptCondition</c> (132 bytes / 0x84) in <c>arcanum-ce/src/game/script.h</c>.
/// </summary>
public readonly record struct ScriptConditionData(
    int Type,
    byte[] OpTypes, // [8]
    int[] OpValues, // [8]
    ScriptActionData Action, // "then" branch
    ScriptActionData Else // "else" branch
);

/// <summary>
/// Parsed contents of an Arcanum compiled script (.scr) file.
/// Source: <c>arcanum-ce/src/game/script.c</c> — <c>script_file_load_hdr</c> and
/// <c>script_file_load_code</c>.
/// </summary>
public sealed class ScrFile
{
    /// <summary>Flags from the 8-byte <c>ScriptHeader</c> struct written at offset 0.</summary>
    public required uint HeaderFlags { get; init; }

    /// <summary>Counter bitmask from the 8-byte <c>ScriptHeader</c> struct at offset 4.</summary>
    public required uint HeaderCounters { get; init; }

    /// <summary>
    /// Human-readable script description; up to 40 ASCII chars, zero-padded on disk.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>Script behaviour flags (<c>SF_*</c> bitmask).</summary>
    public required ScriptFlags Flags { get; init; }

    /// <summary>Ordered list of condition/action tree entries.</summary>
    public required IReadOnlyList<ScriptConditionData> Entries { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum compiled script (.scr) files.
/// Layout: 8-byte <c>ScriptHeader</c> then 52 bytes of metadata then
/// <c>n × 132-byte ScriptCondition</c> entries.
/// </summary>
public sealed class ScriptFormat : IFormatReader<ScrFile>, IFormatWriter<ScrFile>
{
    private const int DescriptionLength = 40;

    /// <inheritdoc/>
    public static ScrFile Parse(scoped ref SpanReader reader)
    {
        // ScriptHeader — 8 bytes
        var hdrFlags = reader.ReadUInt32();
        var hdrCounters = reader.ReadUInt32();

        // Body metadata
        var descBytes = reader.ReadBytes(DescriptionLength).ToArray();
        var description = Encoding.ASCII.GetString(descBytes).TrimEnd('\0');
        var flags = (ScriptFlags)reader.ReadUInt32();
        var numEntries = reader.ReadInt32();
        reader.ReadInt32(); // max_entries — discard on read
        reader.ReadInt32(); // x86 pointer slot — always 0

        var entries = new ScriptConditionData[numEntries];
        for (var i = 0; i < numEntries; i++)
            entries[i] = ReadCondition(ref reader);

        return new ScrFile
        {
            HeaderFlags = hdrFlags,
            HeaderCounters = hdrCounters,
            Description = description,
            Flags = flags,
            Entries = entries,
        };
    }

    private static ScriptActionData ReadAction(scoped ref SpanReader reader)
    {
        var type = reader.ReadInt32();
        var opTypes = reader.ReadBytes(8).ToArray();
        var opVals = new int[8];
        for (var i = 0; i < 8; i++)
            opVals[i] = reader.ReadInt32();

        return new ScriptActionData(type, opTypes, opVals);
    }

    private static ScriptConditionData ReadCondition(scoped ref SpanReader reader)
    {
        var type = reader.ReadInt32();
        var opTypes = reader.ReadBytes(8).ToArray();
        var opVals = new int[8];
        for (var i = 0; i < 8; i++)
            opVals[i] = reader.ReadInt32();

        var action = ReadAction(ref reader);
        var els = ReadAction(ref reader);

        return new ScriptConditionData(type, opTypes, opVals, action, els);
    }

    /// <inheritdoc/>
    public static ScrFile ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static ScrFile ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in ScrFile value, ref SpanWriter writer)
    {
        // ScriptHeader — 8 bytes
        writer.WriteUInt32(value.HeaderFlags);
        writer.WriteUInt32(value.HeaderCounters);

        // Description — 40 bytes, zero-padded
        var descBuf = new byte[DescriptionLength];
        Encoding.ASCII.GetBytes(
            value.Description.AsSpan(0, Math.Min(value.Description.Length, DescriptionLength)),
            descBuf
        );
        writer.WriteBytes(descBuf);

        writer.WriteUInt32((uint)value.Flags);
        writer.WriteInt32(value.Entries.Count); // num_entries
        writer.WriteInt32(value.Entries.Count); // max_entries == num_entries on round-trip
        writer.WriteInt32(0); // x86 pointer slot

        foreach (var cond in value.Entries)
            WriteCondition(cond, ref writer);
    }

    private static void WriteAction(ScriptActionData a, ref SpanWriter writer)
    {
        writer.WriteInt32(a.Type);
        writer.WriteBytes(a.OpTypes);
        foreach (var v in a.OpValues)
            writer.WriteInt32(v);
    }

    private static void WriteCondition(ScriptConditionData c, ref SpanWriter writer)
    {
        writer.WriteInt32(c.Type);
        writer.WriteBytes(c.OpTypes);
        foreach (var v in c.OpValues)
            writer.WriteInt32(v);

        WriteAction(c.Action, ref writer);
        WriteAction(c.Else, ref writer);
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in ScrFile value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in ScrFile value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
