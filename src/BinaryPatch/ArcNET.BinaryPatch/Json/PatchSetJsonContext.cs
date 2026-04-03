using System.Text.Json.Serialization;

namespace ArcNET.BinaryPatch.Json;

[JsonSerializable(typeof(PatchSetDescriptor))]
[JsonSerializable(typeof(List<PatchDescriptor>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class PatchSetJsonContext : JsonSerializerContext { }
