using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

internal static class EditorAssetReferenceCounter
{
    public static void CountReferences(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath,
        out IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> protoReferencesByNumber,
        out IReadOnlyDictionary<string, IReadOnlyList<EditorProtoReference>> protoReferencesByAssetPath,
        out IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> scriptReferencesById,
        out IReadOnlyDictionary<string, IReadOnlyList<EditorScriptReference>> scriptReferencesByAssetPath,
        out IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> artReferencesById,
        out IReadOnlyDictionary<string, IReadOnlyList<EditorArtReference>> artReferencesByAssetPath
    )
    {
        var rawProtoByNumber = new Dictionary<int, List<EditorProtoReference>>();
        var rawProtoByAsset = new Dictionary<string, List<EditorProtoReference>>(StringComparer.OrdinalIgnoreCase);

        var rawScriptById = new Dictionary<int, List<EditorScriptReference>>();
        var rawScriptByAsset = new Dictionary<string, List<EditorScriptReference>>(StringComparer.OrdinalIgnoreCase);

        var rawArtById = new Dictionary<uint, List<EditorArtReference>>();
        var rawArtByAsset = new Dictionary<string, List<EditorArtReference>>(StringComparer.OrdinalIgnoreCase);

        var tempIntCounts = new Dictionary<int, int>();
        var tempUintCounts = new Dictionary<uint, int>();

        // 1. Traverse Mobs By Source
        foreach (var (assetPath, mobs) in gameData.MobsBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset))
                continue;

            // Proto references in Mobs
            CountMobProtoReferences(mobs, tempIntCounts);
            foreach (var (protoNumber, count) in tempIntCounts)
            {
                var reference = new EditorProtoReference
                {
                    Asset = asset,
                    ProtoNumber = protoNumber,
                    Count = count,
                };
                GetOrCreate(rawProtoByNumber, protoNumber).Add(reference);
                GetOrCreate(rawProtoByAsset, assetPath).Add(reference);
            }

            // Script references in Mobs
            CountMobScriptReferences(mobs, tempIntCounts);
            foreach (var (scriptId, count) in tempIntCounts)
            {
                var reference = new EditorScriptReference
                {
                    Asset = asset,
                    ScriptId = scriptId,
                    Count = count,
                };
                GetOrCreate(rawScriptById, scriptId).Add(reference);
                GetOrCreate(rawScriptByAsset, assetPath).Add(reference);
            }

            // Art references in Mobs
            CountMobArtReferences(mobs, tempUintCounts);
            foreach (var (artId, count) in tempUintCounts)
            {
                var reference = new EditorArtReference
                {
                    Asset = asset,
                    ArtId = artId,
                    Count = count,
                };
                GetOrCreate(rawArtById, artId).Add(reference);
                GetOrCreate(rawArtByAsset, assetPath).Add(reference);
            }
        }

        // 2. Traverse Protos By Source
        foreach (var (assetPath, protos) in gameData.ProtosBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset))
                continue;

            // Script references in Protos
            CountProtoScriptReferences(protos, tempIntCounts);
            foreach (var (scriptId, count) in tempIntCounts)
            {
                var reference = new EditorScriptReference
                {
                    Asset = asset,
                    ScriptId = scriptId,
                    Count = count,
                };
                GetOrCreate(rawScriptById, scriptId).Add(reference);
                GetOrCreate(rawScriptByAsset, assetPath).Add(reference);
            }

            // Art references in Protos
            CountProtoArtReferences(protos, tempUintCounts);
            foreach (var (artId, count) in tempUintCounts)
            {
                var reference = new EditorArtReference
                {
                    Asset = asset,
                    ArtId = artId,
                    Count = count,
                };
                GetOrCreate(rawArtById, artId).Add(reference);
                GetOrCreate(rawArtByAsset, assetPath).Add(reference);
            }
        }

        // 3. Traverse Sectors By Source
        foreach (var (assetPath, sectors) in gameData.SectorsBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset))
                continue;

            // Proto references in Sectors
            CountSectorProtoReferences(sectors, tempIntCounts);
            foreach (var (protoNumber, count) in tempIntCounts)
            {
                var reference = new EditorProtoReference
                {
                    Asset = asset,
                    ProtoNumber = protoNumber,
                    Count = count,
                };
                GetOrCreate(rawProtoByNumber, protoNumber).Add(reference);
                GetOrCreate(rawProtoByAsset, assetPath).Add(reference);
            }

            // Script references in Sectors
            CountSectorScriptReferences(sectors, tempIntCounts);
            foreach (var (scriptId, count) in tempIntCounts)
            {
                var reference = new EditorScriptReference
                {
                    Asset = asset,
                    ScriptId = scriptId,
                    Count = count,
                };
                GetOrCreate(rawScriptById, scriptId).Add(reference);
                GetOrCreate(rawScriptByAsset, assetPath).Add(reference);
            }

            // Art references in Sectors
            CountSectorArtReferences(sectors, tempUintCounts);
            foreach (var (artId, count) in tempUintCounts)
            {
                var reference = new EditorArtReference
                {
                    Asset = asset,
                    ArtId = artId,
                    Count = count,
                };
                GetOrCreate(rawArtById, artId).Add(reference);
                GetOrCreate(rawArtByAsset, assetPath).Add(reference);
            }
        }

        // 4. In-place sorting to match original behavior exactly

        // Proto references sorted:
        // - By AssetPath when grouped by ProtoNumber
        // - By ProtoNumber when grouped by AssetPath
        foreach (var list in rawProtoByNumber.Values)
            list.Sort(
                static (a, b) =>
                    string.Compare(a.Asset.AssetPath, b.Asset.AssetPath, StringComparison.OrdinalIgnoreCase)
            );
        foreach (var list in rawProtoByAsset.Values)
            list.Sort(static (a, b) => a.ProtoNumber.CompareTo(b.ProtoNumber));

        // Script references sorted:
        // - By AssetPath when grouped by ScriptId
        // - By ScriptId when grouped by AssetPath
        foreach (var list in rawScriptById.Values)
            list.Sort(
                static (a, b) =>
                    string.Compare(a.Asset.AssetPath, b.Asset.AssetPath, StringComparison.OrdinalIgnoreCase)
            );
        foreach (var list in rawScriptByAsset.Values)
            list.Sort(static (a, b) => a.ScriptId.CompareTo(b.ScriptId));

        // Art references sorted:
        // - By AssetPath when grouped by ArtId
        // - By ArtId when grouped by AssetPath
        foreach (var list in rawArtById.Values)
            list.Sort(
                static (a, b) =>
                    string.Compare(a.Asset.AssetPath, b.Asset.AssetPath, StringComparison.OrdinalIgnoreCase)
            );
        foreach (var list in rawArtByAsset.Values)
            list.Sort(static (a, b) => a.ArtId.CompareTo(b.ArtId));

        // 5. Expose as read-only views (no Array copy allocations, directly cast Lists!)
        protoReferencesByNumber = AsReadOnly(rawProtoByNumber);
        protoReferencesByAssetPath = AsReadOnly(rawProtoByAsset);

        scriptReferencesById = AsReadOnly(rawScriptById);
        scriptReferencesByAssetPath = AsReadOnly(rawScriptByAsset);

        artReferencesById = AsReadOnly(rawArtById);
        artReferencesByAssetPath = AsReadOnly(rawArtByAsset);
    }

    private static void CountMobProtoReferences(IReadOnlyList<MobData> mobs, Dictionary<int, int> counts)
    {
        counts.Clear();
        for (var index = 0; index < mobs.Count; index++)
        {
            var protoNumber = mobs[index].Header.ProtoId.GetProtoNumber();
            if (protoNumber.HasValue)
                IncrementCount(counts, protoNumber.Value);
        }
    }

    private static void CountMobScriptReferences(IReadOnlyList<MobData> mobs, Dictionary<int, int> counts)
    {
        counts.Clear();
        for (var index = 0; index < mobs.Count; index++)
            AddObjectScriptReferences(counts, mobs[index].Properties);
    }

    private static void CountMobArtReferences(IReadOnlyList<MobData> mobs, Dictionary<uint, int> counts)
    {
        counts.Clear();
        for (var index = 0; index < mobs.Count; index++)
            AddObjectArtReferences(counts, mobs[index].Properties);
    }

    private static void CountProtoScriptReferences(IReadOnlyList<ProtoData> protos, Dictionary<int, int> counts)
    {
        counts.Clear();
        for (var index = 0; index < protos.Count; index++)
            AddObjectScriptReferences(counts, protos[index].Properties);
    }

    private static void CountProtoArtReferences(IReadOnlyList<ProtoData> protos, Dictionary<uint, int> counts)
    {
        counts.Clear();
        for (var index = 0; index < protos.Count; index++)
            AddObjectArtReferences(counts, protos[index].Properties);
    }

    private static void CountSectorProtoReferences(IReadOnlyList<Sector> sectors, Dictionary<int, int> counts)
    {
        counts.Clear();
        for (var i = 0; i < sectors.Count; i++)
        {
            var sector = sectors[i];
            for (var j = 0; j < sector.Objects.Count; j++)
            {
                var protoNumber = sector.Objects[j].Header.ProtoId.GetProtoNumber();
                if (protoNumber.HasValue)
                    IncrementCount(counts, protoNumber.Value);
            }
        }
    }

    private static void CountSectorScriptReferences(IReadOnlyList<Sector> sectors, Dictionary<int, int> counts)
    {
        counts.Clear();
        for (var i = 0; i < sectors.Count; i++)
        {
            var sector = sectors[i];
            if (sector.SectorScript is { } sectorScript && !sectorScript.IsEmpty)
                IncrementCount(counts, sectorScript.ScriptId);

            for (var j = 0; j < sector.TileScripts.Count; j++)
            {
                var tileScript = sector.TileScripts[j];
                if (tileScript.ScriptNum != 0)
                    IncrementCount(counts, tileScript.ScriptNum);
            }

            for (var j = 0; j < sector.Objects.Count; j++)
                AddObjectScriptReferences(counts, sector.Objects[j].Properties);
        }
    }

    private static void CountSectorArtReferences(IReadOnlyList<Sector> sectors, Dictionary<uint, int> counts)
    {
        counts.Clear();
        for (var i = 0; i < sectors.Count; i++)
        {
            var sector = sectors[i];
            for (var j = 0; j < sector.Lights.Count; j++)
                IncrementNonZeroCount(counts, sector.Lights[j].ArtId);

            for (var j = 0; j < sector.Tiles.Length; j++)
                IncrementNonZeroCount(counts, sector.Tiles[j]);

            if (sector.Roofs is not null)
            {
                for (var j = 0; j < sector.Roofs.Length; j++)
                    IncrementNonZeroCount(counts, sector.Roofs[j]);
            }

            for (var j = 0; j < sector.Objects.Count; j++)
                AddObjectArtReferences(counts, sector.Objects[j].Properties);
        }
    }

    private static void AddObjectScriptReferences(Dictionary<int, int> counts, IReadOnlyList<ObjectProperty> properties)
    {
        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            if (property.Field != ObjectField.ScriptsIdx)
                continue;

            if (!TryGetScriptArray(property, out var scripts))
                continue;

            for (var j = 0; j < scripts.Length; j++)
            {
                var script = scripts[j];
                if (script.ScriptId != 0)
                    IncrementCount(counts, script.ScriptId);
            }
        }
    }

    private static void AddObjectArtReferences(Dictionary<uint, int> counts, IReadOnlyList<ObjectProperty> properties)
    {
        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            switch (property.Field)
            {
                case ObjectField.CurrentAid:
                case ObjectField.Shadow:
                case ObjectField.LightAid:
                case ObjectField.Aid:
                case ObjectField.DestroyedAid:
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

    private static List<T> GetOrCreate<TKey, T>(Dictionary<TKey, List<T>> dict, TKey key)
        where TKey : notnull
    {
        if (!dict.TryGetValue(key, out var list))
        {
            list = [];
            dict[key] = list;
        }
        return list;
    }

    private static IReadOnlyDictionary<TKey, IReadOnlyList<TValue>> AsReadOnly<TKey, TValue>(
        Dictionary<TKey, List<TValue>> dict
    )
        where TKey : notnull
    {
        var result = new Dictionary<TKey, IReadOnlyList<TValue>>(dict.Count);
        foreach (var pair in dict)
        {
            result[pair.Key] = pair.Value;
        }
        return result;
    }
}
