using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

internal static class EditorArtReferenceReplacer
{
    public static bool ContainsArtReference(MobData mob, uint artId) =>
        TryReplaceObjectProperties(mob.Properties, artId, artId, out _, out var changed) && changed;

    public static bool ContainsArtReference(ProtoData proto, uint artId) =>
        TryReplaceObjectProperties(proto.Properties, artId, artId, out _, out var changed) && changed;

    public static bool ContainsArtReference(Sector sector, uint artId)
    {
        if (sector.Lights.Any(light => light.ArtId == artId))
            return true;

        if (sector.Tiles.Any(tileArtId => tileArtId == artId))
            return true;

        if (sector.Roofs?.Any(roofArtId => roofArtId == artId) == true)
            return true;

        return sector.Objects.Any(mob => ContainsArtReference(mob, artId));
    }

    public static bool TryReplace(MobData mob, uint sourceArtId, uint targetArtId, out MobData updated)
    {
        _ = TryReplaceObjectProperties(
            mob.Properties,
            sourceArtId,
            targetArtId,
            out var updatedProperties,
            out var changed
        );
        if (!changed)
        {
            updated = mob;
            return false;
        }

        updated = new MobData { Header = CloneHeader(mob.Header), Properties = updatedProperties };
        return true;
    }

    public static bool TryReplace(ProtoData proto, uint sourceArtId, uint targetArtId, out ProtoData updated)
    {
        _ = TryReplaceObjectProperties(
            proto.Properties,
            sourceArtId,
            targetArtId,
            out var updatedProperties,
            out var changed
        );
        if (!changed)
        {
            updated = proto;
            return false;
        }

        updated = new ProtoData { Header = CloneHeader(proto.Header), Properties = updatedProperties };
        return true;
    }

    public static bool TryReplace(Sector sector, uint sourceArtId, uint targetArtId, out Sector updated)
    {
        var changed = false;

        var updatedLights = new SectorLight[sector.Lights.Count];
        for (var i = 0; i < sector.Lights.Count; i++)
        {
            var light = sector.Lights[i];
            if (light.ArtId == sourceArtId)
            {
                updatedLights[i] = light with { ArtId = targetArtId };
                changed = true;
                continue;
            }

            updatedLights[i] = light;
        }

        uint[] updatedTiles = [.. sector.Tiles];
        for (var i = 0; i < updatedTiles.Length; i++)
        {
            if (updatedTiles[i] != sourceArtId)
                continue;

            updatedTiles[i] = targetArtId;
            changed = true;
        }

        uint[]? updatedRoofs = null;
        if (sector.Roofs is not null)
        {
            updatedRoofs = [.. sector.Roofs];
            for (var i = 0; i < updatedRoofs.Length; i++)
            {
                if (updatedRoofs[i] != sourceArtId)
                    continue;

                updatedRoofs[i] = targetArtId;
                changed = true;
            }
        }

        var updatedObjects = new MobData[sector.Objects.Count];
        for (var i = 0; i < sector.Objects.Count; i++)
        {
            if (TryReplace(sector.Objects[i], sourceArtId, targetArtId, out var updatedMob))
            {
                updatedObjects[i] = updatedMob;
                changed = true;
                continue;
            }

            updatedObjects[i] = sector.Objects[i];
        }

        if (!changed)
        {
            updated = sector;
            return false;
        }

        updated = new Sector
        {
            Lights = updatedLights,
            Tiles = updatedTiles,
            HasRoofs = sector.HasRoofs,
            Roofs = updatedRoofs,
            SectorScript = sector.SectorScript,
            TileScripts = sector.TileScripts.ToArray(),
            TownmapInfo = sector.TownmapInfo,
            AptitudeAdjustment = sector.AptitudeAdjustment,
            LightSchemeIdx = sector.LightSchemeIdx,
            SoundList = sector.SoundList,
            BlockMask = [.. sector.BlockMask],
            Objects = updatedObjects,
        };
        return true;
    }

    private static bool TryReplaceObjectProperties(
        IReadOnlyList<ObjectProperty> properties,
        uint sourceArtId,
        uint targetArtId,
        out IReadOnlyList<ObjectProperty> updatedProperties,
        out bool changed
    )
    {
        var replacements = new ObjectProperty[properties.Count];
        changed = false;

        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            if (!IsArtProperty(property.Field) || !TryGetArtId(property, out var artId) || artId != sourceArtId)
            {
                replacements[i] = property;
                continue;
            }

            replacements[i] = ObjectPropertyFactory.ForInt32(property.Field, unchecked((int)targetArtId));
            changed = true;
        }

        updatedProperties = replacements;
        return true;
    }

    private static bool IsArtProperty(ObjectField field) =>
        field
            is ObjectField.ObjFCurrentAid
                or ObjectField.ObjFShadow
                or ObjectField.ObjFLightAid
                or ObjectField.ObjFAid
                or ObjectField.ObjFDestroyedAid;

    private static bool TryGetArtId(ObjectProperty property, out uint artId)
    {
        try
        {
            artId = unchecked((uint)property.GetInt32());
            return true;
        }
        catch (InvalidOperationException)
        {
            artId = 0;
            return false;
        }
    }

    private static GameObjectHeader CloneHeader(GameObjectHeader header) =>
        new()
        {
            Version = header.Version,
            ProtoId = header.ProtoId,
            ObjectId = header.ObjectId,
            GameObjectType = header.GameObjectType,
            PropCollectionItems = header.PropCollectionItems,
            Bitmap = [.. header.Bitmap],
        };
}
