using System.Text.Json;
using System.Text.Json.Serialization;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData;

// ── DTOs ────────────────────────────────────────────────────────────────────

/// <summary>JSON-serialisable snapshot of a single object header.</summary>
public sealed record GameObjectHeaderDto(int Version, string Type, string ObjectId, string ProtoId, bool IsPrototype);

/// <summary>JSON-serialisable snapshot of a single message entry.</summary>
public sealed record MessageEntryDto(int Index, string? SoundId, string Text);

/// <summary>JSON-serialisable summary of a sector.</summary>
public sealed record SectorDto(int LightCount, int TileCount, bool HasRoofs, int TileScriptCount, int ObjectCount);

/// <summary>JSON-serialisable summary of a prototype.</summary>
public sealed record ProtoDto(int Version, string Type, string ObjectId, string ProtoId, int PropertyCount);

/// <summary>JSON-serialisable summary of a mobile object.</summary>
public sealed record MobDto(int Version, string Type, string ObjectId, string ProtoId, int PropertyCount);

/// <summary>Top-level export payload.</summary>
public sealed record GameDataExportDto(
    IReadOnlyList<GameObjectHeaderDto> Objects,
    IReadOnlyList<MessageEntryDto> Messages,
    IReadOnlyList<SectorDto> Sectors,
    IReadOnlyList<ProtoDto> Protos,
    IReadOnlyList<MobDto> Mobs
);

// ── Source-generated JSON context ────────────────────────────────────────────

[JsonSerializable(typeof(GameDataExportDto))]
[JsonSerializable(typeof(GameObjectHeaderDto))]
[JsonSerializable(typeof(MessageEntryDto))]
[JsonSerializable(typeof(SectorDto))]
[JsonSerializable(typeof(ProtoDto))]
[JsonSerializable(typeof(MobDto))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class GameDataJsonContext : JsonSerializerContext { }

// ── Exporter ─────────────────────────────────────────────────────────────────

/// <summary>
/// Exports a <see cref="GameDataStore"/> to JSON using <see cref="System.Text.Json"/> source generation
/// (no reflection; compatible with AOT-trimmed builds).
/// </summary>
public static class GameDataExporter
{
    /// <summary>Serialises <paramref name="store"/> to a JSON string.</summary>
    public static string ExportToJson(GameDataStore store)
    {
        var dto = ToDto(store);
        return JsonSerializer.Serialize(dto, GameDataJsonContext.Default.GameDataExportDto);
    }

    /// <summary>Writes <paramref name="store"/> to <paramref name="outputPath"/> as JSON.</summary>
    public static Task ExportToJsonFileAsync(
        GameDataStore store,
        string outputPath,
        CancellationToken cancellationToken = default
    )
    {
        var json = ExportToJson(store);
        return File.WriteAllTextAsync(outputPath, json, cancellationToken);
    }

    private static GameDataExportDto ToDto(GameDataStore store)
    {
        var objects = store
            .Objects.Select(h => new GameObjectHeaderDto(
                h.Version,
                h.GameObjectType.ToString(),
                h.ObjectId.ToString(),
                h.ProtoId.ToString(),
                h.IsPrototype
            ))
            .ToList();

        var messages = store.Messages.Select(e => new MessageEntryDto(e.Index, e.SoundId, e.Text)).ToList();

        var sectors = store
            .Sectors.Select(s => new SectorDto(
                s.Lights.Count,
                s.Tiles.Length,
                s.HasRoofs,
                s.TileScripts.Count,
                s.Objects.Count
            ))
            .ToList();

        var protos = store
            .Protos.Select(p => new ProtoDto(
                p.Header.Version,
                p.Header.GameObjectType.ToString(),
                p.Header.ObjectId.ToString(),
                p.Header.ProtoId.ToString(),
                p.Properties.Count
            ))
            .ToList();

        var mobs = store
            .Mobs.Select(m => new MobDto(
                m.Header.Version,
                m.Header.GameObjectType.ToString(),
                m.Header.ObjectId.ToString(),
                m.Header.ProtoId.ToString(),
                m.Properties.Count
            ))
            .ToList();

        return new GameDataExportDto(objects, messages, sectors, protos, mobs);
    }
}
