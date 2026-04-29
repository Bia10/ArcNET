namespace ArcNET.Formats;

/// <summary>
/// Loads a complete Arcanum save slot from disk or from raw bytes.
/// <para>
/// A save slot consists of three files sharing a stem name:
/// <c>&lt;slot&gt;.tfai</c>, <c>&lt;slot&gt;.tfaf</c>, and <c>&lt;slot&gt;.gsi</c>.
/// When only a TFAI path is supplied the companion paths are derived by replacing the extension.
/// </para>
/// </summary>
public static class SaveGameReader
{
    private const string TfafExtension = ".tfaf";
    private const string GsiExtension = ".gsi";

    /// <summary>
    /// Loads the save slot whose TFAI index is at <paramref name="tfaiPath"/>.
    /// The companion <c>.tfaf</c> and <c>.gsi</c> paths are derived automatically.
    /// </summary>
    /// <param name="tfaiPath">Full filesystem path to the <c>.tfai</c> index file.</param>
    /// <returns>A fully-parsed <see cref="SaveGame"/> containing all map states.</returns>
    public static SaveGame Load(string tfaiPath)
    {
        var tfafPath = Path.ChangeExtension(tfaiPath, TfafExtension);
        var gsiPath = Path.ChangeExtension(tfaiPath, GsiExtension);
        return Load(tfaiPath, tfafPath, gsiPath);
    }

    /// <summary>
    /// Loads the save slot whose TFAI index is at <paramref name="tfaiPath"/> and whose
    /// TFAF data blob is at <paramref name="tfafPath"/>.
    /// The companion <c>.gsi</c> path is derived from <paramref name="tfaiPath"/>.
    /// </summary>
    public static SaveGame Load(string tfaiPath, string tfafPath)
    {
        var gsiPath = Path.ChangeExtension(tfaiPath, GsiExtension);
        return Load(tfaiPath, tfafPath, gsiPath);
    }

    /// <summary>
    /// Loads the save slot from explicitly specified paths for all three companion files.
    /// </summary>
    public static SaveGame Load(string tfaiPath, string tfafPath, string gsiPath)
    {
        var index = SaveIndexFormat.ParseFile(tfaiPath);
        var tfafData = (ReadOnlyMemory<byte>)File.ReadAllBytes(tfafPath);
        var gsiData = (ReadOnlyMemory<byte>)File.ReadAllBytes(gsiPath);

        var payloads = TfafFormat.ExtractAll(index, tfafData);
        var info = SaveInfoFormat.ParseMemory(gsiData);

        return ParseSaveGame(info, payloads);
    }

    /// <summary>
    /// Parses a <see cref="SaveGame"/> from pre-loaded byte blobs without filesystem access.
    /// </summary>
    /// <param name="tfaiData">Raw bytes of the <c>.tfai</c> index file.</param>
    /// <param name="tfafData">Raw bytes of the <c>.tfaf</c> data blob.</param>
    /// <param name="gsiData">Raw bytes of the <c>.gsi</c> metadata file.</param>
    public static SaveGame ParseMemory(
        ReadOnlyMemory<byte> tfaiData,
        ReadOnlyMemory<byte> tfafData,
        ReadOnlyMemory<byte> gsiData
    )
    {
        var index = SaveIndexFormat.ParseMemory(tfaiData);
        var payloads = TfafFormat.ExtractAll(index, tfafData);
        var info = SaveInfoFormat.ParseMemory(gsiData);
        return ParseSaveGame(info, payloads);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private static SaveGame ParseSaveGame(SaveInfo info, IReadOnlyDictionary<string, byte[]> payloads)
    {
        var payloadCatalog = SaveGamePayloadClassifier.Classify(payloads);
        var maps = payloadCatalog
            .MapBuilders.OrderBy(static b => b.MapPath, StringComparer.OrdinalIgnoreCase)
            .Select(static b => b.Build())
            .ToList();

        return new SaveGame
        {
            Info = info,
            EngineVersion = SaveGameEngineVersionDetector.Detect(maps),
            Maps = maps,
            MessageFiles = payloadCatalog.MessageFiles,
            TownMapFogs = payloadCatalog.TownMapFogs,
            DataSavFiles = payloadCatalog.DataSavFiles,
            Data2SavFiles = payloadCatalog.Data2SavFiles,
            RawFiles = payloadCatalog.RawFiles,
        };
    }
}
