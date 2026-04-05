using ArcNET.Core;

namespace ArcNET.GameObjects;

/// <summary>
/// A 12-byte script attachment on a game object or sector tile.
/// Value type — stored inline in arrays; no per-instance heap allocation.
/// </summary>
public readonly record struct GameObjectScript(uint Counters, int Flags, int ScriptId)
    : IBinarySerializable<GameObjectScript, SpanReader>
{
    /// <summary>Returns <see langword="true"/> when this script is effectively empty (no-op).</summary>
    public bool IsEmpty => ScriptId == 0 && Flags == 0 && Counters == 0;

    /// <inheritdoc/>
    public static GameObjectScript Read(ref SpanReader reader) =>
        new(reader.ReadUInt32(), reader.ReadInt32(), reader.ReadInt32());

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer)
    {
        writer.WriteUInt32(Counters);
        writer.WriteInt32(Flags);
        writer.WriteInt32(ScriptId);
    }
}
