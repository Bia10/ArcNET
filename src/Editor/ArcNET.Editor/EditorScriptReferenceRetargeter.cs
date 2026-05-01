using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

internal static class EditorScriptReferenceRetargeter
{
    public static bool ContainsScriptReference(MobData mob, int scriptId) =>
        TryRetargetObjectProperties(mob.Properties, scriptId, scriptId, out _, out var changed) && changed;

    public static bool ContainsScriptReference(ProtoData proto, int scriptId) =>
        TryRetargetObjectProperties(proto.Properties, scriptId, scriptId, out _, out var changed) && changed;

    public static bool ContainsScriptReference(Sector sector, int scriptId)
    {
        if (sector.SectorScript is { } sectorScript && !sectorScript.IsEmpty && sectorScript.ScriptId == scriptId)
            return true;

        if (sector.TileScripts.Any(tileScript => tileScript.ScriptNum == scriptId))
            return true;

        return sector.Objects.Any(mob => ContainsScriptReference(mob, scriptId));
    }

    public static bool TryRetarget(MobData mob, int sourceScriptId, int targetScriptId, out MobData updated)
    {
        if (
            !TryRetargetObjectProperties(
                mob.Properties,
                sourceScriptId,
                targetScriptId,
                out var updatedProperties,
                out var changed
            )
        )
        {
            updated = mob;
            return false;
        }

        if (!changed)
        {
            updated = mob;
            return false;
        }

        updated = new MobData { Header = CloneHeader(mob.Header), Properties = updatedProperties };
        return true;
    }

    public static bool TryRetarget(ProtoData proto, int sourceScriptId, int targetScriptId, out ProtoData updated)
    {
        if (
            !TryRetargetObjectProperties(
                proto.Properties,
                sourceScriptId,
                targetScriptId,
                out var updatedProperties,
                out var changed
            )
        )
        {
            updated = proto;
            return false;
        }

        if (!changed)
        {
            updated = proto;
            return false;
        }

        updated = new ProtoData { Header = CloneHeader(proto.Header), Properties = updatedProperties };
        return true;
    }

    public static bool TryRetarget(Sector sector, int sourceScriptId, int targetScriptId, out Sector updated)
    {
        var changed = false;

        var updatedSectorScript = sector.SectorScript;
        if (updatedSectorScript is { } sectorScript && !sectorScript.IsEmpty && sectorScript.ScriptId == sourceScriptId)
        {
            updatedSectorScript = sectorScript with { ScriptId = targetScriptId };
            changed = true;
        }

        var updatedTileScripts = new TileScript[sector.TileScripts.Count];
        for (var i = 0; i < sector.TileScripts.Count; i++)
        {
            var tileScript = sector.TileScripts[i];
            if (tileScript.ScriptNum == sourceScriptId)
            {
                updatedTileScripts[i] = tileScript with { ScriptNum = targetScriptId };
                changed = true;
                continue;
            }

            updatedTileScripts[i] = tileScript;
        }

        var updatedObjects = new MobData[sector.Objects.Count];
        for (var i = 0; i < sector.Objects.Count; i++)
        {
            if (TryRetarget(sector.Objects[i], sourceScriptId, targetScriptId, out var updatedMob))
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
            Lights = sector.Lights.ToArray(),
            Tiles = [.. sector.Tiles],
            HasRoofs = sector.HasRoofs,
            Roofs = sector.Roofs is null ? null : [.. sector.Roofs],
            SectorScript = updatedSectorScript,
            TileScripts = updatedTileScripts,
            TownmapInfo = sector.TownmapInfo,
            AptitudeAdjustment = sector.AptitudeAdjustment,
            LightSchemeIdx = sector.LightSchemeIdx,
            SoundList = sector.SoundList,
            BlockMask = [.. sector.BlockMask],
            Objects = updatedObjects,
        };
        return true;
    }

    private static bool TryRetargetObjectProperties(
        IReadOnlyList<ObjectProperty> properties,
        int sourceScriptId,
        int targetScriptId,
        out IReadOnlyList<ObjectProperty> updatedProperties,
        out bool changed
    )
    {
        var replacements = new ObjectProperty[properties.Count];
        changed = false;

        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            if (property.Field != ObjectField.ObjFScriptsIdx)
            {
                replacements[i] = property;
                continue;
            }

            if (!TryGetScriptArray(property, out var scripts))
            {
                updatedProperties = properties;
                return false;
            }

            var updatedScripts = new ObjectPropertyScript[scripts.Length];
            var propertyChanged = false;
            for (var scriptIndex = 0; scriptIndex < scripts.Length; scriptIndex++)
            {
                var script = scripts[scriptIndex];
                if (script.ScriptId == sourceScriptId)
                {
                    updatedScripts[scriptIndex] = script with { ScriptId = targetScriptId };
                    propertyChanged = true;
                    changed = true;
                    continue;
                }

                updatedScripts[scriptIndex] = script;
            }

            replacements[i] = propertyChanged ? property.WithScriptArray(updatedScripts) : property;
        }

        updatedProperties = replacements;
        return true;
    }

    private static bool TryGetScriptArray(ObjectProperty property, out ObjectPropertyScript[] scripts)
    {
        try
        {
            scripts = property.GetScriptArray();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            scripts = [];
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
