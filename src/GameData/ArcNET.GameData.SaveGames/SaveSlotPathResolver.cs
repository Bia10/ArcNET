namespace ArcNET.Editor;

internal static class SaveSlotPathResolver
{
    public static SaveSlotPaths ResolveFromFolder(string saveFolder, string slotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);

        return new SaveSlotPaths(
            ResolveGsiPath(saveFolder, slotName),
            Path.Combine(saveFolder, slotName + ".tfai"),
            Path.Combine(saveFolder, slotName + ".tfaf")
        );
    }

    private static string ResolveGsiPath(string saveFolder, string slotName)
    {
        var searchFolder = string.IsNullOrEmpty(saveFolder) ? "." : saveFolder;
        var exactPath = string.IsNullOrEmpty(saveFolder)
            ? slotName + ".gsi"
            : Path.Combine(saveFolder, slotName + ".gsi");
        if (File.Exists(exactPath) || !Directory.Exists(searchFolder))
            return exactPath;

        var matches = Directory
            .EnumerateFiles(searchFolder, "*.gsi", SearchOption.TopDirectoryOnly)
            .Where(path =>
                Path.GetFileNameWithoutExtension(path).StartsWith(slotName, StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();
        if (matches.Length == 1)
            return string.IsNullOrEmpty(saveFolder) ? Path.GetFileName(matches[0]) : matches[0];

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple .gsi files matched logical save slot stem '{slotName}' in '{saveFolder}'."
            );
        }

        return exactPath;
    }
}

internal readonly record struct SaveSlotPaths(string GsiPath, string TfaiPath, string TfafPath);
