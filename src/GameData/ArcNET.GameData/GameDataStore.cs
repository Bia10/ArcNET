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
    private readonly List<MessageEntry> _messages = [];
    private readonly List<Sector> _sectors = [];
    private readonly List<ProtoData> _protos = [];
    private readonly List<MobData> _mobs = [];
    private readonly HashSet<GameObjectGuid> _dirty = [];
    private FrozenDictionary<GameObjectGuid, GameObjectHeader>? _indexByGuid;

    // Origin-tracking — populated by the loader; empty when entries are added programmatically.
    private readonly Dictionary<string, List<MessageEntry>> _messagesBySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Sector>> _sectorsBySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ProtoData>> _protosBySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<MobData>> _mobsBySource = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets all loaded object headers.</summary>
    public IReadOnlyList<GameObjectHeader> Objects => _objects;

    /// <summary>Gets all loaded message entries (index, optional sound ID, and text).</summary>
    public IReadOnlyList<MessageEntry> Messages => _messages;

    /// <summary>Gets all loaded sector data.</summary>
    public IReadOnlyList<Sector> Sectors => _sectors;

    /// <summary>Gets all loaded prototype data.</summary>
    public IReadOnlyList<ProtoData> Protos => _protos;

    /// <summary>Gets all loaded mobile (MOB) data.</summary>
    public IReadOnlyList<MobData> Mobs => _mobs;

    /// <summary>
    /// Gets message entries grouped by their source filename.
    /// Empty when entries were added programmatically without origin information.
    /// Use this to restore per-file structure during save.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<MessageEntry>> MessagesBySource =>
        _messagesBySource.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<MessageEntry>)kv.Value.AsReadOnly(),
            StringComparer.OrdinalIgnoreCase
        );

    /// <summary>
    /// Gets sectors grouped by their source filename.
    /// Empty when entries were added programmatically without origin information.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<Sector>> SectorsBySource =>
        _sectorsBySource.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<Sector>)kv.Value.AsReadOnly(),
            StringComparer.OrdinalIgnoreCase
        );

    /// <summary>
    /// Gets prototypes grouped by their source filename.
    /// Empty when entries were added programmatically without origin information.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ProtoData>> ProtosBySource =>
        _protosBySource.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<ProtoData>)kv.Value.AsReadOnly(),
            StringComparer.OrdinalIgnoreCase
        );

    /// <summary>
    /// Gets mobile objects grouped by their source filename.
    /// Empty when entries were added programmatically without origin information.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<MobData>> MobsBySource =>
        _mobsBySource.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<MobData>)kv.Value.AsReadOnly(),
            StringComparer.OrdinalIgnoreCase
        );

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

    /// <summary>Appends a fully-parsed message entry (preserving index and optional sound ID).</summary>
    public void AddMessage(MessageEntry entry) => _messages.Add(entry);

    internal void AddMessage(MessageEntry entry, string sourcePath)
    {
        _messages.Add(entry);
        GetOrCreate(_messagesBySource, sourcePath).Add(entry);
    }

    /// <summary>Appends a parsed sector.</summary>
    public void AddSector(Sector sector) => _sectors.Add(sector);

    internal void AddSector(Sector sector, string sourcePath)
    {
        _sectors.Add(sector);
        GetOrCreate(_sectorsBySource, sourcePath).Add(sector);
    }

    /// <summary>Appends a parsed prototype.</summary>
    public void AddProto(ProtoData proto) => _protos.Add(proto);

    internal void AddProto(ProtoData proto, string sourcePath)
    {
        _protos.Add(proto);
        GetOrCreate(_protosBySource, sourcePath).Add(proto);
    }

    /// <summary>Appends a parsed mobile object.</summary>
    public void AddMob(MobData mob) => _mobs.Add(mob);

    internal void AddMob(MobData mob, string sourcePath)
    {
        _mobs.Add(mob);
        GetOrCreate(_mobsBySource, sourcePath).Add(mob);
    }

    /// <summary>
    /// Returns all loaded object headers whose <see cref="GameObjectHeader.GameObjectType"/>
    /// matches <paramref name="type"/>.
    /// </summary>
    public IReadOnlyList<GameObjectHeader> FindByType(ObjectType type)
    {
        var result = new List<GameObjectHeader>();
        foreach (var h in _objects)
            if (h.GameObjectType == type)
                result.Add(h);
        return result;
    }

    /// <summary>
    /// Returns all loaded object headers whose <see cref="GameObjectHeader.ProtoId"/>
    /// matches <paramref name="protoId"/>.
    /// </summary>
    public IReadOnlyList<GameObjectHeader> FindByProtoId(in GameObjectGuid protoId)
    {
        var result = new List<GameObjectHeader>();
        foreach (var h in _objects)
            if (h.ProtoId == protoId)
                result.Add(h);
        return result;
    }

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

    /// <summary>Removes all data and resets dirty state.</summary>
    public void Clear()
    {
        _objects.Clear();
        _messages.Clear();
        _sectors.Clear();
        _protos.Clear();
        _mobs.Clear();
        _messagesBySource.Clear();
        _sectorsBySource.Clear();
        _protosBySource.Clear();
        _mobsBySource.Clear();
        _dirty.Clear();
        _indexByGuid = null;
    }

    private FrozenDictionary<GameObjectGuid, GameObjectHeader> BuildGuidIndex() =>
        _objects.ToFrozenDictionary(h => h.ObjectId);

    private static List<T> GetOrCreate<T>(Dictionary<string, List<T>> dict, string key)
    {
        if (!dict.TryGetValue(key, out var list))
        {
            list = [];
            dict[key] = list;
        }

        return list;
    }
}
