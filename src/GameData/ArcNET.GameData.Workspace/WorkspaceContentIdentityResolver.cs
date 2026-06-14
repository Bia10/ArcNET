namespace ArcNET.GameData.Workspace;

internal enum WorkspaceContentIdentityKind
{
    GameInstall,
    Module,
}

internal readonly record struct WorkspaceContentIdentity(WorkspaceContentIdentityKind Kind, string Path)
{
    public string CacheKey => $"{(Kind == WorkspaceContentIdentityKind.Module ? "module" : "install")}::{Path}";
}

internal static class WorkspaceContentIdentityResolver
{
    private const string ModuleCacheKeyPrefix = "module::";
    private const string InstallCacheKeyPrefix = "install::";

    public static WorkspaceContentIdentity Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return WorkspaceInstallPathResolver.TryResolveWorkspaceModuleDirectory(path, out var moduleDirectory)
            ? new WorkspaceContentIdentity(WorkspaceContentIdentityKind.Module, Path.GetFullPath(moduleDirectory))
            : new WorkspaceContentIdentity(
                WorkspaceContentIdentityKind.GameInstall,
                Path.GetFullPath(WorkspaceInstallPathResolver.ResolveGameInstallDirectory(path))
            );
    }

    public static string ResolveGameDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var identity = Resolve(path);
        return identity.Kind == WorkspaceContentIdentityKind.Module
            ? WorkspaceInstallPathResolver.ResolveOwningGameDirectoryFromModuleDirectory(identity.Path)
            : identity.Path;
    }

    public static bool ReferencesGameDirectory(WorkspaceContentIdentity identity, string gameDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);

        var resolvedGameDirectory = Path.GetFullPath(gameDirectory);
        return identity.Kind == WorkspaceContentIdentityKind.Module
            ? PathsEqual(
                WorkspaceInstallPathResolver.ResolveOwningGameDirectoryFromModuleDirectory(identity.Path),
                resolvedGameDirectory
            )
            : PathsEqual(identity.Path, resolvedGameDirectory);
    }

    public static bool TryParseCacheKey(string cacheKey, out WorkspaceContentIdentity identity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        if (cacheKey.StartsWith(ModuleCacheKeyPrefix, StringComparison.Ordinal))
        {
            identity = new WorkspaceContentIdentity(
                WorkspaceContentIdentityKind.Module,
                cacheKey[ModuleCacheKeyPrefix.Length..]
            );
            return true;
        }

        if (cacheKey.StartsWith(InstallCacheKeyPrefix, StringComparison.Ordinal))
        {
            identity = new WorkspaceContentIdentity(
                WorkspaceContentIdentityKind.GameInstall,
                cacheKey[InstallCacheKeyPrefix.Length..]
            );
            return true;
        }

        identity = default;
        return false;
    }

    private static bool PathsEqual(string left, string right) =>
        Path.GetFullPath(left).Equals(Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
}
