using System.Collections.Concurrent;
using ArcNET.Core;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Loads and caches the shared local game-data catalog for a resolved install.
/// </summary>
public static class WorkspaceGameDataCatalogLoader
{
    public static Task<WorkspaceGameDataCatalog> LoadFromModulePathAsync(string modulePath, bool forceReload = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modulePath);

        var identity = WorkspaceContentIdentityResolver.Resolve(modulePath);
        return LoadAsync(identity, forceReload);
    }

    public static Task<WorkspaceGameDataCatalog> LoadFromGameDirectoryAsync(
        string gameDirectory,
        bool forceReload = false
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);

        return LoadAsync(
            new WorkspaceContentIdentity(WorkspaceContentIdentityKind.GameInstall, Path.GetFullPath(gameDirectory)),
            forceReload
        );
    }

    private static async Task<WorkspaceGameDataCatalog> LoadAsync(WorkspaceContentIdentity identity, bool forceReload)
    {
        if (forceReload)
            InvalidateForGameDirectory(identity);

        while (true)
        {
            var cached = s_catalogs.GetOrAdd(
                identity.CacheKey,
                _ => new Lazy<Task<WorkspaceGameDataCatalog>>(
                    () => CreateAsync(identity),
                    LazyThreadSafetyMode.ExecutionAndPublication
                )
            );
            try
            {
                return await cached.Value.ConfigureAwait(false);
            }
            catch
            {
                if (s_catalogs.TryGetValue(identity.CacheKey, out var current) && ReferenceEquals(current, cached))
                    _ = s_catalogs.TryRemove(identity.CacheKey, out _);

                throw;
            }
        }
    }

    private static async Task<WorkspaceGameDataCatalog> CreateAsync(WorkspaceContentIdentity identity)
    {
        var loadResult =
            identity.Kind == WorkspaceContentIdentityKind.Module
                ? await WorkspaceContentLoader.LoadModuleAsync(identity.Path).ConfigureAwait(false)
                : await WorkspaceContentLoader.LoadGameInstallAsync(identity.Path).ConfigureAwait(false);
        var installationType = ArcanumInstallation.Detect(
            identity.Kind == WorkspaceContentIdentityKind.Module
                ? WorkspaceInstallPathResolver.ResolveOwningGameDirectoryFromModuleDirectory(identity.Path)
                : identity.Path
        );
        var prototypeEntries = WorkspacePrototypeCatalogBuilder.Build(loadResult.GameData, installationType);
        var worldAreaCatalog = WorkspaceWorldAreaCatalogBuilder.Build(loadResult.GameData);
        var tileArtEntries = WorkspaceTileArtCatalogBuilder.Build(loadResult.GameData);
        var staticObjectEntries = WorkspaceStaticObjectCatalogBuilder.Build(loadResult.GameData, prototypeEntries);

        return new WorkspaceGameDataCatalog(
            [
                .. prototypeEntries
                    .OrderBy(static entry => entry.ObjectType.ToString(), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static entry => entry.DisplayName ?? entry.AssetPath, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static entry => entry.ProtoNumber),
            ],
            worldAreaCatalog,
            [
                .. tileArtEntries
                    .OrderBy(static entry => entry.ArtId.Type.ToString(), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static entry => entry.ArtId.Value),
            ],
            [.. staticObjectEntries]
        );
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

    private static readonly ConcurrentDictionary<string, Lazy<Task<WorkspaceGameDataCatalog>>> s_catalogs = new(
        StringComparer.OrdinalIgnoreCase
    );
}
