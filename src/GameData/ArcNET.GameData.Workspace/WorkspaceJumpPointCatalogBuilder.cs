using System.Globalization;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Projects loaded map jump points into workspace catalog entries.
/// </summary>
public static class WorkspaceJumpPointCatalogBuilder
{
    public static IReadOnlyList<WorkspaceJumpPointCatalogEntry> Build(GameDataStore gameData)
    {
        ArgumentNullException.ThrowIfNull(gameData);

        List<WorkspaceJumpPointCatalogEntry> entries = [];

        foreach (var (assetPath, jumpFiles) in gameData.JumpFilesBySource)
        {
            if (!TryResolveSourceMapName(assetPath, out var sourceMapName))
            {
                continue;
            }

            foreach (var jumpFile in jumpFiles)
            {
                foreach (var jump in jumpFile.Jumps)
                {
                    entries.Add(
                        new WorkspaceJumpPointCatalogEntry(
                            assetPath,
                            sourceMapName,
                            jump.Flags,
                            jump.SourceLoc,
                            jump.SourceX,
                            jump.SourceY,
                            jump.DestinationMapId,
                            jump.DestinationLoc,
                            jump.DestX,
                            jump.DestY,
                            FormatSummary(
                                sourceMapName,
                                jump.SourceX,
                                jump.SourceY,
                                jump.DestinationMapId,
                                jump.DestX,
                                jump.DestY
                            )
                        )
                    );
                }
            }
        }

        return
        [
            .. entries
                .OrderBy(static entry => entry.SourceMapName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.SourceTileY)
                .ThenBy(static entry => entry.SourceTileX)
                .ThenBy(static entry => entry.DestinationMapId)
                .ThenBy(static entry => entry.DestinationTileY)
                .ThenBy(static entry => entry.DestinationTileX),
        ];
    }

    private static bool TryResolveSourceMapName(string sourceAssetPath, out string sourceMapName)
    {
        sourceMapName = string.Empty;
        if (string.IsNullOrWhiteSpace(sourceAssetPath))
        {
            return false;
        }

        var segments = sourceAssetPath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (!string.Equals(segments[index], "maps", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            sourceMapName = segments[index + 1];
            return sourceMapName.Length > 0;
        }

        return false;
    }

    private static string FormatSummary(
        string sourceMapName,
        int sourceX,
        int sourceY,
        int destinationMapId,
        int destinationX,
        int destinationY
    ) =>
        FormattableString.Invariant(
            $"{sourceMapName} ({sourceX}, {sourceY}) -> world map {destinationMapId} ({destinationX}, {destinationY})"
        );
}
