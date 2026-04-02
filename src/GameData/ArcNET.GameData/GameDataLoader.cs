using ArcNET.Formats;

namespace ArcNET.GameData;

/// <summary>
/// High-level loader that discovers and categorizes game data files in a directory tree.
/// Replaces the old <c>Parser.LoadLocalData</c> method.
/// </summary>
public static class GameDataLoader
{
    /// <summary>Discovers all files grouped by their <see cref="FileFormat"/>.</summary>
    /// <param name="dirPath">Root directory to search recursively.</param>
    /// <returns>A dictionary mapping format to matched file paths.</returns>
    public static IReadOnlyDictionary<FileFormat, IReadOnlyList<string>> DiscoverFiles(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            throw new DirectoryNotFoundException($"Directory not found: {dirPath}");

        var allFiles = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories);
        var result = new Dictionary<FileFormat, List<string>>();

        foreach (var format in Enum.GetValues<FileFormat>())
            result[format] = [];

        foreach (var file in allFiles)
        {
            var format = FileFormatExtensions.FromPath(file);

            // FacadeWalk uses filename pattern instead of extension
            if (format == FileFormat.Unknown)
            {
                var name = Path.GetFileName(file);
                if (name.StartsWith("facwalk.", StringComparison.OrdinalIgnoreCase))
                    format = FileFormat.FacadeWalk;
            }

            result[format].Add(file);
        }

        return result.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());
    }

    /// <summary>Parses all .mes message files in the given directory and returns a merged lookup.</summary>
    public static IReadOnlyDictionary<int, string> LoadMessages(string dirPath)
    {
        var files = DiscoverFiles(dirPath);
        var result = new Dictionary<int, string>();

        if (!files.TryGetValue(FileFormat.Message, out var mesFiles))
            return result;

        foreach (var file in mesFiles)
        {
            var entries = MessageFormat.ParseFile(file);
            foreach (var entry in entries)
                result.TryAdd(entry.Index, entry.Text);
        }

        return result;
    }
}
