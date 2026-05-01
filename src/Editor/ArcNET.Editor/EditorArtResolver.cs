using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Workspace-owned ART resolver that binds known <see cref="ArtId"/> values to loaded ART asset paths.
/// The current SDK still treats <see cref="ArtId"/> as opaque, so bindings must be supplied explicitly.
/// </summary>
public sealed class EditorArtResolver
{
    private readonly EditorWorkspace _workspace;
    private readonly Dictionary<ArtId, string> _assetPathsByArtId = new();

    internal EditorArtResolver(EditorWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        _workspace = workspace;
    }

    /// <summary>
    /// Number of explicit <see cref="ArtId"/> bindings currently registered on this resolver.
    /// </summary>
    public int BindingCount => _assetPathsByArtId.Count;

    /// <summary>
    /// Registers one explicit binding from <paramref name="artId"/> to a loaded ART asset path.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the workspace did not load an ART asset at <paramref name="assetPath"/>.
    /// </exception>
    public void Bind(ArtId artId, string assetPath)
    {
        if (artId.Value == 0)
            throw new ArgumentOutOfRangeException(nameof(artId), artId, "Art IDs must be non-zero.");

        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        if (_workspace.FindArt(normalizedPath) is null)
            throw new InvalidOperationException($"No loaded ART asset matched '{normalizedPath}'.");

        _assetPathsByArtId[artId] = normalizedPath;
    }

    /// <summary>
    /// Registers multiple explicit <see cref="ArtId"/> bindings.
    /// </summary>
    public void BindRange(IEnumerable<KeyValuePair<ArtId, string>> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        foreach (var (artId, assetPath) in bindings)
            Bind(artId, assetPath);
    }

    /// <summary>
    /// Returns the bound asset path for <paramref name="artId"/>, or <see langword="null"/> when no binding exists.
    /// </summary>
    public string? FindAssetPath(ArtId artId) =>
        _assetPathsByArtId.TryGetValue(artId, out var assetPath) ? assetPath : null;

    /// <summary>
    /// Returns the loaded ART asset bound to <paramref name="artId"/>, or <see langword="null"/> when no binding exists.
    /// </summary>
    public ArtFile? FindArt(ArtId artId) =>
        FindAssetPath(artId) is { } assetPath ? _workspace.FindArt(assetPath) : null;

    private static string NormalizeAssetPath(string assetPath) =>
        assetPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
}
