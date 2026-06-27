using System.Buffers.Binary;
using System.Runtime.InteropServices;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

internal static class EditorAssetReferenceCounter
{
    private const int SarDataOffset = 13;
    private const int ScriptArrayElementSize = 12;
    private const int LegacyScriptIdArrayElementSize = 4;
    private const int ParallelSectorSourceThreshold = 32;

    public static void CountReferences(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath,
        out IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> protoReferencesByNumber,
        out IReadOnlyDictionary<string, IReadOnlyList<EditorProtoReference>> protoReferencesByAssetPath,
        out IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> scriptReferencesById,
        out IReadOnlyDictionary<string, IReadOnlyList<EditorScriptReference>> scriptReferencesByAssetPath,
        out IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> artReferencesById,
        out IReadOnlyDictionary<string, IReadOnlyList<EditorArtReference>> artReferencesByAssetPath,
        bool includeArtReferences = true
    )
    {
        var rawProtoByNumber = new Dictionary<int, List<EditorProtoReference>>();
        var rawProtoByAsset = new Dictionary<string, List<EditorProtoReference>>(StringComparer.OrdinalIgnoreCase);

        var rawScriptById = new Dictionary<int, List<EditorScriptReference>>();
        var rawScriptByAsset = new Dictionary<string, List<EditorScriptReference>>(StringComparer.OrdinalIgnoreCase);

        var rawArtById = new Dictionary<uint, List<EditorArtReference>>();
        var rawArtByAsset = new Dictionary<string, List<EditorArtReference>>(StringComparer.OrdinalIgnoreCase);

        var tempProtoCounts = new Dictionary<int, int>();
        var tempScriptCounts = new Dictionary<int, int>();
        Dictionary<uint, int>? tempArtCounts = includeArtReferences ? [] : null;

        foreach (var (assetPath, mobs) in gameData.MobsBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset))
                continue;

            CountMobReferences(mobs, tempProtoCounts, tempScriptCounts, tempArtCounts);
            AddProtoReferences(asset, assetPath, tempProtoCounts, rawProtoByNumber, rawProtoByAsset);
            AddScriptReferences(asset, assetPath, tempScriptCounts, rawScriptById, rawScriptByAsset);
            AddArtReferences(asset, assetPath, tempArtCounts, rawArtById, rawArtByAsset);
        }

        foreach (var (assetPath, protos) in gameData.ProtosBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset))
                continue;

            CountProtoReferences(protos, tempScriptCounts, tempArtCounts);
            AddScriptReferences(asset, assetPath, tempScriptCounts, rawScriptById, rawScriptByAsset);
            AddArtReferences(asset, assetPath, tempArtCounts, rawArtById, rawArtByAsset);
        }

        var sectorGroups = CreateSourceGroups(gameData.SectorsBySource, assetsByPath);
        var sectorCounts = CountSectorReferences(sectorGroups, includeArtReferences);
        for (var index = 0; index < sectorCounts.Length; index++)
        {
            var counts = sectorCounts[index];
            AddProtoReferences(counts, rawProtoByNumber, rawProtoByAsset);
            AddScriptReferences(counts, rawScriptById, rawScriptByAsset);
            AddArtReferences(counts, rawArtById, rawArtByAsset);
        }

        SortReferences(rawProtoByNumber, rawProtoByAsset, rawScriptById, rawScriptByAsset, rawArtById, rawArtByAsset);

        protoReferencesByNumber = AsReadOnly(rawProtoByNumber);
        protoReferencesByAssetPath = AsReadOnly(rawProtoByAsset);

        scriptReferencesById = AsReadOnly(rawScriptById);
        scriptReferencesByAssetPath = AsReadOnly(rawScriptByAsset);

        artReferencesById = AsReadOnly(rawArtById);
        artReferencesByAssetPath = AsReadOnly(rawArtByAsset);
    }

    private static SourceGroup<T>[] CreateSourceGroups<T>(
        IReadOnlyDictionary<string, IReadOnlyList<T>> assetsBySource,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var groups = new List<SourceGroup<T>>(assetsBySource.Count);
        foreach (var (assetPath, entries) in assetsBySource)
        {
            if (assetsByPath.TryGetValue(assetPath, out var asset))
                groups.Add(new SourceGroup<T>(assetPath, asset, entries));
        }

        groups.Sort(static (a, b) => string.Compare(a.AssetPath, b.AssetPath, StringComparison.OrdinalIgnoreCase));
        return [.. groups];
    }

    private static SourceReferenceCounts[] CountSectorReferences(
        SourceGroup<Sector>[] groups,
        bool includeArtReferences
    )
    {
        var counts = new SourceReferenceCounts[groups.Length];
        if (groups.Length < ParallelSectorSourceThreshold || Environment.ProcessorCount <= 1)
        {
            for (var index = 0; index < groups.Length; index++)
                counts[index] = CountSectorReferences(groups[index], includeArtReferences);

            return counts;
        }

        Parallel.For(
            0,
            groups.Length,
            new ParallelOptions { MaxDegreeOfParallelism = EditorParallelism.InteractiveMaxDegreeOfParallelism },
            index => counts[index] = CountSectorReferences(groups[index], includeArtReferences)
        );
        return counts;
    }

    private static SourceReferenceCounts CountSectorReferences(SourceGroup<Sector> group, bool includeArtReferences)
    {
        var counts = new SourceReferenceCounts(group.AssetPath, group.Asset);

        for (var i = 0; i < group.Entries.Count; i++)
        {
            var sector = group.Entries[i];

            if (sector.SectorScript is { } sectorScript && !sectorScript.IsEmpty)
                counts.IncrementScript(sectorScript.ScriptId);

            for (var j = 0; j < sector.TileScripts.Count; j++)
            {
                var tileScript = sector.TileScripts[j];
                if (tileScript.ScriptNum != 0)
                    counts.IncrementScript(tileScript.ScriptNum);
            }

            if (includeArtReferences)
            {
                for (var j = 0; j < sector.Lights.Count; j++)
                    counts.IncrementNonZeroArt(sector.Lights[j].ArtId);

                counts.IncrementNonZeroArts(sector.Tiles);

                if (sector.Roofs is not null)
                    counts.IncrementNonZeroArts(sector.Roofs);
            }

            for (var j = 0; j < sector.Objects.Count; j++)
            {
                var obj = sector.Objects[j];
                var protoNumber = obj.Header.ProtoId.GetProtoNumber();
                if (protoNumber.HasValue)
                    counts.IncrementProto(protoNumber.Value);

                AddObjectReferences(counts, obj.Properties, includeArtReferences);
            }
        }

        return counts;
    }

    private static void CountMobReferences(
        IReadOnlyList<MobData> mobs,
        Dictionary<int, int> protoCounts,
        Dictionary<int, int> scriptCounts,
        Dictionary<uint, int>? artCounts
    )
    {
        protoCounts.Clear();
        scriptCounts.Clear();
        artCounts?.Clear();

        for (var index = 0; index < mobs.Count; index++)
        {
            var mob = mobs[index];
            var protoNumber = mob.Header.ProtoId.GetProtoNumber();
            if (protoNumber.HasValue)
                IncrementCount(protoCounts, protoNumber.Value);

            AddObjectReferences(scriptCounts, artCounts, mob.Properties);
        }
    }

    private static void CountProtoReferences(
        IReadOnlyList<ProtoData> protos,
        Dictionary<int, int> scriptCounts,
        Dictionary<uint, int>? artCounts
    )
    {
        scriptCounts.Clear();
        artCounts?.Clear();

        for (var index = 0; index < protos.Count; index++)
            AddObjectReferences(scriptCounts, artCounts, protos[index].Properties);
    }

    private static void AddObjectReferences(
        Dictionary<int, int> scriptCounts,
        Dictionary<uint, int>? artCounts,
        IReadOnlyList<ObjectProperty> properties
    )
    {
        for (var index = 0; index < properties.Count; index++)
        {
            var property = properties[index];
            switch (property.Field)
            {
                case ObjectField.ScriptsIdx:
                    AddObjectScriptReferences(scriptCounts, property);
                    break;
                case ObjectField.CurrentAid:
                case ObjectField.Shadow:
                case ObjectField.LightAid:
                case ObjectField.Aid:
                case ObjectField.DestroyedAid:
                    if (artCounts is not null && TryGetArtId(property, out var artId))
                        IncrementNonZeroCount(artCounts, artId);
                    break;
            }
        }
    }

    private static void AddObjectReferences(
        SourceReferenceCounts counts,
        IReadOnlyList<ObjectProperty> properties,
        bool includeArtReferences
    )
    {
        for (var index = 0; index < properties.Count; index++)
        {
            var property = properties[index];
            switch (property.Field)
            {
                case ObjectField.ScriptsIdx:
                    AddObjectScriptReferences(counts, property);
                    break;
                case ObjectField.CurrentAid:
                case ObjectField.Shadow:
                case ObjectField.LightAid:
                case ObjectField.Aid:
                case ObjectField.DestroyedAid:
                    if (includeArtReferences && TryGetArtId(property, out var artId))
                        counts.IncrementNonZeroArt(artId);
                    break;
            }
        }
    }

    private static void AddObjectScriptReferences(Dictionary<int, int> counts, ObjectProperty property)
    {
        var rawBytes = property.RawBytes;
        if (!TryGetScriptArrayPayload(rawBytes, out var elementSize, out var elementCount, out var dataOffset))
            return;

        switch (elementSize)
        {
            case ScriptArrayElementSize:
                for (var index = 0; index < elementCount; index++)
                {
                    var scriptId = BinaryPrimitives.ReadInt32LittleEndian(
                        rawBytes.AsSpan(dataOffset + (index * ScriptArrayElementSize) + 8)
                    );
                    if (scriptId != 0)
                        IncrementCount(counts, scriptId);
                }
                break;
            case LegacyScriptIdArrayElementSize:
                for (var index = 0; index < elementCount; index++)
                {
                    var scriptId = BinaryPrimitives.ReadInt32LittleEndian(
                        rawBytes.AsSpan(dataOffset + (index * LegacyScriptIdArrayElementSize))
                    );
                    if (scriptId > 0)
                        IncrementCount(counts, scriptId);
                }
                break;
        }
    }

    private static void AddObjectScriptReferences(SourceReferenceCounts counts, ObjectProperty property)
    {
        var rawBytes = property.RawBytes;
        if (!TryGetScriptArrayPayload(rawBytes, out var elementSize, out var elementCount, out var dataOffset))
            return;

        switch (elementSize)
        {
            case ScriptArrayElementSize:
                for (var index = 0; index < elementCount; index++)
                {
                    var scriptId = BinaryPrimitives.ReadInt32LittleEndian(
                        rawBytes.AsSpan(dataOffset + (index * ScriptArrayElementSize) + 8)
                    );
                    if (scriptId != 0)
                        counts.IncrementScript(scriptId);
                }
                break;
            case LegacyScriptIdArrayElementSize:
                for (var index = 0; index < elementCount; index++)
                {
                    var scriptId = BinaryPrimitives.ReadInt32LittleEndian(
                        rawBytes.AsSpan(dataOffset + (index * LegacyScriptIdArrayElementSize))
                    );
                    if (scriptId > 0)
                        counts.IncrementScript(scriptId);
                }
                break;
        }
    }

    private static bool TryGetScriptArrayPayload(
        byte[] rawBytes,
        out int elementSize,
        out int elementCount,
        out int dataOffset
    )
    {
        elementSize = 0;
        elementCount = 0;
        dataOffset = SarDataOffset;

        if ((rawBytes.Length == 1 && rawBytes[0] == 0) || rawBytes.Length < SarDataOffset)
            return false;

        elementSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(rawBytes.AsSpan(1));
        elementCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(rawBytes.AsSpan(5));
        if (elementSize is not (ScriptArrayElementSize or LegacyScriptIdArrayElementSize))
            return false;

        return HasCompleteSarPayload(elementSize, elementCount, rawBytes.Length);
    }

    private static bool HasCompleteSarPayload(int elementSize, int elementCount, int rawLength)
    {
        if (elementSize <= 0 || elementCount < 0)
            return false;

        var dataByteLength = (long)elementSize * elementCount;
        return dataByteLength <= int.MaxValue && SarDataOffset + dataByteLength + sizeof(int) <= rawLength;
    }

    private static bool TryGetArtId(ObjectProperty property, out uint artId)
    {
        if (property.RawBytes.Length == sizeof(int))
        {
            artId = unchecked((uint)BinaryPrimitives.ReadInt32LittleEndian(property.RawBytes));
            return true;
        }

        artId = 0;
        return false;
    }

    private static void AddProtoReferences(
        EditorAssetEntry asset,
        string assetPath,
        IReadOnlyDictionary<int, int>? counts,
        Dictionary<int, List<EditorProtoReference>> referencesByNumber,
        Dictionary<string, List<EditorProtoReference>> referencesByAsset
    )
    {
        if (counts is null)
            return;

        foreach (var (protoNumber, count) in counts)
        {
            var reference = new EditorProtoReference
            {
                Asset = asset,
                ProtoNumber = protoNumber,
                Count = count,
            };
            GetOrCreate(referencesByNumber, protoNumber).Add(reference);
            GetOrCreate(referencesByAsset, assetPath).Add(reference);
        }
    }

    private static void AddProtoReferences(
        SourceReferenceCounts counts,
        Dictionary<int, List<EditorProtoReference>> referencesByNumber,
        Dictionary<string, List<EditorProtoReference>> referencesByAsset
    ) => AddProtoReferences(counts.Asset, counts.AssetPath, counts.ProtoCounts, referencesByNumber, referencesByAsset);

    private static void AddScriptReferences(
        EditorAssetEntry asset,
        string assetPath,
        IReadOnlyDictionary<int, int>? counts,
        Dictionary<int, List<EditorScriptReference>> referencesById,
        Dictionary<string, List<EditorScriptReference>> referencesByAsset
    )
    {
        if (counts is null)
            return;

        foreach (var (scriptId, count) in counts)
        {
            var reference = new EditorScriptReference
            {
                Asset = asset,
                ScriptId = scriptId,
                Count = count,
            };
            GetOrCreate(referencesById, scriptId).Add(reference);
            GetOrCreate(referencesByAsset, assetPath).Add(reference);
        }
    }

    private static void AddScriptReferences(
        SourceReferenceCounts counts,
        Dictionary<int, List<EditorScriptReference>> referencesById,
        Dictionary<string, List<EditorScriptReference>> referencesByAsset
    ) => AddScriptReferences(counts.Asset, counts.AssetPath, counts.ScriptCounts, referencesById, referencesByAsset);

    private static void AddArtReferences(
        EditorAssetEntry asset,
        string assetPath,
        IReadOnlyDictionary<uint, int>? counts,
        Dictionary<uint, List<EditorArtReference>> referencesById,
        Dictionary<string, List<EditorArtReference>> referencesByAsset
    )
    {
        if (counts is null)
            return;

        foreach (var (artId, count) in counts)
        {
            var reference = new EditorArtReference
            {
                Asset = asset,
                ArtId = artId,
                Count = count,
            };
            GetOrCreate(referencesById, artId).Add(reference);
            GetOrCreate(referencesByAsset, assetPath).Add(reference);
        }
    }

    private static void AddArtReferences(
        SourceReferenceCounts counts,
        Dictionary<uint, List<EditorArtReference>> referencesById,
        Dictionary<string, List<EditorArtReference>> referencesByAsset
    ) => AddArtReferences(counts.Asset, counts.AssetPath, counts.ArtCounts, referencesById, referencesByAsset);

    private static void SortReferences(
        Dictionary<int, List<EditorProtoReference>> rawProtoByNumber,
        Dictionary<string, List<EditorProtoReference>> rawProtoByAsset,
        Dictionary<int, List<EditorScriptReference>> rawScriptById,
        Dictionary<string, List<EditorScriptReference>> rawScriptByAsset,
        Dictionary<uint, List<EditorArtReference>> rawArtById,
        Dictionary<string, List<EditorArtReference>> rawArtByAsset
    )
    {
        foreach (var list in rawProtoByNumber.Values)
        {
            if (list.Count > 1)
                list.Sort(
                    static (a, b) =>
                        string.Compare(a.Asset.AssetPath, b.Asset.AssetPath, StringComparison.OrdinalIgnoreCase)
                );
        }
        foreach (var list in rawProtoByAsset.Values)
        {
            if (list.Count > 1)
                list.Sort(static (a, b) => a.ProtoNumber.CompareTo(b.ProtoNumber));
        }

        foreach (var list in rawScriptById.Values)
        {
            if (list.Count > 1)
                list.Sort(
                    static (a, b) =>
                        string.Compare(a.Asset.AssetPath, b.Asset.AssetPath, StringComparison.OrdinalIgnoreCase)
                );
        }
        foreach (var list in rawScriptByAsset.Values)
        {
            if (list.Count > 1)
                list.Sort(static (a, b) => a.ScriptId.CompareTo(b.ScriptId));
        }

        foreach (var list in rawArtById.Values)
        {
            if (list.Count > 1)
                list.Sort(
                    static (a, b) =>
                        string.Compare(a.Asset.AssetPath, b.Asset.AssetPath, StringComparison.OrdinalIgnoreCase)
                );
        }
        foreach (var list in rawArtByAsset.Values)
        {
            if (list.Count > 1)
                list.Sort(static (a, b) => a.ArtId.CompareTo(b.ArtId));
        }
    }

    private static void IncrementCount<TKey>(Dictionary<TKey, int> counts, TKey key)
        where TKey : notnull
    {
        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(counts, key, out _);
        count++;
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
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);
        if (!exists)
            list = [];

        return list!;
    }

    private static IReadOnlyDictionary<TKey, IReadOnlyList<TValue>> AsReadOnly<TKey, TValue>(
        Dictionary<TKey, List<TValue>> dict
    )
        where TKey : notnull
    {
        var result = new Dictionary<TKey, IReadOnlyList<TValue>>(dict.Count);
        foreach (var pair in dict)
            result[pair.Key] = pair.Value;
        return result;
    }

    private sealed class SourceGroup<T>(string assetPath, EditorAssetEntry asset, IReadOnlyList<T> entries)
    {
        public string AssetPath { get; } = assetPath;
        public EditorAssetEntry Asset { get; } = asset;
        public IReadOnlyList<T> Entries { get; } = entries;
    }

    private sealed class SourceReferenceCounts(string assetPath, EditorAssetEntry asset)
    {
        public string AssetPath { get; } = assetPath;
        public EditorAssetEntry Asset { get; } = asset;
        public Dictionary<int, int>? ProtoCounts { get; private set; }
        public Dictionary<int, int>? ScriptCounts { get; private set; }
        public Dictionary<uint, int>? ArtCounts { get; private set; }

        public void IncrementProto(int protoNumber) => IncrementCount(ProtoCounts ??= [], protoNumber);

        public void IncrementScript(int scriptId) => IncrementCount(ScriptCounts ??= [], scriptId);

        public void IncrementNonZeroArt(uint artId)
        {
            if (artId == 0)
                return;

            IncrementCount(ArtCounts ??= [], artId);
        }

        public void IncrementNonZeroArts(ReadOnlySpan<uint> artIds)
        {
            Dictionary<uint, int>? artCounts = null;
            for (var index = 0; index < artIds.Length; index++)
            {
                var artId = artIds[index];
                if (artId == 0)
                    continue;

                artCounts ??= ArtCounts ??= [];
                ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(artCounts, artId, out _);
                count++;
            }
        }
    }
}
