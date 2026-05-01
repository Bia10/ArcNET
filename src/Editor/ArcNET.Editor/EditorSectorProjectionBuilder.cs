using System.Globalization;

namespace ArcNET.Editor;

internal static class EditorSectorProjectionBuilder
{
    private const int SectorCoordinateShift = 26;
    private const ulong SectorCoordinateMask = (1UL << SectorCoordinateShift) - 1;
    private const string SaveSectorAssetPrefix = "sector_";

    public static IReadOnlyDictionary<string, EditorMapProjection> Build(
        IReadOnlyDictionary<string, IReadOnlyList<EditorSectorSummary>> mapSectorsByName
    )
    {
        var projectionsByName = new Dictionary<string, EditorMapProjection>(StringComparer.OrdinalIgnoreCase);

        foreach (var (mapName, sectors) in mapSectorsByName)
        {
            var positionedSectors = new List<(EditorSectorSummary Sector, SectorCoordinates Coordinates)>();
            var unpositionedSectorCount = 0;
            var minSectorX = int.MaxValue;
            var minSectorY = int.MaxValue;
            var maxSectorX = int.MinValue;
            var maxSectorY = int.MinValue;

            foreach (var sector in sectors)
            {
                if (!TryGetSectorCoordinates(sector.Asset.AssetPath, out var coordinates))
                {
                    unpositionedSectorCount++;
                    continue;
                }

                positionedSectors.Add((sector, coordinates));
                minSectorX = Math.Min(minSectorX, coordinates.X);
                minSectorY = Math.Min(minSectorY, coordinates.Y);
                maxSectorX = Math.Max(maxSectorX, coordinates.X);
                maxSectorY = Math.Max(maxSectorY, coordinates.Y);
            }

            IReadOnlyList<EditorMapSectorProjection> projectedSectors =
                positionedSectors.Count == 0
                    ? []
                    : BuildDenseProjection(positionedSectors)
                        .OrderBy(static sector => sector.LocalY)
                        .ThenBy(static sector => sector.LocalX)
                        .ThenBy(static sector => sector.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                        .ToArray();

            var width = projectedSectors.Count == 0 ? 0 : projectedSectors.Max(static sector => sector.LocalX) + 1;
            var height = projectedSectors.Count == 0 ? 0 : projectedSectors.Max(static sector => sector.LocalY) + 1;

            projectionsByName[mapName] = new EditorMapProjection
            {
                MapName = mapName,
                MinSectorX = positionedSectors.Count == 0 ? 0 : minSectorX,
                MinSectorY = positionedSectors.Count == 0 ? 0 : minSectorY,
                MaxSectorX = positionedSectors.Count == 0 ? 0 : maxSectorX,
                MaxSectorY = positionedSectors.Count == 0 ? 0 : maxSectorY,
                Width = width,
                Height = height,
                Sectors = projectedSectors,
                UnpositionedSectorCount = unpositionedSectorCount,
            };
        }

        return projectionsByName;
    }

    private static IReadOnlyList<EditorMapSectorProjection> BuildDenseProjection(
        IReadOnlyList<(EditorSectorSummary Sector, SectorCoordinates Coordinates)> positionedSectors
    )
    {
        var maxObjectCount = positionedSectors.Max(static sector => sector.Sector.ObjectCount);
        var maxBlockedTileCount = positionedSectors.Max(static sector => sector.Sector.BlockedTileCount);
        var xAxis = positionedSectors
            .Select(static sector => sector.Coordinates.X)
            .Distinct()
            .OrderBy(static value => value)
            .Select(static (value, index) => (value, index))
            .ToDictionary(pair => pair.value, pair => pair.index);
        var yAxis = positionedSectors
            .Select(static sector => sector.Coordinates.Y)
            .Distinct()
            .OrderBy(static value => value)
            .Select(static (value, index) => (value, index))
            .ToDictionary(pair => pair.value, pair => pair.index);

        return positionedSectors
            .Select(position => new EditorMapSectorProjection
            {
                Sector = position.Sector,
                SectorX = position.Coordinates.X,
                SectorY = position.Coordinates.Y,
                LocalX = xAxis[position.Coordinates.X],
                LocalY = yAxis[position.Coordinates.Y],
                ObjectDensityBand = GetDensityBand(position.Sector.ObjectCount, maxObjectCount),
                BlockedTileDensityBand = GetDensityBand(position.Sector.BlockedTileCount, maxBlockedTileCount),
            })
            .ToArray();
    }

    private static EditorMapSectorDensityBand GetDensityBand(int value, int maxValue)
    {
        if (value <= 0 || maxValue <= 0)
            return EditorMapSectorDensityBand.None;

        var peakBand = (int)EditorMapSectorDensityBand.Peak;
        var band = (int)Math.Ceiling(value * (double)peakBand / maxValue);
        return (EditorMapSectorDensityBand)Math.Clamp(band, (int)EditorMapSectorDensityBand.Low, peakBand);
    }

    private static bool TryGetSectorCoordinates(string assetPath, out SectorCoordinates coordinates)
    {
        coordinates = default;

        if (!assetPath.EndsWith(".sec", StringComparison.OrdinalIgnoreCase))
            return false;

        var fileName = Path.GetFileNameWithoutExtension(assetPath);
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        if (fileName.StartsWith(SaveSectorAssetPrefix, StringComparison.OrdinalIgnoreCase))
            fileName = fileName[SaveSectorAssetPrefix.Length..];

        if (!ulong.TryParse(fileName, NumberStyles.None, CultureInfo.InvariantCulture, out var sectorKey))
            return false;

        coordinates = new SectorCoordinates(
            (int)(sectorKey & SectorCoordinateMask),
            (int)(sectorKey >> SectorCoordinateShift)
        );
        return true;
    }

    private readonly record struct SectorCoordinates(int X, int Y);
}
