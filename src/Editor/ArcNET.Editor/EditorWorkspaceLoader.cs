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
        var audioAssets = EditorAudioAssetLoader
            .LoadFromContentDirectoryAsync(contentDirectory)
            .GetAwaiter()
            .GetResult();
        var save = HasSaveSelection(options) ? SaveGameLoader.Load(options.SaveFolder!, options.SaveSlotName!) : null;
        return BuildWorkspace(
            contentDirectory,
            options,
            gameData,
            assets,
            audioAssets,
            EditorWorkspaceLoadReport.Empty,
            save
        );
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

        if (!string.IsNullOrWhiteSpace(effectiveOptions.ModuleName))
            return LoadFromModuleDirectory(
                ResolveModuleDirectory(effectiveOptions.GameDirectory!, effectiveOptions.ModuleName!),
                effectiveOptions
            );

        var (gameData, assets, loadReport) = GameInstallContentLoader
            .LoadAsync(effectiveOptions.GameDirectory!)
            .GetAwaiter()
            .GetResult();
        var audioAssets = EditorAudioAssetLoader
            .LoadFromGameInstallAsync(effectiveOptions.GameDirectory!)
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
            audioAssets,
            loadReport,
            save
        );
    }

    /// <summary>
    /// Loads one workspace from one module directory, overlaying sibling module DAT/PATCH archives and loose module files.
    /// </summary>
    public static EditorWorkspace LoadFromModuleDirectory(
        string moduleDirectory,
        EditorWorkspaceLoadOptions? options = null
    )
    {
        options ??= new EditorWorkspaceLoadOptions();
        var effectiveOptions = CreateModuleOptions(moduleDirectory, options);
        ValidateInstallWorkspaceArguments(effectiveOptions);

        var (gameData, assets, loadReport, archivePaths) = ModuleInstallContentLoader
            .LoadAsync(moduleDirectory)
            .GetAwaiter()
            .GetResult();
        var audioAssets = EditorAudioAssetLoader.LoadFromModuleDirectoryAsync(moduleDirectory).GetAwaiter().GetResult();
        var save = HasSaveSelection(effectiveOptions)
            ? SaveGameLoader.Load(effectiveOptions.SaveFolder!, effectiveOptions.SaveSlotName!)
            : null;

        return BuildWorkspace(
            moduleDirectory,
            effectiveOptions,
            gameData,
            assets,
            audioAssets,
            loadReport,
            save,
            CreateModuleContext(moduleDirectory, archivePaths)
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
        var audioAssets = await EditorAudioAssetLoader
            .LoadFromContentDirectoryAsync(contentDirectory, cancellationToken)
            .ConfigureAwait(false);

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
        return BuildWorkspace(
            contentDirectory,
            options,
            gameData,
            assets,
            audioAssets,
            EditorWorkspaceLoadReport.Empty,
            save
        );
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

        if (!string.IsNullOrWhiteSpace(effectiveOptions.ModuleName))
        {
            return await LoadFromModuleDirectoryAsync(
                    ResolveModuleDirectory(effectiveOptions.GameDirectory!, effectiveOptions.ModuleName!),
                    effectiveOptions,
                    progress,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        var hasSave = HasSaveSelection(effectiveOptions);
        var (gameData, assets, loadReport) = await GameInstallContentLoader
            .LoadAsync(
                effectiveOptions.GameDirectory!,
                hasSave ? CreateWeightedProgress(progress, 0f, GameDataProgressWeight) : progress,
                cancellationToken
            )
            .ConfigureAwait(false);
        var audioAssets = await EditorAudioAssetLoader
            .LoadFromGameInstallAsync(effectiveOptions.GameDirectory!, cancellationToken)
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
            audioAssets,
            loadReport,
            save
        );
    }

    /// <summary>
    /// Loads one workspace from one module directory, overlaying sibling module DAT/PATCH archives and loose module files.
    /// </summary>
    public static async Task<EditorWorkspace> LoadFromModuleDirectoryAsync(
        string moduleDirectory,
        EditorWorkspaceLoadOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        options ??= new EditorWorkspaceLoadOptions();
        var effectiveOptions = CreateModuleOptions(moduleDirectory, options);
        ValidateInstallWorkspaceArguments(effectiveOptions);

        var hasSave = HasSaveSelection(effectiveOptions);
        var (gameData, assets, loadReport, archivePaths) = await ModuleInstallContentLoader
            .LoadAsync(
                moduleDirectory,
                hasSave ? CreateWeightedProgress(progress, 0f, GameDataProgressWeight) : progress,
                cancellationToken
            )
            .ConfigureAwait(false);
        var audioAssets = await EditorAudioAssetLoader
            .LoadFromModuleDirectoryAsync(moduleDirectory, cancellationToken)
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
            moduleDirectory,
            effectiveOptions,
            gameData,
            assets,
            audioAssets,
            loadReport,
            save,
            CreateModuleContext(moduleDirectory, archivePaths)
        );
    }

    private static EditorWorkspace BuildWorkspace(
        string contentDirectory,
        EditorWorkspaceLoadOptions options,
        GameDataStore gameData,
        EditorAssetCatalog assets,
        EditorAudioAssetLoader.EditorAudioAssetLoadResult audioAssets,
        EditorWorkspaceLoadReport loadReport,
        LoadedSave? save,
        EditorWorkspaceModuleContext? module = null
    )
    {
        ArcanumInstallationType? installationType = options.GameDirectory is null
            ? null
            : ArcanumInstallation.Detect(options.GameDirectory);
        var effectiveGameData = EditorWorkspaceSaveComposition.OverlayWorldAssets(gameData, assets, save);
        var (index, validation) = EditorAssetIndexBuilder.Create(effectiveGameData, assets, installationType);

        return new()
        {
            ContentDirectory = contentDirectory,
            GameDirectory = options.GameDirectory,
            Module = module,
            InstallationType = installationType,
            GameData = effectiveGameData,
            Assets = assets,
            AudioAssets = audioAssets.Catalog,
            Index = index,
            LoadReport = loadReport,
            Validation = validation,
            AudioAssetData = audioAssets.DataByPath,
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

        var resolvedGameDirectory = ResolveGameInstallDirectory(gameDir);

        if (
            options.GameDirectory is not null
            && !PathsEqual(resolvedGameDirectory, ResolveGameInstallDirectory(options.GameDirectory))
        )
        {
            throw new ArgumentException(
                "options.GameDirectory must match the explicit gameDir argument when both are supplied.",
                nameof(options)
            );
        }

        return new EditorWorkspaceLoadOptions
        {
            GameDirectory = resolvedGameDirectory,
            ModuleName = options.ModuleName,
            SaveFolder = options.SaveFolder,
            SaveSlotName = options.SaveSlotName,
        };
    }

    private static EditorWorkspaceLoadOptions CreateModuleOptions(
        string moduleDirectory,
        EditorWorkspaceLoadOptions options
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);

        var resolvedModuleDirectory = Path.GetFullPath(moduleDirectory);
        if (!Directory.Exists(resolvedModuleDirectory))
            throw new DirectoryNotFoundException($"Module directory not found: {resolvedModuleDirectory}");

        var moduleName = Path.GetFileName(
            resolvedModuleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );
        var modulesDirectory =
            Directory.GetParent(resolvedModuleDirectory)?.FullName
            ?? throw new ArgumentException(
                "Module directory must have one parent modules directory.",
                nameof(moduleDirectory)
            );
        var gameDirectory =
            Directory.GetParent(modulesDirectory)?.FullName
            ?? throw new ArgumentException(
                "Module directory must live under one game install modules directory.",
                nameof(moduleDirectory)
            );

        if (
            options.GameDirectory is not null
            && !PathsEqual(gameDirectory, ResolveGameInstallDirectory(options.GameDirectory))
        )
        {
            throw new ArgumentException(
                "options.GameDirectory must match the module directory's owning game install when both are supplied.",
                nameof(options)
            );
        }

        if (
            options.ModuleName is not null
            && !string.Equals(options.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new ArgumentException(
                "options.ModuleName must match the supplied module directory name when both are supplied.",
                nameof(options)
            );
        }

        return new EditorWorkspaceLoadOptions
        {
            GameDirectory = gameDirectory,
            ModuleName = moduleName,
            SaveFolder = options.SaveFolder,
            SaveSlotName = options.SaveSlotName,
        };
    }

    private static string ResolveGameInstallDirectory(string gameDir)
    {
        var fullPath = Path.GetFullPath(gameDir);
        if (!Directory.Exists(fullPath) || LooksLikeGameInstallDirectory(fullPath))
            return fullPath;

        var preferredNestedPath = Path.Combine(fullPath, "Arcanum");
        if (LooksLikeGameInstallDirectory(preferredNestedPath))
            return preferredNestedPath;

        var matchingChildDirectories = Directory
            .EnumerateDirectories(fullPath)
            .Where(LooksLikeGameInstallDirectory)
            .Take(2)
            .ToArray();

        return matchingChildDirectories.Length == 1 ? matchingChildDirectories[0] : fullPath;
    }

    private static bool LooksLikeGameInstallDirectory(string path)
    {
        if (!Directory.Exists(path))
            return false;

        return Directory.Exists(Path.Combine(path, LooseDataDirectoryName))
            || Directory.Exists(Path.Combine(path, "modules"))
            || Directory.EnumerateFiles(path, "*.dat", SearchOption.TopDirectoryOnly).Any();
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

        if (
            !string.IsNullOrWhiteSpace(options.ModuleName)
            && !Directory.Exists(Path.Combine(options.GameDirectory, "modules", options.ModuleName))
        )
        {
            throw new DirectoryNotFoundException(
                $"Module directory not found: {Path.Combine(options.GameDirectory, "modules", options.ModuleName)}"
            );
        }

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

    private static string ResolveModuleDirectory(string gameDir, string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        return Path.Combine(gameDir, "modules", moduleName);
    }

    private static EditorWorkspaceModuleContext CreateModuleContext(
        string moduleDirectory,
        IReadOnlyList<string> archivePaths
    ) =>
        new()
        {
            ModuleName = Path.GetFileName(
                moduleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            ),
            ModuleDirectory = moduleDirectory,
            SaveDirectory = Directory.Exists(Path.Combine(moduleDirectory, "Save"))
                ? Path.Combine(moduleDirectory, "Save")
                : null,
            ArchivePaths = archivePaths,
        };

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase
        );
}
