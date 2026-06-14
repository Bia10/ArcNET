using ArcNET.Core.Primitives;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Projects tile-art browser entries directly from loaded workspace game data.
/// </summary>
public static class WorkspaceTileArtCatalogBuilder
{
    private const string TileNameAssetPath = "art/tile/tilename.mes";
    private static readonly char[] s_tileEdgeCodes =
    [
        '0',
        '6',
        'b',
        '4',
        '8',
        '9',
        '2',
        '3',
        '7',
        'e',
        'a',
        '5',
        'd',
        'c',
        '1',
        '0',
    ];
    private static readonly int[] s_tileEdgeDecodeWhenFlagsClear = [0, 1, 8, 3, 4, 5, 6, 7, 8, 3, 10, 11, 6, 7, 14, 15];
    private static readonly int[] s_tileEdgeDecodeWhenFlagsSet =
    [
        0,
        1,
        2,
        9,
        4,
        5,
        12,
        13,
        2,
        9,
        10,
        11,
        12,
        13,
        14,
        15,
    ];

    public static IReadOnlyList<WorkspaceTileArtCatalogEntry> Build(GameDataStore gameData)
    {
        ArgumentNullException.ThrowIfNull(gameData);

        var lookupData = BuildTileNameLookupData(gameData);
        if (!lookupData.HasNames)
            return [];

        List<WorkspaceTileArtCatalogEntry> entries = [];
        foreach (
            var assetPath in gameData.ArtsBySource.Keys.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
        )
        {
            if (
                !assetPath.StartsWith("art/tile/", StringComparison.OrdinalIgnoreCase)
                || !TryResolveTileArtEntryId(assetPath, lookupData, out var artId)
            )
            {
                continue;
            }

            entries.Add(
                new WorkspaceTileArtCatalogEntry(artId, Path.GetFileNameWithoutExtension(assetPath), assetPath)
            );
        }

        return entries;
    }

    private static TileNameLookupData BuildTileNameLookupData(GameDataStore gameData)
    {
        var messageFile = WorkspaceMessageLookup.FindMessageFile(gameData, TileNameAssetPath);
        if (messageFile is null)
            return TileNameLookupData.Empty;

        var orderedEntries = messageFile.Entries.OrderBy(static entry => entry.Index).ToArray();
        List<string> outdoorFlippableNames = [];
        List<string> outdoorNonFlippableNames = [];
        List<string> indoorFlippableNames = [];
        List<string> indoorNonFlippableNames = [];

        for (var entryIndex = 0; entryIndex < orderedEntries.Length; entryIndex++)
        {
            var entry = orderedEntries[entryIndex];
            if (entry.Index is < 0 or >= 400)
                continue;

            var tileName = TryNormalizeTileNameEntry(entry.Text);
            if (string.IsNullOrWhiteSpace(tileName))
                continue;

            switch (entry.Index)
            {
                case < 100:
                    outdoorFlippableNames.Add(tileName);
                    break;
                case < 200:
                    outdoorNonFlippableNames.Add(tileName);
                    break;
                case < 300:
                    indoorFlippableNames.Add(tileName);
                    break;
                default:
                    indoorNonFlippableNames.Add(tileName);
                    break;
            }
        }

        Dictionary<string, int> orderByName = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<TileNamePaletteInfo>> infosByName = new(StringComparer.OrdinalIgnoreCase);

        for (var entryIndex = 0; entryIndex < outdoorFlippableNames.Count; entryIndex++)
        {
            var tileName = outdoorFlippableNames[entryIndex];
            orderByName.TryAdd(tileName, entryIndex);
            AddTileNameInfo(infosByName, new(tileName, Type: 1, Flippable: 1, Number: entryIndex, Order: entryIndex));
        }

        for (var entryIndex = 0; entryIndex < outdoorNonFlippableNames.Count; entryIndex++)
        {
            var tileName = outdoorNonFlippableNames[entryIndex];
            var order = outdoorFlippableNames.Count + entryIndex;
            orderByName.TryAdd(tileName, order);
            AddTileNameInfo(infosByName, new(tileName, Type: 1, Flippable: 0, Number: entryIndex, Order: order));
        }

        for (var entryIndex = 0; entryIndex < indoorFlippableNames.Count; entryIndex++)
            AddTileNameInfo(
                infosByName,
                new(indoorFlippableNames[entryIndex], Type: 0, Flippable: 1, Number: entryIndex, Order: int.MaxValue)
            );

        for (var entryIndex = 0; entryIndex < indoorNonFlippableNames.Count; entryIndex++)
            AddTileNameInfo(
                infosByName,
                new(indoorNonFlippableNames[entryIndex], Type: 0, Flippable: 0, Number: entryIndex, Order: int.MaxValue)
            );

        return new TileNameLookupData(
            [.. outdoorFlippableNames],
            [.. outdoorNonFlippableNames],
            [.. indoorFlippableNames],
            [.. indoorNonFlippableNames],
            orderByName,
            infosByName.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyList<TileNamePaletteInfo>)pair.Value,
                StringComparer.OrdinalIgnoreCase
            ),
            [.. infosByName.Keys.OrderByDescending(static name => name.Length)]
        );
    }

    private static void AddTileNameInfo(
        Dictionary<string, List<TileNamePaletteInfo>> infosByName,
        TileNamePaletteInfo info
    )
    {
        if (!infosByName.TryGetValue(info.Name, out var infos))
        {
            infos = [];
            infosByName[info.Name] = infos;
        }

        infos.Add(info);
    }

    private static bool TryResolveTileArtEntryId(string assetPath, TileNameLookupData lookupData, out ArtId artId)
    {
        artId = default;

        var normalizedAssetPath = WorkspaceMessageLookup.NormalizeAssetPath(assetPath);
        if (
            !normalizedAssetPath.StartsWith("art/tile/", StringComparison.OrdinalIgnoreCase)
            || !normalizedAssetPath.EndsWith(".art", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        var fileName = Path.GetFileNameWithoutExtension(normalizedAssetPath);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length < 2)
            return false;

        var frame = char.ToLowerInvariant(fileName[^1]) - 'a';
        if (frame is < 0 or > 7)
            return false;

        var encodedEdge = Array.IndexOf(s_tileEdgeCodes, char.ToLowerInvariant(fileName[^2]));
        if (encodedEdge < 0)
            return false;

        var body = fileName[..^2];
        if (body.EndsWith("bse", StringComparison.OrdinalIgnoreCase))
        {
            var solidTileName = body[..^3];
            return TryCreateTileArtEntryId(lookupData, solidTileName, solidTileName, encodedEdge, frame, out artId);
        }

        for (var nameIndex = 0; nameIndex < lookupData.NamesByDescendingLength.Count; nameIndex++)
        {
            var firstName = lookupData.NamesByDescendingLength[nameIndex];
            if (!body.StartsWith(firstName, StringComparison.OrdinalIgnoreCase))
                continue;

            var secondName = body[firstName.Length..];
            if (
                string.IsNullOrWhiteSpace(secondName)
                || !lookupData.InfosByName.TryGetValue(firstName, out var firstInfos)
                || !lookupData.InfosByName.TryGetValue(secondName, out var secondInfos)
            )
            {
                continue;
            }

            for (var firstInfoIndex = 0; firstInfoIndex < firstInfos.Count; firstInfoIndex++)
            {
                var firstInfo = firstInfos[firstInfoIndex];
                for (var secondInfoIndex = 0; secondInfoIndex < secondInfos.Count; secondInfoIndex++)
                {
                    var secondInfo = secondInfos[secondInfoIndex];
                    if (firstInfo.Type != secondInfo.Type)
                        continue;

                    var desiredEdge = firstInfo.Order < secondInfo.Order ? encodedEdge : 15 - encodedEdge;
                    if (
                        TryCreateTileArtEntryId(
                            lookupData,
                            firstInfo,
                            secondInfo,
                            desiredEdge,
                            frame,
                            normalizedAssetPath,
                            out artId
                        )
                    )
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool TryCreateTileArtEntryId(
        TileNameLookupData lookupData,
        string firstName,
        string secondName,
        int edge,
        int frame,
        out ArtId artId
    )
    {
        artId = default;
        if (
            !lookupData.InfosByName.TryGetValue(firstName, out var firstInfos)
            || !lookupData.InfosByName.TryGetValue(secondName, out var secondInfos)
        )
        {
            return false;
        }

        for (var firstInfoIndex = 0; firstInfoIndex < firstInfos.Count; firstInfoIndex++)
        {
            var firstInfo = firstInfos[firstInfoIndex];
            for (var secondInfoIndex = 0; secondInfoIndex < secondInfos.Count; secondInfoIndex++)
            {
                var secondInfo = secondInfos[secondInfoIndex];
                if (firstInfo.Type != secondInfo.Type)
                    continue;

                if (
                    TryCreateTileArtEntryId(
                        lookupData,
                        firstInfo,
                        secondInfo,
                        edge,
                        frame,
                        expectedAssetPath: null,
                        out artId
                    )
                )
                    return true;
            }
        }

        return false;
    }

    private static bool TryCreateTileArtEntryId(
        TileNameLookupData lookupData,
        TileNamePaletteInfo firstInfo,
        TileNamePaletteInfo secondInfo,
        int edge,
        int frame,
        string? expectedAssetPath,
        out ArtId artId
    )
    {
        artId = default;
        if (firstInfo.Type != secondInfo.Type || edge is < 0 or > 15 || frame is < 0 or > 7)
            return false;

        var artIdValue =
            (((uint)firstInfo.Number & 0x3Fu) << 22)
            | (((uint)secondInfo.Number & 0x3Fu) << 16)
            | (((uint)edge & 0xFu) << 12)
            | (((uint)frame & 0x7u) << 9)
            | (((uint)firstInfo.Type & 0x1u) << 8)
            | (((uint)firstInfo.Flippable & 0x1u) << 7)
            | (((uint)secondInfo.Flippable & 0x1u) << 6);
        artId = new ArtId(artIdValue);

        if (expectedAssetPath is null)
            return true;

        return TryBuildTileArtAssetPath(lookupData, artIdValue, out var candidateAssetPath)
            && string.Equals(candidateAssetPath, expectedAssetPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryBuildTileArtAssetPath(TileNameLookupData lookupData, uint artIdValue, out string assetPath)
    {
        assetPath = string.Empty;
        if (
            !TryGetTileName(
                lookupData,
                DecodeTileArtNum1(artIdValue),
                DecodeTileArtType(artIdValue),
                DecodeTileArtFlippable1(artIdValue),
                out var name1
            )
            || !TryGetTileName(
                lookupData,
                DecodeTileArtNum2(artIdValue),
                DecodeTileArtType(artIdValue),
                DecodeTileArtFlippable2(artIdValue),
                out var name2
            )
        )
        {
            return false;
        }

        assetPath = BuildTileArtAssetPath(
            lookupData,
            name1,
            name2,
            DecodeTileArtEdge(artIdValue),
            DecodeTileArtFrame(artIdValue)
        );
        return true;
    }

    private static bool TryGetTileName(
        TileNameLookupData lookupData,
        int number,
        int type,
        int flippable,
        out string name
    )
    {
        IReadOnlyList<string> tileNames =
            type != 0
                ? flippable != 0
                    ? lookupData.OutdoorFlippableNames
                    : lookupData.OutdoorNonFlippableNames
                : flippable != 0
                    ? lookupData.IndoorFlippableNames
                    : lookupData.IndoorNonFlippableNames;

        if ((uint)number >= (uint)tileNames.Count)
        {
            name = string.Empty;
            return false;
        }

        name = tileNames[number];
        return !string.IsNullOrEmpty(name);
    }

    private static string BuildTileArtAssetPath(
        TileNameLookupData lookupData,
        string name1,
        string name2,
        int edge,
        int frame
    )
    {
        if (frame >= 8)
            frame -= 8;

        var edgeCode = s_tileEdgeCodes[edge];
        var frameCode = (char)('a' + frame);

        if (edge == 15 || string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase))
            return WorkspaceMessageLookup.NormalizeAssetPath($"art/tile/{name1}bse{edgeCode}{frameCode}.art");

        if (edge == 0)
            return WorkspaceMessageLookup.NormalizeAssetPath($"art/tile/{name2}bse{s_tileEdgeCodes[0]}{frameCode}.art");

        if (!lookupData.OrderByName.TryGetValue(name1, out var name1Order))
            return WorkspaceMessageLookup.NormalizeAssetPath($"art/tile/{name1}bse{edgeCode}{frameCode}.art");

        if (!lookupData.OrderByName.TryGetValue(name2, out var name2Order))
            return WorkspaceMessageLookup.NormalizeAssetPath(
                $"art/tile/{name2}bse{s_tileEdgeCodes[15 - edge]}{frameCode}.art"
            );

        return name1Order < name2Order
            ? WorkspaceMessageLookup.NormalizeAssetPath($"art/tile/{name1}{name2}{edgeCode}{frameCode}.art")
            : WorkspaceMessageLookup.NormalizeAssetPath(
                $"art/tile/{name2}{name1}{s_tileEdgeCodes[15 - edge]}{frameCode}.art"
            );
    }

    private static int DecodeTileArtNum1(uint artIdValue) => checked((int)((artIdValue >> 22) & 0x3Fu));

    private static int DecodeTileArtNum2(uint artIdValue) => checked((int)((artIdValue >> 16) & 0x3Fu));

    private static int DecodeTileArtType(uint artIdValue) => checked((int)((artIdValue >> 8) & 1u));

    private static int DecodeTileArtFlippable1(uint artIdValue) => checked((int)((artIdValue >> 7) & 1u));

    private static int DecodeTileArtFlippable2(uint artIdValue) => checked((int)((artIdValue >> 6) & 1u));

    private static int DecodeTileArtRawEdge(uint artIdValue) => checked((int)((artIdValue >> 12) & 0xFu));

    private static int DecodeTileArtEdge(uint artIdValue)
    {
        var rawEdge = DecodeTileArtRawEdge(artIdValue);
        return IsTileArtMirrored(artIdValue) ? s_tileEdgeDecodeWhenFlagsSet[rawEdge] : rawEdge;
    }

    private static int DecodeTileArtFrame(uint artIdValue)
    {
        var frame = checked((int)((artIdValue >> 9) & 0x7u));
        var decodedEdge = DecodeTileArtEdge(artIdValue);
        if (
            IsTileArtMirrored(artIdValue)
            && s_tileEdgeDecodeWhenFlagsSet[decodedEdge] == s_tileEdgeDecodeWhenFlagsClear[decodedEdge]
        )
            frame += 8;

        return frame;
    }

    private static bool IsTileArtMirrored(uint artIdValue) => (artIdValue & 1u) != 0u;

    private static string? TryGetFirstMessageToken(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.AsSpan().Trim();
        var separatorIndex = trimmed.IndexOfAny(' ', '\t');
        return separatorIndex < 0 ? trimmed.ToString() : trimmed[..separatorIndex].ToString();
    }

    private static string? TryNormalizeTileNameEntry(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        var slashIndex = trimmed.IndexOf('/');
        if (slashIndex >= 0)
            trimmed = trimmed[..slashIndex].TrimEnd();

        var token = TryGetFirstMessageToken(trimmed);
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private readonly record struct TileNamePaletteInfo(string Name, int Type, int Flippable, int Number, int Order);

    private sealed class TileNameLookupData(
        IReadOnlyList<string> outdoorFlippableNames,
        IReadOnlyList<string> outdoorNonFlippableNames,
        IReadOnlyList<string> indoorFlippableNames,
        IReadOnlyList<string> indoorNonFlippableNames,
        IReadOnlyDictionary<string, int> orderByName,
        IReadOnlyDictionary<string, IReadOnlyList<TileNamePaletteInfo>> infosByName,
        IReadOnlyList<string> namesByDescendingLength
    )
    {
        public static TileNameLookupData Empty { get; } =
            new(
                [],
                [],
                [],
                [],
                new Dictionary<string, int>(),
                new Dictionary<string, IReadOnlyList<TileNamePaletteInfo>>(),
                []
            );

        public IReadOnlyList<string> OutdoorFlippableNames { get; } = outdoorFlippableNames;

        public IReadOnlyList<string> OutdoorNonFlippableNames { get; } = outdoorNonFlippableNames;

        public IReadOnlyList<string> IndoorFlippableNames { get; } = indoorFlippableNames;

        public IReadOnlyList<string> IndoorNonFlippableNames { get; } = indoorNonFlippableNames;

        public IReadOnlyDictionary<string, int> OrderByName { get; } = orderByName;

        public IReadOnlyDictionary<string, IReadOnlyList<TileNamePaletteInfo>> InfosByName { get; } = infosByName;

        public IReadOnlyList<string> NamesByDescendingLength { get; } = namesByDescendingLength;

        public bool HasNames =>
            OutdoorFlippableNames.Count > 0
            || OutdoorNonFlippableNames.Count > 0
            || IndoorFlippableNames.Count > 0
            || IndoorNonFlippableNames.Count > 0;
    }
}
