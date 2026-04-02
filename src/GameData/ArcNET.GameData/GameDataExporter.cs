using System.Text.Json;
using System.Text.Json.Serialization;
using ArcNET.GameObjects;

namespace ArcNET.GameData;

// ── DTOs ────────────────────────────────────────────────────────────────────

/// <summary>JSON-serialisable snapshot of a single object header.</summary>
public sealed record GameObjectHeaderDto(int Version, string Type, string ObjectId, string ProtoId, bool IsPrototype);

/// <summary>Top-level export payload.</summary>
public sealed record GameDataExportDto(IReadOnlyList<GameObjectHeaderDto> Objects, IReadOnlyList<string> Messages);

// ── Source-generated JSON context ────────────────────────────────────────────

[JsonSerializable(typeof(GameDataExportDto))]
[JsonSerializable(typeof(GameObjectHeaderDto))]
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

        return new GameDataExportDto(objects, store.Messages);
    }
}
