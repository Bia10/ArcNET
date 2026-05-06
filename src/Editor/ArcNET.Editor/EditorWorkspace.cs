using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using ArcNET.Archive;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// High-level editor session that composes loose game data and an optional save slot into
/// one frontend-facing SDK surface.
/// </summary>
/// <remarks>
/// This type is intentionally UI-agnostic. Desktop, web, and CLI frontends can bind to one
/// workspace instance instead of manually coordinating <see cref="GameDataLoader"/>,
/// <see cref="SaveGameLoader"/>, <see cref="GameDataStore"/>, and <see cref="LoadedSave"/>.
/// The workspace may be backed either by a loose/extracted content directory or by a real
/// game installation whose DAT archives are read directly.
/// </remarks>
public sealed class EditorWorkspace
{
    private const long DefaultMaxLoadedArtRetainedBytes = 64L * 1024L * 1024L;
    private const int DefaultMaxLoadedArtEntryCount = 256;
    private readonly object _artBindingCacheGate = new();
    private readonly object _loadedArtCacheGate = new();
    private readonly object _objectPaletteCacheGate = new();
    private readonly ConcurrentDictionary<string, Lazy<ArtFile?>> _loadedArtsByPath = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly RetainedCacheBudget<string> _loadedArtCacheBudget = new(
        StringComparer.OrdinalIgnoreCase,
        DefaultMaxLoadedArtRetainedBytes,
        DefaultMaxLoadedArtEntryCount
    );
    private readonly Dictionary<
        ObjectPaletteCacheKey,
        IReadOnlyList<EditorObjectPaletteEntry>
    > _cachedObjectPaletteEntries = [];
    private readonly Dictionary<
        SelectedObjectPaletteEntryCacheKey,
        EditorObjectPaletteEntry?
    > _cachedObjectPaletteEntriesByProto = [];
    private static readonly char[] s_eyeCandyTypeCodes = ['F', 'B', 'U'];
    private static readonly char[] s_critterGenderCodes = ['F', 'M', 'X'];
    private static readonly char[] s_tileEdgeCodes =
    [
        '0',
        '6',
        'b',
        '4',
        '8',
        '9',
        '2',
        '3',
        '7',
        'e',
        'a',
        '5',
        'd',
        'c',
        '1',
        '0',
    ];
    private static readonly int[] s_tileEdgeDecodeWhenFlagsClear = [0, 1, 8, 3, 4, 5, 6, 7, 8, 3, 10, 11, 6, 7, 14, 15];
    private static readonly int[] s_tileEdgeDecodeWhenFlagsSet =
    [
        0,
        1,
        2,
        9,
        4,
        5,
        12,
        13,
        2,
        9,
        10,
        11,
        12,
        13,
        14,
        15,
    ];
    private static readonly string[] s_critterBodyTypeCodes = ["HM", "DF", "GH", "HG", "EF"];
    private static readonly string[] s_critterArmorTypeCodes = ["UW", "V1", "LA", "CM", "PM", "RB", "PC", "BN", "CD"];
    private static readonly char[] s_critterShieldCodes = ['X', 'S'];
    private static readonly char[] s_critterWeaponTypeCodes =
    [
        'A',
        'B',
        'C',
        'D',
        'E',
        'F',
        'G',
        'H',
        'I',
        'J',
        'K',
        'X',
        'Y',
        'N',
        'Z',
    ];
    private static readonly string[] s_wallPieceSuffixes =
    [
        "bse",
        "lfc",
        "bse",
        "bcl",
        "bcr",
        "tcl",
        "tcr",
        "uec",
        "lec",
        "w3l",
        "w3a",
        "w3r",
        "w4l",
        "w4a",
        "w4b",
        "w4r",
        "w5l",
        "w5a",
        "w5b",
        "w5c",
        "w5r",
        "d3l",
        "d3a",
        "d3r",
        "d4l",
        "d4a",
        "d4b",
        "d4r",
        "d6l",
        "d6a",
        "d6b",
        "d6c",
        "d6d",
        "d6r",
        "p3l",
        "p3a",
        "p3r",
        "p4l",
        "p4a",
        "p4b",
        "p4r",
        "p5l",
        "p5a",
        "p5b",
        "p5c",
        "p5r",
    ];
    private const uint WallArtTypeMask = 0x10000000u;
    private const int CritterBodyTypeHuman = 0;
    private const int CritterBodyTypeDwarf = 1;
    private const int CritterBodyTypeHalfling = 2;
    private const int CritterBodyTypeElf = 4;
    private const int CritterGenderFemale = 0;
    private const int CritterGenderMale = 1;
    private const int CritterArmorTypePlate = 4;
    private const int CritterArmorTypePlateClassic = 6;
    private const int CritterWeaponTypeUnarmed = 1;
    private const int CritterWeaponTypeSword = 3;
    private const int CritterWeaponTypeTwoHandedSword = 7;
    private const int CritterAnimationWalk = 1;
    private const int CritterAnimationStealthWalk = 3;
    private const int CritterAnimationConcealFidget = 5;
    private const int CritterAnimationRun = 6;
    private const int CritterAnimationSeveredLeg = 19;
    private const int CritterAnimationStunned = 23;
    private const int CritterAnimationExplode = 24;
    private const int ItemTypeArmor = 2;
    private const int ItemArmorCoverageTorso = 0;
    private IReadOnlyDictionary<uint, string>? _conservativeArtBindings;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<int, string>> _messageEntriesByIndexByAssetPath =
        new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>>? _directMessageTableArtAssetPaths;
    private IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>? _eyeCandyArtAssetPathsByMessageIndex;
    private TileNameLookupData? _tileNameLookupData;
    private WallArtLookupData? _wallArtLookupData;

    /// <summary>
    /// Loose or extracted content directory associated with this workspace.
    /// For install-backed workspaces this is the conventional <c>data</c> override directory
    /// under <see cref="GameDirectory"/>, even when the actual content was also loaded from DAT archives.
    /// </summary>
    public required string ContentDirectory { get; init; }

    /// <summary>
    /// Optional Arcanum installation root used for installation-type detection and
    /// future install-root services.
    /// </summary>
    public string? GameDirectory { get; init; }

    /// <summary>
    /// Optional module-backed context used when the workspace was loaded for one specific module instead of one
    /// whole-install aggregate.
    /// </summary>
    public EditorWorkspaceModuleContext? Module { get; init; }

    /// <summary>
    /// Installation type detected from <see cref="GameDirectory"/>, or <see langword="null"/>
    /// when the workspace was loaded without an installation root.
    /// </summary>
    public ArcanumInstallationType? InstallationType { get; init; }

    /// <summary>
    /// Parsed loose game data used by frontend editors, browsers, and validation tools.
    /// </summary>
    public required GameDataStore GameData { get; init; }

    /// <summary>
    /// Read-only catalog of parsed game-data assets and their winning provenance.
    /// </summary>
    public EditorAssetCatalog Assets { get; init; } = EditorAssetCatalog.Empty;

    /// <summary>
    /// Read-only catalog of loaded audio assets and their winning provenance.
    /// </summary>
    public EditorAudioAssetCatalog AudioAssets { get; init; } = EditorAudioAssetCatalog.Empty;

    /// <summary>
    /// Higher-level workspace index for message ownership, proto lookup, and reverse proto references.
    /// </summary>
    public EditorAssetIndex Index { get; init; } = EditorAssetIndex.Empty;

    /// <summary>
    /// Diagnostics captured while the workspace was loaded.
    /// </summary>
    public EditorWorkspaceLoadReport LoadReport { get; init; } = EditorWorkspaceLoadReport.Empty;

    /// <summary>
    /// Cross-file validation findings derived from the indexed workspace assets.
    /// </summary>
    public EditorWorkspaceValidationReport Validation { get; init; } = EditorWorkspaceValidationReport.Empty;

    /// <summary>
    /// Optional loaded save slot associated with this workspace.
    /// </summary>
    public LoadedSave? Save { get; init; }

    /// <summary>
    /// Save directory used when <see cref="Save"/> was loaded.
    /// </summary>
    public string? SaveFolder { get; init; }

    /// <summary>
    /// Save slot name used when <see cref="Save"/> was loaded.
    /// </summary>
    public string? SaveSlotName { get; init; }

    /// <summary>
    /// Total number of full ART payloads currently retained by the workspace ART cache.
    /// </summary>
    public int LoadedArtCacheEntryCount
    {
        get
        {
            lock (_loadedArtCacheGate)
                return _loadedArtCacheBudget.EntryCount;
        }
    }

    /// <summary>
    /// Approximate retained bytes currently held by the workspace ART cache.
    /// </summary>
    public long LoadedArtCacheRetainedBytes
    {
        get
        {
            lock (_loadedArtCacheGate)
                return _loadedArtCacheBudget.RetainedBytes;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when this workspace includes a loaded save slot.
    /// </summary>
    public bool HasSaveLoaded => Save is not null;

    /// <summary>
    /// Creates an empty persisted editor-project model seeded with this workspace reference.
    /// </summary>
    public EditorProject CreateProject() => EditorProject.FromWorkspace(this);

    /// <summary>
    /// Returns a stable capability-discovery snapshot for the current SDK build and loaded workspace.
    /// Hosts can use this to light up editor features without guessing which backend slices are present.
    /// </summary>
    public EditorCapabilitySummary GetCapabilities() => EditorCapabilitySummary.Create(this);

    /// <summary>
    /// Creates a live mutable editor session on top of this loaded workspace snapshot.
    /// Hosts can reuse the returned session to keep transactional dialog, script, and save editors alive
    /// while tracking dirty state across the current workspace.
    /// </summary>
    public EditorWorkspaceSession CreateSession() => new(this);

    /// <summary>
    /// Creates a workspace-owned ART resolver that can bind known <see cref="ArtId"/> values to loaded ART asset paths.
    /// </summary>
    public EditorArtResolver CreateArtResolver() => new(this);

    /// <summary>
    /// Creates a workspace-owned ART resolver asynchronously.
    /// </summary>
    public Task<EditorArtResolver> CreateArtResolverAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateArtResolver());
    }

    /// <summary>
    /// Creates a workspace-owned ART resolver that resolves workspace-backed bindings on demand using the supplied strategy.
    /// </summary>
    public EditorArtResolver CreateArtResolver(EditorArtResolverBindingStrategy bindingStrategy) =>
        CreateArtResolver(bindingStrategy, CancellationToken.None);

    /// <summary>
    /// Creates a workspace-owned ART resolver asynchronously using the supplied binding strategy.
    /// </summary>
    public Task<EditorArtResolver> CreateArtResolverAsync(
        EditorArtResolverBindingStrategy bindingStrategy,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateArtResolver(bindingStrategy));
    }

    internal EditorArtResolver CreateArtResolver(
        EditorArtResolverBindingStrategy bindingStrategy,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new EditorArtResolver(this, bindingStrategy);
    }

    internal bool TryResolveArtAssetPath(
        ArtId artId,
        EditorArtResolverBindingStrategy bindingStrategy,
        out string assetPath
    )
    {
        switch (bindingStrategy)
        {
            case EditorArtResolverBindingStrategy.None:
                assetPath = string.Empty;
                return false;
            case EditorArtResolverBindingStrategy.Conservative:
                return GetOrCreateConservativeArtBindings().TryGetValue(artId.Value, out assetPath!);
            case EditorArtResolverBindingStrategy.ArcanumMessageTables:
                if (TryResolveArcanumMessageTableArtAssetPathCore(artId, out assetPath))
                    return true;

                return GetOrCreateConservativeArtBindings().TryGetValue(artId.Value, out assetPath!);
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(bindingStrategy),
                    bindingStrategy,
                    "Unknown art binding strategy."
                );
        }
    }

    /// <summary>
    /// Creates one workspace-backed sprite source that resolves bound <see cref="ArtId"/> values into cached paintable frames.
    /// </summary>
    public EditorWorkspaceMapRenderSpriteSource CreateMapRenderSpriteSource(
        EditorArtResolver artResolver,
        EditorArtPreviewOptions? previewOptions = null
    ) => new(this, artResolver, previewOptions);

    /// <summary>
    /// Creates one workspace-backed sprite source using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public EditorWorkspaceMapRenderSpriteSource CreateMapRenderSpriteSource(
        EditorArtResolverBindingStrategy bindingStrategy,
        EditorArtPreviewOptions? previewOptions = null
    ) => new(this, CreateArtResolver(bindingStrategy), previewOptions);

    /// <summary>
    /// Creates one workspace-backed sprite source asynchronously using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public async Task<EditorWorkspaceMapRenderSpriteSource> CreateMapRenderSpriteSourceAsync(
        EditorArtResolverBindingStrategy bindingStrategy,
        EditorArtPreviewOptions? previewOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        var resolver = await CreateArtResolverAsync(bindingStrategy, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return CreateMapRenderSpriteSource(resolver, previewOptions);
    }

    /// <summary>
    /// Resolves one host-facing default map for this workspace or loaded module.
    /// Returns <see langword="null"/> when the workspace has no indexed maps.
    /// </summary>
    public EditorWorkspaceDefaultMap? ResolveDefaultMap()
    {
        if (Index.MapNames.Count == 0)
            return null;

        if (TryResolveSaveLinkedDefaultMap(out var saveLinkedMap))
            return saveLinkedMap;

        var conventionalMap = Index.MapNames.FirstOrDefault(mapName =>
            string.Equals(mapName, "map01", StringComparison.OrdinalIgnoreCase)
        );
        if (conventionalMap is not null)
        {
            return new EditorWorkspaceDefaultMap
            {
                MapName = conventionalMap,
                Source = EditorWorkspaceDefaultMapSource.ConventionalMap01,
                SaveMapId = Save?.Info.MapId,
            };
        }

        if (Index.MapNames.Count == 1)
        {
            return new EditorWorkspaceDefaultMap
            {
                MapName = Index.MapNames[0],
                Source = EditorWorkspaceDefaultMapSource.SingleIndexedMap,
                SaveMapId = Save?.Info.MapId,
            };
        }

        return new EditorWorkspaceDefaultMap
        {
            MapName = Index.MapNames[0],
            Source = EditorWorkspaceDefaultMapSource.FirstIndexedMap,
            SaveMapId = Save?.Info.MapId,
        };
    }

    internal IReadOnlyDictionary<string, ReadOnlyMemory<byte>> AudioAssetData { get; init; } =
        new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a stateful <see cref="SaveGameEditor"/> for the loaded save slot.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the workspace was loaded without a save slot.
    /// </exception>
    public SaveGameEditor CreateSaveEditor()
    {
        if (Save is null)
        {
            throw new InvalidOperationException(
                "This workspace was loaded without a save slot. Provide SaveFolder and SaveSlotName to EditorWorkspaceLoader first."
            );
        }

        return new SaveGameEditor(Save);
    }

    /// <summary>
    /// Looks up one loaded compiled script by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public ScrFile? FindScript(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return GameData.ScriptsBySource.TryGetValue(normalizedPath, out var scripts) ? scripts.FirstOrDefault() : null;
    }

    /// <summary>
    /// Looks up one loaded dialog file by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public DlgFile? FindDialog(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return GameData.DialogsBySource.TryGetValue(normalizedPath, out var dialogs) ? dialogs.FirstOrDefault() : null;
    }

    /// <summary>
    /// Looks up one loaded message file by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public MesFile? FindMessageFile(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return GameData.MessagesBySource.TryGetValue(normalizedPath, out var entries)
            ? new MesFile { Entries = [.. entries] }
            : null;
    }

    /// <summary>
    /// Looks up browser-friendly detail for one loaded message asset by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public EditorMessageDefinition? FindMessageDetail(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return
            Assets.Find(normalizedPath) is { Format: FileFormat.Message } asset
            && GameData.MessagesBySource.TryGetValue(normalizedPath, out var entries)
            ? CreateMessageDetail(asset, entries)
            : null;
    }

    /// <summary>
    /// Returns message details whose asset path, entry indexes, or entry text contain the supplied text.
    /// </summary>
    public IReadOnlyList<EditorMessageDefinition> SearchMessageDetails(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var searchText = text.Trim();
        return Assets
            .FindByFormat(FileFormat.Message)
            .Where(asset =>
                GameData.MessagesBySource.TryGetValue(asset.AssetPath, out var entries)
                && MessageMatches(asset, entries, searchText)
            )
            .Select(asset => CreateMessageDetail(asset, GameData.MessagesBySource[asset.AssetPath]))
            .OrderBy(detail => detail.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Looks up one loaded prototype file by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public ProtoData? FindProto(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return GameData.ProtosBySource.TryGetValue(normalizedPath, out var protos) ? protos.FirstOrDefault() : null;
    }

    /// <summary>
    /// Looks up one proto-backed object palette entry by proto number.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectPaletteEntry? FindObjectPaletteEntry(int protoNumber)
    {
        var asset = Index.FindProtoDefinition(protoNumber);
        if (asset is null)
            return null;

        var proto = FindProto(asset.AssetPath);
        return proto is null ? null : CreateObjectPaletteEntry(asset, proto, protoNumber);
    }

    /// <summary>
    /// Looks up one host-facing object/proto inspector summary by proto number.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectInspectorSummary? FindObjectInspectorSummary(int protoNumber)
    {
        var asset = Index.FindProtoDefinition(protoNumber);
        if (asset is null)
            return null;

        var proto = FindProto(asset.AssetPath);
        if (proto is null)
            return null;

        var entry = CreateObjectPaletteEntry(asset, proto, protoNumber);
        return new EditorObjectInspectorSummary
        {
            TargetKind = EditorObjectInspectorTargetKind.ProtoDefinition,
            Proto = entry,
            ProtoNumber = protoNumber,
            TargetObjectType = entry.ObjectType,
            Panes = EditorObjectInspectorPaneSummary.CreateList(
                EditorObjectInspectorTargetKind.ProtoDefinition,
                entry.ObjectType
            ),
        };
    }

    /// <summary>
    /// Looks up one typed flags-pane inspector contract by proto number.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectInspectorFlagsSummary? FindObjectInspectorFlagsSummary(int protoNumber)
    {
        var inspector = FindObjectInspectorSummary(protoNumber);
        if (inspector?.Proto is null)
            return null;

        var proto = FindProto(inspector.Proto.Asset.AssetPath);
        return proto is null ? null : EditorObjectInspectorFlagsSummary.Create(inspector, proto.Properties);
    }

    /// <summary>
    /// Looks up one typed critter-progression inspector contract by proto number.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectInspectorCritterProgressionSummary? FindObjectInspectorCritterProgressionSummary(int protoNumber)
    {
        var inspector = FindObjectInspectorSummary(protoNumber);
        if (inspector?.Proto is null)
            return null;

        var proto = FindProto(inspector.Proto.Asset.AssetPath);
        return proto is null
            ? null
            : EditorObjectInspectorCritterProgressionSummary.Create(inspector, proto.Properties);
    }

    /// <summary>
    /// Looks up one typed light-pane inspector contract by proto number.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectInspectorLightSummary? FindObjectInspectorLightSummary(int protoNumber)
    {
        var inspector = FindObjectInspectorSummary(protoNumber);
        if (inspector?.Proto is null)
            return null;

        var proto = FindProto(inspector.Proto.Asset.AssetPath);
        return proto is null ? null : EditorObjectInspectorLightSummary.Create(inspector, proto.Properties);
    }

    /// <summary>
    /// Looks up one typed generator-pane inspector contract by proto number.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectInspectorGeneratorSummary? FindObjectInspectorGeneratorSummary(int protoNumber)
    {
        var inspector = FindObjectInspectorSummary(protoNumber);
        if (inspector?.Proto is null)
            return null;

        var proto = FindProto(inspector.Proto.Asset.AssetPath);
        return proto is null ? null : EditorObjectInspectorGeneratorSummary.Create(inspector, proto.Properties);
    }

    /// <summary>
    /// Looks up one typed blending-pane inspector contract by proto number.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectInspectorBlendingSummary? FindObjectInspectorBlendingSummary(int protoNumber)
    {
        var inspector = FindObjectInspectorSummary(protoNumber);
        if (inspector?.Proto is null)
            return null;

        var proto = FindProto(inspector.Proto.Asset.AssetPath);
        return proto is null ? null : EditorObjectInspectorBlendingSummary.Create(inspector, proto.Properties);
    }

    /// <summary>
    /// Looks up one typed script-attachments inspector contract by proto number.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectInspectorScriptAttachmentsSummary? FindObjectInspectorScriptAttachmentsSummary(int protoNumber)
    {
        var inspector = FindObjectInspectorSummary(protoNumber);
        if (inspector?.Proto is null)
            return null;

        var proto = FindProto(inspector.Proto.Asset.AssetPath);
        return proto is null
            ? null
            : EditorObjectInspectorScriptAttachmentsSummary.Create(
                inspector,
                proto.Properties,
                CreateObjectInspectorScriptReference
            );
    }

    /// <summary>
    /// Looks up one proto-backed object palette entry by proto number and resolves optional ART bindings.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectPaletteEntry? FindObjectPaletteEntry(int protoNumber, EditorArtResolver artResolver)
    {
        ArgumentNullException.ThrowIfNull(artResolver);

        var asset = Index.FindProtoDefinition(protoNumber);
        if (asset is null)
            return null;

        var proto = FindProto(asset.AssetPath);
        return proto is null ? null : CreateObjectPaletteEntry(asset, proto, protoNumber, artResolver);
    }

    /// <summary>
    /// Looks up one proto-backed object palette entry by proto number and resolves optional ART bindings
    /// using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public EditorObjectPaletteEntry? FindObjectPaletteEntry(
        int protoNumber,
        EditorArtResolverBindingStrategy artBindingStrategy
    ) => GetOrCreateCachedObjectPaletteEntry(protoNumber, artBindingStrategy);

    /// <summary>
    /// Looks up one proto-backed object palette entry by proto number and resolves ART binding,
    /// browser-friendly detail, and preview payload for the bound art when available.
    /// Returns <see langword="null"/> when no matching proto asset was loaded.
    /// </summary>
    public EditorObjectPaletteEntry? FindObjectPaletteEntry(
        int protoNumber,
        EditorArtResolver artResolver,
        EditorArtPreviewOptions artPreviewOptions
    )
    {
        ArgumentNullException.ThrowIfNull(artResolver);
        ArgumentNullException.ThrowIfNull(artPreviewOptions);

        var asset = Index.FindProtoDefinition(protoNumber);
        if (asset is null)
            return null;

        var proto = FindProto(asset.AssetPath);
        return proto is null
            ? null
            : CreateObjectPaletteEntry(asset, proto, protoNumber, artResolver, artPreviewOptions);
    }

    /// <summary>
    /// Looks up one proto-backed object palette entry by proto number and resolves ART bindings,
    /// browser-friendly detail, and preview payload using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public EditorObjectPaletteEntry? FindObjectPaletteEntry(
        int protoNumber,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions artPreviewOptions
    ) => FindObjectPaletteEntry(protoNumber, CreateArtResolver(artBindingStrategy), artPreviewOptions);

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> GetObjectPalette() =>
        BuildObjectPaletteEntries(artResolver: null, artPreviewOptions: null, searchText: null, CancellationToken.None);

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order asynchronously.
    /// </summary>
    public Task<IReadOnlyList<EditorObjectPaletteEntry>> GetObjectPaletteAsync(
        CancellationToken cancellationToken = default
    ) =>
        Task.Run(
            () =>
                BuildObjectPaletteEntries(
                    artResolver: null,
                    artPreviewOptions: null,
                    searchText: null,
                    cancellationToken
                ),
            cancellationToken
        );

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order and resolves optional ART bindings.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> GetObjectPalette(EditorArtResolver artResolver)
    {
        ArgumentNullException.ThrowIfNull(artResolver);

        return BuildObjectPaletteEntries(
            artResolver,
            artPreviewOptions: null,
            searchText: null,
            CancellationToken.None
        );
    }

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order asynchronously and resolves optional ART bindings.
    /// </summary>
    public Task<IReadOnlyList<EditorObjectPaletteEntry>> GetObjectPaletteAsync(
        EditorArtResolver artResolver,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(artResolver);
        return Task.Run(
            () => BuildObjectPaletteEntries(artResolver, artPreviewOptions: null, searchText: null, cancellationToken),
            cancellationToken
        );
    }

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order and resolves optional ART bindings
    /// using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> GetObjectPalette(
        EditorArtResolverBindingStrategy artBindingStrategy
    ) => GetOrCreateCachedObjectPaletteEntries(artBindingStrategy, searchText: null);

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order asynchronously using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public async Task<IReadOnlyList<EditorObjectPaletteEntry>> GetObjectPaletteAsync(
        EditorArtResolverBindingStrategy artBindingStrategy,
        CancellationToken cancellationToken = default
    )
    {
        if (TryGetCachedObjectPaletteEntries(artBindingStrategy, searchText: null, out var cachedEntries))
            return cachedEntries;

        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() => GetObjectPalette(artBindingStrategy), cancellationToken).ConfigureAwait(false);
    }

    private EditorObjectInspectorScriptReference? CreateObjectInspectorScriptReference(int scriptId)
    {
        if (scriptId <= 0)
            return null;

        var asset = Index.FindScriptDefinition(scriptId);
        if (asset is null)
            return null;

        var script = FindScript(asset.AssetPath);
        return script is null ? null : EditorObjectInspectorScriptReference.Create(asset, script, scriptId);
    }

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order and enriches bound entries with
    /// browser-friendly ART detail plus preview payload.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> GetObjectPalette(
        EditorArtResolver artResolver,
        EditorArtPreviewOptions artPreviewOptions
    )
    {
        ArgumentNullException.ThrowIfNull(artResolver);
        ArgumentNullException.ThrowIfNull(artPreviewOptions);

        return BuildObjectPaletteEntries(artResolver, artPreviewOptions, searchText: null, CancellationToken.None);
    }

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order asynchronously and enriches bound entries with browser-friendly ART detail plus preview payload.
    /// </summary>
    public Task<IReadOnlyList<EditorObjectPaletteEntry>> GetObjectPaletteAsync(
        EditorArtResolver artResolver,
        EditorArtPreviewOptions artPreviewOptions,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(artResolver);
        ArgumentNullException.ThrowIfNull(artPreviewOptions);
        return Task.Run(
            () => BuildObjectPaletteEntries(artResolver, artPreviewOptions, searchText: null, cancellationToken),
            cancellationToken
        );
    }

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order and enriches bound entries with
    /// browser-friendly ART detail plus preview payload using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> GetObjectPalette(
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions artPreviewOptions
    ) => GetObjectPalette(CreateArtResolver(artBindingStrategy), artPreviewOptions);

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order asynchronously and enriches bound entries with browser-friendly ART detail plus preview payload using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public async Task<IReadOnlyList<EditorObjectPaletteEntry>> GetObjectPaletteAsync(
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions artPreviewOptions,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(artPreviewOptions);
        var artResolver = await CreateArtResolverAsync(artBindingStrategy, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await GetObjectPaletteAsync(artResolver, artPreviewOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns proto-backed object palette entries whose proto number, asset path, display name,
    /// description, or object type contain the supplied text.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> SearchObjectPalette(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return BuildObjectPaletteEntries(
            artResolver: null,
            artPreviewOptions: null,
            text.Trim(),
            CancellationToken.None
        );
    }

    /// <summary>
    /// Returns proto-backed object palette entries asynchronously whose proto number, asset path, display name, description, or object type contain the supplied text.
    /// </summary>
    public Task<IReadOnlyList<EditorObjectPaletteEntry>> SearchObjectPaletteAsync(
        string text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return Task.Run(
            () => BuildObjectPaletteEntries(artResolver: null, artPreviewOptions: null, text.Trim(), cancellationToken),
            cancellationToken
        );
    }

    /// <summary>
    /// Returns proto-backed object palette entries whose proto number, asset path, display name,
    /// description, grouping, bound art path, or object type contain the supplied text.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> SearchObjectPalette(string text, EditorArtResolver artResolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(artResolver);

        return BuildObjectPaletteEntries(artResolver, artPreviewOptions: null, text.Trim(), CancellationToken.None);
    }

    /// <summary>
    /// Returns proto-backed object palette entries asynchronously whose proto number, asset path, display name, description, grouping, bound art path, or object type contain the supplied text.
    /// </summary>
    public Task<IReadOnlyList<EditorObjectPaletteEntry>> SearchObjectPaletteAsync(
        string text,
        EditorArtResolver artResolver,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(artResolver);
        return Task.Run(
            () => BuildObjectPaletteEntries(artResolver, artPreviewOptions: null, text.Trim(), cancellationToken),
            cancellationToken
        );
    }

    /// <summary>
    /// Returns proto-backed object palette entries whose proto number, asset path, display name,
    /// description, grouping, bound art path, or object type contain the supplied text using one
    /// workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> SearchObjectPalette(
        string text,
        EditorArtResolverBindingStrategy artBindingStrategy
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return GetOrCreateCachedObjectPaletteEntries(artBindingStrategy, text.Trim());
    }

    /// <summary>
    /// Returns proto-backed object palette entries asynchronously using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public async Task<IReadOnlyList<EditorObjectPaletteEntry>> SearchObjectPaletteAsync(
        string text,
        EditorArtResolverBindingStrategy artBindingStrategy,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (TryGetCachedObjectPaletteEntries(artBindingStrategy, text.Trim(), out var cachedEntries))
            return cachedEntries;

        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() => SearchObjectPalette(text, artBindingStrategy), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns proto-backed object palette entries whose proto number, asset path, display name,
    /// description, grouping, bound art path, or object type contain the supplied text, and enriches
    /// bound entries with browser-friendly ART detail plus preview payload.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> SearchObjectPalette(
        string text,
        EditorArtResolver artResolver,
        EditorArtPreviewOptions artPreviewOptions
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(artResolver);
        ArgumentNullException.ThrowIfNull(artPreviewOptions);

        return BuildObjectPaletteEntries(artResolver, artPreviewOptions, text.Trim(), CancellationToken.None);
    }

    /// <summary>
    /// Returns proto-backed object palette entries asynchronously and enriches bound entries with browser-friendly ART detail plus preview payload.
    /// </summary>
    public Task<IReadOnlyList<EditorObjectPaletteEntry>> SearchObjectPaletteAsync(
        string text,
        EditorArtResolver artResolver,
        EditorArtPreviewOptions artPreviewOptions,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(artResolver);
        ArgumentNullException.ThrowIfNull(artPreviewOptions);
        return Task.Run(
            () => BuildObjectPaletteEntries(artResolver, artPreviewOptions, text.Trim(), cancellationToken),
            cancellationToken
        );
    }

    /// <summary>
    /// Returns proto-backed object palette entries whose proto number, asset path, display name,
    /// description, grouping, bound art path, or object type contain the supplied text, and enriches
    /// bound entries with browser-friendly ART detail plus preview payload using one workspace-created
    /// resolver seeded with the supplied strategy.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> SearchObjectPalette(
        string text,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions artPreviewOptions
    ) => SearchObjectPalette(text, CreateArtResolver(artBindingStrategy), artPreviewOptions);

    /// <summary>
    /// Returns proto-backed object palette entries asynchronously and enriches bound entries with browser-friendly ART detail plus preview payload using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public async Task<IReadOnlyList<EditorObjectPaletteEntry>> SearchObjectPaletteAsync(
        string text,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions artPreviewOptions,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(artPreviewOptions);
        var artResolver = await CreateArtResolverAsync(artBindingStrategy, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await SearchObjectPaletteAsync(text, artResolver, artPreviewOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a transactional dialog editor from one loaded dialog asset path.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no dialog asset with the supplied path was loaded into this workspace.
    /// </exception>
    public DialogEditor CreateDialogEditor(string assetPath)
    {
        var dialog = FindDialog(assetPath);
        if (dialog is null)
            throw new InvalidOperationException($"No loaded dialog asset matched '{NormalizeAssetPath(assetPath)}'.");

        return new DialogEditor(dialog);
    }

    /// <summary>
    /// Creates a transactional script editor from one loaded script asset path.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no script asset with the supplied path was loaded into this workspace.
    /// </exception>
    public ScriptEditor CreateScriptEditor(string assetPath)
    {
        var script = FindScript(assetPath);
        if (script is null)
            throw new InvalidOperationException($"No loaded script asset matched '{NormalizeAssetPath(assetPath)}'.");

        return new ScriptEditor(script);
    }

    /// <summary>
    /// Looks up one loaded sector by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public Sector? FindSector(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return GameData.SectorsBySource.TryGetValue(normalizedPath, out var sectors) ? sectors.FirstOrDefault() : null;
    }

    /// <summary>
    /// Looks up one loaded jump file by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public JmpFile? FindJumpFile(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return GameData.JumpFilesBySource.TryGetValue(normalizedPath, out var jumpFiles)
            ? jumpFiles.FirstOrDefault()
            : null;
    }

    /// <summary>
    /// Looks up one loaded map-properties file by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public MapProperties? FindMapProperties(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return GameData.MapPropertiesBySource.TryGetValue(normalizedPath, out var properties)
            ? properties.FirstOrDefault()
            : null;
    }

    /// <summary>
    /// Looks up one loaded audio asset by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public EditorAudioAssetEntry? FindAudioAsset(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        return AudioAssets.Find(assetPath);
    }

    /// <summary>
    /// Looks up browser-friendly detail for one loaded audio asset by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public EditorAudioDefinition? FindAudioDetail(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return AudioAssets.Find(normalizedPath) is { } asset ? CreateAudioDetail(asset) : null;
    }

    /// <summary>
    /// Returns audio details whose asset path contains the supplied text.
    /// </summary>
    public IReadOnlyList<EditorAudioDefinition> SearchAudioDetails(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var searchText = text.Trim();
        return AudioAssets
            .Entries.Where(asset => asset.AssetPath.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Select(CreateAudioDetail)
            .OrderBy(detail => detail.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Looks up one loaded ART sprite file by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public ArtFile? FindArt(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        var asset = Assets.Find(normalizedPath);
        if (asset is not { Format: FileFormat.Art })
            return null;

        if (GameData.ArtsBySource.TryGetValue(normalizedPath, out var arts) && arts.FirstOrDefault() is { } storedArt)
        {
            if (!storedArt.IsMetadataOnly)
                return storedArt;
        }

        var loadedArt = _loadedArtsByPath
            .GetOrAdd(
                normalizedPath,
                static (path, workspace) =>
                    new Lazy<ArtFile?>(() => workspace.LoadFullArt(path), LazyThreadSafetyMode.ExecutionAndPublication),
                this
            )
            .Value;

        TrackLoadedArt(normalizedPath, loadedArt);
        return loadedArt;
    }

    internal bool HasLoadedArtAsset(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        return Assets.Find(NormalizeAssetPath(assetPath)) is { Format: FileFormat.Art };
    }

    private ArtFile? LoadFullArt(string assetPath)
    {
        var asset = Assets.Find(assetPath);
        if (asset is not { Format: FileFormat.Art })
            return null;

        return asset.SourceKind switch
        {
            EditorAssetSourceKind.LooseFile => ArtFormat.ParseFile(asset.SourcePath),
            EditorAssetSourceKind.DatArchive => LoadArtFromArchive(asset),
            _ => throw new InvalidOperationException(
                $"Unsupported ART asset source kind '{asset.SourceKind}' for '{asset.AssetPath}'."
            ),
        };
    }

    private void TrackLoadedArt(string assetPath, ArtFile? art)
    {
        lock (_loadedArtCacheGate)
        {
            if (art is null)
            {
                _loadedArtCacheBudget.TryTouch(assetPath);
                return;
            }

            var evictedPaths = _loadedArtCacheBudget.Register(assetPath, EstimateRetainedBytes(art));
            for (var index = 0; index < evictedPaths.Count; index++)
                _loadedArtsByPath.TryRemove(evictedPaths[index], out _);
        }
    }

    private static long EstimateRetainedBytes(ArtFile art)
    {
        ArgumentNullException.ThrowIfNull(art);

        long retainedBytes = 0L;
        for (var paletteIndex = 0; paletteIndex < art.Palettes.Length; paletteIndex++)
        {
            if (art.Palettes[paletteIndex] is { } palette)
                retainedBytes += palette.Length * 4L;
        }

        for (var rotationIndex = 0; rotationIndex < art.Frames.Length; rotationIndex++)
        {
            var rotationFrames = art.Frames[rotationIndex];
            for (var frameIndex = 0; frameIndex < rotationFrames.Length; frameIndex++)
                retainedBytes += rotationFrames[frameIndex].Pixels.Length;
        }

        return Math.Max(retainedBytes, 1L);
    }

    private ArtFile LoadArtFromArchive(EditorAssetEntry asset)
    {
        if (string.IsNullOrWhiteSpace(asset.SourceEntryPath))
        {
            throw new InvalidOperationException(
                $"Archive-backed ART asset '{asset.AssetPath}' did not record a source entry path."
            );
        }

        using var archive = DatArchive.Open(asset.SourcePath);
        return ArtFormat.ParseMemory(archive.GetEntryData(asset.SourceEntryPath));
    }

    internal ArtFile? ResolveMapRenderArt(ArtId artId)
    {
        if (artId.Value == 0u)
            return null;

        return TryResolveMapRenderArtAssetPath(artId, renderItemKind: null, out var assetPath)
            ? FindArt(assetPath)
            : null;
    }

    /// <summary>
    /// Looks up one loaded terrain-definition file by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public TerrainData? FindTerrain(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return GameData.TerrainsBySource.TryGetValue(normalizedPath, out var terrains)
            ? terrains.FirstOrDefault()
            : null;
    }

    /// <summary>
    /// Returns all terrain palette entries derived from one loaded map-properties asset.
    /// </summary>
    public IReadOnlyList<EditorTerrainPaletteEntry> GetTerrainPalette(string mapPropertiesAssetPath) =>
        GetTerrainPalette(mapPropertiesAssetPath, artResolver: null, artPreviewOptions: null);

    /// <summary>
    /// Returns all terrain palette entries derived from one loaded map-properties asset
    /// using one workspace-created ART resolver seeded with the supplied strategy.
    /// </summary>
    public IReadOnlyList<EditorTerrainPaletteEntry> GetTerrainPalette(
        string mapPropertiesAssetPath,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions? artPreviewOptions = null
    ) => GetTerrainPalette(mapPropertiesAssetPath, CreateArtResolver(artBindingStrategy), artPreviewOptions);

    /// <summary>
    /// Returns all terrain palette entries asynchronously derived from one loaded map-properties asset using one workspace-created ART resolver seeded with the supplied strategy.
    /// </summary>
    public async Task<IReadOnlyList<EditorTerrainPaletteEntry>> GetTerrainPaletteAsync(
        string mapPropertiesAssetPath,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions? artPreviewOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapPropertiesAssetPath);
        var artResolver = await CreateArtResolverAsync(artBindingStrategy, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await GetTerrainPaletteAsync(mapPropertiesAssetPath, artResolver, artPreviewOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns all terrain palette entries derived from one loaded map-properties asset and enriches
    /// entries with optional ART binding and preview data.
    /// </summary>
    public IReadOnlyList<EditorTerrainPaletteEntry> GetTerrainPalette(
        string mapPropertiesAssetPath,
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapPropertiesAssetPath);

        return BuildTerrainPaletteEntries(
            mapPropertiesAssetPath,
            artResolver,
            artPreviewOptions,
            CancellationToken.None
        );
    }

    /// <summary>
    /// Returns all terrain palette entries asynchronously derived from one loaded map-properties asset and enriches entries with optional ART binding and preview data.
    /// </summary>
    public Task<IReadOnlyList<EditorTerrainPaletteEntry>> GetTerrainPaletteAsync(
        string mapPropertiesAssetPath,
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapPropertiesAssetPath);
        return Task.Run(
            () => BuildTerrainPaletteEntries(mapPropertiesAssetPath, artResolver, artPreviewOptions, cancellationToken),
            cancellationToken
        );
    }

    private IReadOnlyList<EditorTerrainPaletteEntry> BuildTerrainPaletteEntries(
        string mapPropertiesAssetPath,
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedPath = NormalizeAssetPath(mapPropertiesAssetPath);
        var asset = Assets.Find(normalizedPath);
        var properties = FindMapProperties(normalizedPath);
        if (asset is null || properties is null)
            return [];

        if (!TryGetTerrainPaletteEntryCount(properties, out var entryCount))
            return [];

        if (entryCount == 0)
            return [];

        var entries = new EditorTerrainPaletteEntry[entryCount];
        var index = 0;
        for (ulong paletteY = 0; paletteY < properties.LimitY; paletteY++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (ulong paletteX = 0; paletteX < properties.LimitX; paletteX++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = CreateTerrainPaletteEntry(
                    asset,
                    properties,
                    paletteX,
                    paletteY,
                    artResolver,
                    artPreviewOptions
                );

                if (entry is null)
                    return [];

                entries[index] = entry;
                index++;
            }
        }

        return entries;
    }

    /// <summary>
    /// Returns all terrain palette entries for one map's conventional <c>map.prp</c> asset.
    /// Returns an empty result when no matching map-properties asset was loaded.
    /// </summary>
    public IReadOnlyList<EditorTerrainPaletteEntry> GetTerrainPaletteForMap(string mapName) =>
        GetTerrainPalette(ResolveMapPropertiesAssetPath(mapName));

    /// <summary>
    /// Returns all terrain palette entries asynchronously for one map's conventional <c>map.prp</c> asset.
    /// </summary>
    public Task<IReadOnlyList<EditorTerrainPaletteEntry>> GetTerrainPaletteForMapAsync(
        string mapName,
        CancellationToken cancellationToken = default
    ) =>
        GetTerrainPaletteAsync(
            ResolveMapPropertiesAssetPath(mapName),
            artResolver: null,
            artPreviewOptions: null,
            cancellationToken
        );

    /// <summary>
    /// Returns all terrain palette entries for one map's conventional <c>map.prp</c> asset
    /// using one workspace-created ART resolver seeded with the supplied strategy.
    /// Returns an empty result when no matching map-properties asset was loaded.
    /// </summary>
    public IReadOnlyList<EditorTerrainPaletteEntry> GetTerrainPaletteForMap(
        string mapName,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions? artPreviewOptions = null
    ) => GetTerrainPalette(ResolveMapPropertiesAssetPath(mapName), artBindingStrategy, artPreviewOptions);

    /// <summary>
    /// Returns all terrain palette entries asynchronously for one map's conventional <c>map.prp</c> asset using one workspace-created ART resolver seeded with the supplied strategy.
    /// </summary>
    public Task<IReadOnlyList<EditorTerrainPaletteEntry>> GetTerrainPaletteForMapAsync(
        string mapName,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions? artPreviewOptions = null,
        CancellationToken cancellationToken = default
    ) =>
        GetTerrainPaletteAsync(
            ResolveMapPropertiesAssetPath(mapName),
            artBindingStrategy,
            artPreviewOptions,
            cancellationToken
        );

    /// <summary>
    /// Returns all terrain palette entries whose asset path, palette coordinates, or derived ART identifier
    /// contain the supplied text.
    /// </summary>
    public IReadOnlyList<EditorTerrainPaletteEntry> SearchTerrainPalette(string text) =>
        SearchTerrainPalette(text, artResolver: null, artPreviewOptions: null);

    /// <summary>
    /// Returns all terrain palette entries asynchronously whose asset path, palette coordinates, or derived ART identifier contain the supplied text.
    /// </summary>
    public Task<IReadOnlyList<EditorTerrainPaletteEntry>> SearchTerrainPaletteAsync(
        string text,
        CancellationToken cancellationToken = default
    ) => SearchTerrainPaletteAsync(text, artResolver: null, artPreviewOptions: null, cancellationToken);

    /// <summary>
    /// Returns all terrain palette entries whose asset path, palette coordinates, or derived ART identifier
    /// contain the supplied text, using one workspace-created ART resolver seeded with the supplied strategy.
    /// </summary>
    public IReadOnlyList<EditorTerrainPaletteEntry> SearchTerrainPalette(
        string text,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions? artPreviewOptions = null
    ) => SearchTerrainPalette(text, CreateArtResolver(artBindingStrategy), artPreviewOptions);

    /// <summary>
    /// Returns all terrain palette entries asynchronously whose asset path, palette coordinates, or derived ART identifier contain the supplied text using one workspace-created ART resolver seeded with the supplied strategy.
    /// </summary>
    public async Task<IReadOnlyList<EditorTerrainPaletteEntry>> SearchTerrainPaletteAsync(
        string text,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions? artPreviewOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var artResolver = await CreateArtResolverAsync(artBindingStrategy, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await SearchTerrainPaletteAsync(text, artResolver, artPreviewOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns all terrain palette entries whose asset path, palette coordinates, or derived ART identifier
    /// contain the supplied text and enriches entries with optional ART binding and preview data.
    /// </summary>
    public IReadOnlyList<EditorTerrainPaletteEntry> SearchTerrainPalette(
        string text,
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return BuildTerrainPaletteSearchEntries(text, artResolver, artPreviewOptions, CancellationToken.None);
    }

    /// <summary>
    /// Returns all terrain palette entries asynchronously whose asset path, palette coordinates, or derived ART identifier contain the supplied text and enriches entries with optional ART binding and preview data.
    /// </summary>
    public Task<IReadOnlyList<EditorTerrainPaletteEntry>> SearchTerrainPaletteAsync(
        string text,
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return Task.Run(
            () => BuildTerrainPaletteSearchEntries(text, artResolver, artPreviewOptions, cancellationToken),
            cancellationToken
        );
    }

    private IReadOnlyList<EditorTerrainPaletteEntry> BuildTerrainPaletteSearchEntries(
        string text,
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var searchText = text.Trim();
        List<EditorTerrainPaletteEntry> results = [];
        foreach (var asset in Assets.FindByFormat(FileFormat.MapProperties))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var paletteEntries = BuildTerrainPaletteEntries(
                asset.AssetPath,
                artResolver,
                artPreviewOptions,
                cancellationToken
            );
            for (var entryIndex = 0; entryIndex < paletteEntries.Count; entryIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = paletteEntries[entryIndex];
                if (
                    entry.Asset.AssetPath.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || entry
                        .ArtId.Value.ToString(CultureInfo.InvariantCulture)
                        .Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || entry
                        .PaletteX.ToString(CultureInfo.InvariantCulture)
                        .Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || entry
                        .PaletteY.ToString(CultureInfo.InvariantCulture)
                        .Contains(searchText, StringComparison.OrdinalIgnoreCase)
                )
                {
                    results.Add(entry);
                }
            }
        }

        return results
            .OrderBy(entry => entry.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.PaletteY)
            .ThenBy(entry => entry.PaletteX)
            .ToArray();
    }

    private IReadOnlyList<EditorObjectPaletteEntry> BuildObjectPaletteEntries(
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions,
        string? searchText,
        CancellationToken cancellationToken
    )
    {
        List<EditorObjectPaletteEntry> entries = [];

        foreach (var asset in Assets.FindByFormat(FileFormat.Proto))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var proto = FindProto(asset.AssetPath);
            if (proto is null || !TryGetProtoNumberFromAssetPath(asset.AssetPath, out var protoNumber))
                continue;

            EditorObjectPaletteEntry entry = artResolver switch
            {
                null when artPreviewOptions is null => CreateObjectPaletteEntry(asset, proto, protoNumber),
                not null when artPreviewOptions is null => CreateObjectPaletteEntry(
                    asset,
                    proto,
                    protoNumber,
                    artResolver
                ),
                not null => CreateObjectPaletteEntry(asset, proto, protoNumber, artResolver, artPreviewOptions!),
                _ => CreateObjectPaletteEntry(asset, proto, protoNumber),
            };

            if (searchText is null || ObjectPaletteEntryMatches(entry, searchText))
                entries.Add(entry);
        }

        return entries
            .OrderBy(entry => entry.ProtoNumber)
            .ThenBy(entry => entry.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private bool TryGetCachedObjectPaletteEntries(
        EditorArtResolverBindingStrategy artBindingStrategy,
        string? searchText,
        out IReadOnlyList<EditorObjectPaletteEntry> entries
    )
    {
        lock (_objectPaletteCacheGate)
        {
            if (
                _cachedObjectPaletteEntries.TryGetValue(
                    new ObjectPaletteCacheKey(artBindingStrategy, NormalizeObjectPaletteSearchCacheKey(searchText)),
                    out var cachedEntries
                )
            )
            {
                entries = cachedEntries;
                return true;
            }

            entries = [];
            return false;
        }
    }

    private IReadOnlyList<EditorObjectPaletteEntry> GetOrCreateCachedObjectPaletteEntries(
        EditorArtResolverBindingStrategy artBindingStrategy,
        string? searchText
    )
    {
        var cacheKey = new ObjectPaletteCacheKey(artBindingStrategy, NormalizeObjectPaletteSearchCacheKey(searchText));

        lock (_objectPaletteCacheGate)
        {
            if (_cachedObjectPaletteEntries.TryGetValue(cacheKey, out var cachedEntries))
                return cachedEntries;

            var entries = BuildObjectPaletteEntries(
                CreateArtResolver(artBindingStrategy),
                artPreviewOptions: null,
                NormalizeObjectPaletteSearchText(searchText),
                CancellationToken.None
            );
            _cachedObjectPaletteEntries.Add(cacheKey, entries);
            return entries;
        }
    }

    private EditorObjectPaletteEntry? GetOrCreateCachedObjectPaletteEntry(
        int protoNumber,
        EditorArtResolverBindingStrategy artBindingStrategy
    )
    {
        var cacheKey = new SelectedObjectPaletteEntryCacheKey(protoNumber, artBindingStrategy);
        lock (_objectPaletteCacheGate)
        {
            if (_cachedObjectPaletteEntriesByProto.TryGetValue(cacheKey, out var cachedEntry))
                return cachedEntry;

            var entry = FindObjectPaletteEntry(protoNumber, CreateArtResolver(artBindingStrategy));
            _cachedObjectPaletteEntriesByProto.Add(cacheKey, entry);
            return entry;
        }
    }

    private static string? NormalizeObjectPaletteSearchText(string? searchText) =>
        string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim();

    private static string? NormalizeObjectPaletteSearchCacheKey(string? searchText) =>
        string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToUpperInvariant();

    /// <summary>
    /// Looks up one terrain palette entry by source map-properties asset path and palette coordinates.
    /// Returns <see langword="null"/> when the asset was not loaded or the coordinates are outside the palette bounds.
    /// </summary>
    public EditorTerrainPaletteEntry? FindTerrainPaletteEntry(
        string mapPropertiesAssetPath,
        ulong paletteX,
        ulong paletteY
    ) =>
        FindTerrainPaletteEntry(mapPropertiesAssetPath, paletteX, paletteY, artResolver: null, artPreviewOptions: null);

    /// <summary>
    /// Looks up one terrain palette entry by source map-properties asset path and palette coordinates
    /// using one workspace-created ART resolver seeded with the supplied strategy.
    /// Returns <see langword="null"/> when the asset was not loaded or the coordinates are outside the palette bounds.
    /// </summary>
    public EditorTerrainPaletteEntry? FindTerrainPaletteEntry(
        string mapPropertiesAssetPath,
        ulong paletteX,
        ulong paletteY,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions? artPreviewOptions = null
    ) =>
        FindTerrainPaletteEntry(
            mapPropertiesAssetPath,
            paletteX,
            paletteY,
            CreateArtResolver(artBindingStrategy),
            artPreviewOptions
        );

    /// <summary>
    /// Looks up one terrain palette entry by source map-properties asset path and palette coordinates and enriches
    /// the entry with optional ART binding and preview data.
    /// Returns <see langword="null"/> when the asset was not loaded or the coordinates are outside the palette bounds.
    /// </summary>
    public EditorTerrainPaletteEntry? FindTerrainPaletteEntry(
        string mapPropertiesAssetPath,
        ulong paletteX,
        ulong paletteY,
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapPropertiesAssetPath);

        var normalizedPath = NormalizeAssetPath(mapPropertiesAssetPath);
        var asset = Assets.Find(normalizedPath);
        var properties = FindMapProperties(normalizedPath);
        if (asset is null || properties is null || paletteX >= properties.LimitX || paletteY >= properties.LimitY)
            return null;

        return CreateTerrainPaletteEntry(asset, properties, paletteX, paletteY, artResolver, artPreviewOptions);
    }

    /// <summary>
    /// Looks up one terrain palette entry using persisted terrain-tool state.
    /// Returns <see langword="null"/> when the persisted state does not currently resolve to one loaded palette entry.
    /// </summary>
    public EditorTerrainPaletteEntry? FindTerrainPaletteEntry(EditorProjectMapTerrainToolState toolState)
    {
        ArgumentNullException.ThrowIfNull(toolState);

        return string.IsNullOrWhiteSpace(toolState.MapPropertiesAssetPath)
            ? null
            : FindTerrainPaletteEntry(toolState.MapPropertiesAssetPath, toolState.PaletteX, toolState.PaletteY);
    }

    /// <summary>
    /// Looks up one terrain palette entry using persisted terrain-tool state and enriches the entry with
    /// optional ART binding and preview data.
    /// Returns <see langword="null"/> when the persisted state does not currently resolve to one loaded palette entry.
    /// </summary>
    public EditorTerrainPaletteEntry? FindTerrainPaletteEntry(
        EditorProjectMapTerrainToolState toolState,
        EditorArtResolverBindingStrategy artBindingStrategy,
        EditorArtPreviewOptions? artPreviewOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(toolState);

        return string.IsNullOrWhiteSpace(toolState.MapPropertiesAssetPath)
            ? null
            : FindTerrainPaletteEntry(
                toolState.MapPropertiesAssetPath,
                toolState.PaletteX,
                toolState.PaletteY,
                artBindingStrategy,
                artPreviewOptions
            );
    }

    /// <summary>
    /// Looks up one loaded facade-walkability file by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public FacadeWalk? FindFacadeWalk(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return GameData.FacadeWalksBySource.TryGetValue(normalizedPath, out var facadeWalks)
            ? facadeWalks.FirstOrDefault()
            : null;
    }

    /// <summary>
    /// Builds an editor ART preview from one loaded asset path.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no ART asset with the supplied path was loaded into this workspace.
    /// </exception>
    public EditorArtPreview CreateArtPreview(string assetPath, EditorArtPreviewOptions? options = null)
    {
        var art = FindArt(assetPath);
        if (art is null)
            throw new InvalidOperationException($"No loaded ART asset matched '{NormalizeAssetPath(assetPath)}'.");

        return EditorArtPreviewBuilder.Build(art, options);
    }

    /// <summary>
    /// Builds a richer scene preview for one indexed map using the loaded sector payloads.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the workspace has no indexed map projection for <paramref name="mapName"/>,
    /// or when one projected sector no longer has a loaded sector payload.
    /// </exception>
    public EditorMapScenePreview CreateMapScenePreview(string mapName) =>
        CreateMapScenePreview(mapName, (Func<ArtId, ArtFile?>?)null);

    /// <summary>
    /// Builds a richer scene preview for one indexed map using the workspace-owned ART resolver bindings.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the workspace has no indexed map projection for <paramref name="mapName"/>,
    /// or when one projected sector no longer has a loaded sector payload.
    /// </exception>
    public EditorMapScenePreview CreateMapScenePreview(string mapName, EditorArtResolver artResolver)
    {
        ArgumentNullException.ThrowIfNull(artResolver);
        return CreateMapScenePreview(mapName, artResolver.FindArt);
    }

    /// <summary>
    /// Builds a richer scene preview for one indexed map using one workspace-created ART resolver
    /// seeded with the supplied strategy.
    /// </summary>
    public EditorMapScenePreview CreateMapScenePreview(
        string mapName,
        EditorArtResolverBindingStrategy artBindingStrategy
    ) => CreateMapScenePreview(mapName, CreateArtResolver(artBindingStrategy));

    /// <summary>
    /// Builds a richer scene preview for one indexed map using the loaded sectors that back its asset paths.
    /// When <paramref name="artResolver"/> is provided, placed objects also receive conservative sprite-bounds metadata
    /// derived from the resolved ART frames.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the workspace has no indexed map projection for <paramref name="mapName"/>,
    /// or when one projected sector no longer has a loaded sector payload.
    /// </exception>
    public EditorMapScenePreview CreateMapScenePreview(string mapName, Func<ArtId, ArtFile?>? artResolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapName);

        var projection = Index.FindMapProjection(mapName);
        if (projection is null)
            throw new InvalidOperationException($"No indexed map projection matched '{mapName}'.");

        var sectorsByAssetPath = new Dictionary<string, Sector>(StringComparer.OrdinalIgnoreCase);
        foreach (var sectorProjection in projection.Sectors)
        {
            var sector = FindSector(sectorProjection.Asset.AssetPath);
            if (sector is null)
            {
                throw new InvalidOperationException(
                    $"No loaded sector payload matched '{sectorProjection.Asset.AssetPath}' for map '{mapName}'."
                );
            }

            sectorsByAssetPath[sectorProjection.Asset.AssetPath] = sector;
        }

        return EditorMapScenePreviewBuilder.Build(
            projection,
            sectorsByAssetPath,
            artResolver,
            ResolveSceneObjectCurrentArtIdFallback,
            GetMapSceneMobAssets(mapName)
        );
    }

    internal IReadOnlyDictionary<string, IReadOnlyList<MobData>> GetMapSceneMobAssets(string mapName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapName);

        var mobsByAssetPath = new Dictionary<string, IReadOnlyList<MobData>>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in Index.FindMapAssets(mapName))
        {
            if (asset.Format != FileFormat.Mob)
                continue;

            if (GameData.MobsBySource.TryGetValue(asset.AssetPath, out var mobs) && mobs.Count > 0)
                mobsByAssetPath[asset.AssetPath] = mobs;
        }

        return mobsByAssetPath;
    }

    internal ArtId? ResolveSceneObjectCurrentArtIdForScene(MobData mob) => ResolveSceneObjectCurrentArtIdFallback(mob);

    private ArtId? ResolveSceneObjectCurrentArtIdFallback(MobData mob)
    {
        ArgumentNullException.ThrowIfNull(mob);

        var protoNumber = mob.Header.ProtoId.GetProtoNumber();
        return
            protoNumber is int resolvedProtoNumber
            && TryResolveProtoCurrentArtId(resolvedProtoNumber, out var currentArtId)
            ? currentArtId
            : null;
    }

    /// <summary>
    /// Builds an audio preview from one loaded audio asset path.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no supported audio asset with the supplied path was loaded into this workspace.
    /// </exception>
    public EditorAudioPreview CreateAudioPreview(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        if (!AudioAssetData.TryGetValue(normalizedPath, out var audioData))
            throw new InvalidOperationException($"No loaded audio asset matched '{normalizedPath}'.");

        return EditorAudioPreviewBuilder.BuildWave(audioData, normalizedPath);
    }

    private EditorAudioDefinition CreateAudioDetail(EditorAudioAssetEntry asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var preview = CreateAudioPreview(asset.AssetPath);
        return new EditorAudioDefinition
        {
            Asset = asset,
            Encoding = preview.Encoding,
            ChannelCount = preview.ChannelCount,
            SampleRate = preview.SampleRate,
            BitsPerSample = preview.BitsPerSample,
            BlockAlign = preview.BlockAlign,
            ByteRate = preview.ByteRate,
            SampleFrameCount = preview.SampleFrameCount,
            SampleByteLength = preview.SampleData.Length,
            Duration = preview.Duration,
        };
    }

    private bool TryResolveSaveLinkedDefaultMap(out EditorWorkspaceDefaultMap? resolution)
    {
        resolution = null;

        var saveMapId = Save?.Info.MapId;
        if (!saveMapId.HasValue || saveMapId.Value < 0)
            return false;

        foreach (var candidate in EnumerateSaveMapNameCandidates(saveMapId.Value))
        {
            var matchedMap = Index.MapNames.FirstOrDefault(mapName =>
                string.Equals(mapName, candidate, StringComparison.OrdinalIgnoreCase)
            );
            if (matchedMap is null)
                continue;

            resolution = new EditorWorkspaceDefaultMap
            {
                MapName = matchedMap,
                Source = EditorWorkspaceDefaultMapSource.SaveInfoMapId,
                SaveMapId = saveMapId.Value,
            };
            return true;
        }

        return false;
    }

    private static IEnumerable<string> EnumerateSaveMapNameCandidates(int mapId)
    {
        yield return mapId.ToString(CultureInfo.InvariantCulture);
        yield return $"map{mapId:00}";
        yield return $"map{mapId:000}";
        yield return $"map{mapId:0000}";
    }

    private static string ResolveMapPropertiesAssetPath(string mapName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapName);
        return $"maps/{mapName.Trim()}/map.prp";
    }

    private IReadOnlyDictionary<uint, string> GetOrCreateConservativeArtBindings(
        CancellationToken cancellationToken = default
    )
    {
        if (_conservativeArtBindings is not null)
            return _conservativeArtBindings;

        lock (_artBindingCacheGate)
        {
            return _conservativeArtBindings ??= BuildConservativeArtBindings(cancellationToken);
        }
    }

    private IReadOnlyDictionary<uint, string> BuildConservativeArtBindings(
        CancellationToken cancellationToken = default
    )
    {
        var assetPathsByArtId = new Dictionary<uint, string?>();
        foreach (var artAsset in Assets.FindByFormat(FileFormat.Art))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidateIds = EnumerateConservativeArtBindingIds(artAsset.AssetPath).Distinct().ToArray();
            for (var candidateIndex = 0; candidateIndex < candidateIds.Length; candidateIndex++)
            {
                var candidateId = candidateIds[candidateIndex];
                if (
                    !assetPathsByArtId.TryAdd(candidateId, artAsset.AssetPath)
                    && !string.Equals(
                        assetPathsByArtId[candidateId],
                        artAsset.AssetPath,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    assetPathsByArtId[candidateId] = null;
                }
            }
        }

        return assetPathsByArtId
            .Where(static pair => pair.Key != 0u && !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value!, EqualityComparer<uint>.Default);
    }

    private IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> GetOrCreateDirectMessageTableArtAssetPaths(
        CancellationToken cancellationToken = default
    )
    {
        if (_directMessageTableArtAssetPaths is not null)
            return _directMessageTableArtAssetPaths;

        lock (_artBindingCacheGate)
        {
            return _directMessageTableArtAssetPaths ??= BuildDirectMessageTableArtAssetPaths(cancellationToken);
        }
    }

    private TileNameLookupData GetOrCreateTileNameLookupData(CancellationToken cancellationToken = default)
    {
        if (_tileNameLookupData is not null)
            return _tileNameLookupData;

        lock (_artBindingCacheGate)
        {
            return _tileNameLookupData ??= BuildTileNameLookupData(cancellationToken);
        }
    }

    private IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> GetOrCreateEyeCandyArtAssetPathsByMessageIndex(
        CancellationToken cancellationToken = default
    )
    {
        if (_eyeCandyArtAssetPathsByMessageIndex is not null)
            return _eyeCandyArtAssetPathsByMessageIndex;

        lock (_artBindingCacheGate)
        {
            return _eyeCandyArtAssetPathsByMessageIndex ??= BuildEyeCandyArtAssetPathsByMessageIndex(cancellationToken);
        }
    }

    private WallArtLookupData GetOrCreateWallArtLookupData(CancellationToken cancellationToken = default)
    {
        if (_wallArtLookupData is not null)
            return _wallArtLookupData;

        lock (_artBindingCacheGate)
        {
            return _wallArtLookupData ??= BuildWallArtLookupData(cancellationToken);
        }
    }

    private IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> BuildDirectMessageTableArtAssetPaths(
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new Dictionary<string, IReadOnlyDictionary<int, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["art/scenery/scenery.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/scenery/scenery.mes",
                "art/scenery",
                cancellationToken
            ),
            ["art/interface/interface.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/interface/interface.mes",
                "art/interface",
                cancellationToken
            ),
            ["art/item/item_ground.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/item/item_ground.mes",
                "art/item",
                cancellationToken
            ),
            ["art/item/item_inven.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/item/item_inven.mes",
                "art/item",
                cancellationToken
            ),
            ["art/item/item_paper.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/item/item_paper.mes",
                "art/item",
                cancellationToken
            ),
            ["art/item/item_schematic.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/item/item_schematic.mes",
                "art/item",
                cancellationToken
            ),
            ["art/container/container.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/container/container.mes",
                "art/container",
                cancellationToken
            ),
            ["art/facade/facadename.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/facade/facadename.mes",
                "art/facade",
                cancellationToken
            ),
            ["art/roof/roofname.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/roof/roofname.mes",
                "art/roof",
                cancellationToken
            ),
            ["art/wall/wallname.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/wall/wallname.mes",
                "art/wall",
                cancellationToken
            ),
            ["art/light/light.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/light/light.mes",
                "art/light",
                cancellationToken
            ),
            ["art/portal/portal.mes"] = BuildDirectMessageTableArtAssetPathMap(
                "art/portal/portal.mes",
                "art/portal",
                cancellationToken
            ),
        };
    }

    private WallArtLookupData BuildWallArtLookupData(CancellationToken cancellationToken = default)
    {
        var structuresByIndex = new Dictionary<int, WallStructureArtSides>();
        var structureFile = FindMessageFile("art/wall/structure.mes");
        if (structureFile is not null)
        {
            foreach (var entry in structureFile.Entries.OrderBy(static entry => entry.Index))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.Index >= 1000)
                    break;

                if (TryParseWallStructureArtSides(entry.Text, out var sides))
                    structuresByIndex[entry.Index] = sides;
            }
        }

        var defaultPaletteArtAssetPathsByProtoNumber = new Dictionary<int, string>();
        var defaultPaletteArtIdsByProtoNumber = new Dictionary<int, ArtId>();
        var wallNameFile = FindMessageFile("art/wall/wallname.mes");
        var wallProtoFile = FindMessageFile("art/wall/wallproto.mes");
        if (wallNameFile is not null && wallProtoFile is not null)
        {
            var protoNumbersByIndex = wallProtoFile.Entries.ToDictionary(
                static entry => entry.Index,
                static entry => TryParseMessageEntryInt(entry.Text, out var protoNumber) ? protoNumber : 0
            );

            foreach (var entry in wallNameFile.Entries.OrderBy(static entry => entry.Index))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var token = TryGetFirstMessageToken(entry.Text);
                if (!TryGetWallBaseName(token, out var baseName))
                    continue;

                var protoLookupIndex = 0;
                if (!string.IsNullOrWhiteSpace(token) && token.Length > 3)
                {
                    if (
                        !int.TryParse(
                            token[3..],
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out protoLookupIndex
                        )
                    )
                    {
                        continue;
                    }
                }

                if (
                    !protoNumbersByIndex.TryGetValue(protoLookupIndex, out var protoNumber)
                    || protoNumber <= 0
                    || defaultPaletteArtAssetPathsByProtoNumber.ContainsKey(protoNumber)
                    || !TryCreateDefaultWallArtId(protoLookupIndex, out var defaultArtId)
                    || !TryBuildWallArtAssetPath(baseName, 0, 0, 0, out var assetPath)
                )
                {
                    continue;
                }

                defaultPaletteArtAssetPathsByProtoNumber[protoNumber] = assetPath;
                defaultPaletteArtIdsByProtoNumber[protoNumber] = defaultArtId;
            }
        }

        return new WallArtLookupData(
            structuresByIndex,
            defaultPaletteArtAssetPathsByProtoNumber,
            defaultPaletteArtIdsByProtoNumber
        );
    }

    private TileNameLookupData BuildTileNameLookupData(CancellationToken cancellationToken = default)
    {
        var messageFile = FindMessageFile("art/tile/tilename.mes");
        if (messageFile is null)
            return TileNameLookupData.Empty;

        var orderedEntries = messageFile.Entries.OrderBy(static entry => entry.Index).ToArray();
        List<string> outdoorFlippableNames = [];
        List<string> outdoorNonFlippableNames = [];
        List<string> indoorFlippableNames = [];
        List<string> indoorNonFlippableNames = [];

        for (var entryIndex = 0; entryIndex < orderedEntries.Length; entryIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = orderedEntries[entryIndex];
            if (entry.Index is < 0 or >= 400)
                continue;

            var tileName = TryNormalizeTileNameEntry(entry.Text);
            if (string.IsNullOrWhiteSpace(tileName))
                continue;

            switch (entry.Index)
            {
                case < 100:
                    outdoorFlippableNames.Add(tileName);
                    break;
                case < 200:
                    outdoorNonFlippableNames.Add(tileName);
                    break;
                case < 300:
                    indoorFlippableNames.Add(tileName);
                    break;
                default:
                    indoorNonFlippableNames.Add(tileName);
                    break;
            }
        }

        Dictionary<string, int> outdoorOrderByName = new(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < outdoorFlippableNames.Count; index++)
        {
            outdoorOrderByName.TryAdd(outdoorFlippableNames[index], index);
        }

        for (var index = 0; index < outdoorNonFlippableNames.Count; index++)
        {
            outdoorOrderByName.TryAdd(outdoorNonFlippableNames[index], outdoorFlippableNames.Count + index);
        }

        return new TileNameLookupData(
            outdoorFlippableNames,
            outdoorNonFlippableNames,
            indoorFlippableNames,
            indoorNonFlippableNames,
            outdoorOrderByName
        );
    }

    private IReadOnlyDictionary<int, string> BuildDirectMessageTableArtAssetPathMap(
        string messageAssetPath,
        string assetRootPath,
        CancellationToken cancellationToken = default
    )
    {
        var messageFile = FindMessageFile(messageAssetPath);
        if (messageFile is null)
            return new Dictionary<int, string>();

        var assetPathsByIndex = new Dictionary<int, string>();
        for (var entryIndex = 0; entryIndex < messageFile.Entries.Count; entryIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = messageFile.Entries[entryIndex];
            if (TryResolveMessageEntryArtAssetPath(entry.Text, assetRootPath, out var assetPath))
                assetPathsByIndex[entry.Index] = assetPath;
        }

        return assetPathsByIndex;
    }

    private IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> BuildEyeCandyArtAssetPathsByMessageIndex(
        CancellationToken cancellationToken = default
    )
    {
        var messageFile = FindMessageFile("art/eye_candy/eye_candy.mes");
        if (messageFile is null)
            return new Dictionary<int, IReadOnlyDictionary<int, string>>();

        var assetPathsByMessageIndex = new Dictionary<int, IReadOnlyDictionary<int, string>>();
        for (var entryIndex = 0; entryIndex < messageFile.Entries.Count; entryIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = messageFile.Entries[entryIndex];
            if (TryResolveEyeCandyArtAssetPaths(entry.Text, out var assetPathsByType))
                assetPathsByMessageIndex[entry.Index] = assetPathsByType;
        }

        return assetPathsByMessageIndex;
    }

    internal bool TryResolveMapRenderArtAssetPath(
        ArtId artId,
        EditorMapRenderQueueItemKind? renderItemKind,
        out string assetPath
    )
    {
        if (IsSectorArtId(artId.Value))
        {
            switch (renderItemKind)
            {
                case EditorMapRenderQueueItemKind.FloorTile:
                    if (TryResolveTileArtAssetPath(artId, out assetPath))
                        return true;
                    return TryResolveMessageTableArtAssetPath(
                        "art/facade/facadename.mes",
                        DecodeSectorArtMessageIndex(artId.Value),
                        "art/facade",
                        out assetPath
                    );
                case EditorMapRenderQueueItemKind.Roof:
                    assetPath = string.Empty;
                    return false;
            }
        }

        return TryResolveArcanumMessageTableArtAssetPathCore(artId, out assetPath);
    }

    private bool TryResolveArcanumMessageTableArtAssetPathCore(ArtId artId, out string assetPath)
    {
        var artIdValue = artId.Value;
        if (artIdValue == 0u)
        {
            assetPath = string.Empty;
            return false;
        }

        switch (artIdValue & 0xF0000000u)
        {
            case 0x10000000u:
                return TryResolveWallArtAssetPath(artIdValue, out assetPath);
            case 0x20000000u:
                return TryResolveCritterArtAssetPath(artIdValue, out assetPath);
            case 0x30000000u:
                return TryResolvePortalArtAssetPath(artIdValue, out assetPath);
            case 0x40000000u:
                return TryResolveSceneryArtAssetPath(artIdValue, out assetPath);
            case 0x50000000u:
                return TryResolveInterfaceArtAssetPath(artIdValue, out assetPath);
            case 0x60000000u:
                return TryResolveItemArtAssetPath(artIdValue, out assetPath);
            case 0x70000000u:
                return TryResolveContainerArtAssetPath(artIdValue, out assetPath);
            case 0x90000000u:
                return TryResolveLightArtAssetPath(artIdValue, out assetPath);
            case 0xA0000000u:
                return TryResolveRoofArtAssetPath(artIdValue, out assetPath)
                    || TryResolveEyeCandyArtAssetPath(artIdValue, out assetPath);
            case 0xB0000000u:
                return TryResolveFacadeArtAssetPath(artIdValue, out assetPath);
            case 0xC0000000u:
                return TryResolveMonsterArtAssetPath(artIdValue, out assetPath);
            case 0xD0000000u:
                return TryResolveUniqueNpcArtAssetPath(artIdValue, out assetPath);
            case 0xE0000000u:
                return TryResolveEyeCandyArtAssetPath(artIdValue, out assetPath);
        }

        if (IsSectorArtId(artIdValue))
            return TryResolveTileArtAssetPath(new ArtId(artIdValue), out assetPath);

        assetPath = string.Empty;
        return false;
    }

    private bool TryResolveCritterArtAssetPath(uint artIdValue, out string assetPath)
    {
        assetPath = string.Empty;

        var bodyType = DecodeCritterBodyType(artIdValue);
        var gender = DecodeCritterGender(artIdValue);
        var armorType = DecodeCritterArmorType(artIdValue);
        var shield = DecodeCritterShield(artIdValue);
        var weapon = DecodeCritterWeapon(artIdValue);
        var animation = DecodeCritterAnimation(artIdValue);

        NormalizeCritterArtParts(ref bodyType, ref gender, ref armorType, ref shield, ref weapon, animation);

        if (!IsValidCritterBodyType(bodyType) || !IsValidCritterArmorType(armorType) || !IsValidCritterWeapon(weapon))
            return false;

        var bodyTypeCode = s_critterBodyTypeCodes[bodyType];
        var genderCode = s_critterGenderCodes[gender];
        var armorTypeCode = s_critterArmorTypeCodes[armorType];
        if (armorType is CritterArmorTypePlate or CritterArmorTypePlateClassic)
            genderCode = s_critterGenderCodes[2];

        if (animation == CritterAnimationExplode)
        {
            armorTypeCode = "XX";
            genderCode = s_critterGenderCodes[2];
        }

        return TryResolveCandidateArtAssetPath(
            $"art/critter/{bodyTypeCode}{genderCode}/{bodyTypeCode}{genderCode}{armorTypeCode}{s_critterShieldCodes[shield]}{GetCritterWeaponCode(weapon, shield)}{GetAnimationCode(animation)}.art",
            out assetPath
        );
    }

    private bool TryResolvePortalArtAssetPath(uint artIdValue, out string assetPath)
    {
        var messageIndex = DecodePortalArtMessageIndex(artIdValue);
        if (!TryResolveMessageTableArtAssetPath("art/portal/portal.mes", messageIndex, "art/portal", out assetPath))
            return false;

        if ((artIdValue & 0x200u) == 0u)
            return true;

        var damagedAssetPath =
            assetPath.Length > 6
                ? assetPath.Remove(assetPath.Length - 6, 1).Insert(assetPath.Length - 6, "D")
                : assetPath;
        if (TryResolveCandidateArtAssetPath(damagedAssetPath, out var resolvedDamagedAssetPath))
        {
            assetPath = resolvedDamagedAssetPath;
            return true;
        }

        return true;
    }

    private bool TryResolveInterfaceArtAssetPath(uint artIdValue, out string assetPath)
    {
        return TryResolveMessageTableArtAssetPath(
            "art/interface/interface.mes",
            DecodeInterfaceArtMessageIndex(artIdValue),
            "art/interface",
            out assetPath
        );
    }

    private bool TryResolveItemArtAssetPath(uint artIdValue, out string assetPath)
    {
        if (
            TryResolveMessageTableArtAssetPath(
                DecodeItemArtMessageTableAssetPath(artIdValue),
                DecodeItemArtMessageIndex(artIdValue),
                "art/item",
                out assetPath
            )
        )
        {
            return true;
        }

        return TryResolveMessageTableArtAssetPath(
            "art/item/item_ground.mes",
            DecodeBigEndianArtMessageIndex(artIdValue),
            "art/item",
            out assetPath
        );
    }

    private bool TryResolveContainerArtAssetPath(uint artIdValue, out string assetPath)
    {
        if (
            TryResolveMessageTableArtAssetPath(
                "art/container/container.mes",
                DecodeContainerArtMessageIndex(artIdValue),
                "art/container",
                out assetPath
            )
        )
        {
            return true;
        }

        return TryResolveMessageTableArtAssetPath(
            "art/container/container.mes",
            DecodeBigEndianArtMessageIndex(artIdValue),
            "art/container",
            out assetPath
        );
    }

    private bool TryResolveLightArtAssetPath(uint artIdValue, out string assetPath)
    {
        if (
            TryResolveMessageTableArtAssetPath(
                "art/light/light.mes",
                DecodeDefaultArtMessageIndex(artIdValue),
                "art/light",
                out assetPath
            )
        )
        {
            return true;
        }

        return TryResolveMessageTableArtAssetPath(
            "art/light/light.mes",
            DecodeBigEndianArtMessageIndex(artIdValue),
            "art/light",
            out assetPath
        );
    }

    private bool TryResolveRoofArtAssetPath(uint artIdValue, out string assetPath)
    {
        return TryResolveMessageTableArtAssetPath(
            "art/roof/roofname.mes",
            DecodeDefaultArtMessageIndex(artIdValue),
            "art/roof",
            out assetPath
        );
    }

    private bool TryResolveMonsterArtAssetPath(uint artIdValue, out string assetPath)
    {
        assetPath = string.Empty;

        var species = DecodeMonsterSpecies(artIdValue);
        if (!TryGetMessageEntryText("art/monster/monster.mes", species, out var monsterName))
            return false;

        var armorType = DecodeMonsterArmorType(artIdValue);
        var shield = DecodeCritterShield(artIdValue);
        var weapon = DecodeCritterWeapon(artIdValue);
        var animation = DecodeCritterAnimation(artIdValue);

        NormalizeMonsterArtParts(ref armorType, ref shield, ref weapon, animation);

        if (!IsValidCritterArmorType(armorType) || !IsValidCritterWeapon(weapon))
            return false;

        var armorTypeCode = animation == CritterAnimationExplode ? "XX" : s_critterArmorTypeCodes[armorType];
        return TryResolveCandidateArtAssetPath(
            $"art/monster/{monsterName}/{monsterName}{armorTypeCode}{s_critterShieldCodes[shield]}{GetCritterWeaponCode(weapon, shield)}{GetAnimationCode(animation)}.art",
            out assetPath
        );
    }

    private bool TryResolveUniqueNpcArtAssetPath(uint artIdValue, out string assetPath)
    {
        assetPath = string.Empty;

        var number = DecodeUniqueNpcNumber(artIdValue);
        if (!TryGetMessageEntryText("art/unique_npc/unique_npc.mes", number, out var uniqueNpcName))
            return false;

        var shield = DecodeCritterShield(artIdValue);
        var weapon = DecodeCritterWeapon(artIdValue);
        var animation = DecodeCritterAnimation(artIdValue);

        NormalizeUniqueNpcArtParts(ref shield, ref weapon, animation);

        if (!IsValidCritterWeapon(weapon))
            return false;

        return TryResolveCandidateArtAssetPath(
            $"art/unique_npc/{uniqueNpcName}/{uniqueNpcName}{s_critterShieldCodes[shield]}{GetCritterWeaponCode(weapon, shield)}{GetAnimationCode(animation)}.art",
            out assetPath
        );
    }

    private bool TryResolveSceneryArtAssetPath(uint artIdValue, out string assetPath)
    {
        if (
            TryResolveMessageTableArtAssetPath(
                "art/scenery/scenery.mes",
                DecodeSceneryArtMessageIndex(artIdValue),
                "art/scenery",
                out assetPath
            )
        )
        {
            return true;
        }

        return TryResolveMessageTableArtAssetPath(
            "art/scenery/scenery.mes",
            DecodeBigEndianArtMessageIndex(artIdValue),
            "art/scenery",
            out assetPath
        );
    }

    private bool TryResolveFacadeArtAssetPath(uint artIdValue, out string assetPath)
    {
        return TryResolveMessageTableArtAssetPath(
            "art/facade/facadename.mes",
            DecodeFacadeArtMessageIndex(artIdValue),
            "art/facade",
            out assetPath
        );
    }

    private bool TryResolveEyeCandyArtAssetPath(uint artIdValue, out string assetPath)
    {
        assetPath = string.Empty;

        var messageIndex = DecodeEyeCandyArtMessageIndex(artIdValue);
        var type = DecodeEyeCandyArtType(artIdValue);
        if (type < 0 || type >= s_eyeCandyTypeCodes.Length)
            return false;

        var assetPathsByMessageIndex = GetOrCreateEyeCandyArtAssetPathsByMessageIndex();
        for (var candidateMessageIndex = messageIndex; candidateMessageIndex <= 512; candidateMessageIndex++)
        {
            var startType = candidateMessageIndex == messageIndex ? type : 0;
            for (var candidateType = startType; candidateType < s_eyeCandyTypeCodes.Length; candidateType++)
            {
                if (
                    assetPathsByMessageIndex.TryGetValue(candidateMessageIndex, out var assetPathsByType)
                    && assetPathsByType.TryGetValue(candidateType, out assetPath!)
                )
                {
                    return true;
                }
            }
        }

        assetPath = string.Empty;
        return false;
    }

    private bool TryResolveWallArtAssetPath(uint artIdValue, out string assetPath)
    {
        assetPath = string.Empty;

        var structureIndex = DecodeWallArtStructureIndex(artIdValue);
        var piece = DecodeWallArtPiece(artIdValue);
        var rotation = DecodeWallArtRotation(artIdValue);
        var variation = DecodeWallArtVariation(artIdValue);
        var damage = DecodeWallArtDamage(artIdValue);

        NormalizeWallDamageAndPiece(rotation, ref piece, ref damage);

        var lookupData = GetOrCreateWallArtLookupData();
        if (!lookupData.StructuresByIndex.TryGetValue(structureIndex, out var structureSides))
            return false;

        var sideBaseName = rotation / 2 is 0 or 3 ? structureSides.InteriorBaseName : structureSides.ExteriorBaseName;
        return TryBuildWallArtAssetPath(sideBaseName, piece, damage, variation, out assetPath);
    }

    private bool TryResolveWallProtoArtAssetPath(int protoNumber, out string assetPath)
    {
        assetPath = string.Empty;
        if (protoNumber <= 0)
            return false;

        if (
            GetOrCreateWallArtLookupData()
                .DefaultPaletteArtAssetPathsByProtoNumber.TryGetValue(protoNumber, out var resolvedAssetPath)
        )
        {
            assetPath = resolvedAssetPath;
            return true;
        }

        return false;
    }

    internal bool TryResolveWallProtoCurrentArtId(int protoNumber, out ArtId artId)
    {
        artId = default;
        if (protoNumber <= 0)
            return false;

        if (
            GetOrCreateWallArtLookupData()
                .DefaultPaletteArtIdsByProtoNumber.TryGetValue(protoNumber, out var resolvedArtId)
        )
        {
            artId = resolvedArtId;
            return true;
        }

        return false;
    }

    private bool TryResolveUnambiguousSectorArtAssetPath(uint artIdValue, out string assetPath)
    {
        var messageIndex = DecodeSectorArtMessageIndex(artIdValue);
        var candidateAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddTileMessageTableArtCandidate(candidateAssetPaths, new ArtId(artIdValue));
        AddMessageTableArtCandidate(candidateAssetPaths, "art/facade/facadename.mes", messageIndex, "art/facade");
        AddMessageTableArtCandidate(candidateAssetPaths, "art/roof/roofname.mes", messageIndex, "art/roof");
        AddMessageTableArtCandidate(candidateAssetPaths, "art/wall/wallname.mes", messageIndex, "art/wall");
        AddMessageTableArtCandidate(candidateAssetPaths, "art/light/light.mes", messageIndex, "art/light");
        AddMessageTableArtCandidate(candidateAssetPaths, "art/portal/portal.mes", messageIndex, "art/portal");

        if (candidateAssetPaths.Count == 1)
        {
            assetPath = candidateAssetPaths.First();
            return true;
        }

        assetPath = string.Empty;
        return false;
    }

    private void AddMessageTableArtCandidate(
        HashSet<string> candidateAssetPaths,
        string messageAssetPath,
        int messageIndex,
        string assetRootPath
    )
    {
        if (TryResolveMessageTableArtAssetPath(messageAssetPath, messageIndex, assetRootPath, out var assetPath))
            candidateAssetPaths.Add(assetPath);
    }

    private void AddTileMessageTableArtCandidate(HashSet<string> candidateAssetPaths, ArtId artId)
    {
        if (TryResolveTileArtAssetPath(artId, out var assetPath))
            candidateAssetPaths.Add(assetPath);
    }

    private bool TryResolveTileArtAssetPath(ArtId artId, out string assetPath)
    {
        assetPath = string.Empty;
        if (!TryBuildTileArtAssetPath(artId.Value, out var candidateAssetPath))
            return false;

        if (TryResolveCandidateArtAssetPath(candidateAssetPath, out assetPath))
            return true;

        if (!TryBuildAlternateTileArtAssetPath(artId.Value, out var alternateCandidateAssetPath))
            return false;

        if (string.Equals(candidateAssetPath, alternateCandidateAssetPath, StringComparison.OrdinalIgnoreCase))
            return false;

        return TryResolveCandidateArtAssetPath(alternateCandidateAssetPath, out assetPath);
    }

    private bool TryBuildTileArtAssetPath(uint artIdValue, out string assetPath)
    {
        return TryBuildTileArtAssetPath(artIdValue, useAlternateEdgeTable: false, out assetPath);
    }

    private bool TryBuildAlternateTileArtAssetPath(uint artIdValue, out string assetPath)
    {
        return TryBuildTileArtAssetPath(artIdValue, useAlternateEdgeTable: true, out assetPath);
    }

    private bool TryBuildTileArtAssetPath(uint artIdValue, bool useAlternateEdgeTable, out string assetPath)
    {
        assetPath = string.Empty;
        var lookupData = GetOrCreateTileNameLookupData();
        if (!lookupData.HasNames)
            return false;

        if (
            !TryGetTileName(
                lookupData,
                DecodeTileArtNum1(artIdValue),
                DecodeTileArtType(artIdValue),
                DecodeTileArtFlippable1(artIdValue),
                out var name1
            )
            || !TryGetTileName(
                lookupData,
                DecodeTileArtNum2(artIdValue),
                DecodeTileArtType(artIdValue),
                DecodeTileArtFlippable2(artIdValue),
                out var name2
            )
        )
        {
            return false;
        }

        assetPath = BuildTileArtAssetPath(
            lookupData,
            name1,
            name2,
            DecodeTileArtEdge(artIdValue, useAlternateEdgeTable),
            DecodeTileArtFrame(artIdValue, useAlternateEdgeTable)
        );
        return true;
    }

    private static bool TryGetTileName(
        TileNameLookupData lookupData,
        int number,
        int type,
        int flippable,
        out string name
    )
    {
        IReadOnlyList<string> tileNames =
            type != 0
                ? flippable != 0
                    ? lookupData.OutdoorFlippableNames
                    : lookupData.OutdoorNonFlippableNames
                : flippable != 0
                    ? lookupData.IndoorFlippableNames
                    : lookupData.IndoorNonFlippableNames;

        if ((uint)number >= (uint)tileNames.Count)
        {
            name = string.Empty;
            return false;
        }

        name = tileNames[number];
        return true;
    }

    private static string BuildTileArtAssetPath(
        TileNameLookupData lookupData,
        string name1,
        string name2,
        int edge,
        int frame
    )
    {
        if (frame >= 8)
            frame -= 8;

        var edgeCode = s_tileEdgeCodes[edge];
        var frameCode = (char)('a' + frame);

        if (edge == 15 || string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase))
            return NormalizeAssetPath($"art/tile/{name1}bse{edgeCode}{frameCode}.art");

        if (edge == 0)
            return NormalizeAssetPath($"art/tile/{name2}bse{s_tileEdgeCodes[0]}{frameCode}.art");

        if (!lookupData.OutdoorOrderByName.TryGetValue(name1, out var name1Order))
            return NormalizeAssetPath($"art/tile/{name1}bse{edgeCode}{frameCode}.art");

        if (!lookupData.OutdoorOrderByName.TryGetValue(name2, out var name2Order))
            return NormalizeAssetPath($"art/tile/{name2}bse{s_tileEdgeCodes[15 - edge]}{frameCode}.art");

        return name1Order < name2Order
            ? NormalizeAssetPath($"art/tile/{name1}{name2}{edgeCode}{frameCode}.art")
            : NormalizeAssetPath($"art/tile/{name2}{name1}{s_tileEdgeCodes[15 - edge]}{frameCode}.art");
    }

    private bool TryResolveMessageTableArtAssetPath(
        string messageAssetPath,
        int messageIndex,
        string assetRootPath,
        out string assetPath
    )
    {
        assetPath = string.Empty;
        var directAssetPathsByMessageTable = GetOrCreateDirectMessageTableArtAssetPaths();
        if (
            directAssetPathsByMessageTable.TryGetValue(messageAssetPath, out var assetPathsByIndex)
            && assetPathsByIndex.TryGetValue(messageIndex, out var resolvedAssetPath)
        )
        {
            assetPath = resolvedAssetPath;
            return true;
        }
        return false;
    }

    private bool TryGetMessageEntryText(string messageAssetPath, int messageIndex, out string entryText)
    {
        var entriesByIndex = _messageEntriesByIndexByAssetPath.GetOrAdd(
            NormalizeAssetPath(messageAssetPath),
            BuildMessageEntriesByIndex
        );
        if (entriesByIndex.TryGetValue(messageIndex, out var text) && !string.IsNullOrWhiteSpace(text))
        {
            entryText = text.Trim();
            return true;
        }

        entryText = string.Empty;
        return false;
    }

    private IReadOnlyDictionary<int, string> BuildMessageEntriesByIndex(string messageAssetPath)
    {
        var messageFile = FindMessageFile(messageAssetPath);
        if (messageFile is null)
            return new Dictionary<int, string>();

        var entriesByIndex = new Dictionary<int, string>();
        foreach (var entry in messageFile.Entries)
        {
            if (!entriesByIndex.ContainsKey(entry.Index))
                entriesByIndex[entry.Index] = entry.Text;
        }

        return entriesByIndex;
    }

    private bool TryResolveCandidateArtAssetPath(string candidateAssetPath, out string assetPath)
    {
        var normalizedAssetPath = NormalizeAssetPath(candidateAssetPath);
        if (Assets.Find(normalizedAssetPath) is not { Format: FileFormat.Art })
        {
            assetPath = string.Empty;
            return false;
        }

        assetPath = normalizedAssetPath;
        return true;
    }

    private bool TryResolveMessageEntryArtAssetPath(string entryText, string assetRootPath, out string assetPath)
    {
        foreach (var candidateToken in EnumerateMessageEntryArtPathCandidates(entryText))
        {
            var normalizedToken = candidateToken.Replace('\\', '/');
            if (!normalizedToken.EndsWith(".art", StringComparison.OrdinalIgnoreCase))
            {
                if (
                    normalizedToken.Contains('/', StringComparison.Ordinal)
                    || normalizedToken.Contains('.', StringComparison.Ordinal)
                )
                {
                    continue;
                }

                normalizedToken += ".art";
            }

            var candidateAssetPath = NormalizeAssetPath(
                $"{assetRootPath.TrimEnd('/')}/{normalizedToken.TrimStart('/')}"
            );
            if (Assets.Find(candidateAssetPath) is { Format: FileFormat.Art })
            {
                assetPath = candidateAssetPath;
                return true;
            }
        }

        assetPath = string.Empty;
        return false;
    }

    private static string? TryGetFirstMessageToken(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.AsSpan().Trim();
        var separatorIndex = trimmed.IndexOfAny(' ', '\t');
        return separatorIndex < 0 ? trimmed.ToString() : trimmed[..separatorIndex].ToString();
    }

    private static IEnumerable<string> EnumerateMessageEntryArtPathCandidates(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var trimmed = text.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
            yield return trimmed;

        var firstToken = TryGetFirstMessageToken(trimmed);
        if (!string.IsNullOrWhiteSpace(firstToken) && !string.Equals(firstToken, trimmed, StringComparison.Ordinal))
        {
            yield return firstToken;
        }
    }

    private static bool TryNormalizeTileFamilyToken(string? token, out string normalizedToken)
    {
        normalizedToken = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var trimmedToken = token.Trim();
        if (
            trimmedToken.Contains('/', StringComparison.Ordinal) || trimmedToken.Contains('.', StringComparison.Ordinal)
        )
        {
            return false;
        }

        normalizedToken = trimmedToken;
        return true;
    }

    private static string? TryNormalizeTileNameEntry(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        var slashIndex = trimmed.IndexOf('/');
        if (slashIndex >= 0)
            trimmed = trimmed[..slashIndex].TrimEnd();

        var token = TryGetFirstMessageToken(trimmed);
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private bool TryResolveEyeCandyArtAssetPaths(
        string? entryText,
        out IReadOnlyDictionary<int, string> assetPathsByType
    )
    {
        assetPathsByType = new Dictionary<int, string>();

        var token = TryGetFirstMessageToken(entryText);
        if (!TryNormalizeTileFamilyToken(token, out var normalizedToken))
            return false;

        var resolvedAssetPathsByType = new Dictionary<int, string>();
        for (var typeIndex = 0; typeIndex < s_eyeCandyTypeCodes.Length; typeIndex++)
        {
            var candidateAssetPath = NormalizeAssetPath(
                $"art/eye_candy/{normalizedToken}_{s_eyeCandyTypeCodes[typeIndex]}.art"
            );
            if (Assets.Find(candidateAssetPath) is { Format: FileFormat.Art })
                resolvedAssetPathsByType[typeIndex] = candidateAssetPath;
        }

        if (resolvedAssetPathsByType.Count == 0)
            return false;

        assetPathsByType = resolvedAssetPathsByType;
        return true;
    }

    private bool TryBuildWallArtAssetPath(string baseName, int piece, int damage, int variation, out string assetPath)
    {
        assetPath = string.Empty;
        if (
            string.IsNullOrWhiteSpace(baseName)
            || variation < 0
            || variation >= 4
            || piece < 0
            || piece >= s_wallPieceSuffixes.Length
        )
        {
            return false;
        }

        var damageChar = GetWallDamageVariantCharacter(piece, damage);
        var candidateAssetPath = NormalizeAssetPath(
            $"art/wall/{baseName}{s_wallPieceSuffixes[piece]}{damageChar}{variation.ToString(CultureInfo.InvariantCulture)}.art"
        );
        if (Assets.Find(candidateAssetPath) is not { Format: FileFormat.Art })
            return false;

        assetPath = candidateAssetPath;
        return true;
    }

    private static bool TryParseWallStructureArtSides(string? entryText, out WallStructureArtSides sides)
    {
        sides = default;
        if (string.IsNullOrWhiteSpace(entryText))
            return false;

        var tokens = entryText.Split(
            [' ', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (tokens.Length < 2)
            return false;

        if (!TryGetWallBaseName(tokens[0], out var interiorBaseName))
            return false;

        if (!TryGetWallBaseName(tokens[1], out var exteriorBaseName))
            return false;

        sides = new WallStructureArtSides(interiorBaseName, exteriorBaseName);
        return true;
    }

    private static bool TryGetWallBaseName(string? token, out string baseName)
    {
        baseName = string.Empty;
        var normalizedToken = TryGetFirstMessageToken(token);
        if (string.IsNullOrWhiteSpace(normalizedToken))
            return false;

        var trimmedToken = normalizedToken.Trim();
        if (
            trimmedToken.Length < 3
            || trimmedToken.Equals("nul", StringComparison.OrdinalIgnoreCase)
            || trimmedToken.Contains('/', StringComparison.Ordinal)
            || trimmedToken.Contains('.', StringComparison.Ordinal)
        )
        {
            return false;
        }

        baseName = trimmedToken[..3];
        return true;
    }

    private static bool TryParseMessageEntryInt(string? text, out int value)
    {
        value = 0;
        var token = TryGetFirstMessageToken(text);
        return !string.IsNullOrWhiteSpace(token)
            && int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static int DecodeWallArtStructureIndex(uint artIdValue) => checked((int)((artIdValue >> 20) & 0xFFu));

    private static int DecodeWallArtPiece(uint artIdValue) => checked((int)((artIdValue >> 14) & 0x3Fu));

    private static int DecodeWallArtRotation(uint artIdValue) => checked((int)((artIdValue >> 11) & 0x7u));

    private static int DecodeWallArtVariation(uint artIdValue) => checked((int)((artIdValue >> 8) & 0x3u));

    private static int DecodeWallArtDamage(uint artIdValue) => checked((int)(artIdValue & 0x480u));

    private static int DecodeCritterAnimation(uint artIdValue) => checked((int)((artIdValue >> 6) & 0x1Fu));

    private static int DecodeCritterShield(uint artIdValue) => checked((int)((artIdValue >> 19) & 1u));

    private static int DecodeCritterArmorType(uint artIdValue) => checked((int)((artIdValue >> 20) & 0xFu));

    private static int DecodeCritterBodyType(uint artIdValue) => checked((int)((artIdValue >> 24) & 0x7u));

    private static int DecodeCritterGender(uint artIdValue) => checked((int)((artIdValue >> 27) & 1u));

    private static int DecodeCritterWeapon(uint artIdValue) => checked((int)(artIdValue & 0xFu));

    private static int DecodeMonsterArmorType(uint artIdValue) => checked((int)((artIdValue >> 20) & 0x7u));

    private static int DecodeMonsterSpecies(uint artIdValue) => checked((int)((artIdValue >> 23) & 0x1Fu));

    private static int DecodeUniqueNpcNumber(uint artIdValue) => checked((int)((artIdValue >> 20) & 0xFFu));

    private static int DecodePortalArtMessageIndex(uint artIdValue)
    {
        var number = DecodeDefaultArtMessageIndex(artIdValue);
        var type = (int)((artIdValue >> 10) & 1u);
        return type == 0 ? checked(number + 1001) : number;
    }

    private static int DecodeDefaultArtMessageIndex(uint artIdValue) => checked((int)((artIdValue >> 19) & 0x1FFu));

    private static int DecodeInterfaceArtMessageIndex(uint artIdValue) => checked((int)((artIdValue >> 16) & 0xFFFu));

    private static int DecodeItemArtMessageIndex(uint artIdValue)
    {
        var number = checked((int)((artIdValue >> 17) & 0x7FFu));
        var subtype = checked((int)((artIdValue >> 6) & 0xFu));
        var type = checked((int)(artIdValue & 0xFu));
        var messageIndex = checked(number + (20 * (subtype + (50 * type))));

        if (type == ItemTypeArmor)
        {
            var armorCoverage = checked((int)((artIdValue >> 14) & 0x7u));
            if (armorCoverage != ItemArmorCoverageTorso)
                messageIndex = checked(messageIndex + (20 * ((5 * armorCoverage) + 10)));
        }

        return messageIndex;
    }

    private static string DecodeItemArtMessageTableAssetPath(uint artIdValue)
    {
        var disposition = checked((int)((artIdValue >> 12) & 0x3u));
        return disposition switch
        {
            0 => "art/item/item_ground.mes",
            2 => "art/item/item_paper.mes",
            3 => "art/item/item_schematic.mes",
            _ => "art/item/item_inven.mes",
        };
    }

    private static int DecodeContainerArtMessageIndex(uint artIdValue)
    {
        var number = DecodeDefaultArtMessageIndex(artIdValue);
        var type = checked((int)((artIdValue >> 6) & 0x1Fu));
        return checked((1000 * type) + number);
    }

    private static bool TryCreateDefaultWallArtId(int structureIndex, out ArtId artId)
    {
        artId = default;
        if ((uint)structureIndex > 0xFFu)
            return false;

        artId = new ArtId(WallArtTypeMask | ((uint)structureIndex << 20));
        return true;
    }

    private static void NormalizeWallDamageAndPiece(int rotation, ref int piece, ref int damage)
    {
        if (rotation is 2 or 3 or 6 or 7)
        {
            var rotatedDamage = 0;
            if ((damage & 0x400) != 0)
                rotatedDamage |= 0x80;

            if ((damage & 0x80) != 0)
                rotatedDamage |= 0x400;

            damage = rotatedDamage;
        }

        if ((damage & 0x400) != 0)
        {
            damage = 0x400;
            if (piece == 7)
                piece = 0;

            return;
        }

        if ((damage & 0x80) != 0)
        {
            damage = 0x80;
            if (piece == 8)
                piece = 0;

            return;
        }

        damage = 0;
    }

    private static char GetWallDamageVariantCharacter(int piece, int damage)
    {
        if ((damage & 0x400) != 0)
            return 'L';

        if ((damage & 0x80) != 0)
            return piece is >= 2 and <= 6 ? 'L' : 'R';

        return 'U';
    }

    private static int DecodeBigEndianArtMessageIndex(uint artIdValue) =>
        checked((int)((artIdValue & 0x00FFFF00u) >> 8));

    private static bool IsValidCritterBodyType(int bodyType) => (uint)bodyType < (uint)s_critterBodyTypeCodes.Length;

    private static bool IsValidCritterArmorType(int armorType) =>
        (uint)armorType < (uint)s_critterArmorTypeCodes.Length;

    private static bool IsValidCritterWeapon(int weapon) => (uint)weapon < (uint)s_critterWeaponTypeCodes.Length;

    private static void NormalizeCritterArtParts(
        ref int bodyType,
        ref int gender,
        ref int armorType,
        ref int shield,
        ref int weapon,
        int animation
    )
    {
        if (bodyType == CritterBodyTypeElf && gender == CritterGenderFemale)
            bodyType = CritterBodyTypeHuman;

        if (armorType is CritterArmorTypePlate or CritterArmorTypePlateClassic)
        {
            gender = CritterGenderMale;
            if (bodyType == CritterBodyTypeElf)
                bodyType = CritterBodyTypeHuman;
            else if (bodyType == CritterBodyTypeHalfling)
                bodyType = CritterBodyTypeDwarf;
        }

        if (animation == CritterAnimationExplode)
        {
            armorType = 0;
            shield = 0;
            weapon = 0;
            gender = CritterGenderFemale;
            return;
        }

        NormalizeAnimatedCritterEquipment(ref shield, ref weapon, animation);
    }

    private static void NormalizeMonsterArtParts(ref int armorType, ref int shield, ref int weapon, int animation)
    {
        if (animation == CritterAnimationExplode)
        {
            armorType = 0;
            shield = 0;
            weapon = 0;
            return;
        }

        NormalizeAnimatedCritterEquipment(ref shield, ref weapon, animation);
    }

    private static void NormalizeUniqueNpcArtParts(ref int shield, ref int weapon, int animation)
    {
        if (animation == CritterAnimationExplode)
        {
            shield = 0;
            weapon = 0;
            return;
        }

        NormalizeAnimatedCritterEquipment(ref shield, ref weapon, animation);
    }

    private static void NormalizeAnimatedCritterEquipment(ref int shield, ref int weapon, int animation)
    {
        if (animation == CritterAnimationStunned)
        {
            weapon = CritterWeaponTypeUnarmed;
            shield = 0;
            return;
        }

        if (
            weapon == CritterWeaponTypeUnarmed
            && animation is CritterAnimationStealthWalk or CritterAnimationConcealFidget
        )
        {
            weapon = 0;
        }
        else if (animation is >= CritterAnimationRun and <= CritterAnimationSeveredLeg)
        {
            weapon = 0;
            shield = 0;
        }
        else if (animation == CritterAnimationWalk)
        {
            shield = 0;
        }
    }

    private static char GetCritterWeaponCode(int weapon, int shield)
    {
        var codeIndex = weapon == CritterWeaponTypeTwoHandedSword && shield == 1 ? CritterWeaponTypeSword : weapon;
        return s_critterWeaponTypeCodes[codeIndex];
    }

    private static char GetAnimationCode(int animation) => checked((char)('a' + animation));

    private static int DecodeFacadeArtMessageIndex(uint artIdValue)
    {
        var number = (int)((artIdValue >> 17) & 0xFFu);
        if ((artIdValue & (1u << 27)) != 0)
            number += 256;

        return number;
    }

    private static int DecodeSceneryArtMessageIndex(uint artIdValue)
    {
        var number = (int)((artIdValue >> 19) & 0x1FFu);
        var type = (int)((artIdValue >> 6) & 0x1Fu);
        return checked((1000 * type) + number);
    }

    private static int DecodeEyeCandyArtMessageIndex(uint artIdValue) => checked((int)((artIdValue >> 19) & 0x1FFu));

    private static int DecodeEyeCandyArtType(uint artIdValue) => checked((int)((artIdValue >> 6) & 0x3u));

    private static bool IsSectorArtId(uint artIdValue) => (artIdValue & 0xF0000000u) == 0u;

    private static int DecodeSectorArtMessageIndex(uint artIdValue) => checked((int)((artIdValue & 0x003FFF00u) >> 8));

    private static int DecodeTileArtNum1(uint artIdValue) => checked((int)((artIdValue >> 22) & 0x3Fu));

    private static int DecodeTileArtNum2(uint artIdValue) => checked((int)((artIdValue >> 16) & 0x3Fu));

    private static int DecodeTileArtType(uint artIdValue) => checked((int)((artIdValue >> 8) & 1u));

    private static int DecodeTileArtFlippable1(uint artIdValue) => checked((int)((artIdValue >> 7) & 1u));

    private static int DecodeTileArtFlippable2(uint artIdValue) => checked((int)((artIdValue >> 6) & 1u));

    private static int DecodeTileArtRawEdge(uint artIdValue) => checked((int)((artIdValue >> 12) & 0xFu));

    private static int DecodeTileArtEdge(uint artIdValue, bool useAlternateEdgeTable = false)
    {
        var rawEdge = DecodeTileArtRawEdge(artIdValue);
        return (IsTileArtMirrored(artIdValue), useAlternateEdgeTable) switch
        {
            (true, false) => s_tileEdgeDecodeWhenFlagsSet[rawEdge],
            (true, true) => s_tileEdgeDecodeWhenFlagsClear[rawEdge],
            (false, false) => s_tileEdgeDecodeWhenFlagsClear[rawEdge],
            _ => s_tileEdgeDecodeWhenFlagsSet[rawEdge],
        };
    }

    private static int DecodeTileArtFrame(uint artIdValue, bool useAlternateEdgeTable = false)
    {
        var frame = checked((int)((artIdValue >> 9) & 0x7u));
        if (useAlternateEdgeTable)
            return frame;

        var rawEdge = DecodeTileArtRawEdge(artIdValue);
        if (
            IsTileArtMirrored(artIdValue)
            && s_tileEdgeDecodeWhenFlagsSet[rawEdge] == s_tileEdgeDecodeWhenFlagsClear[rawEdge]
        )
        {
            frame += 8;
        }

        return frame;
    }

    private static bool IsTileArtMirrored(uint artIdValue) => (artIdValue & 1u) != 0u;

    private static IEnumerable<int> EnumerateSectorFloorMessageIndices(int messageIndex)
    {
        HashSet<int> candidateMessageIndices = [];

        if (candidateMessageIndices.Add(messageIndex))
            yield return messageIndex;

        var positiveOffsetMessageIndex = checked(messageIndex + 16);
        if (candidateMessageIndices.Add(positiveOffsetMessageIndex))
            yield return positiveOffsetMessageIndex;

        for (var candidateMessageIndex = messageIndex - 48; candidateMessageIndex >= 0; candidateMessageIndex -= 48)
        {
            if (candidateMessageIndices.Add(candidateMessageIndex))
                yield return candidateMessageIndex;
        }
    }

    private static IEnumerable<uint> EnumerateConservativeArtBindingIds(string assetPath)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        var fileName = Path.GetFileNameWithoutExtension(normalizedPath);
        if (TryParseConservativeArtBindingToken(fileName, out var fileNameId))
            yield return fileNameId;

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
        {
            var segment =
                segmentIndex == segments.Length - 1
                    ? Path.GetFileNameWithoutExtension(segments[segmentIndex])
                    : segments[segmentIndex];
            if (TryParseConservativeArtBindingToken(segment, out var segmentId))
                yield return segmentId;
        }
    }

    private static bool TryParseConservativeArtBindingToken(string? token, out uint artId)
    {
        artId = 0u;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var trimmedToken = token.Trim();
        if (trimmedToken.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(
                trimmedToken[2..],
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out artId
            );
        }

        return uint.TryParse(trimmedToken, NumberStyles.None, CultureInfo.InvariantCulture, out artId);
    }

    private EditorTerrainPaletteEntry? CreateTerrainPaletteEntry(
        EditorAssetEntry asset,
        MapProperties properties,
        ulong paletteX,
        ulong paletteY,
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions
    )
    {
        if (!TryGetTerrainPaletteIndex(properties, paletteX, paletteY, out var paletteIndex) || properties.ArtId < 0)
            return null;

        var baseArtId = (uint)properties.ArtId;
        if (paletteIndex > (uint.MaxValue - baseArtId))
            return null;

        var artIdValue = baseArtId + (uint)paletteIndex;
        var artId = new ArtId(artIdValue);
        var artAssetPath = artResolver?.FindAssetPath(artId);
        if (
            string.IsNullOrWhiteSpace(artAssetPath)
            && IsSectorArtId(artIdValue)
            && TryResolveTileArtAssetPath(artId, out var tileAssetPath)
        )
        {
            artAssetPath = tileAssetPath;
        }
        var artDetail = !string.IsNullOrWhiteSpace(artAssetPath) ? Index.FindArtDetail(artAssetPath) : null;
        var artPreview =
            !string.IsNullOrWhiteSpace(artAssetPath) && artPreviewOptions is not null
                ? CreateArtPreview(artAssetPath, artPreviewOptions)
                : null;

        return new EditorTerrainPaletteEntry
        {
            Asset = asset,
            BaseArtId = properties.ArtId,
            LimitX = properties.LimitX,
            LimitY = properties.LimitY,
            PaletteX = paletteX,
            PaletteY = paletteY,
            PaletteIndex = paletteIndex,
            ArtId = artId,
            ArtAssetPath = artAssetPath,
            ArtDetail = artDetail,
            ArtPreview = artPreview,
        };
    }

    private static bool TryGetTerrainPaletteEntryCount(MapProperties properties, out int entryCount)
    {
        entryCount = 0;
        if (properties.LimitX == 0 || properties.LimitY == 0)
            return true;

        const ulong maxEntryCount = int.MaxValue;
        if (properties.LimitX > (maxEntryCount / properties.LimitY))
            return false;

        entryCount = (int)(properties.LimitX * properties.LimitY);
        return true;
    }

    private static bool TryGetTerrainPaletteIndex(
        MapProperties properties,
        ulong paletteX,
        ulong paletteY,
        out ulong paletteIndex
    )
    {
        paletteIndex = 0;
        if (paletteY != 0 && properties.LimitX > ((ulong.MaxValue - paletteX) / paletteY))
            return false;

        paletteIndex = (paletteY * properties.LimitX) + paletteX;
        return true;
    }

    private EditorObjectPaletteEntry CreateObjectPaletteEntry(
        EditorAssetEntry asset,
        ProtoData proto,
        int protoNumber,
        EditorArtResolver? artResolver = null,
        EditorArtPreviewOptions? artPreviewOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(proto);

        var isWallProto = proto.Header.GameObjectType is ObjectType.Wall;
        ArtId? currentArtId = TryResolveProtoCurrentArtId(proto, protoNumber, out var resolvedCurrentArtId)
            ? resolvedCurrentArtId
            : null;

        var artAssetPath =
            currentArtId.HasValue && artResolver is not null ? artResolver.FindAssetPath(currentArtId.Value) : null;
        if (
            string.IsNullOrWhiteSpace(artAssetPath)
            && isWallProto
            && TryResolveWallProtoArtAssetPath(protoNumber, out var wallArtAssetPath)
        )
        {
            artAssetPath = wallArtAssetPath;
        }
        var artDetail = !string.IsNullOrWhiteSpace(artAssetPath) ? Index.FindArtDetail(artAssetPath) : null;
        var artPreview =
            !string.IsNullOrWhiteSpace(artAssetPath) && artPreviewOptions is not null
                ? CreateArtPreview(artAssetPath, artPreviewOptions)
                : null;

        return new EditorObjectPaletteEntry
        {
            Asset = asset,
            ProtoNumber = protoNumber,
            ObjectType = proto.Header.GameObjectType,
            PaletteGroup = GetObjectPaletteGroup(asset.AssetPath),
            DisplayName = ResolveProtoDisplayName(protoNumber),
            NameMessageIndex = TryGetInt32Property(proto, ObjectField.ObjFName),
            DescriptionMessageIndex = TryGetInt32Property(proto, ObjectField.ObjFDescription),
            Description = ResolveMessageText(TryGetInt32Property(proto, ObjectField.ObjFDescription)),
            CurrentArtId = currentArtId,
            ArtAssetPath = artAssetPath,
            ArtDetail = artDetail,
            ArtPreview = artPreview,
        };
    }

    private bool TryResolveProtoCurrentArtId(int protoNumber, out ArtId currentArtId)
    {
        currentArtId = default;
        if (protoNumber <= 0)
            return false;

        var asset = Index.FindProtoDefinition(protoNumber);
        if (asset is null)
            return false;

        var proto = FindProto(asset.AssetPath);
        return proto is not null && TryResolveProtoCurrentArtId(proto, protoNumber, out currentArtId);
    }

    private bool TryResolveProtoCurrentArtId(ProtoData proto, int protoNumber, out ArtId currentArtId)
    {
        ArgumentNullException.ThrowIfNull(proto);

        currentArtId = default;
        var resolvedCurrentArtId = TryGetArtId(proto, ObjectField.ObjFCurrentAid);
        if (resolvedCurrentArtId is { } protoArtId && protoArtId.Value == 0u)
            resolvedCurrentArtId = null;

        if (
            !resolvedCurrentArtId.HasValue
            && proto.Header.GameObjectType is ObjectType.Wall
            && TryResolveWallProtoCurrentArtId(protoNumber, out var wallArtId)
        )
        {
            resolvedCurrentArtId = wallArtId;
        }

        if (resolvedCurrentArtId is not { } finalArtId || finalArtId.Value == 0u)
            return false;

        currentArtId = finalArtId;
        return true;
    }

    private string? ResolveProtoDisplayName(int protoNumber)
    {
        foreach (var messageIndex in EnumerateProtoDisplayNameKeys(protoNumber))
        {
            var overrideText = TryGetMessageText("oemes/oname.mes", messageIndex);
            if (!string.IsNullOrWhiteSpace(overrideText))
                return overrideText;

            var descriptionText = TryGetMessageText("mes/description.mes", messageIndex);
            if (!string.IsNullOrWhiteSpace(descriptionText))
                return descriptionText;
        }

        return null;
    }

    private string? ResolveMessageText(int? messageIndex)
    {
        if (!messageIndex.HasValue)
            return null;

        var assets = Index.FindMessageAssets(messageIndex.Value);
        for (var assetIndex = 0; assetIndex < assets.Count; assetIndex++)
        {
            var text = TryGetMessageText(assets[assetIndex].AssetPath, messageIndex.Value);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }

    private string? TryGetMessageText(string assetPath, int messageIndex)
    {
        var messageFile = FindMessageFile(assetPath);
        if (messageFile is null)
            return null;

        for (var entryIndex = 0; entryIndex < messageFile.Entries.Count; entryIndex++)
        {
            var entry = messageFile.Entries[entryIndex];
            if (entry.Index == messageIndex)
                return entry.Text;
        }

        return null;
    }

    private IEnumerable<int> EnumerateProtoDisplayNameKeys(int protoNumber)
    {
        if (InstallationType.HasValue)
        {
            var translatedKey = ArcanumInstallation.ToVanillaProtoId(protoNumber, InstallationType.Value);
            if (translatedKey > 0 && translatedKey != protoNumber)
                yield return translatedKey;
        }

        yield return protoNumber;
    }

    private static int? TryGetInt32Property(ProtoData proto, ObjectField field)
    {
        var property = proto.GetProperty(field);
        return property is null ? null : property.GetInt32();
    }

    private static ArtId? TryGetArtId(ProtoData proto, ObjectField field)
    {
        var value = TryGetInt32Property(proto, field);
        return value.HasValue ? new ArtId(unchecked((uint)value.Value)) : null;
    }

    private static string? GetObjectPaletteGroup(string assetPath)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        if (!normalizedPath.StartsWith("proto/", StringComparison.OrdinalIgnoreCase))
            return null;

        var groupPath = Path.GetDirectoryName(normalizedPath[("proto/".Length)..]);
        if (string.IsNullOrWhiteSpace(groupPath))
            return null;

        return groupPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private sealed class WallArtLookupData(
        IReadOnlyDictionary<int, WallStructureArtSides> structuresByIndex,
        IReadOnlyDictionary<int, string> defaultPaletteArtAssetPathsByProtoNumber,
        IReadOnlyDictionary<int, ArtId> defaultPaletteArtIdsByProtoNumber
    )
    {
        public IReadOnlyDictionary<int, WallStructureArtSides> StructuresByIndex { get; } = structuresByIndex;

        public IReadOnlyDictionary<int, string> DefaultPaletteArtAssetPathsByProtoNumber { get; } =
            defaultPaletteArtAssetPathsByProtoNumber;

        public IReadOnlyDictionary<int, ArtId> DefaultPaletteArtIdsByProtoNumber { get; } =
            defaultPaletteArtIdsByProtoNumber;
    }

    private sealed class TileNameLookupData(
        IReadOnlyList<string> outdoorFlippableNames,
        IReadOnlyList<string> outdoorNonFlippableNames,
        IReadOnlyList<string> indoorFlippableNames,
        IReadOnlyList<string> indoorNonFlippableNames,
        IReadOnlyDictionary<string, int> outdoorOrderByName
    )
    {
        public static TileNameLookupData Empty { get; } = new([], [], [], [], new Dictionary<string, int>());

        public IReadOnlyList<string> OutdoorFlippableNames { get; } = outdoorFlippableNames;

        public IReadOnlyList<string> OutdoorNonFlippableNames { get; } = outdoorNonFlippableNames;

        public IReadOnlyList<string> IndoorFlippableNames { get; } = indoorFlippableNames;

        public IReadOnlyList<string> IndoorNonFlippableNames { get; } = indoorNonFlippableNames;

        public IReadOnlyDictionary<string, int> OutdoorOrderByName { get; } = outdoorOrderByName;

        public bool HasNames =>
            OutdoorFlippableNames.Count > 0
            || OutdoorNonFlippableNames.Count > 0
            || IndoorFlippableNames.Count > 0
            || IndoorNonFlippableNames.Count > 0;
    }

    private readonly record struct WallStructureArtSides(string InteriorBaseName, string ExteriorBaseName);

    private readonly record struct ObjectPaletteCacheKey(
        EditorArtResolverBindingStrategy ArtBindingStrategy,
        string? SearchText
    );

    private readonly record struct SelectedObjectPaletteEntryCacheKey(
        int ProtoNumber,
        EditorArtResolverBindingStrategy ArtBindingStrategy
    );

    private static bool ObjectPaletteEntryMatches(EditorObjectPaletteEntry entry, string searchText) =>
        entry
            .ProtoNumber.ToString(CultureInfo.InvariantCulture)
            .Contains(searchText, StringComparison.OrdinalIgnoreCase)
        || entry.Asset.AssetPath.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        || entry.ObjectType.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
        || (
            !string.IsNullOrWhiteSpace(entry.PaletteGroup)
            && entry.PaletteGroup.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        )
        || (
            !string.IsNullOrWhiteSpace(entry.ArtAssetPath)
            && entry.ArtAssetPath.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        )
        || (
            !string.IsNullOrWhiteSpace(entry.DisplayName)
            && entry.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        )
        || (
            !string.IsNullOrWhiteSpace(entry.Description)
            && entry.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        );

    private static bool TryGetProtoNumberFromAssetPath(string assetPath, out int protoNumber)
    {
        protoNumber = 0;
        if (string.IsNullOrWhiteSpace(assetPath))
            return false;

        var fileName = Path.GetFileNameWithoutExtension(assetPath);
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var separatorIndex = fileName.IndexOf(' ');
        var numericPrefix = separatorIndex >= 0 ? fileName[..separatorIndex] : fileName;
        return int.TryParse(numericPrefix, NumberStyles.Integer, CultureInfo.InvariantCulture, out protoNumber);
    }

    private static EditorMessageDefinition CreateMessageDetail(
        EditorAssetEntry asset,
        IReadOnlyList<MessageEntry> entries
    )
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(entries);

        var orderedEntries = entries.OrderBy(entry => entry.Index).ToArray();
        return new EditorMessageDefinition
        {
            Asset = asset,
            EntryCount = entries.Count,
            MinEntryIndex = orderedEntries.Length == 0 ? null : orderedEntries[0].Index,
            MaxEntryIndex = orderedEntries.Length == 0 ? null : orderedEntries[^1].Index,
            Entries = [.. entries],
        };
    }

    private static bool MessageMatches(
        EditorAssetEntry asset,
        IReadOnlyList<MessageEntry> entries,
        string searchText
    ) =>
        asset.AssetPath.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        || entries.Any(entry =>
            entry.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || entry
                .Index.ToString(CultureInfo.InvariantCulture)
                .Contains(searchText, StringComparison.OrdinalIgnoreCase)
        );

    private static string NormalizeAssetPath(string assetPath) =>
        assetPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
}
