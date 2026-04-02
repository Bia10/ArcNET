using ArcNET.Core;

namespace ArcNET.GameObjects;

/// <summary>A script attachment on a game object or sector tile.</summary>
public sealed class GameObjectScript : IBinarySerializable<GameObjectScript, SpanReader>
{
    /// <summary>Gets the four counter bytes.</summary>
    public required byte[] Counters { get; init; }

    /// <summary>Gets the script flags.</summary>
    public required int Flags { get; init; }

    /// <summary>Gets the script identifier.</summary>
    public required int ScriptId { get; init; }

    /// <summary>Returns <see langword="true"/> when this script is effectively empty (no-op).</summary>
    public bool IsEmpty => ScriptId == 0 && Flags == 0 && Counters is [0x00, 0x00, 0x00, 0x00];

    /// <inheritdoc/>
    public static GameObjectScript Read(ref SpanReader reader)
    {
        var counters = reader.ReadBytes(4).ToArray();
        return new GameObjectScript
        {
            Counters = counters,
            Flags = reader.ReadInt32(),
            ScriptId = reader.ReadInt32(),
        };
    }

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer)
    {
        writer.WriteBytes(Counters);
        writer.WriteInt32(Flags);
        writer.WriteInt32(ScriptId);
    }
}
