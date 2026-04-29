using ArcNET.Formats;

namespace ArcNET.App;

internal static partial class AgentOutput
{
    internal static object Project(MobData mob)
    {
        var header = mob.Header;
        return new
        {
            version = header.Version,
            objectType = header.GameObjectType.ToString(),
            isPrototype = header.IsPrototype,
            protoId = header.ProtoId.ToString(),
            objectId = header.IsPrototype ? null : header.ObjectId.ToString(),
            propertyCount = mob.Properties.Count,
            properties = mob
                .Properties.Select(property => new
                {
                    bit = (int)property.Field,
                    field = property.Field.ToString(),
                    rawHex = Convert.ToHexString(property.RawBytes),
                })
                .ToArray(),
        };
    }

    internal static object Project(SaveInfo saveInfo) =>
        new
        {
            displayName = saveInfo.DisplayName,
            moduleName = saveInfo.ModuleName,
            leaderName = saveInfo.LeaderName,
            leaderLevel = saveInfo.LeaderLevel,
            leaderPortraitId = saveInfo.LeaderPortraitId,
            mapId = saveInfo.MapId,
            leaderTileX = saveInfo.LeaderTileX,
            leaderTileY = saveInfo.LeaderTileY,
            gameTimeDays = saveInfo.GameTimeDays,
            gameTimeMs = saveInfo.GameTimeMs,
            storyState = saveInfo.StoryState,
        };

    internal static object Project(SaveIndex saveIndex) => ProjectEntries(saveIndex.Root);

    internal static object Project(Sector sector) =>
        new
        {
            objectCount = sector.Objects.Count,
            lightCount = sector.Lights.Count,
            tileScriptCount = sector.TileScripts.Count,
            hasRoofs = sector.HasRoofs,
            lightSchemeIdx = sector.LightSchemeIdx,
            townmapInfo = sector.TownmapInfo,
            aptitudeAdjustment = sector.AptitudeAdjustment,
            soundList = new
            {
                musicSchemeIdx = sector.SoundList.MusicSchemeIdx,
                ambientSchemeIdx = sector.SoundList.AmbientSchemeIdx,
            },
            blockedTileCount = sector.BlockMask.Sum(mask => int.PopCount((int)mask)),
            distinctTileArtIds = sector.Tiles.Distinct().Count(),
            lights = sector
                .Lights.Select(light => new
                {
                    tileX = light.TileX,
                    tileY = light.TileY,
                    offsetX = light.OffsetX,
                    offsetY = light.OffsetY,
                    r = light.R,
                    g = light.G,
                    b = light.B,
                    artId = light.ArtId,
                    flags = light.Flags.ToString(),
                    attached = light.ObjHandle != -1L,
                    objHandle = light.ObjHandle == -1L ? null : (long?)light.ObjHandle,
                })
                .ToArray(),
        };

    internal static object ProjectSave(SaveInfo info, SaveIndex index, IReadOnlyDictionary<string, byte[]> payloads) =>
        new
        {
            info = Project(info),
            index = Project(index),
            payloadCount = payloads.Count,
            totalBytes = payloads.Values.Sum(payload => (long)payload.Length),
            files = payloads
                .OrderBy(pair => pair.Key)
                .Select(pair => new
                {
                    path = pair.Key,
                    size = pair.Value.Length,
                    ext = Path.GetExtension(pair.Key).ToLowerInvariant(),
                })
                .ToArray(),
        };

    private static object[] ProjectEntries(IReadOnlyList<TfaiEntry> entries) =>
        entries
            .Select(entry =>
                entry switch
                {
                    TfaiFileEntry file => (object)
                        new
                        {
                            type = "file",
                            name = file.Name,
                            size = file.Size,
                        },
                    TfaiDirectoryEntry directory => new
                    {
                        type = "directory",
                        name = directory.Name,
                        children = ProjectEntries(directory.Children),
                    },
                    _ => new { type = "unknown", name = entry.Name },
                }
            )
            .ToArray();
}
