using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;

namespace ArcanumDebugger.App.ViewModels;

public sealed record class GameDataQuickPickValueOption(
    string OptionKey,
    string LabelText,
    string ValueText,
    bool IsRecommended,
    string HintText
)
{
    public override string ToString() => $"{LabelText}: {ValueText}";
}

public sealed record class GameDataSupportedInputValueChoice(
    GameDataQuickPickEntry Entry,
    GameDataQuickPickValueOption Option
)
{
    public bool IsRecommended => Option.IsRecommended;

    public bool IsAlternative => !Option.IsRecommended;

    public string ButtonText => $"Use {Option.LabelText}";

    public string ValueText => Option.ValueText;

    public string HintText => Option.HintText;

    public override string ToString() => $"{Entry.TitleText} · {Option.LabelText}";
}

public sealed record class GameDataQuickPickEntry(
    string EntryKey,
    string BadgeText,
    string TitleText,
    string SubtitleText,
    string DetailText,
    IReadOnlyList<GameDataQuickPickValueOption> ValueOptions,
    int? WorldX,
    int? WorldY
)
{
    public GameDataQuickPickValueOption RecommendedValueOption =>
        ValueOptions.FirstOrDefault(static option => option.IsRecommended) ?? ValueOptions[0];

    public GameDataSupportedInputValueChoice RecommendedValueChoice => new(this, RecommendedValueOption);

    public IReadOnlyList<GameDataSupportedInputValueChoice> ValueChoices =>
        [.. ValueOptions.Select(option => new GameDataSupportedInputValueChoice(this, option))];

    public bool HasSingleValueOption => ValueOptions.Count == 1;

    public bool HasMultipleValueOptions => ValueOptions.Count > 1;

    public string ApplyValueText => RecommendedValueOption.ValueText;

    public string ValueSummaryText =>
        ValueOptions.Count == 1
            ? ApplyValueText
            : string.Join(" · ", ValueOptions.Select(option => $"{option.LabelText}: {option.ValueText}"));

    public override string ToString() => TitleText;
}

public static class GameDataQuickPickCatalog
{
    public static GameDataQuickPickEntry CreateCatalogEntry(PrototypePaletteEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return CreatePrototypeEntry(entry);
    }

    public static GameDataQuickPickEntry CreateCatalogEntry(WorldMapCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return CreateWorldEntry(entry);
    }

    public static GameDataQuickPickEntry CreateCatalogEntry(TileArtCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return CreateTileArtEntry(entry);
    }

    public static GameDataQuickPickEntry CreateCatalogEntry(StaticObjectCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return CreateStaticObjectPrototypeEntry(entry);
    }

    public static IReadOnlyList<GameDataQuickPickEntry> BuildLookupTokenEntries(
        IReadOnlyList<PrototypePaletteEntry> prototypeEntries,
        IReadOnlyList<StaticObjectCatalogEntry> staticObjectEntries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) =>
        BuildCombinedEntries(
            prototypeEntries,
            staticObjectEntries,
            filterText,
            maxEntries,
            static _ => true,
            static entry => entry.HasPrototype,
            static entry => CreatePrototypeEntry(entry, "Proto"),
            static entry => CreateStaticObjectPrototypeEntry(entry, "Placed")
        );

    public static IReadOnlyList<GameDataQuickPickEntry> BuildInventoryTokenEntries(
        IReadOnlyList<PrototypePaletteEntry> prototypeEntries,
        IReadOnlyList<StaticObjectCatalogEntry> staticObjectEntries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) =>
        BuildCombinedEntries(
            prototypeEntries,
            staticObjectEntries,
            filterText,
            maxEntries,
            static entry => IsInventoryObjectType(entry.ObjectType),
            static entry => entry.HasPrototype && IsInventoryObjectType(entry.ObjectType),
            static entry => CreatePrototypeEntry(entry, "Proto"),
            static entry => CreateStaticObjectPrototypeEntry(entry, "Placed")
        );

    public static IReadOnlyList<GameDataQuickPickEntry> BuildSpawnTokenEntries(
        IReadOnlyList<PrototypePaletteEntry> prototypeEntries,
        IReadOnlyList<StaticObjectCatalogEntry> staticObjectEntries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) =>
        BuildCombinedEntries(
            prototypeEntries,
            staticObjectEntries,
            filterText,
            maxEntries,
            static _ => true,
            static entry => entry.HasPrototype,
            static entry => CreatePrototypeEntry(entry, "Proto"),
            static entry => CreateStaticObjectPrototypeEntry(entry, "Placed")
        );

    public static IReadOnlyList<GameDataQuickPickEntry> BuildLookupPrototypeEntries(
        IReadOnlyList<PrototypePaletteEntry> entries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) => BuildPrototypeEntries(entries, filterText, maxEntries, static _ => true);

    public static IReadOnlyList<GameDataQuickPickEntry> BuildInventoryPrototypeEntries(
        IReadOnlyList<PrototypePaletteEntry> entries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) =>
        BuildPrototypeEntries(entries, filterText, maxEntries, static entry => IsInventoryObjectType(entry.ObjectType));

    public static IReadOnlyList<GameDataQuickPickEntry> BuildLookupStaticObjectPrototypeEntries(
        IReadOnlyList<StaticObjectCatalogEntry> entries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) => BuildStaticObjectPrototypeEntries(entries, filterText, maxEntries, static entry => entry.HasPrototype);

    public static IReadOnlyList<GameDataQuickPickEntry> BuildInventoryStaticObjectPrototypeEntries(
        IReadOnlyList<StaticObjectCatalogEntry> entries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) =>
        BuildStaticObjectPrototypeEntries(
            entries,
            filterText,
            maxEntries,
            static entry => entry.HasPrototype && IsInventoryObjectType(entry.ObjectType)
        );

    public static IReadOnlyList<GameDataQuickPickEntry> BuildSpawnPrototypeEntries(
        IReadOnlyList<PrototypePaletteEntry> entries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) => BuildPrototypeEntries(entries, filterText, maxEntries, static _ => true);

    public static IReadOnlyList<GameDataQuickPickEntry> BuildSpawnStaticObjectPrototypeEntries(
        IReadOnlyList<StaticObjectCatalogEntry> entries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) => BuildStaticObjectPrototypeEntries(entries, filterText, maxEntries, static entry => entry.HasPrototype);

    public static IReadOnlyList<GameDataQuickPickEntry> BuildWorldLocationEntries(
        IReadOnlyList<WorldMapCatalogEntry> entries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    )
    {
        ArgumentNullException.ThrowIfNull(entries);

        var normalizedFilter = Normalize(filterText);
        var searchTokens = Tokenize(filterText);
        return
        [
            .. entries
                .Where(static entry => entry.HasWorldCoordinates)
                .Select(entry => new RankedQuickPickEntry<GameDataQuickPickEntry>(
                    CreateWorldEntry(entry),
                    ScoreWorldEntry(entry, normalizedFilter, searchTokens)
                ))
                .Where(static candidate => candidate.Score >= 0)
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.TitleText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.WorldX)
                .ThenBy(static candidate => candidate.Entry.WorldY)
                .Select(static candidate => candidate.Entry)
                .Take(maxEntries),
        ];
    }

    public static IReadOnlyList<GameDataQuickPickEntry> BuildTileArtEntries(
        IReadOnlyList<TileArtCatalogEntry> entries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    )
    {
        ArgumentNullException.ThrowIfNull(entries);

        var normalizedFilter = Normalize(filterText);
        var searchTokens = Tokenize(filterText);
        return
        [
            .. entries
                .Select(entry => new RankedQuickPickEntry<GameDataQuickPickEntry>(
                    CreateTileArtEntry(entry),
                    ScoreNamedEntry(EnumerateTileArtSearchTexts(entry), normalizedFilter, searchTokens)
                ))
                .Where(static candidate => candidate.Score >= 0)
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.BadgeText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.TitleText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.ApplyValueText, StringComparer.OrdinalIgnoreCase)
                .Take(maxEntries)
                .Select(static candidate => candidate.Entry),
        ];
    }

    public static IReadOnlyList<GameDataQuickPickEntry> BuildSchematicEntries(
        IReadOnlyList<PrototypePaletteEntry> entries,
        string? filterText,
        int maxEntries = DefaultMaxEntries
    ) =>
        BuildPrototypeEntries(
            entries,
            filterText,
            maxEntries,
            static entry => IsInventoryObjectType(entry.ObjectType),
            CreateSchematicEntry
        );

    public static IReadOnlyList<GameDataQuickPickEntry> BuildSpellEntries(
        string? filterText,
        int maxEntries = DefaultMaxEntries
    )
    {
        var normalizedFilter = Normalize(filterText);
        var searchTokens = Tokenize(filterText);
        return
        [
            .. SpellTechCatalog
                .EnumerateSpells()
                .Select(spell => new RankedQuickPickEntry<GameDataQuickPickEntry>(
                    CreateSpellEntry(spell),
                    ScoreNamedEntry(EnumerateSpellSearchTexts(spell), normalizedFilter, searchTokens)
                ))
                .Where(static candidate => candidate.Score >= 0)
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.TitleText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.ApplyValueText, StringComparer.OrdinalIgnoreCase)
                .Take(maxEntries)
                .Select(static candidate => candidate.Entry),
        ];
    }

    public static IReadOnlyList<GameDataQuickPickEntry> BuildSpellCollegeEntries(
        string? filterText,
        int maxEntries = DefaultMaxEntries
    )
    {
        var normalizedFilter = Normalize(filterText);
        var searchTokens = Tokenize(filterText);
        return
        [
            .. SpellTechCatalog
                .EnumerateSpellCollegeNames()
                .Select(
                    (name, index) =>
                        new RankedQuickPickEntry<GameDataQuickPickEntry>(
                            CreateSpellCollegeEntry(index, name),
                            ScoreNamedEntry(
                                [name, index.ToString(CultureInfo.InvariantCulture), $"spell college {name}"],
                                normalizedFilter,
                                searchTokens
                            )
                        )
                )
                .Where(static candidate => candidate.Score >= 0)
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.TitleText, StringComparer.OrdinalIgnoreCase)
                .Take(maxEntries)
                .Select(static candidate => candidate.Entry),
        ];
    }

    public static IReadOnlyList<GameDataQuickPickEntry> BuildTechDisciplineEntries(
        string? filterText,
        int maxEntries = DefaultMaxEntries
    )
    {
        var normalizedFilter = Normalize(filterText);
        var searchTokens = Tokenize(filterText);
        return
        [
            .. SpellTechCatalog
                .EnumerateTechDisciplineNames()
                .Select(
                    (name, index) =>
                        new RankedQuickPickEntry<GameDataQuickPickEntry>(
                            CreateTechDisciplineEntry(index, name),
                            ScoreNamedEntry(
                                [name, index.ToString(CultureInfo.InvariantCulture), $"tech discipline {name}"],
                                normalizedFilter,
                                searchTokens
                            )
                        )
                )
                .Where(static candidate => candidate.Score >= 0)
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.TitleText, StringComparer.OrdinalIgnoreCase)
                .Take(maxEntries)
                .Select(static candidate => candidate.Entry),
        ];
    }

    public static IReadOnlyList<GameDataQuickPickEntry> BuildTechSkillEntries(
        string? filterText,
        int maxEntries = DefaultMaxEntries
    )
    {
        var normalizedFilter = Normalize(filterText);
        var searchTokens = Tokenize(filterText);
        return
        [
            .. SpellTechCatalog
                .EnumerateTechSkillNames()
                .Select(
                    (name, index) =>
                        new RankedQuickPickEntry<GameDataQuickPickEntry>(
                            CreateTechSkillEntry(index, name),
                            ScoreNamedEntry(
                                [name, index.ToString(CultureInfo.InvariantCulture), $"tech skill {name}"],
                                normalizedFilter,
                                searchTokens
                            )
                        )
                )
                .Where(static candidate => candidate.Score >= 0)
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.TitleText, StringComparer.OrdinalIgnoreCase)
                .Take(maxEntries)
                .Select(static candidate => candidate.Entry),
        ];
    }

    private static IReadOnlyList<GameDataQuickPickEntry> BuildCombinedEntries(
        IReadOnlyList<PrototypePaletteEntry> prototypeEntries,
        IReadOnlyList<StaticObjectCatalogEntry> staticObjectEntries,
        string? filterText,
        int maxEntries,
        Func<PrototypePaletteEntry, bool> prototypePredicate,
        Func<StaticObjectCatalogEntry, bool> staticObjectPredicate,
        Func<PrototypePaletteEntry, GameDataQuickPickEntry> createPrototypeEntry,
        Func<StaticObjectCatalogEntry, GameDataQuickPickEntry> createStaticObjectEntry
    )
    {
        ArgumentNullException.ThrowIfNull(prototypeEntries);
        ArgumentNullException.ThrowIfNull(staticObjectEntries);
        ArgumentNullException.ThrowIfNull(prototypePredicate);
        ArgumentNullException.ThrowIfNull(staticObjectPredicate);
        ArgumentNullException.ThrowIfNull(createPrototypeEntry);
        ArgumentNullException.ThrowIfNull(createStaticObjectEntry);

        var normalizedFilter = Normalize(filterText);
        var searchTokens = Tokenize(filterText);
        return
        [
            .. prototypeEntries
                .Where(prototypePredicate)
                .Select(entry => new RankedQuickPickEntry<GameDataQuickPickEntry>(
                    createPrototypeEntry(entry),
                    ScorePrototypeEntry(entry, normalizedFilter, searchTokens)
                ))
                .Concat(
                    staticObjectEntries
                        .Where(staticObjectPredicate)
                        .Select(entry => new RankedQuickPickEntry<GameDataQuickPickEntry>(
                            createStaticObjectEntry(entry),
                            ScoreStaticObjectEntry(entry, normalizedFilter, searchTokens)
                        ))
                )
                .Where(static candidate => candidate.Score >= 0)
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.BadgeText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.TitleText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.ApplyValueText, StringComparer.OrdinalIgnoreCase)
                .Take(maxEntries)
                .Select(static candidate => candidate.Entry),
        ];
    }

    private static IReadOnlyList<GameDataQuickPickEntry> BuildPrototypeEntries(
        IReadOnlyList<PrototypePaletteEntry> entries,
        string? filterText,
        int maxEntries,
        Func<PrototypePaletteEntry, bool> predicate,
        Func<PrototypePaletteEntry, GameDataQuickPickEntry>? createEntry = null
    )
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(predicate);

        var normalizedFilter = Normalize(filterText);
        var searchTokens = Tokenize(filterText);
        var resolvedCreateEntry = createEntry ?? (static entry => CreatePrototypeEntry(entry));
        return
        [
            .. entries
                .Where(predicate)
                .Select(entry => new RankedQuickPickEntry<GameDataQuickPickEntry>(
                    resolvedCreateEntry(entry),
                    ScorePrototypeEntry(entry, normalizedFilter, searchTokens)
                ))
                .Where(static candidate => candidate.Score >= 0)
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.BadgeText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.TitleText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.ApplyValueText, StringComparer.OrdinalIgnoreCase)
                .Select(static candidate => candidate.Entry)
                .Take(maxEntries),
        ];
    }

    private static IReadOnlyList<GameDataQuickPickEntry> BuildStaticObjectPrototypeEntries(
        IReadOnlyList<StaticObjectCatalogEntry> entries,
        string? filterText,
        int maxEntries,
        Func<StaticObjectCatalogEntry, bool> predicate
    )
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(predicate);

        var normalizedFilter = Normalize(filterText);
        var searchTokens = Tokenize(filterText);
        return
        [
            .. entries
                .Where(predicate)
                .Select(entry => new RankedQuickPickEntry<GameDataQuickPickEntry>(
                    CreateStaticObjectPrototypeEntry(entry),
                    ScoreStaticObjectEntry(entry, normalizedFilter, searchTokens)
                ))
                .Where(static candidate => candidate.Score >= 0)
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.BadgeText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.TitleText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.EntryKey, StringComparer.OrdinalIgnoreCase)
                .Select(static candidate => candidate.Entry)
                .Take(maxEntries),
        ];
    }

    private static GameDataQuickPickEntry CreatePrototypeEntry(
        PrototypePaletteEntry entry,
        string? sourceBadgePrefix = null
    )
    {
        List<string> detailParts = [];
        if (!string.IsNullOrWhiteSpace(entry.Description))
            detailParts.Add(entry.Description);

        detailParts.Add(entry.AssetPath);

        if (!string.IsNullOrWhiteSpace(entry.ArtAssetPath))
            detailParts.Add(entry.ArtAssetPath);

        return new GameDataQuickPickEntry(
            $"proto:{entry.ProtoNumber.ToString(CultureInfo.InvariantCulture)}",
            FormatBadgeText(entry.ObjectType, sourceBadgePrefix),
            entry.DisplayName ?? entry.AssetPath,
            $"proto:{entry.ProtoNumber.ToString(CultureInfo.InvariantCulture)}",
            string.Join(" · ", detailParts),
            [
                CreateValueOption(
                    "proto",
                    "Proto token",
                    $"proto:{entry.ProtoNumber.ToString(CultureInfo.InvariantCulture)}"
                ),
            ],
            null,
            null
        );
    }

    private static GameDataQuickPickEntry CreateSchematicEntry(PrototypePaletteEntry entry)
    {
        List<string> detailParts = [];
        if (!string.IsNullOrWhiteSpace(entry.Description))
            detailParts.Add(entry.Description);

        detailParts.Add(entry.AssetPath);

        if (!string.IsNullOrWhiteSpace(entry.ArtAssetPath))
            detailParts.Add(entry.ArtAssetPath);

        return new GameDataQuickPickEntry(
            $"schematic:{entry.ProtoNumber.ToString(CultureInfo.InvariantCulture)}",
            entry.ObjectType,
            entry.DisplayName ?? entry.AssetPath,
            $"Schematic id {entry.ProtoNumber.ToString(CultureInfo.InvariantCulture)}",
            string.Join(" · ", detailParts),
            [
                CreateValueOption(
                    "schematic-id",
                    "Schematic id",
                    entry.ProtoNumber.ToString(CultureInfo.InvariantCulture)
                ),
            ],
            null,
            null
        );
    }

    private static GameDataQuickPickEntry CreateWorldEntry(WorldMapCatalogEntry entry)
    {
        var detailText =
            string.IsNullOrWhiteSpace(entry.MapSummaryText) ? entry.Description ?? string.Empty
            : string.IsNullOrWhiteSpace(entry.Description) ? entry.MapSummaryText
            : $"{entry.MapSummaryText} · {entry.Description}";
        return new GameDataQuickPickEntry(
            $"area:{entry.AreaId.ToString(CultureInfo.InvariantCulture)}",
            "World",
            entry.DisplayName,
            $"Area {entry.AreaId.ToString(CultureInfo.InvariantCulture)} · {entry.CoordinateText}",
            detailText,
            [
                CreateValueOption(
                    "world-coordinates",
                    "World coordinates",
                    $"{entry.WorldX.ToString(CultureInfo.InvariantCulture)}, {entry.WorldY.ToString(CultureInfo.InvariantCulture)}",
                    "Applies the X/Y pair into the guided teleport destination fields."
                ),
            ],
            entry.WorldX,
            entry.WorldY
        );
    }

    private static GameDataQuickPickEntry CreateTileArtEntry(TileArtCatalogEntry entry)
    {
        List<string> detailParts = [entry.SummaryText];
        if (!string.IsNullOrWhiteSpace(entry.AssetPath))
            detailParts.Add(entry.AssetPath);

        return new GameDataQuickPickEntry(
            $"tile-art:{entry.ArtIdValue.ToString(CultureInfo.InvariantCulture)}",
            entry.ArtTypeText,
            entry.DisplayName,
            $"AID {entry.ArtIdText}",
            string.Join(" · ", detailParts.Where(static part => !string.IsNullOrWhiteSpace(part))),
            [
                CreateValueOption(
                    "art-id",
                    "Art id",
                    entry.ArtIdText,
                    "Canonical art id from the local tile-art palette."
                ),
            ],
            null,
            null
        );
    }

    private static GameDataQuickPickEntry CreateStaticObjectPrototypeEntry(
        StaticObjectCatalogEntry entry,
        string? sourceBadgePrefix = null
    )
    {
        List<string> detailParts =
        [
            entry.SourceKindText,
            entry.SourceAssetPath,
            entry.LocationText,
            entry.ObjectIdText,
        ];
        if (!string.IsNullOrWhiteSpace(entry.ObjectGuidText))
            detailParts.Add(entry.ObjectGuidText);

        return new GameDataQuickPickEntry(
            $"static:{entry.SourceAssetPath}|{entry.ObjectIdText}",
            FormatBadgeText(entry.ObjectType, sourceBadgePrefix),
            string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.PrototypeText : entry.DisplayName,
            $"{entry.PrototypeText} · {entry.LocationText}",
            string.Join(" · ", detailParts),
            CreateStaticObjectValueOptions(entry),
            null,
            null
        );
    }

    private static string CreatePreferredStaticObjectToken(StaticObjectCatalogEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.ObjectGuidText) ? entry.ObjectGuidText : entry.ObjectIdText;

    private static string FormatBadgeText(string objectType, string? sourceBadgePrefix) =>
        string.IsNullOrWhiteSpace(sourceBadgePrefix) ? objectType : $"{sourceBadgePrefix} · {objectType}";

    private static GameDataQuickPickEntry CreateSpellEntry(SpellDescriptor spell) =>
        new(
            $"spell:{spell.Id.ToString(CultureInfo.InvariantCulture)}",
            "Spell",
            spell.Name,
            $"Spell {spell.Id.ToString(CultureInfo.InvariantCulture)} · {spell.CollegeName} {spell.Level.ToString(CultureInfo.InvariantCulture)}",
            $"Spell school {spell.CollegeName} · rank {spell.Level.ToString(CultureInfo.InvariantCulture)}",
            [CreateValueOption("spell-token", "Spell token", spell.Name)],
            null,
            null
        );

    private static GameDataQuickPickEntry CreateSpellCollegeEntry(int collegeId, string name) =>
        new(
            $"spell-college:{collegeId.ToString(CultureInfo.InvariantCulture)}",
            "College",
            name,
            $"College {collegeId.ToString(CultureInfo.InvariantCulture)}",
            "Applies one canonical spell-college token into the live rank editor.",
            [CreateValueOption("college-token", "College token", name)],
            null,
            null
        );

    private static GameDataQuickPickEntry CreateTechDisciplineEntry(int disciplineId, string name) =>
        new(
            $"tech-discipline:{disciplineId.ToString(CultureInfo.InvariantCulture)}",
            "Discipline",
            name,
            $"Discipline {disciplineId.ToString(CultureInfo.InvariantCulture)}",
            "Applies one canonical tech-discipline token into the live degree editor.",
            [CreateValueOption("discipline-token", "Discipline token", name)],
            null,
            null
        );

    private static GameDataQuickPickEntry CreateTechSkillEntry(int skillId, string name) =>
        new(
            $"tech-skill:{skillId.ToString(CultureInfo.InvariantCulture)}",
            "Skill",
            name,
            $"Skill {skillId.ToString(CultureInfo.InvariantCulture)}",
            "Applies one canonical tech-skill token into the live points editor.",
            [CreateValueOption("skill-token", "Skill token", name)],
            null,
            null
        );

    private static IReadOnlyList<GameDataQuickPickValueOption> CreateStaticObjectValueOptions(
        StaticObjectCatalogEntry entry
    )
    {
        List<GameDataQuickPickValueOption> options = [];
        if (!string.IsNullOrWhiteSpace(entry.ObjectGuidText))
        {
            options.Add(
                CreateValueOption(
                    "guid-token",
                    "Exact GUID token",
                    entry.ObjectGuidText,
                    "Recommended exact workspace token when this placed object exposes a GUID."
                )
            );
        }

        if (!string.IsNullOrWhiteSpace(entry.ObjectIdText))
        {
            options.Add(
                CreateValueOption(
                    "object-id",
                    "Object id",
                    entry.ObjectIdText,
                    string.IsNullOrWhiteSpace(entry.ObjectGuidText)
                        ? "Recommended exact workspace object id from the source asset."
                        : "Alternative workspace object id from the same placed object.",
                    isRecommended: string.IsNullOrWhiteSpace(entry.ObjectGuidText)
                )
            );
        }

        if (entry.ProtoNumber is int protoNumber and > 0)
        {
            options.Add(
                CreateValueOption(
                    "proto-token",
                    "Proto token",
                    $"proto:{protoNumber.ToString(CultureInfo.InvariantCulture)}",
                    "Palette-backed prototype token resolved from this placed object's prototype.",
                    isRecommended: false
                )
            );
        }

        return options;
    }

    private static GameDataQuickPickValueOption CreateValueOption(
        string optionKey,
        string labelText,
        string valueText,
        string? hintText = null,
        bool isRecommended = true
    ) => new(optionKey, labelText, valueText, isRecommended, hintText ?? $"Applies {valueText} into the target field.");

    private static int ScorePrototypeEntry(
        PrototypePaletteEntry entry,
        string normalizedFilter,
        IReadOnlyList<string> searchTokens
    )
    {
        if (normalizedFilter.Length == 0)
            return 0;

        var candidateTexts = EnumeratePrototypeSearchTexts(entry)
            .Select(Normalize)
            .Where(static text => text.Length != 0);
        return ScoreTexts(candidateTexts, normalizedFilter, searchTokens);
    }

    private static int ScoreWorldEntry(
        WorldMapCatalogEntry entry,
        string normalizedFilter,
        IReadOnlyList<string> searchTokens
    )
    {
        if (normalizedFilter.Length == 0)
            return 0;

        IEnumerable<string> candidateTexts = EnumerateWorldSearchTexts(entry)
            .Select(Normalize)
            .Where(static text => text.Length != 0);
        return ScoreTexts(candidateTexts, normalizedFilter, searchTokens);
    }

    private static int ScoreStaticObjectEntry(
        StaticObjectCatalogEntry entry,
        string normalizedFilter,
        IReadOnlyList<string> searchTokens
    )
    {
        if (normalizedFilter.Length == 0)
            return 0;

        IEnumerable<string> candidateTexts = EnumerateStaticObjectSearchTexts(entry)
            .Select(Normalize)
            .Where(static text => text.Length != 0);
        return ScoreTexts(candidateTexts, normalizedFilter, searchTokens);
    }

    private static int ScoreNamedEntry(
        IEnumerable<string> candidateTexts,
        string normalizedFilter,
        IReadOnlyList<string> searchTokens
    )
    {
        if (normalizedFilter.Length == 0)
            return 0;

        return ScoreTexts(
            candidateTexts.Select(Normalize).Where(static text => text.Length != 0),
            normalizedFilter,
            searchTokens
        );
    }

    private static int ScoreTexts(
        IEnumerable<string> candidateTexts,
        string normalizedFilter,
        IReadOnlyList<string> searchTokens
    )
    {
        var texts = candidateTexts.ToArray();
        if (texts.Length == 0)
            return -1;

        foreach (var text in texts)
        {
            if (text == normalizedFilter)
                return 0;
        }

        foreach (var text in texts)
        {
            if (text.StartsWith(normalizedFilter, StringComparison.Ordinal))
                return 1;
        }

        foreach (var text in texts)
        {
            if (text.Contains(normalizedFilter, StringComparison.Ordinal))
                return 2;
        }

        if (
            searchTokens.Count != 0
            && searchTokens.All(token => texts.Any(text => text.Contains(token, StringComparison.Ordinal)))
        )
        {
            return 3;
        }

        return -1;
    }

    private static IEnumerable<string> EnumeratePrototypeSearchTexts(PrototypePaletteEntry entry)
    {
        yield return entry.ProtoNumber.ToString(CultureInfo.InvariantCulture);
        yield return entry.ObjectType;
        yield return entry.AssetPath;

        if (!string.IsNullOrWhiteSpace(entry.DisplayName))
            yield return entry.DisplayName;

        if (!string.IsNullOrWhiteSpace(entry.Description))
            yield return entry.Description;

        if (!string.IsNullOrWhiteSpace(entry.PaletteGroup))
            yield return entry.PaletteGroup;

        if (!string.IsNullOrWhiteSpace(entry.ArtAssetPath))
            yield return entry.ArtAssetPath;
    }

    private static IEnumerable<string> EnumerateWorldSearchTexts(WorldMapCatalogEntry entry)
    {
        yield return entry.AreaId.ToString(CultureInfo.InvariantCulture);
        yield return entry.DisplayName;
        yield return entry.CoordinateText;
        yield return entry.MapSummaryText;

        if (!string.IsNullOrWhiteSpace(entry.Description))
            yield return entry.Description;

        foreach (var mapName in entry.MapNames)
            yield return mapName;
    }

    private static IEnumerable<string> EnumerateStaticObjectSearchTexts(StaticObjectCatalogEntry entry)
    {
        yield return entry.DisplayName;
        yield return entry.ObjectType;
        yield return entry.ObjectIdText;
        yield return entry.PrototypeText;
        yield return entry.SourceAssetPath;
        yield return entry.LocationText;
        yield return entry.SourceKindText;
        yield return entry.SummaryText;

        if (entry.ProtoNumber is int protoNumber and > 0)
            yield return protoNumber.ToString(CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(entry.ObjectGuidText))
            yield return entry.ObjectGuidText;
    }

    private static IEnumerable<string> EnumerateSpellSearchTexts(SpellDescriptor spell)
    {
        yield return spell.Id.ToString(CultureInfo.InvariantCulture);
        yield return spell.Name;
        yield return spell.CollegeName;
        yield return $"{spell.CollegeName} {spell.Level.ToString(CultureInfo.InvariantCulture)}";
        yield return $"spell {spell.Name}";
    }

    private static IEnumerable<string> EnumerateTileArtSearchTexts(TileArtCatalogEntry entry)
    {
        yield return entry.ArtIdText;
        yield return entry.DisplayName;
        yield return entry.ArtTypeText;
        yield return entry.AssetPath;
        yield return entry.SummaryText;
        yield return entry.ArtNumber.ToString(CultureInfo.InvariantCulture);
        yield return entry.FrameIndex.ToString(CultureInfo.InvariantCulture);
        yield return entry.PaletteIndex.ToString(CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<string> Tokenize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            :
            [
                .. value
                    .Split(
                        [' ', '\t', '_', '-', '/', '\\', ',', ';', ':'],
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(Normalize)
                    .Where(static token => token.Length != 0)
                    .Distinct(StringComparer.Ordinal),
            ];

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (!char.IsLetterOrDigit(ch))
                continue;

            buffer[count++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..count]);
    }

    private static bool IsInventoryObjectType(string objectType) =>
        objectType
            is "Weapon"
                or "Ammo"
                or "Armor"
                or "Gold"
                or "Food"
                or "Scroll"
                or "Key"
                or "KeyRing"
                or "Written"
                or "Generic";

    private const int DefaultMaxEntries = 24;

    private sealed record class RankedQuickPickEntry<TEntry>(TEntry Entry, int Score);
}
