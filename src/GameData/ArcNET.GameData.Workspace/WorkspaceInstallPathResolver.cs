namespace ArcNET.GameData.Workspace;

public static class WorkspaceInstallPathResolver
{
    private const string LooseDataDirectoryName = "data";
    private const string ModulesDirectoryName = "modules";

    public static string ResolveGameInstallDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
            fullPath = Path.GetDirectoryName(fullPath) ?? fullPath;

        if (!Directory.Exists(fullPath))
            return fullPath;

        if (TryResolveOwningGameDirectoryFromModuleDirectory(fullPath, out var gameDirectory))
            return gameDirectory;

        if (TryResolveOwningGameDirectoryFromModulesDirectory(fullPath, out gameDirectory))
            return gameDirectory;

        if (LooksLikeGameInstallDirectory(fullPath))
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

    public static string ResolveWorkspaceDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return TryResolveWorkspaceModuleDirectory(path, out var moduleDirectory)
            ? moduleDirectory
            : ResolveGameInstallDirectory(ResolveDirectoryPath(Path.GetFullPath(path)));
    }

    public static string ResolveModuleDirectory(string gameDirectory, string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        return Path.Combine(ResolveGameInstallDirectory(gameDirectory), ModulesDirectoryName, moduleName);
    }

    public static bool TryResolveWorkspaceModuleDirectory(string path, out string moduleDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        if (TryResolveModuleDirectoryFromArchivePath(fullPath, out moduleDirectory))
            return true;

        var current = new DirectoryInfo(ResolveDirectoryPath(fullPath));
        while (current.Parent is not null)
        {
            var parent = current.Parent;
            if (
                parent.Parent is not null
                && parent.Name.Equals(ModulesDirectoryName, StringComparison.OrdinalIgnoreCase)
                && LooksLikeGameInstallDirectory(parent.Parent.FullName)
            )
            {
                moduleDirectory = current.FullName;
                return true;
            }

            current = parent;
        }

        moduleDirectory = string.Empty;
        return false;
    }

    public static bool TryResolveSingleModuleDirectory(string gameDirectory, out string moduleDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);

        var resolvedGameDirectory = ResolveGameInstallDirectory(gameDirectory);
        var modulesDirectory = Path.Combine(resolvedGameDirectory, ModulesDirectoryName);
        if (!Directory.Exists(modulesDirectory))
        {
            moduleDirectory = string.Empty;
            return false;
        }

        HashSet<string> candidateDirectories = new(StringComparer.OrdinalIgnoreCase);
        foreach (
            var candidateDirectory in Directory.EnumerateDirectories(
                modulesDirectory,
                "*",
                SearchOption.TopDirectoryOnly
            )
        )
            candidateDirectories.Add(Path.GetFullPath(candidateDirectory));

        foreach (var archivePath in Directory.EnumerateFiles(modulesDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            if (TryParseModuleArchiveName(Path.GetFileName(archivePath), out var moduleName))
                candidateDirectories.Add(Path.Combine(modulesDirectory, moduleName));
        }

        var matchingModuleDirectories = candidateDirectories
            .Where(WorkspaceContentLoader.HasModuleContent)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();
        if (matchingModuleDirectories.Length == 1)
        {
            moduleDirectory = matchingModuleDirectories[0];
            return true;
        }

        moduleDirectory = string.Empty;
        return false;
    }

    public static string ResolveOwningGameDirectoryFromModuleDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var resolvedModuleDirectory = Path.GetFullPath(path);
        var modulesDirectory =
            Directory.GetParent(resolvedModuleDirectory)?.FullName
            ?? throw new ArgumentException("Module directory must have one parent modules directory.", nameof(path));
        if (!IsNamedDirectory(modulesDirectory, ModulesDirectoryName))
        {
            throw new ArgumentException(
                "Module directory must live directly under one game install modules directory.",
                nameof(path)
            );
        }

        return Directory.GetParent(modulesDirectory)?.FullName
            ?? throw new ArgumentException(
                "Module directory must live under one game install modules directory.",
                nameof(path)
            );
    }

    public static bool LooksLikeGameInstallDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Directory.Exists(path)
            && (
                Directory.Exists(Path.Combine(path, LooseDataDirectoryName))
                || Directory.Exists(Path.Combine(path, ModulesDirectoryName))
                || Directory.EnumerateFiles(path, "*.dat", SearchOption.TopDirectoryOnly).Any()
            );
    }

    public static bool TryResolveOwningGameDirectoryFromModulesDirectory(string path, out string gameDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        gameDirectory = string.Empty;
        if (!IsNamedDirectory(path, ModulesDirectoryName))
            return false;

        var candidateGameDirectory = Directory.GetParent(path)?.FullName;
        if (candidateGameDirectory is null || !LooksLikeGameInstallDirectory(candidateGameDirectory))
            return false;

        gameDirectory = candidateGameDirectory;
        return true;
    }

    public static bool TryResolveOwningGameDirectoryFromModuleDirectory(string path, out string gameDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        gameDirectory = string.Empty;
        var modulesDirectory = Directory.GetParent(Path.GetFullPath(path))?.FullName;
        if (modulesDirectory is null || !IsNamedDirectory(modulesDirectory, ModulesDirectoryName))
            return false;

        var candidateGameDirectory = Directory.GetParent(modulesDirectory)?.FullName;
        if (candidateGameDirectory is null || !LooksLikeGameInstallDirectory(candidateGameDirectory))
            return false;

        gameDirectory = candidateGameDirectory;
        return true;
    }

    private static string ResolveDirectoryPath(string path) =>
        File.Exists(path) || (!Directory.Exists(path) && Path.HasExtension(path))
            ? Path.GetDirectoryName(path) ?? path
            : path;

    private static bool TryResolveModuleDirectoryFromArchivePath(string path, out string moduleDirectory)
    {
        var parentDirectory = Path.GetDirectoryName(path);
        if (
            string.IsNullOrWhiteSpace(parentDirectory)
            || !Path.GetFileName(parentDirectory).Equals(ModulesDirectoryName, StringComparison.OrdinalIgnoreCase)
        )
        {
            moduleDirectory = string.Empty;
            return false;
        }

        var gameDirectory = Directory.GetParent(parentDirectory)?.FullName;
        if (string.IsNullOrWhiteSpace(gameDirectory) || !LooksLikeGameInstallDirectory(gameDirectory))
        {
            moduleDirectory = string.Empty;
            return false;
        }

        if (!TryParseModuleArchiveName(Path.GetFileName(path), out var moduleName))
        {
            moduleDirectory = string.Empty;
            return false;
        }

        moduleDirectory = Path.Combine(parentDirectory, moduleName);
        return true;
    }

    private static bool TryParseModuleArchiveName(string fileName, out string moduleName)
    {
        if (fileName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
        {
            moduleName = Path.GetFileNameWithoutExtension(fileName);
            return !string.IsNullOrWhiteSpace(moduleName);
        }

        var patchIndex = fileName.IndexOf(".PATCH", StringComparison.OrdinalIgnoreCase);
        if (patchIndex > 0)
        {
            moduleName = fileName[..patchIndex];
            return true;
        }

        moduleName = string.Empty;
        return false;
    }

    private static bool IsNamedDirectory(string path, string expectedName) =>
        string.Equals(
            Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            expectedName,
            StringComparison.OrdinalIgnoreCase
        );
}
