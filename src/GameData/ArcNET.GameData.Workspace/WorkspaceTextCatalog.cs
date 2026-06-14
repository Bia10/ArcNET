using System.Collections.Concurrent;
using System.Globalization;
using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Shared local text catalog projected from a loaded game install or workspace module.
/// </summary>
public sealed class WorkspaceTextCatalog
{
    private readonly Dictionary<int, ResolvedBackground> _backgroundsById;
    private readonly Dictionary<int, ResolvedBackground> _backgroundsByTextId;
    private readonly Dictionary<int, string> _blessingsById;
    private readonly Dictionary<int, string> _cursesById;
    private readonly Dictionary<int, string> _descriptionsById;
    private readonly Dictionary<int, string> _keysById;
    private readonly Dictionary<int, ResolvedQuest> _questsById;
    private readonly Dictionary<int, string> _reputationsById;
    private readonly Dictionary<int, string> _rumorsById;
    private readonly Dictionary<int, string> _dumbRumorsById;

    private WorkspaceTextCatalog(
        Dictionary<int, ResolvedBackground> backgroundsById,
        Dictionary<int, ResolvedBackground> backgroundsByTextId,
        Dictionary<int, string> blessingsById,
        Dictionary<int, string> cursesById,
        Dictionary<int, string> descriptionsById,
        Dictionary<int, string> keysById,
        Dictionary<int, ResolvedQuest> questsById,
        Dictionary<int, string> reputationsById,
        Dictionary<int, string> rumorsById,
        Dictionary<int, string> dumbRumorsById,
        string? availabilityNote
    )
    {
        _backgroundsById = backgroundsById;
        _backgroundsByTextId = backgroundsByTextId;
        _blessingsById = blessingsById;
        _cursesById = cursesById;
        _descriptionsById = descriptionsById;
        _keysById = keysById;
        _questsById = questsById;
        _reputationsById = reputationsById;
        _rumorsById = rumorsById;
        _dumbRumorsById = dumbRumorsById;
        AvailabilityNote = availabilityNote;
    }

    public string? AvailabilityNote { get; }

    public static Task<WorkspaceTextCatalog> LoadFromModulePathAsync(string modulePath, bool forceReload = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modulePath);

        var identity = WorkspaceContentIdentityResolver.Resolve(modulePath);
        return LoadAsync(identity, forceReload);
    }

    public static Task<WorkspaceTextCatalog> LoadFromGameDirectoryAsync(string gameDirectory, bool forceReload = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);

        return LoadAsync(
            new WorkspaceContentIdentity(WorkspaceContentIdentityKind.GameInstall, Path.GetFullPath(gameDirectory)),
            forceReload
        );
    }

    public static WorkspaceTextCatalog Build(GameDataStore gameData)
    {
        ArgumentNullException.ThrowIfNull(gameData);

        return Create(gameData, availabilityNote: null);
    }

    public ResolvedBackground ResolveBackground(int textId) =>
        _backgroundsByTextId.TryGetValue(textId, out var background)
            ? background
            : new ResolvedBackground(-1, textId, null, null);

    public ResolvedQuest ResolveQuest(int questId) =>
        _questsById.TryGetValue(questId, out var quest) ? quest : new ResolvedQuest(questId, null, null, null);

    public string ResolveDescription(int descriptionId) =>
        _descriptionsById.TryGetValue(descriptionId, out var description) ? description : string.Empty;

    public string ResolveKeyName(int keyId) => _keysById.TryGetValue(keyId, out var keyName) ? keyName : string.Empty;

    public string ResolveReputationName(int reputationId) =>
        _reputationsById.TryGetValue(reputationId, out var reputationName) ? reputationName : string.Empty;

    public string ResolveRumorText(int rumorId, bool dumb = false)
    {
        var source = dumb ? _dumbRumorsById : _rumorsById;
        return source.TryGetValue(rumorId, out var rumorText) ? rumorText : string.Empty;
    }

    public string ResolveBlessingName(int blessingId) =>
        _blessingsById.TryGetValue(blessingId, out var blessingName) ? blessingName : string.Empty;

    public string ResolveCurseName(int curseId) =>
        _cursesById.TryGetValue(curseId, out var curseName) ? curseName : string.Empty;

    public IReadOnlyList<ResolvedBackground> EnumerateBackgrounds() =>
        [
            .. _backgroundsById
                .Values.OrderBy(static background => background.Name ?? background.SummaryLabel)
                .ThenBy(static background => background.BackgroundId),
        ];

    public IReadOnlyList<ResolvedQuest> EnumerateQuests() =>
        [.. _questsById.Values.OrderBy(static quest => quest.SummaryLabel).ThenBy(static quest => quest.QuestId)];

    public IReadOnlyList<ResolvedRumor> EnumerateRumors()
    {
        var rumorIds = _rumorsById.Keys.Union(_dumbRumorsById.Keys).OrderBy(static rumorId => rumorId);
        return
        [
            .. rumorIds.Select(rumorId =>
            {
                _rumorsById.TryGetValue(rumorId, out var normalText);
                _dumbRumorsById.TryGetValue(rumorId, out var dumbText);
                return new ResolvedRumor(rumorId, NullIfWhiteSpace(normalText), NullIfWhiteSpace(dumbText));
            }),
        ];
    }

    public IReadOnlyList<ResolvedNamedEntry> EnumerateReputations() => EnumerateNamedEntries(_reputationsById);

    public IReadOnlyList<ResolvedNamedEntry> EnumerateBlessings() => EnumerateNamedEntries(_blessingsById);

    public IReadOnlyList<ResolvedNamedEntry> EnumerateCurses() => EnumerateNamedEntries(_cursesById);

    public IReadOnlyList<ResolvedNamedEntry> EnumerateDescriptions() => EnumerateNamedEntries(_descriptionsById);

    public IReadOnlyList<ResolvedNamedEntry> EnumerateKeys() => EnumerateNamedEntries(_keysById);

    private static async Task<WorkspaceTextCatalog> LoadAsync(WorkspaceContentIdentity identity, bool forceReload)
    {
        if (forceReload)
            InvalidateForGameDirectory(identity);

        var cached = s_catalogs.GetOrAdd(
            identity.CacheKey,
            _ => new Lazy<Task<WorkspaceTextCatalog>>(
                () => LoadCoreAsync(identity),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );
        var catalog = await cached.Value.ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(catalog.AvailabilityNote))
        {
            if (s_catalogs.TryGetValue(identity.CacheKey, out var current) && ReferenceEquals(current, cached))
                _ = s_catalogs.TryRemove(identity.CacheKey, out _);
        }

        return catalog;
    }

    private static async Task<WorkspaceTextCatalog> LoadCoreAsync(WorkspaceContentIdentity identity)
    {
        try
        {
            var loadResult =
                identity.Kind == WorkspaceContentIdentityKind.Module
                    ? await WorkspaceContentLoader.LoadModuleAsync(identity.Path).ConfigureAwait(false)
                    : await WorkspaceContentLoader.LoadGameInstallAsync(identity.Path).ConfigureAwait(false);
            return Create(loadResult.GameData, availabilityNote: null);
        }
        catch (Exception ex)
        {
            return new WorkspaceTextCatalog(
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                $"Game text catalog unavailable ({ex.GetType().Name}: {ex.Message})."
            );
        }
    }

    private static void InvalidateForGameDirectory(WorkspaceContentIdentity identity)
    {
        var gameDirectory =
            identity.Kind == WorkspaceContentIdentityKind.Module
                ? WorkspaceInstallPathResolver.ResolveOwningGameDirectoryFromModuleDirectory(identity.Path)
                : identity.Path;
        foreach (var cacheKey in s_catalogs.Keys)
        {
            if (
                WorkspaceContentIdentityResolver.TryParseCacheKey(cacheKey, out var cachedIdentity)
                && WorkspaceContentIdentityResolver.ReferencesGameDirectory(cachedIdentity, gameDirectory)
            )
            {
                _ = s_catalogs.TryRemove(cacheKey, out _);
            }
        }
    }

    private static WorkspaceTextCatalog Create(GameDataStore gameData, string? availabilityNote)
    {
        var backgrounds = LoadBackgrounds(gameData);
        return new WorkspaceTextCatalog(
            backgrounds.ById,
            backgrounds.ByTextId,
            LoadBlessingNames(gameData),
            LoadCurseNames(gameData),
            LoadDescriptions(gameData),
            LoadKeyNames(gameData),
            LoadQuests(gameData),
            LoadReputationNames(gameData),
            LoadRumors(gameData, "mes/game_rd_npc_m2m.mes"),
            LoadRumors(gameData, "mes/game_rd_npc_m2m_dumb.mes"),
            availabilityNote
        );
    }

    private static Dictionary<int, ResolvedQuest> LoadQuests(GameDataStore gameData)
    {
        Dictionary<int, ResolvedQuest> quests = [];
        MergeQuestDescriptions(quests, FindMessageFile(gameData, "mes/gamequestlog.mes"), isDumb: false);
        MergeQuestDescriptions(quests, FindMessageFile(gameData, "mes/gamequestlogdumb.mes"), isDumb: true);
        return quests;
    }

    private static BackgroundCatalogData LoadBackgrounds(GameDataStore gameData)
    {
        Dictionary<int, ResolvedBackground> backgroundsById = [];
        Dictionary<int, ResolvedBackground> backgroundsByTextId = [];
        Dictionary<int, (string? Name, string? Body)> descriptionsByTextId = [];
        var descriptionsFile = FindMessageFile(gameData, "mes/gameback.mes");
        if (descriptionsFile is not null)
        {
            foreach (var entry in descriptionsFile.Entries)
            {
                if (!TrySplitBackgroundText(entry.Text, out var name, out var body))
                    continue;

                descriptionsByTextId[entry.Index] = (name, body);
            }
        }

        var rulesFile = FindMessageFile(gameData, "rules/backgrnd.mes");
        if (rulesFile is not null)
        {
            foreach (var entry in rulesFile.Entries)
            {
                if (entry.Index < 0 || entry.Index % 10 != 0)
                    continue;

                if (
                    !int.TryParse(
                        entry.Text?.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var textId
                    )
                    || textId <= 0
                )
                {
                    continue;
                }

                descriptionsByTextId.TryGetValue(textId, out var description);
                var background = new ResolvedBackground(entry.Index / 10, textId, description.Name, description.Body);
                backgroundsById[background.BackgroundId] = background;
                backgroundsByTextId[textId] = background;
            }
        }

        foreach (var (textId, description) in descriptionsByTextId)
        {
            if (backgroundsByTextId.ContainsKey(textId))
                continue;

            var backgroundId = textId >= 1000 && (textId - 1000) % 10 == 0 ? (textId - 1000) / 10 : -1;
            var background = new ResolvedBackground(backgroundId, textId, description.Name, description.Body);
            if (backgroundId >= 0)
                backgroundsById.TryAdd(backgroundId, background);

            backgroundsByTextId[textId] = background;
        }

        return new BackgroundCatalogData(backgroundsById, backgroundsByTextId);
    }

    private static Dictionary<int, string> LoadDescriptions(GameDataStore gameData)
    {
        Dictionary<int, string> descriptions = [];
        MergeMessageEntries(descriptions, FindMessageFile(gameData, "mes/description.mes"));
        MergeMessageEntries(descriptions, FindMessageFile(gameData, "mes/gamedesc.mes"));
        return descriptions;
    }

    private static Dictionary<int, string> LoadKeyNames(GameDataStore gameData)
    {
        Dictionary<int, string> keys = [];
        MergeMessageEntries(keys, FindMessageFile(gameData, "mes/gamekey.mes"));
        return keys;
    }

    private static Dictionary<int, string> LoadReputationNames(GameDataStore gameData)
    {
        Dictionary<int, string> reputations = [];
        MergeMessageEntries(reputations, FindMessageFile(gameData, "mes/gamereplog.mes"));
        return reputations;
    }

    private static Dictionary<int, string> LoadRumors(GameDataStore gameData, string path)
    {
        Dictionary<int, string> rumors = [];
        var file = FindMessageFile(gameData, path);
        if (file is null)
            return rumors;

        foreach (var entry in file.Entries)
        {
            if (entry.Index < 20000 || entry.Index % 20 != 0)
                continue;

            var text = NormalizeMessageText(entry.Text);
            if (text is not null)
                rumors[entry.Index / 20] = text;
        }

        return rumors;
    }

    private static Dictionary<int, string> LoadBlessingNames(GameDataStore gameData) =>
        LoadModuloMessageBank(FindMessageFile(gameData, "mes/gamebless.mes"));

    private static Dictionary<int, string> LoadCurseNames(GameDataStore gameData) =>
        LoadModuloMessageBank(FindMessageFile(gameData, "mes/gamecurse.mes"));

    private static MesFile? FindMessageFile(GameDataStore gameData, string assetPath) =>
        WorkspaceMessageLookup.FindMessageFile(gameData, assetPath);

    private static Dictionary<int, string> LoadModuloMessageBank(MesFile? file)
    {
        Dictionary<int, string> entries = [];
        if (file is null)
            return entries;

        foreach (var entry in file.Entries)
        {
            if (entry.Index < 0 || entry.Index % 10 != 0)
                continue;

            var text = NormalizeMessageText(entry.Text);
            if (text is not null)
                entries[entry.Index / 10] = text;
        }

        return entries;
    }

    private static void MergeMessageEntries(Dictionary<int, string> entries, MesFile? file)
    {
        if (file is null)
            return;

        foreach (var entry in file.Entries)
        {
            var text = NormalizeMessageText(entry.Text);
            if (text is not null)
                entries[entry.Index] = text;
        }
    }

    private static void MergeQuestDescriptions(Dictionary<int, ResolvedQuest> quests, MesFile? file, bool isDumb)
    {
        if (file is null)
            return;

        foreach (var entry in file.Entries)
        {
            var description = NormalizeMessageText(entry.Text);
            if (entry.Index < 1000 || description is null)
                continue;

            quests.TryGetValue(entry.Index, out var existing);
            var label = existing.Label ?? CreateQuestLabel(description);
            quests[entry.Index] = new ResolvedQuest(
                entry.Index,
                label,
                isDumb ? existing.Description : description,
                isDumb ? description : existing.DumbDescription
            );
        }
    }

    private static bool TrySplitBackgroundText(string? value, out string? name, out string? body)
    {
        name = null;
        body = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (normalized.Length == 0)
            return false;

        var separatorIndex = normalized.IndexOf('\n');
        if (separatorIndex < 0)
        {
            name = NormalizeMessageText(normalized);
            body = NormalizeMessageText(normalized);
            return true;
        }

        name = NormalizeMessageText(normalized[..separatorIndex]);
        body = NormalizeMessageText(normalized[(separatorIndex + 1)..]);
        return true;
    }

    private static string? NormalizeMessageText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return string.Join(
            ' ',
            value.Replace('\r', ' ').Replace('\n', ' ').Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
        );
    }

    private static string CreateQuestLabel(string description)
    {
        var sentenceEnd = description.IndexOfAny(['.', '!', '?']);
        var candidate = sentenceEnd is > 0 and <= 72 ? description[..sentenceEnd] : description;
        if (candidate.Length <= 72)
            return candidate;

        return $"{candidate[..69].TrimEnd()}...";
    }

    private static IReadOnlyList<ResolvedNamedEntry> EnumerateNamedEntries(Dictionary<int, string> entries) =>
        [
            .. entries
                .Select(static entry => new ResolvedNamedEntry(entry.Key, entry.Value))
                .OrderBy(static entry => entry.Name)
                .ThenBy(static entry => entry.Id),
        ];

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static readonly ConcurrentDictionary<string, Lazy<Task<WorkspaceTextCatalog>>> s_catalogs = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly record struct BackgroundCatalogData(
        Dictionary<int, ResolvedBackground> ById,
        Dictionary<int, ResolvedBackground> ByTextId
    );

    public readonly record struct ResolvedQuest(
        int QuestId,
        string? Label,
        string? Description,
        string? DumbDescription
    )
    {
        public string SummaryLabel => !string.IsNullOrWhiteSpace(Label) ? Label! : $"Quest {QuestId}";
    }

    public readonly record struct ResolvedRumor(int RumorId, string? NormalText, string? DumbText)
    {
        public string SummaryText =>
            !string.IsNullOrWhiteSpace(NormalText) ? NormalText!
            : !string.IsNullOrWhiteSpace(DumbText) ? DumbText!
            : $"Rumor {RumorId}";
    }

    public readonly record struct ResolvedNamedEntry(int Id, string Name);

    public readonly record struct ResolvedBackground(int BackgroundId, int TextId, string? Name, string? Body)
    {
        public string SummaryLabel => !string.IsNullOrWhiteSpace(Name) ? Name! : $"Background {TextId}";
    }
}
