using ArcNET.Core;
using ArcNET.GameData;

namespace ArcNET.Editor;

/// <summary>
/// Loads an <see cref="EditorWorkspace"/> for frontend-oriented editing scenarios.
/// </summary>
public static class EditorWorkspaceLoader
{
    private const float GameDataProgressWeight = 0.75f;
    private const string LooseDataDirectoryName = "data";

    /// <summary>
    /// Loads loose game data from <paramref name="contentDirectory"/> and, when requested,
    /// also loads a save slot into the returned <see cref="EditorWorkspace"/>.
    /// </summary>
    /// <param name="contentDirectory">
    /// Loose or extracted content directory loaded through <see cref="GameDataLoader"/>.
    /// </param>
    /// <param name="options">
    /// Optional game-install and save-slot inputs for the workspace.
    /// </param>
    public static EditorWorkspace Load(string contentDirectory, EditorWorkspaceLoadOptions? options = null)
    {
        options ??= new EditorWorkspaceLoadOptions();
        ValidateContentWorkspaceArguments(contentDirectory, options);

        var gameData = GameDataLoader.LoadFromDirectoryAsync(contentDirectory).GetAwaiter().GetResult();
        var assets = EditorAssetCatalogBuilder.CreateForContentDirectory(contentDirectory, gameData);
        var save = HasSaveSelection(options) ? SaveGameLoader.Load(options.SaveFolder!, options.SaveSlotName!) : null;
        return BuildWorkspace(contentDirectory, options, gameData, assets, EditorWorkspaceLoadReport.Empty, save);
    }

    /// <summary>
    /// Loads a workspace directly from an Arcanum installation by reading supported loose
    /// overrides from <c>data\</c> and supported entries from the installation's DAT archives.
    /// </summary>
    /// <param name="gameDir">Root directory of the Arcanum installation.</param>
    /// <param name="options">Optional save-slot inputs for the workspace.</param>
    public static EditorWorkspace LoadFromGameInstall(string gameDir, EditorWorkspaceLoadOptions? options = null)
    {
        var effectiveOptions = CreateInstallOptions(gameDir, options);
        ValidateInstallWorkspaceArguments(effectiveOptions);

        var (gameData, assets, loadReport) = GameInstallContentLoader
            .LoadAsync(effectiveOptions.GameDirectory!)
            .GetAwaiter()
            .GetResult();
        var save = HasSaveSelection(effectiveOptions)
            ? SaveGameLoader.Load(effectiveOptions.SaveFolder!, effectiveOptions.SaveSlotName!)
            : null;

        return BuildWorkspace(
            GetLooseDataDirectory(effectiveOptions.GameDirectory!),
            effectiveOptions,
            gameData,
            assets,
            loadReport,
            save
        );
    }

    /// <summary>
    /// Loads loose game data from <paramref name="contentDirectory"/> and, when requested,
    /// also loads a save slot into the returned <see cref="EditorWorkspace"/>.
    /// </summary>
    /// <param name="contentDirectory">
    /// Loose or extracted content directory loaded through <see cref="GameDataLoader"/>.
    /// </param>
    /// <param name="options">
    /// Optional game-install and save-slot inputs for the workspace.
    /// </param>
    /// <param name="progress">
    /// Optional progress reporter. When a save is requested, progress is weighted so game-data
    /// loading contributes 75% and save loading contributes 25% of the total.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the load operation.</param>
    public static async Task<EditorWorkspace> LoadAsync(
        string contentDirectory,
        EditorWorkspaceLoadOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        options ??= new EditorWorkspaceLoadOptions();
        ValidateContentWorkspaceArguments(contentDirectory, options);

        var hasSave = HasSaveSelection(options);
        var gameData = await GameDataLoader
            .LoadFromDirectoryAsync(
                contentDirectory,
                hasSave ? CreateWeightedProgress(progress, 0f, GameDataProgressWeight) : progress,
                cancellationToken
            )
            .ConfigureAwait(false);
        var assets = EditorAssetCatalogBuilder.CreateForContentDirectory(contentDirectory, gameData);

        cancellationToken.ThrowIfCancellationRequested();

        LoadedSave? save = null;
        if (hasSave)
        {
            save = await SaveGameLoader
                .LoadAsync(
                    options.SaveFolder!,
                    options.SaveSlotName!,
                    CreateWeightedProgress(progress, GameDataProgressWeight, 1f - GameDataProgressWeight),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        progress?.Report(1f);
        return BuildWorkspace(contentDirectory, options, gameData, assets, EditorWorkspaceLoadReport.Empty, save);
    }

    /// <summary>
    /// Loads a workspace directly from an Arcanum installation by reading supported loose
    /// overrides from <c>data\</c> and supported entries from the installation's DAT archives.
    /// </summary>
    /// <param name="gameDir">Root directory of the Arcanum installation.</param>
    /// <param name="options">Optional save-slot inputs for the workspace.</param>
    /// <param name="progress">
    /// Optional progress reporter. When a save is requested, progress is weighted so game-data
    /// loading contributes 75% and save loading contributes 25% of the total.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the load operation.</param>
    public static async Task<EditorWorkspace> LoadFromGameInstallAsync(
        string gameDir,
        EditorWorkspaceLoadOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var effectiveOptions = CreateInstallOptions(gameDir, options);
        ValidateInstallWorkspaceArguments(effectiveOptions);

        var hasSave = HasSaveSelection(effectiveOptions);
        var (gameData, assets, loadReport) = await GameInstallContentLoader
            .LoadAsync(
                effectiveOptions.GameDirectory!,
                hasSave ? CreateWeightedProgress(progress, 0f, GameDataProgressWeight) : progress,
                cancellationToken
            )
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        LoadedSave? save = null;
        if (hasSave)
        {
            save = await SaveGameLoader
                .LoadAsync(
                    effectiveOptions.SaveFolder!,
                    effectiveOptions.SaveSlotName!,
                    CreateWeightedProgress(progress, GameDataProgressWeight, 1f - GameDataProgressWeight),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        progress?.Report(1f);
        return BuildWorkspace(
            GetLooseDataDirectory(effectiveOptions.GameDirectory!),
            effectiveOptions,
            gameData,
            assets,
            loadReport,
            save
        );
    }

    private static EditorWorkspace BuildWorkspace(
        string contentDirectory,
        EditorWorkspaceLoadOptions options,
        GameDataStore gameData,
        EditorAssetCatalog assets,
        EditorWorkspaceLoadReport loadReport,
        LoadedSave? save
    )
    {
        ArcanumInstallationType? installationType = options.GameDirectory is null
            ? null
            : ArcanumInstallation.Detect(options.GameDirectory);
        var (index, validation) = EditorAssetIndexBuilder.Create(gameData, assets, installationType);

        return new()
        {
            ContentDirectory = contentDirectory,
            GameDirectory = options.GameDirectory,
            InstallationType = installationType,
            GameData = gameData,
            Assets = assets,
            Index = index,
            LoadReport = loadReport,
            Validation = validation,
            Save = save,
            SaveFolder = options.SaveFolder,
            SaveSlotName = options.SaveSlotName,
        };
    }

    private static bool HasSaveSelection(EditorWorkspaceLoadOptions options) =>
        !string.IsNullOrWhiteSpace(options.SaveFolder) || !string.IsNullOrWhiteSpace(options.SaveSlotName);

    private static IProgress<float>? CreateWeightedProgress(IProgress<float>? progress, float offset, float span)
    {
        if (progress is null)
            return null;

        return new Progress<float>(value => progress.Report(offset + value * span));
    }

    private static EditorWorkspaceLoadOptions CreateInstallOptions(string gameDir, EditorWorkspaceLoadOptions? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        options ??= new EditorWorkspaceLoadOptions();

        if (options.GameDirectory is not null && !PathsEqual(gameDir, options.GameDirectory))
        {
            throw new ArgumentException(
                "options.GameDirectory must match the explicit gameDir argument when both are supplied.",
                nameof(options)
            );
        }

        return new EditorWorkspaceLoadOptions
        {
            GameDirectory = gameDir,
            SaveFolder = options.SaveFolder,
            SaveSlotName = options.SaveSlotName,
        };
    }

    private static void ValidateContentWorkspaceArguments(string contentDirectory, EditorWorkspaceLoadOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDirectory);

        if (!Directory.Exists(contentDirectory))
            throw new DirectoryNotFoundException($"Content directory not found: {contentDirectory}");

        ValidateSharedOptions(options);
    }

    private static void ValidateInstallWorkspaceArguments(EditorWorkspaceLoadOptions options)
    {
        if (options.GameDirectory is null)
            throw new ArgumentException(
                "GameDirectory is required for install-backed workspace loading.",
                nameof(options)
            );

        if (!Directory.Exists(options.GameDirectory))
            throw new DirectoryNotFoundException($"Game directory not found: {options.GameDirectory}");

        ValidateSharedOptions(options);
    }

    private static void ValidateSharedOptions(EditorWorkspaceLoadOptions options)
    {
        if (options.GameDirectory is not null && !Directory.Exists(options.GameDirectory))
            throw new DirectoryNotFoundException($"Game directory not found: {options.GameDirectory}");

        var hasSaveFolder = !string.IsNullOrWhiteSpace(options.SaveFolder);
        var hasSaveSlot = !string.IsNullOrWhiteSpace(options.SaveSlotName);
        if (hasSaveFolder != hasSaveSlot)
        {
            throw new ArgumentException(
                "SaveFolder and SaveSlotName must be supplied together when loading a save-backed workspace.",
                nameof(options)
            );
        }

        if (hasSaveFolder && !Directory.Exists(options.SaveFolder!))
            throw new DirectoryNotFoundException($"Save folder not found: {options.SaveFolder}");
    }

    private static string GetLooseDataDirectory(string gameDir) => Path.Combine(gameDir, LooseDataDirectoryName);

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase
        );
}
