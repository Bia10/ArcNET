using ArcNET.Formats;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Shared lookup helpers for message assets loaded into one workspace content store.
/// </summary>
public static class WorkspaceMessageLookup
{
    /// <summary>
    /// Looks up one loaded message file by asset path.
    /// Returns <see langword="null"/> when the workspace did not load that asset.
    /// </summary>
    public static MesFile? FindMessageFile(GameDataStore gameData, string assetPath)
    {
        ArgumentNullException.ThrowIfNull(gameData);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return gameData.MessagesBySource.TryGetValue(normalizedPath, out var entries)
            ? new MesFile { Entries = [.. entries] }
            : null;
    }

    /// <summary>
    /// Normalizes one virtual asset path into the workspace lookup key form.
    /// </summary>
    public static string NormalizeAssetPath(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        return ArcNET.Core.VirtualPath.Normalize(assetPath);
    }
}
