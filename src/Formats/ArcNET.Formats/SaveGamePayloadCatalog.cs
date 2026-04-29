namespace ArcNET.Formats;

internal sealed class SaveGamePayloadCatalog
{
    public required IReadOnlyList<SaveMapStateBuilder> MapBuilders { get; init; }

    public required List<(string VirtualPath, byte[] Data)> MessageFiles { get; init; }

    public required List<(string VirtualPath, TownMapFog Data)> TownMapFogs { get; init; }

    public required List<(string VirtualPath, DataSavFile Data)> DataSavFiles { get; init; }

    public required List<(string VirtualPath, Data2SavFile Data)> Data2SavFiles { get; init; }

    public required List<(string VirtualPath, byte[] Data)> RawFiles { get; init; }
}
