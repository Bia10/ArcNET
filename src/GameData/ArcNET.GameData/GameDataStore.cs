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
    private readonly List<ScrFile> _scripts = [];
    private readonly List<DlgFile> _dialogs = [];
    private readonly HashSet<GameObjectGuid> _dirty = [];
    private FrozenDictionary<GameObjectGuid, GameObjectHeader>? _indexByGuid;

    // Origin-tracking — populated by the loader; empty when entries are added programmatically.
    private readonly Dictionary<string, List<MessageEntry>> _messagesBySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Sector>> _sectorsBySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ProtoData>> _protosBySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<MobData>> _mobsBySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ScrFile>> _scriptsBySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<DlgFile>> _dialogsBySource = new(StringComparer.OrdinalIgnoreCase);

    // Lazy read-only views — rebuilt only when a new source entry is added or the store is cleared.
    private IReadOnlyDictionary<string, IReadOnlyList<MessageEntry>>? _messagesBySourceView;
    private IReadOnlyDictionary<string, IReadOnlyList<Sector>>? _sectorsBySourceView;
    private IReadOnlyDictionary<string, IReadOnlyList<ProtoData>>? _protosBySourceView;
    private IReadOnlyDictionary<string, IReadOnlyList<MobData>>? _mobsBySourceView;
    private IReadOnlyDictionary<string, IReadOnlyList<ScrFile>>? _scriptsBySourceView;
    private IReadOnlyDictionary<string, IReadOnlyList<DlgFile>>? _dialogsBySourceView;

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

    /// <summary>Gets all loaded compiled script (.scr) files.</summary>
    public IReadOnlyList<ScrFile> Scripts => _scripts;

    /// <summary>Gets all loaded dialog (.dlg) files.</summary>
    public IReadOnlyList<DlgFile> Dialogs => _dialogs;

    /// <summary>
    /// Gets message entries grouped by their source path.
    /// Empty when entries were added programmatically without origin information.
    /// Use this to restore per-file structure during save.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<MessageEntry>> MessagesBySource =>
        _messagesBySourceView ??= BuildView(_messagesBySource);

    /// <summary>
    /// Gets sectors grouped by their source path.
    /// Empty when entries were added programmatically without origin information.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<Sector>> SectorsBySource =>
        _sectorsBySourceView ??= BuildView(_sectorsBySource);

    /// <summary>
    /// Gets prototypes grouped by their source path.
    /// Empty when entries were added programmatically without origin information.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ProtoData>> ProtosBySource =>
        _protosBySourceView ??= BuildView(_protosBySource);

    /// <summary>
    /// Gets mobile objects grouped by their source path.
    /// Empty when entries were added programmatically without origin information.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<MobData>> MobsBySource =>
        _mobsBySourceView ??= BuildView(_mobsBySource);

    /// <summary>
    /// Gets compiled scripts grouped by their source path.
    /// Empty when entries were added programmatically without origin information.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ScrFile>> ScriptsBySource =>
        _scriptsBySourceView ??= BuildView(_scriptsBySource);

    /// <summary>
    /// Gets dialogs grouped by their source path.
    /// Empty when entries were added programmatically without origin information.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<DlgFile>> DialogsBySource =>
        _dialogsBySourceView ??= BuildView(_dialogsBySource);

    /// <summary>Gets the set of GUIDs that have been marked dirty since the last <see cref="ClearDirty"/>.</summary>
    public IReadOnlySet<GameObjectGuid> DirtyObjects => _dirty;

    /// <summary>Raised when an object is added or mutated via <see cref="MarkDirty"/>.</summary>
    public event EventHandler<GameObjectGuid>? ObjectChanged;

    /// <summary>Adds an object header to the store and invalidates the GUID index.</summary>
    public void AddObject(GameObjectHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        _objects.Add(header);
        _indexByGuid = null;
    }

    /// <summary>Appends a fully-parsed message entry (preserving index and optional sound ID).</summary>
    public void AddMessage(MessageEntry entry) => AddMessage(entry, null);

    internal void AddMessage(MessageEntry entry, string? sourcePath)
    {
        _messages.Add(entry);
        if (sourcePath is null)
            return;
        GetOrCreate(_messagesBySource, sourcePath).Add(entry);
        _messagesBySourceView = null;
    }

    /// <summary>Appends a parsed sector.</summary>
    public void AddSector(Sector sector) => AddSector(sector, null);

    internal void AddSector(Sector sector, string? sourcePath)
    {
        _sectors.Add(sector);
        if (sourcePath is null)
            return;
        GetOrCreate(_sectorsBySource, sourcePath).Add(sector);
        _sectorsBySourceView = null;
    }

    /// <summary>Appends a parsed prototype.</summary>
    public void AddProto(ProtoData proto) => AddProto(proto, null);

    internal void AddProto(ProtoData proto, string? sourcePath)
    {
        _protos.Add(proto);
        if (sourcePath is null)
            return;
        GetOrCreate(_protosBySource, sourcePath).Add(proto);
        _protosBySourceView = null;
    }

    /// <summary>Appends a parsed mobile object.</summary>
    public void AddMob(MobData mob) => AddMob(mob, null);

    internal void AddMob(MobData mob, string? sourcePath)
    {
        _mobs.Add(mob);
        if (sourcePath is null)
            return;
        GetOrCreate(_mobsBySource, sourcePath).Add(mob);
        _mobsBySourceView = null;
    }

    /// <summary>Appends a parsed compiled script.</summary>
    public void AddScript(ScrFile script) => AddScript(script, null);

    internal void AddScript(ScrFile script, string? sourcePath)
    {
        _scripts.Add(script);
        if (sourcePath is null)
            return;
        GetOrCreate(_scriptsBySource, sourcePath).Add(script);
        _scriptsBySourceView = null;
    }

    /// <summary>Appends a parsed dialog file.</summary>
    public void AddDialog(DlgFile dialog) => AddDialog(dialog, null);

    internal void AddDialog(DlgFile dialog, string? sourcePath)
    {
        _dialogs.Add(dialog);
        if (sourcePath is null)
            return;
        GetOrCreate(_dialogsBySource, sourcePath).Add(dialog);
        _dialogsBySourceView = null;
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
        _scripts.Clear();
        _dialogs.Clear();
        _messagesBySource.Clear();
        _sectorsBySource.Clear();
        _protosBySource.Clear();
        _mobsBySource.Clear();
        _scriptsBySource.Clear();
        _dialogsBySource.Clear();
        _dirty.Clear();
        _indexByGuid = null;
        _messagesBySourceView = null;
        _sectorsBySourceView = null;
        _protosBySourceView = null;
        _mobsBySourceView = null;
        _scriptsBySourceView = null;
        _dialogsBySourceView = null;
    }

    private FrozenDictionary<GameObjectGuid, GameObjectHeader> BuildGuidIndex() =>
        _objects.ToFrozenDictionary(h => h.ObjectId);

    private static IReadOnlyDictionary<string, IReadOnlyList<T>> BuildView<T>(Dictionary<string, List<T>> source) =>
        source.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<T>)kv.Value.AsReadOnly(),
            StringComparer.OrdinalIgnoreCase
        );

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
