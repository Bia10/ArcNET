using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

internal static class EditorAssetReferenceCounter
{
    public static IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> CountProtoReferences(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var protoReferencesByNumber = new Dictionary<int, List<EditorProtoReference>>();

        foreach (var (assetPath, mobs) in gameData.MobsBySource)
            AddProtoReferences(protoReferencesByNumber, assetsByPath, assetPath, CountMobProtoReferences(mobs));

        foreach (var (assetPath, sectors) in gameData.SectorsBySource)
            AddProtoReferences(protoReferencesByNumber, assetsByPath, assetPath, CountSectorProtoReferences(sectors));

        return protoReferencesByNumber.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorProtoReference>)
                    pair
                        .Value.OrderBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
        );
    }

    public static IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> CountScriptReferences(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var scriptReferencesById = new Dictionary<int, List<EditorScriptReference>>();

        foreach (var (assetPath, mobs) in gameData.MobsBySource)
            AddScriptReferences(scriptReferencesById, assetsByPath, assetPath, CountMobScriptReferences(mobs));

        foreach (var (assetPath, protos) in gameData.ProtosBySource)
            AddScriptReferences(scriptReferencesById, assetsByPath, assetPath, CountProtoScriptReferences(protos));

        foreach (var (assetPath, sectors) in gameData.SectorsBySource)
            AddScriptReferences(scriptReferencesById, assetsByPath, assetPath, CountSectorScriptReferences(sectors));

        return scriptReferencesById.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorScriptReference>)
                    pair
                        .Value.OrderBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
        );
    }

    public static IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> CountArtReferences(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var artReferencesById = new Dictionary<uint, List<EditorArtReference>>();

        foreach (var (assetPath, mobs) in gameData.MobsBySource)
            AddArtReferences(artReferencesById, assetsByPath, assetPath, CountMobArtReferences(mobs));

        foreach (var (assetPath, protos) in gameData.ProtosBySource)
            AddArtReferences(artReferencesById, assetsByPath, assetPath, CountProtoArtReferences(protos));

        foreach (var (assetPath, sectors) in gameData.SectorsBySource)
            AddArtReferences(artReferencesById, assetsByPath, assetPath, CountSectorArtReferences(sectors));

        return artReferencesById.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorArtReference>)
                    pair
                        .Value.OrderBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
        );
    }

    private static Dictionary<int, int> CountMobProtoReferences(IReadOnlyList<MobData> mobs)
    {
        var counts = new Dictionary<int, int>();

        foreach (var mob in mobs)
        {
            var protoNumber = mob.Header.ProtoId.GetProtoNumber();
            if (!protoNumber.HasValue)
                continue;

            IncrementCount(counts, protoNumber.Value);
        }

        return counts;
    }

    private static Dictionary<int, int> CountMobScriptReferences(IReadOnlyList<MobData> mobs)
    {
        var counts = new Dictionary<int, int>();

        foreach (var mob in mobs)
            AddObjectScriptReferences(counts, mob.Properties);

        return counts;
    }

    private static Dictionary<uint, int> CountMobArtReferences(IReadOnlyList<MobData> mobs)
    {
        var counts = new Dictionary<uint, int>();

        foreach (var mob in mobs)
            AddObjectArtReferences(counts, mob.Properties);

        return counts;
    }

    private static Dictionary<int, int> CountProtoScriptReferences(IReadOnlyList<ProtoData> protos)
    {
        var counts = new Dictionary<int, int>();

        foreach (var proto in protos)
            AddObjectScriptReferences(counts, proto.Properties);

        return counts;
    }

    private static Dictionary<uint, int> CountProtoArtReferences(IReadOnlyList<ProtoData> protos)
    {
        var counts = new Dictionary<uint, int>();

        foreach (var proto in protos)
            AddObjectArtReferences(counts, proto.Properties);

        return counts;
    }

    private static Dictionary<int, int> CountSectorProtoReferences(IReadOnlyList<Sector> sectors)
    {
        var counts = new Dictionary<int, int>();

        foreach (var sector in sectors)
        foreach (var mob in sector.Objects)
        {
            var protoNumber = mob.Header.ProtoId.GetProtoNumber();
            if (!protoNumber.HasValue)
                continue;

            IncrementCount(counts, protoNumber.Value);
        }

        return counts;
    }

    private static Dictionary<int, int> CountSectorScriptReferences(IReadOnlyList<Sector> sectors)
    {
        var counts = new Dictionary<int, int>();

        foreach (var sector in sectors)
        {
            if (sector.SectorScript is { } sectorScript && !sectorScript.IsEmpty)
                IncrementCount(counts, sectorScript.ScriptId);

            foreach (var tileScript in sector.TileScripts)
            {
                if (tileScript.ScriptNum == 0)
                    continue;

                IncrementCount(counts, tileScript.ScriptNum);
            }

            foreach (var mob in sector.Objects)
                AddObjectScriptReferences(counts, mob.Properties);
        }

        return counts;
    }

    private static Dictionary<uint, int> CountSectorArtReferences(IReadOnlyList<Sector> sectors)
    {
        var counts = new Dictionary<uint, int>();

        foreach (var sector in sectors)
        {
            foreach (var light in sector.Lights)
                IncrementNonZeroCount(counts, light.ArtId);

            foreach (var tileArtId in sector.Tiles)
                IncrementNonZeroCount(counts, tileArtId);

            if (sector.Roofs is not null)
            {
                foreach (var roofArtId in sector.Roofs)
                    IncrementNonZeroCount(counts, roofArtId);
            }

            foreach (var mob in sector.Objects)
                AddObjectArtReferences(counts, mob.Properties);
        }

        return counts;
    }

    private static void AddProtoReferences(
        Dictionary<int, List<EditorProtoReference>> protoReferencesByNumber,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath,
        string assetPath,
        IReadOnlyDictionary<int, int> countsByProtoNumber
    )
    {
        if (!assetsByPath.TryGetValue(assetPath, out var asset))
            return;

        foreach (var (protoNumber, count) in countsByProtoNumber)
        {
            if (!protoReferencesByNumber.TryGetValue(protoNumber, out var references))
            {
                references = [];
                protoReferencesByNumber[protoNumber] = references;
            }

            references.Add(
                new EditorProtoReference
                {
                    Asset = asset,
                    ProtoNumber = protoNumber,
                    Count = count,
                }
            );
        }
    }

    private static void AddScriptReferences(
        Dictionary<int, List<EditorScriptReference>> scriptReferencesById,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath,
        string assetPath,
        IReadOnlyDictionary<int, int> countsByScriptId
    )
    {
        if (!assetsByPath.TryGetValue(assetPath, out var asset))
            return;

        foreach (var (scriptId, count) in countsByScriptId)
        {
            if (!scriptReferencesById.TryGetValue(scriptId, out var references))
            {
                references = [];
                scriptReferencesById[scriptId] = references;
            }

            references.Add(
                new EditorScriptReference
                {
                    Asset = asset,
                    ScriptId = scriptId,
                    Count = count,
                }
            );
        }
    }

    private static void AddArtReferences(
        Dictionary<uint, List<EditorArtReference>> artReferencesById,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath,
        string assetPath,
        IReadOnlyDictionary<uint, int> countsByArtId
    )
    {
        if (!assetsByPath.TryGetValue(assetPath, out var asset))
            return;

        foreach (var (artId, count) in countsByArtId)
        {
            if (!artReferencesById.TryGetValue(artId, out var references))
            {
                references = [];
                artReferencesById[artId] = references;
            }

            references.Add(
                new EditorArtReference
                {
                    Asset = asset,
                    ArtId = artId,
                    Count = count,
                }
            );
        }
    }

    private static void AddObjectScriptReferences(Dictionary<int, int> counts, IReadOnlyList<ObjectProperty> properties)
    {
        foreach (var property in properties)
        {
            if (property.Field != ObjectField.ObjFScriptsIdx)
                continue;

            if (!TryGetScriptArray(property, out var scripts))
                continue;

            foreach (var script in scripts)
            {
                if (script.ScriptId == 0)
                    continue;

                IncrementCount(counts, script.ScriptId);
            }
        }
    }

    private static void AddObjectArtReferences(Dictionary<uint, int> counts, IReadOnlyList<ObjectProperty> properties)
    {
        foreach (var property in properties)
        {
            switch (property.Field)
            {
                case ObjectField.ObjFCurrentAid:
                case ObjectField.ObjFShadow:
                case ObjectField.ObjFLightAid:
                case ObjectField.ObjFAid:
                case ObjectField.ObjFDestroyedAid:
                    if (TryGetArtId(property, out var artId))
                        IncrementNonZeroCount(counts, artId);
                    break;
            }
        }
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

    private static void IncrementCount<TKey>(Dictionary<TKey, int> counts, TKey key)
        where TKey : notnull
    {
        counts[key] = counts.GetValueOrDefault(key) + 1;
    }

    private static void IncrementNonZeroCount(Dictionary<uint, int> counts, uint key)
    {
        if (key == 0)
            return;

        IncrementCount(counts, key);
    }
}
