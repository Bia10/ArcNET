using System.Collections.Frozen;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData;

/// <summary>
/// Injectable, mutable data store for loaded game data.
/// Replaces the old static <c>GameObjectManager</c>.
/// </summary>
public sealed class GameDataStore
{
    private readonly List<GameObjectHeader> _objects = [];
    private readonly List<string> _messages = [];
    private readonly HashSet<GameObjectGuid> _dirty = [];
    private FrozenDictionary<GameObjectGuid, GameObjectHeader>? _indexByGuid;

    /// <summary>Gets all loaded object headers.</summary>
    public IReadOnlyList<GameObjectHeader> Objects => _objects;

    /// <summary>Gets all loaded message strings.</summary>
    public IReadOnlyList<string> Messages => _messages;

    /// <summary>Gets the set of GUIDs that have been marked dirty since the last <see cref="ClearDirty"/>.</summary>
    public IReadOnlySet<GameObjectGuid> DirtyObjects => _dirty;

    /// <summary>Raised when an object is added or mutated via <see cref="MarkDirty"/>.</summary>
    public event EventHandler<GameObjectGuid>? ObjectChanged;

    /// <summary>Adds an object header to the store and invalidates the GUID index.</summary>
    public void AddObject(GameObjectHeader header)
    {
        _objects.Add(header);
        _indexByGuid = null;
    }

    /// <summary>Appends a message string to the message list.</summary>
    public void AddMessage(string text) => _messages.Add(text);

    /// <summary>
    /// Looks up an object header by its <see cref="GameObjectGuid"/> in O(1).
    /// Returns <see langword="null"/> when not found.
    /// </summary>
    public GameObjectHeader? FindByGuid(in GameObjectGuid id)
    {
        _indexByGuid ??= BuildGuidIndex();
        return _indexByGuid.TryGetValue(id, out var header) ? header : null;
    }

    /// <summary>Marks an object as needing re-serialization and raises <see cref="ObjectChanged"/>.</summary>
    public void MarkDirty(in GameObjectGuid id)
    {
        _dirty.Add(id);
        ObjectChanged?.Invoke(this, id);
    }

    /// <summary>Clears all dirty markers.</summary>
    public void ClearDirty() => _dirty.Clear();

    /// <summary>Removes all objects and messages and resets dirty state.</summary>
    public void Clear()
    {
        _objects.Clear();
        _messages.Clear();
        _dirty.Clear();
        _indexByGuid = null;
    }

    private FrozenDictionary<GameObjectGuid, GameObjectHeader> BuildGuidIndex() =>
        _objects.ToFrozenDictionary(h => h.ObjectId);
}
