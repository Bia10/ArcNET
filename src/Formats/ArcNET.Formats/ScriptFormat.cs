using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Eight operand-type bytes stored inline — zero heap allocation per action.</summary>
[InlineArray(8)]
public struct OpTypeBuffer
{
    private byte _element;
}

/// <summary>Eight operand-value ints stored inline — zero heap allocation per action.</summary>
[InlineArray(8)]
public struct OpValueBuffer
{
    private int _element;
}

/// <summary>
/// A single script action (the "then" or "else" branch of a condition).
/// Wire size: 44 bytes (0x2C).
/// </summary>
public readonly record struct ScriptActionData(
    int Type,
    OpTypeBuffer OpTypes, // [8] operand type tags — inline, no heap alloc
    OpValueBuffer OpValues // [8] operand values — inline, no heap alloc
)
{
    /// <summary>
    /// The action opcode as a typed enum.
    /// Values not present in <see cref="ScriptActionType"/> are returned as their raw cast.
    /// </summary>
    public ScriptActionType ActionType => (ScriptActionType)Type;
}

/// <summary>
/// A condition/action node in a compiled script.
/// Wire size: 132 bytes (0x84).
/// </summary>
public readonly record struct ScriptConditionData(
    int Type,
    OpTypeBuffer OpTypes, // [8] — inline, no heap alloc
    OpValueBuffer OpValues, // [8] — inline, no heap alloc
    ScriptActionData Action, // "then" branch
    ScriptActionData Else // "else" branch
)
{
    /// <summary>
    /// The condition opcode as a typed enum.
    /// Values not present in <see cref="ScriptConditionType"/> are returned as their raw cast.
    /// </summary>
    public ScriptConditionType ConditionType => (ScriptConditionType)Type;
}

/// <summary>
/// Parsed contents of an Arcanum compiled script (.scr) file.
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
public sealed class ScriptFormat : IFormatFileReader<ScrFile>, IFormatFileWriter<ScrFile>
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
        OpTypeBuffer opTypes = default;
        reader.ReadBytes(8).CopyTo((Span<byte>)opTypes);
        OpValueBuffer opVals = default;
        reader.ReadBytes(32).CopyTo(MemoryMarshal.AsBytes((Span<int>)opVals));
        return new ScriptActionData(type, opTypes, opVals);
    }

    private static ScriptConditionData ReadCondition(scoped ref SpanReader reader)
    {
        var type = reader.ReadInt32();
        OpTypeBuffer opTypes = default;
        reader.ReadBytes(8).CopyTo((Span<byte>)opTypes);
        OpValueBuffer opVals = default;
        reader.ReadBytes(32).CopyTo(MemoryMarshal.AsBytes((Span<int>)opVals));
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
        Span<byte> descBuf = stackalloc byte[DescriptionLength]; // 40 bytes — safe on stack
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
        var opTypes = a.OpTypes;
        writer.WriteBytes((ReadOnlySpan<byte>)opTypes);
        foreach (var v in a.OpValues)
            writer.WriteInt32(v);
    }

    private static void WriteCondition(ScriptConditionData c, ref SpanWriter writer)
    {
        writer.WriteInt32(c.Type);
        var opTypes = c.OpTypes;
        writer.WriteBytes((ReadOnlySpan<byte>)opTypes);
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
