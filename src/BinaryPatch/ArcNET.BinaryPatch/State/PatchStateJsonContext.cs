using System.Text.Json.Serialization;

namespace ArcNET.BinaryPatch.State;

[JsonSerializable(typeof(PatchState))]
[JsonSerializable(typeof(List<PatchStateEntry>))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class PatchStateJsonContext : JsonSerializerContext { }
