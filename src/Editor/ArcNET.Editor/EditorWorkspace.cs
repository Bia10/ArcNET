using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData;

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
    /// Creates a live mutable editor session on top of this loaded workspace snapshot.
    /// Hosts can reuse the returned session to keep transactional dialog, script, and save editors alive
    /// while tracking dirty state across the current workspace.
    /// </summary>
    public EditorWorkspaceSession CreateSession() => new(this);

    /// <summary>
    /// Creates a workspace-owned ART resolver that can bind known <see cref="ArtId"/> values to loaded ART asset paths.
    /// </summary>
    public EditorArtResolver CreateArtResolver() => new(this);

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

    private static string NormalizeAssetPath(string assetPath) =>
        assetPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
}
