using System.Globalization;
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
    /// Creates a workspace-owned ART resolver and optionally seeds it from conservative workspace evidence.
    /// </summary>
    public EditorArtResolver CreateArtResolver(EditorArtResolverBindingStrategy bindingStrategy)
    {
        var resolver = new EditorArtResolver(this);
        SeedArtResolver(resolver, bindingStrategy);
        return resolver;
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
    ) => FindObjectPaletteEntry(protoNumber, CreateArtResolver(artBindingStrategy));

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
        Assets
            .FindByFormat(FileFormat.Proto)
            .Select(asset => (Asset: asset, Proto: FindProto(asset.AssetPath)))
            .Where(static pair => pair.Proto is not null && TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out _))
            .Select(pair =>
            {
                _ = TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out var protoNumber);
                return CreateObjectPaletteEntry(pair.Asset, pair.Proto!, protoNumber);
            })
            .OrderBy(entry => entry.ProtoNumber)
            .ThenBy(entry => entry.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order and resolves optional ART bindings.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> GetObjectPalette(EditorArtResolver artResolver)
    {
        ArgumentNullException.ThrowIfNull(artResolver);

        return Assets
            .FindByFormat(FileFormat.Proto)
            .Select(asset => (Asset: asset, Proto: FindProto(asset.AssetPath)))
            .Where(static pair => pair.Proto is not null && TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out _))
            .Select(pair =>
            {
                _ = TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out var protoNumber);
                return CreateObjectPaletteEntry(pair.Asset, pair.Proto!, protoNumber, artResolver);
            })
            .OrderBy(entry => entry.ProtoNumber)
            .ThenBy(entry => entry.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns all loaded proto-backed object palette entries in stable browser order and resolves optional ART bindings
    /// using one workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> GetObjectPalette(
        EditorArtResolverBindingStrategy artBindingStrategy
    ) => GetObjectPalette(CreateArtResolver(artBindingStrategy));

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

        return Assets
            .FindByFormat(FileFormat.Proto)
            .Select(asset => (Asset: asset, Proto: FindProto(asset.AssetPath)))
            .Where(static pair => pair.Proto is not null && TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out _))
            .Select(pair =>
            {
                _ = TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out var protoNumber);
                return CreateObjectPaletteEntry(pair.Asset, pair.Proto!, protoNumber, artResolver, artPreviewOptions);
            })
            .OrderBy(entry => entry.ProtoNumber)
            .ThenBy(entry => entry.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
    /// Returns proto-backed object palette entries whose proto number, asset path, display name,
    /// description, or object type contain the supplied text.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> SearchObjectPalette(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var searchText = text.Trim();
        return Assets
            .FindByFormat(FileFormat.Proto)
            .Select(asset => (Asset: asset, Proto: FindProto(asset.AssetPath)))
            .Where(static pair => pair.Proto is not null && TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out _))
            .Select(pair =>
            {
                _ = TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out var protoNumber);
                return CreateObjectPaletteEntry(pair.Asset, pair.Proto!, protoNumber);
            })
            .Where(entry => ObjectPaletteEntryMatches(entry, searchText))
            .OrderBy(entry => entry.ProtoNumber)
            .ThenBy(entry => entry.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns proto-backed object palette entries whose proto number, asset path, display name,
    /// description, grouping, bound art path, or object type contain the supplied text.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> SearchObjectPalette(string text, EditorArtResolver artResolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(artResolver);

        var searchText = text.Trim();
        return Assets
            .FindByFormat(FileFormat.Proto)
            .Select(asset => (Asset: asset, Proto: FindProto(asset.AssetPath)))
            .Where(static pair => pair.Proto is not null && TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out _))
            .Select(pair =>
            {
                _ = TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out var protoNumber);
                return CreateObjectPaletteEntry(pair.Asset, pair.Proto!, protoNumber, artResolver);
            })
            .Where(entry => ObjectPaletteEntryMatches(entry, searchText))
            .OrderBy(entry => entry.ProtoNumber)
            .ThenBy(entry => entry.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns proto-backed object palette entries whose proto number, asset path, display name,
    /// description, grouping, bound art path, or object type contain the supplied text using one
    /// workspace-created resolver seeded with the supplied strategy.
    /// </summary>
    public IReadOnlyList<EditorObjectPaletteEntry> SearchObjectPalette(
        string text,
        EditorArtResolverBindingStrategy artBindingStrategy
    ) => SearchObjectPalette(text, CreateArtResolver(artBindingStrategy));

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

        var searchText = text.Trim();
        return Assets
            .FindByFormat(FileFormat.Proto)
            .Select(asset => (Asset: asset, Proto: FindProto(asset.AssetPath)))
            .Where(static pair => pair.Proto is not null && TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out _))
            .Select(pair =>
            {
                _ = TryGetProtoNumberFromAssetPath(pair.Asset.AssetPath, out var protoNumber);
                return CreateObjectPaletteEntry(pair.Asset, pair.Proto!, protoNumber, artResolver, artPreviewOptions);
            })
            .Where(entry => ObjectPaletteEntryMatches(entry, searchText))
            .OrderBy(entry => entry.ProtoNumber)
            .ThenBy(entry => entry.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
        return GameData.ArtsBySource.TryGetValue(normalizedPath, out var arts) ? arts.FirstOrDefault() : null;
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

        var normalizedPath = NormalizeAssetPath(mapPropertiesAssetPath);
        var asset = Assets.Find(normalizedPath);
        var properties = FindMapProperties(normalizedPath);
        if (asset is null || properties is null)
            return [];

        var entryCount = checked(properties.LimitX * properties.LimitY);
        if (entryCount == 0)
            return [];

        var entries = new EditorTerrainPaletteEntry[checked((int)entryCount)];
        var index = 0;
        for (ulong paletteY = 0; paletteY < properties.LimitY; paletteY++)
        {
            for (ulong paletteX = 0; paletteX < properties.LimitX; paletteX++)
            {
                entries[index] = CreateTerrainPaletteEntry(
                    asset,
                    properties,
                    paletteX,
                    paletteY,
                    artResolver,
                    artPreviewOptions
                );
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
    /// Returns all terrain palette entries whose asset path, palette coordinates, or derived ART identifier
    /// contain the supplied text.
    /// </summary>
    public IReadOnlyList<EditorTerrainPaletteEntry> SearchTerrainPalette(string text) =>
        SearchTerrainPalette(text, artResolver: null, artPreviewOptions: null);

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

        var searchText = text.Trim();
        return Assets
            .FindByFormat(FileFormat.MapProperties)
            .SelectMany(asset => GetTerrainPalette(asset.AssetPath, artResolver, artPreviewOptions))
            .Where(entry =>
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
            .OrderBy(entry => entry.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.PaletteY)
            .ThenBy(entry => entry.PaletteX)
            .ToArray();
    }

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

        return EditorMapScenePreviewBuilder.Build(projection, sectorsByAssetPath, artResolver);
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

    private void SeedArtResolver(EditorArtResolver resolver, EditorArtResolverBindingStrategy bindingStrategy)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        switch (bindingStrategy)
        {
            case EditorArtResolverBindingStrategy.None:
                return;
            case EditorArtResolverBindingStrategy.Conservative:
                ApplyConservativeArtBindings(resolver);
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(bindingStrategy),
                    bindingStrategy,
                    "Unknown art binding strategy."
                );
        }
    }

    private void ApplyConservativeArtBindings(EditorArtResolver resolver)
    {
        var assetPathsByArtId = new Dictionary<uint, string?>();
        foreach (var artAsset in Assets.FindByFormat(FileFormat.Art))
        {
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

        foreach (var (artId, assetPath) in assetPathsByArtId.OrderBy(static pair => pair.Key))
        {
            if (artId != 0u && !string.IsNullOrWhiteSpace(assetPath))
                resolver.Bind(new ArtId(artId), assetPath);
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

    private EditorTerrainPaletteEntry CreateTerrainPaletteEntry(
        EditorAssetEntry asset,
        MapProperties properties,
        ulong paletteX,
        ulong paletteY,
        EditorArtResolver? artResolver,
        EditorArtPreviewOptions? artPreviewOptions
    )
    {
        var paletteIndex = checked((paletteY * properties.LimitX) + paletteX);
        var artIdValue = checked((uint)(properties.ArtId + (long)paletteIndex));
        var artId = new ArtId(artIdValue);
        var artAssetPath = artResolver?.FindAssetPath(artId);
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

        var currentArtId = TryGetArtId(proto, ObjectField.ObjFCurrentAid);
        var artAssetPath =
            currentArtId.HasValue && artResolver is not null ? artResolver.FindAssetPath(currentArtId.Value) : null;
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
