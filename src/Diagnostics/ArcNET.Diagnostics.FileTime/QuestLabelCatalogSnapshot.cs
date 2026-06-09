namespace ArcNET.Diagnostics;

public sealed record QuestLabelCatalogSnapshot(string Source, IReadOnlyDictionary<int, string> Labels)
{
    public string? Resolve(int protoId) => Labels.TryGetValue(protoId, out var label) ? label : null;
}
