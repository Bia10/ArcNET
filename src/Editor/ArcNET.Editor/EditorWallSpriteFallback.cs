using System.Globalization;
using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

public static class EditorWallSpriteFallback
{
    private const uint WallArtTypeMask = 0x10000000u;

    private static readonly string[] s_wallPieceSuffixes =
    [
        "bse",
        "lfc",
        "bse",
        "bcl",
        "bcr",
        "tcl",
        "tcr",
        "uec",
        "lec",
        "w3l",
        "w3a",
        "w3r",
        "w4l",
        "w4a",
        "w4b",
        "w4r",
        "w5l",
        "w5a",
        "w5b",
        "w5c",
        "w5r",
        "d3l",
        "d3a",
        "d3r",
        "d4l",
        "d4a",
        "d4b",
        "d4r",
        "d6l",
        "d6a",
        "d6b",
        "d6c",
        "d6d",
        "d6r",
        "p3l",
        "p3a",
        "p3r",
        "p4l",
        "p4a",
        "p4b",
        "p4r",
        "p5l",
        "p5a",
        "p5b",
        "p5c",
        "p5r",
    ];

    public static int BindFallbackAssets(
        EditorWorkspace workspace,
        EditorArtResolver artResolver,
        IReadOnlyList<ArtId> unresolvedArtIds
    )
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(artResolver);
        ArgumentNullException.ThrowIfNull(unresolvedArtIds);

        if (unresolvedArtIds.Count == 0 || !TryBuildLookupData(workspace, out var lookupData))
            return 0;

        var boundCount = 0;
        foreach (var artId in unresolvedArtIds.Distinct())
        {
            if (
                artResolver.FindAssetPath(artId) is null
                && TryResolveFallbackAssetPath(workspace, lookupData, artId, out var assetPath)
            )
            {
                artResolver.Bind(artId, assetPath);
                boundCount++;
            }
        }

        return boundCount;
    }

    private static bool TryBuildLookupData(EditorWorkspace workspace, out WallArtLookupData lookupData)
    {
        lookupData = default;

        var structureFile = workspace.FindMessageFile("art/wall/structure.mes");
        if (structureFile is null)
            return false;

        Dictionary<int, WallStructureArtSides> structuresByIndex = [];
        foreach (var entry in structureFile.Entries.OrderBy(static entry => entry.Index))
        {
            if (entry.Index >= 1000)
                break;

            if (TryParseWallStructureArtSides(entry.Text, out var sides))
                structuresByIndex[entry.Index] = sides;
        }

        if (structuresByIndex.Count == 0)
            return false;

        lookupData = new WallArtLookupData(structuresByIndex);
        return true;
    }

    private static bool TryResolveFallbackAssetPath(
        EditorWorkspace workspace,
        WallArtLookupData lookupData,
        ArtId artId,
        out string assetPath
    )
    {
        assetPath = string.Empty;
        if ((artId.Value & 0xF0000000u) != WallArtTypeMask)
            return false;

        var structureIndex = DecodeWallArtStructureIndex(artId.Value);
        var piece = DecodeWallArtPiece(artId.Value);
        var rotation = DecodeWallArtRotation(artId.Value);
        var variation = DecodeWallArtVariation(artId.Value);
        var damage = DecodeWallArtDamage(artId.Value);

        NormalizeWallDamageAndPiece(rotation, ref piece, ref damage);

        if (
            variation is < 0 or >= 4
            || piece is < 0 or >= 46
            || !lookupData.StructuresByIndex.TryGetValue(structureIndex, out var sides)
        )
        {
            return false;
        }

        var baseName = rotation / 2 is 0 or 3 ? sides.InteriorBaseName : sides.ExteriorBaseName;
        if (string.IsNullOrWhiteSpace(baseName))
            return false;

        foreach (var damageChar in GetCandidateDamageChars(GetWallDamageVariantCharacter(piece, damage)))
        {
            var candidateAssetPath = string.Create(
                CultureInfo.InvariantCulture,
                $"art/wall/{baseName}{s_wallPieceSuffixes[piece]}{damageChar}{variation}.art"
            );
            if (workspace.FindArt(candidateAssetPath) is not null)
            {
                assetPath = candidateAssetPath;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<char> GetCandidateDamageChars(char primaryDamageChar)
    {
        yield return primaryDamageChar;

        if (primaryDamageChar == 'L')
            yield return 'R';
        else if (primaryDamageChar == 'R')
            yield return 'L';

        if (primaryDamageChar != 'U')
            yield return 'U';

        if (primaryDamageChar is not ('L' or 'U'))
            yield return 'L';

        if (primaryDamageChar is not ('R' or 'U'))
            yield return 'R';
    }

    private static bool TryParseWallStructureArtSides(string? entryText, out WallStructureArtSides sides)
    {
        sides = default;
        if (string.IsNullOrWhiteSpace(entryText))
            return false;

        var tokens = entryText.Split(
            [' ', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (tokens.Length < 2)
            return false;

        if (!TryGetWallBaseName(tokens[0], out var interiorBaseName))
            return false;

        if (!TryGetWallBaseName(tokens[1], out var exteriorBaseName))
            return false;

        sides = new WallStructureArtSides(interiorBaseName, exteriorBaseName);
        return true;
    }

    private static bool TryGetWallBaseName(string? token, out string baseName)
    {
        baseName = string.Empty;
        var normalizedToken = TryGetFirstMessageToken(token);
        if (string.IsNullOrWhiteSpace(normalizedToken))
            return false;

        var trimmedToken = normalizedToken.Trim();
        if (
            trimmedToken.Length < 3
            || trimmedToken.Equals("nul", StringComparison.OrdinalIgnoreCase)
            || trimmedToken.Contains('/', StringComparison.Ordinal)
            || trimmedToken.Contains('.', StringComparison.Ordinal)
        )
        {
            return false;
        }

        baseName = trimmedToken[..3];
        return true;
    }

    private static string? TryGetFirstMessageToken(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var start = text.IndexOf('{');
        while (start >= 0)
        {
            var end = text.IndexOf('}', start + 1);
            if (end < 0)
                break;

            var token = text[(start + 1)..end].Trim();
            if (token.Length > 0 && !int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return token;

            start = text.IndexOf('{', end + 1);
        }

        return text.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private static int DecodeWallArtStructureIndex(uint artIdValue) => checked((int)((artIdValue >> 20) & 0xFFu));

    private static int DecodeWallArtPiece(uint artIdValue) => checked((int)((artIdValue >> 14) & 0x3Fu));

    private static int DecodeWallArtRotation(uint artIdValue) => checked((int)((artIdValue >> 11) & 0x7u));

    private static int DecodeWallArtVariation(uint artIdValue) => checked((int)((artIdValue >> 8) & 0x3u));

    private static int DecodeWallArtDamage(uint artIdValue) => checked((int)(artIdValue & 0x480u));

    private static void NormalizeWallDamageAndPiece(int rotation, ref int piece, ref int damage)
    {
        if (rotation is 2 or 3 or 6 or 7)
        {
            var rotatedDamage = 0;
            if ((damage & 0x400) != 0)
                rotatedDamage |= 0x80;

            if ((damage & 0x80) != 0)
                rotatedDamage |= 0x400;

            damage = rotatedDamage;
        }

        if ((damage & 0x400) != 0)
        {
            damage = 0x400;
            if (piece == 7)
                piece = 0;

            return;
        }

        if ((damage & 0x80) != 0)
        {
            damage = 0x80;
            if (piece == 8)
                piece = 0;

            return;
        }

        damage = 0;
    }

    private static char GetWallDamageVariantCharacter(int piece, int damage)
    {
        if ((damage & 0x400) != 0)
            return 'L';

        if ((damage & 0x80) != 0)
            return piece is >= 2 and <= 6 ? 'L' : 'R';

        return 'U';
    }

    private readonly record struct WallArtLookupData(IReadOnlyDictionary<int, WallStructureArtSides> StructuresByIndex);

    private readonly record struct WallStructureArtSides(string InteriorBaseName, string ExteriorBaseName);
}
