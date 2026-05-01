namespace ArcNET.Formats;

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

    public static SaveSlotPaths ResolveFromTfaiPath(string tfaiPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tfaiPath);

        var saveFolder = Path.GetDirectoryName(tfaiPath) ?? string.Empty;
        var slotName = Path.GetFileNameWithoutExtension(tfaiPath);
        return new SaveSlotPaths(
            ResolveGsiPath(saveFolder, slotName),
            tfaiPath,
            Path.ChangeExtension(tfaiPath, ".tfaf")
        );
    }

    public static SaveSlotPaths ResolveFromTfaiAndTfafPaths(string tfaiPath, string tfafPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tfaiPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(tfafPath);

        var saveFolder = Path.GetDirectoryName(tfaiPath) ?? string.Empty;
        var slotName = Path.GetFileNameWithoutExtension(tfaiPath);
        return new SaveSlotPaths(ResolveGsiPath(saveFolder, slotName), tfaiPath, tfafPath);
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
