using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Read-only catalog of parsed game-data assets available through an <see cref="EditorWorkspace"/>.
/// </summary>
public sealed class EditorAssetCatalog
{
    private readonly IReadOnlyDictionary<string, EditorAssetEntry> _entriesByPath;
    private readonly IReadOnlyDictionary<FileFormat, IReadOnlyList<EditorAssetEntry>> _entriesByFormat;

    private EditorAssetCatalog(EditorAssetEntry[] entries)
    {
        Entries = entries;
        Count = entries.Length;
        _entriesByPath = entries.ToDictionary(entry => entry.AssetPath, StringComparer.OrdinalIgnoreCase);
        _entriesByFormat = entries
            .GroupBy(entry => entry.Format)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<EditorAssetEntry>)group.ToArray());
    }

    /// <summary>
    /// Empty asset catalog used when no assets were loaded.
    /// </summary>
    public static EditorAssetCatalog Empty { get; } = new([]);

    /// <summary>
    /// All parsed asset entries in stable path order.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> Entries { get; }

    /// <summary>
    /// Total number of asset entries in the catalog.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Looks up an asset by its relative or virtual path.
    /// Returns <see langword="null"/> when no matching asset is present.
    /// </summary>
    public EditorAssetEntry? Find(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        var normalizedPath = NormalizeAssetPath(assetPath);
        return _entriesByPath.TryGetValue(normalizedPath, out var entry) ? entry : null;
    }

    /// <summary>
    /// Returns all assets with the supplied parsed format.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> FindByFormat(FileFormat format) =>
        _entriesByFormat.TryGetValue(format, out var entries) ? entries : [];

    /// <summary>
    /// Searches asset paths for the supplied text, optionally constrained to one parsed format.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> Search(string text, FileFormat? format = null)
    {
        var searchText = ValidateSearchText(text);
        var entries = format is { } filteredFormat ? FindByFormat(filteredFormat) : Entries;
        if (entries.Count == 0)
            return [];

        return entries.Where(entry => ContainsSearchText(entry.AssetPath, searchText)).ToArray();
    }

    internal static EditorAssetCatalog Create(IEnumerable<EditorAssetEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var orderedEntries = entries.OrderBy(entry => entry.AssetPath, StringComparer.OrdinalIgnoreCase).ToArray();

        return orderedEntries.Length == 0 ? Empty : new EditorAssetCatalog(orderedEntries);
    }

    private static string NormalizeAssetPath(string assetPath) =>
        assetPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

    private static string ValidateSearchText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return text.Trim();
    }

    private static bool ContainsSearchText(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
