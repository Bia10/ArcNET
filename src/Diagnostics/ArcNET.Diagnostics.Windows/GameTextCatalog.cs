using System.Collections.Concurrent;
using ArcNET.Editor;
using ArcNET.Formats;

namespace ArcNET.Diagnostics.Windows;

internal sealed class GameTextCatalog
{
    private readonly Dictionary<int, ResolvedBackground> _backgroundsByTextId;
    private readonly Dictionary<int, string> _blessingsById;
    private readonly Dictionary<int, string> _cursesById;
    private readonly Dictionary<int, string> _descriptionsById;
    private readonly Dictionary<int, string> _keysById;
    private readonly Dictionary<int, ResolvedQuest> _questsById;
    private readonly Dictionary<int, string> _reputationsById;
    private readonly Dictionary<int, string> _rumorsById;
    private readonly Dictionary<int, string> _dumbRumorsById;

    private GameTextCatalog(
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

    public static GameTextCatalog Load(string modulePath)
    {
        var gameDirectory =
            Path.GetDirectoryName(modulePath)
            ?? throw new InvalidOperationException("Unable to derive the Arcanum installation directory.");
        return s_catalogs
            .GetOrAdd(
                gameDirectory,
                static directory => new Lazy<GameTextCatalog>(
                    () => Create(directory),
                    LazyThreadSafetyMode.ExecutionAndPublication
                )
            )
            .Value;
    }

    public ResolvedBackground ResolveBackground(int textId) =>
        _backgroundsByTextId.TryGetValue(textId, out var background)
            ? background
            : new ResolvedBackground(textId, null, null);

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

    private static GameTextCatalog Create(string gameDirectory)
    {
        try
        {
            using var workspace = EditorWorkspaceLoader
                .LoadFromGameInstallAsync(gameDirectory)
                .GetAwaiter()
                .GetResult();
            return new GameTextCatalog(
                LoadBackgrounds(workspace),
                LoadBlessingNames(workspace),
                LoadCurseNames(workspace),
                LoadDescriptions(workspace),
                LoadKeyNames(workspace),
                LoadQuests(workspace),
                LoadReputationNames(workspace),
                LoadRumors(workspace, "mes/game_rd_npc_m2m.mes"),
                LoadRumors(workspace, "mes/game_rd_npc_m2m_dumb.mes"),
                availabilityNote: null
            );
        }
        catch (Exception ex)
        {
            return new GameTextCatalog(
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

    private static Dictionary<int, ResolvedQuest> LoadQuests(EditorWorkspace workspace)
    {
        Dictionary<int, ResolvedQuest> quests = [];
        MergeQuestDescriptions(quests, workspace.FindMessageFile("mes/gamequestlog.mes"), isDumb: false);
        MergeQuestDescriptions(quests, workspace.FindMessageFile("mes/gamequestlogdumb.mes"), isDumb: true);
        return quests;
    }

    private static Dictionary<int, ResolvedBackground> LoadBackgrounds(EditorWorkspace workspace)
    {
        Dictionary<int, ResolvedBackground> backgrounds = [];
        var file = workspace.FindMessageFile("mes/gameback.mes");
        if (file is null)
            return backgrounds;

        foreach (var entry in file.Entries)
        {
            if (TrySplitBackgroundText(entry.Text, out var name, out var body))
                backgrounds[entry.Index] = new ResolvedBackground(entry.Index, name, body);
        }

        return backgrounds;
    }

    private static Dictionary<int, string> LoadDescriptions(EditorWorkspace workspace)
    {
        Dictionary<int, string> descriptions = [];
        MergeMessageEntries(descriptions, workspace.FindMessageFile("mes/description.mes"));
        MergeMessageEntries(descriptions, workspace.FindMessageFile("mes/gamedesc.mes"));
        return descriptions;
    }

    private static Dictionary<int, string> LoadKeyNames(EditorWorkspace workspace)
    {
        Dictionary<int, string> keys = [];
        MergeMessageEntries(keys, workspace.FindMessageFile("mes/gamekey.mes"));
        return keys;
    }

    private static Dictionary<int, string> LoadReputationNames(EditorWorkspace workspace)
    {
        Dictionary<int, string> reputations = [];
        MergeMessageEntries(reputations, workspace.FindMessageFile("mes/gamereplog.mes"));
        return reputations;
    }

    private static Dictionary<int, string> LoadRumors(EditorWorkspace workspace, string path)
    {
        Dictionary<int, string> rumors = [];
        var file = workspace.FindMessageFile(path);
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

    private static Dictionary<int, string> LoadBlessingNames(EditorWorkspace workspace) =>
        LoadModuloMessageBank(workspace.FindMessageFile("mes/gamebless.mes"));

    private static Dictionary<int, string> LoadCurseNames(EditorWorkspace workspace) =>
        LoadModuloMessageBank(workspace.FindMessageFile("mes/gamecurse.mes"));

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

    private static readonly ConcurrentDictionary<string, Lazy<GameTextCatalog>> s_catalogs = new(
        StringComparer.OrdinalIgnoreCase
    );

    internal readonly record struct ResolvedQuest(
        int QuestId,
        string? Label,
        string? Description,
        string? DumbDescription
    )
    {
        public string SummaryLabel => !string.IsNullOrWhiteSpace(Label) ? Label! : $"Quest {QuestId}";
    }

    internal readonly record struct ResolvedBackground(int TextId, string? Name, string? Body)
    {
        public string SummaryLabel => !string.IsNullOrWhiteSpace(Name) ? Name! : $"Background {TextId}";
    }
}
