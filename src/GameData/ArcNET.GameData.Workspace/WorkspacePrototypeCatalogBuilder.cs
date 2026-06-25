using System.Globalization;
using System.Text.RegularExpressions;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Projects prototype browser entries directly from loaded workspace game data.
/// </summary>
public static partial class WorkspacePrototypeCatalogBuilder
{
    private const string ProtoDisplayOverrideAssetPath = "oemes/oname.mes";
    private const string DescriptionAssetPath = "mes/description.mes";

    public static IReadOnlyList<WorkspacePrototypeCatalogEntry> Build(
        GameDataStore gameData,
        ArcanumInstallationType? installationType = null
    )
    {
        ArgumentNullException.ThrowIfNull(gameData);

        var messageAssetPathsByIndex = BuildMessageAssetPathsByIndex(gameData);
        Dictionary<string, IReadOnlyDictionary<int, string>> messageEntriesByIndexByAssetPath = new(
            StringComparer.OrdinalIgnoreCase
        );
        List<WorkspacePrototypeCatalogEntry> entries = [];

        foreach (var (assetPath, protos) in gameData.ProtosBySource)
        {
            if (!TryGetProtoNumberFromAssetPath(assetPath, out var protoNumber))
                continue;

            for (var protoIndex = 0; protoIndex < protos.Count; protoIndex++)
            {
                var proto = protos[protoIndex];
                entries.Add(
                    new WorkspacePrototypeCatalogEntry(
                        protoNumber,
                        proto.Header.GameObjectType,
                        assetPath,
                        ResolveProtoDisplayName(protoNumber, installationType),
                        ResolveMessageText(TryGetInt32Property(proto, ObjectField.Description)),
                        GetObjectPaletteGroup(assetPath),
                        ResolveCurrentArtId(proto),
                        ResolveDestroyedArtId(proto),
                        null,
                        ResolvePortalFlags(proto),
                        ResolveContainerFlags(proto),
                        ResolveSceneryFlags(proto),
                        ResolvePortalInt32(proto, ObjectField.PortalLockDifficulty),
                        ResolvePortalInt32(proto, ObjectField.PortalKeyId),
                        ResolveContainerInt32(proto, ObjectField.ContainerLockDifficulty),
                        ResolveContainerInt32(proto, ObjectField.ContainerKeyId)
                    )
                );
            }
        }

        return
        [
            .. entries
                .OrderBy(static entry => entry.ProtoNumber)
                .ThenBy(static entry => entry.AssetPath, StringComparer.OrdinalIgnoreCase),
        ];

        string? ResolveProtoDisplayName(int protoNumber, ArcanumInstallationType? type)
        {
            foreach (var messageIndex in EnumerateProtoDisplayNameKeys(protoNumber, type))
            {
                var overrideText = TryGetMessageText(ProtoDisplayOverrideAssetPath, messageIndex);
                if (!string.IsNullOrWhiteSpace(overrideText))
                    return overrideText;

                var descriptionText = TryGetMessageText(DescriptionAssetPath, messageIndex);
                if (!string.IsNullOrWhiteSpace(descriptionText))
                    return descriptionText;
            }

            return null;
        }

        string? ResolveMessageText(int? messageIndex)
        {
            if (!messageIndex.HasValue || !messageAssetPathsByIndex.TryGetValue(messageIndex.Value, out var assetPaths))
                return null;

            for (var assetIndex = 0; assetIndex < assetPaths.Count; assetIndex++)
            {
                var text = TryGetMessageText(assetPaths[assetIndex], messageIndex.Value);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }

            return null;
        }

        string? TryGetMessageText(string assetPath, int messageIndex)
        {
            var entriesByIndex = GetOrCreateMessageEntriesByIndex(assetPath);
            return entriesByIndex.TryGetValue(messageIndex, out var text) && !string.IsNullOrWhiteSpace(text)
                ? text.Trim()
                : null;
        }

        IReadOnlyDictionary<int, string> GetOrCreateMessageEntriesByIndex(string assetPath)
        {
            if (messageEntriesByIndexByAssetPath.TryGetValue(assetPath, out var entriesByIndex))
                return entriesByIndex;

            Dictionary<int, string> resolvedEntriesByIndex = [];
            var messageFile = WorkspaceMessageLookup.FindMessageFile(gameData, assetPath);
            if (messageFile is not null)
            {
                for (var entryIndex = 0; entryIndex < messageFile.Entries.Count; entryIndex++)
                {
                    var entry = messageFile.Entries[entryIndex];
                    resolvedEntriesByIndex.TryAdd(entry.Index, entry.Text);
                }
            }

            entriesByIndex = resolvedEntriesByIndex;
            messageEntriesByIndexByAssetPath[assetPath] = entriesByIndex;
            return entriesByIndex;
        }
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<string>> BuildMessageAssetPathsByIndex(GameDataStore gameData)
    {
        Dictionary<int, List<string>> assetPathsByIndex = [];

        foreach (var (assetPath, entries) in gameData.MessagesBySource)
        {
            for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                var entry = entries[entryIndex];
                if (!assetPathsByIndex.TryGetValue(entry.Index, out var assetPaths))
                {
                    assetPaths = [];
                    assetPathsByIndex[entry.Index] = assetPaths;
                }

                if (!assetPaths.Contains(assetPath, StringComparer.OrdinalIgnoreCase))
                    assetPaths.Add(assetPath);
            }
        }

        return assetPathsByIndex.ToDictionary(
            static pair => pair.Key,
            static pair =>
                (IReadOnlyList<string>)
                    [.. pair.Value.OrderBy(static assetPath => assetPath, StringComparer.OrdinalIgnoreCase)]
        );
    }

    private static IEnumerable<int> EnumerateProtoDisplayNameKeys(
        int protoNumber,
        ArcanumInstallationType? installationType
    )
    {
        if (installationType.HasValue)
        {
            var translatedKey = ArcanumInstallation.ToVanillaProtoId(protoNumber, installationType.Value);
            if (translatedKey > 0 && translatedKey != protoNumber)
                yield return translatedKey;
        }

        yield return protoNumber;
    }

    private static int? TryGetInt32Property(ProtoData proto, ObjectField field)
    {
        var property = proto.GetProperty(field);
        return property is null ? null : property.GetInt32();
    }

    private static ArtId? ResolveCurrentArtId(ProtoData proto)
    {
        var currentArtId = TryGetArtId(proto, ObjectField.CurrentAid);
        if (!IsValidArtId(currentArtId))
            currentArtId = null;

        if (!currentArtId.HasValue)
        {
            currentArtId = TryGetArtId(proto, ObjectField.Aid);
            if (!IsValidArtId(currentArtId))
                currentArtId = null;
        }

        return currentArtId;
    }

    private static ArtId? ResolveDestroyedArtId(ProtoData proto)
    {
        var destroyedArtId = TryGetArtId(proto, ObjectField.DestroyedAid);
        return IsValidArtId(destroyedArtId) ? destroyedArtId : null;
    }

    private static ArtId? TryGetArtId(ProtoData proto, ObjectField field)
    {
        var value = TryGetInt32Property(proto, field);
        return value.HasValue ? new ArtId(unchecked((uint)value.Value)) : null;
    }

    private static PortalFlags? ResolvePortalFlags(ProtoData proto)
    {
        if (proto.Header.GameObjectType is not ObjectType.Portal)
            return null;

        var value = TryGetInt32Property(proto, ObjectField.PortalFlags);
        return value.HasValue ? unchecked((PortalFlags)(uint)value.Value) : null;
    }

    private static ContainerFlags? ResolveContainerFlags(ProtoData proto)
    {
        if (proto.Header.GameObjectType is not ObjectType.Container)
            return null;

        var value = TryGetInt32Property(proto, ObjectField.ContainerFlags);
        return value.HasValue ? unchecked((ContainerFlags)(uint)value.Value) : null;
    }

    private static SceneryFlags? ResolveSceneryFlags(ProtoData proto)
    {
        if (proto.Header.GameObjectType is not ObjectType.Scenery)
            return null;

        var value = TryGetInt32Property(proto, ObjectField.SceneryFlags);
        return value.HasValue ? unchecked((SceneryFlags)(uint)value.Value) : null;
    }

    private static int? ResolvePortalInt32(ProtoData proto, ObjectField field) =>
        proto.Header.GameObjectType is ObjectType.Portal ? TryGetInt32Property(proto, field) : null;

    private static int? ResolveContainerInt32(ProtoData proto, ObjectField field) =>
        proto.Header.GameObjectType is ObjectType.Container ? TryGetInt32Property(proto, field) : null;

    private static bool IsValidArtId(ArtId? artId) => artId is { Value: not 0u and not uint.MaxValue };

    private static string? GetObjectPaletteGroup(string assetPath)
    {
        var normalizedPath = WorkspaceMessageLookup.NormalizeAssetPath(assetPath);
        if (!normalizedPath.StartsWith("proto/", StringComparison.OrdinalIgnoreCase))
            return null;

        var groupPath = Path.GetDirectoryName(normalizedPath["proto/".Length..]);
        return string.IsNullOrWhiteSpace(groupPath) ? null : ArcNET.Core.VirtualPath.Normalize(groupPath);
    }

    private static bool TryGetProtoNumberFromAssetPath(string assetPath, out int protoNumber)
    {
        var match = ProtoAssetPathPattern().Match(assetPath);
        if (!match.Success)
        {
            protoNumber = 0;
            return false;
        }

        return int.TryParse(
            match.Groups["number"].Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out protoNumber
        );
    }

    [GeneratedRegex(@"(?:^|/)(?<number>\d+)(?:\s*-.*)?\.pro$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProtoAssetPathPattern();
}
