using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics;

internal static class DiagnosticsWorkspacePathResolver
{
    public static string ResolveLocalWorkspacePath(params string?[] pathHints)
    {
        foreach (var pathHint in pathHints)
        {
            if (TryResolvePreferredWorkspacePath(pathHint, out var workspacePath))
                return workspacePath;
        }

        return string.Empty;
    }

    private static bool TryResolvePreferredWorkspacePath(string? path, out string workspacePath)
    {
        workspacePath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var resolvedWorkspacePath = Path.GetFullPath(
            WorkspaceInstallPathResolver.ResolveWorkspaceDirectory(path.Trim())
        );
        if (WorkspaceInstallPathResolver.TryResolveOwningGameDirectoryFromModuleDirectory(resolvedWorkspacePath, out _))
        {
            workspacePath = resolvedWorkspacePath;
            return true;
        }

        var gameDirectory = Path.GetFullPath(
            WorkspaceInstallPathResolver.ResolveGameInstallDirectory(resolvedWorkspacePath)
        );
        if (WorkspaceInstallPathResolver.TryResolveSingleModuleDirectory(gameDirectory, out var moduleDirectory))
        {
            workspacePath = Path.GetFullPath(moduleDirectory);
            return true;
        }

        workspacePath = resolvedWorkspacePath;
        return workspacePath.Length > 0;
    }
}
