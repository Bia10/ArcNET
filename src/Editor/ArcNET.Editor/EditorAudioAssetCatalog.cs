namespace ArcNET.Editor;

/// <summary>
/// Read-only catalog of loaded audio assets available through an <see cref="EditorWorkspace"/>.
/// </summary>
public sealed class EditorAudioAssetCatalog
{
    private readonly IReadOnlyDictionary<string, EditorAudioAssetEntry> _entriesByPath;

    private EditorAudioAssetCatalog(EditorAudioAssetEntry[] entries)
    {
        Entries = entries;
        Count = entries.Length;
        _entriesByPath = entries.ToDictionary(entry => entry.AssetPath, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Empty audio catalog used when no audio assets were loaded.
    /// </summary>
    public static EditorAudioAssetCatalog Empty { get; } = new([]);

    /// <summary>
    /// All audio asset entries in stable path order.
    /// </summary>
    public IReadOnlyList<EditorAudioAssetEntry> Entries { get; }

    /// <summary>
    /// Total number of audio asset entries in the catalog.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Looks up an audio asset by its relative or virtual path.
    /// Returns <see langword="null"/> when no matching asset is present.
    /// </summary>
    public EditorAudioAssetEntry? Find(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        var normalizedPath = NormalizeAssetPath(assetPath);
        return _entriesByPath.TryGetValue(normalizedPath, out var entry) ? entry : null;
    }

    internal static EditorAudioAssetCatalog Create(IEnumerable<EditorAudioAssetEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var orderedEntries = entries.OrderBy(entry => entry.AssetPath, StringComparer.OrdinalIgnoreCase).ToArray();
        return orderedEntries.Length == 0 ? Empty : new EditorAudioAssetCatalog(orderedEntries);
    }

    private static string NormalizeAssetPath(string assetPath) =>
        assetPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
}
