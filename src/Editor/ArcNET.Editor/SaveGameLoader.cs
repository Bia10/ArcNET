using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Loads an Arcanum save slot from disk into a <see cref="LoadedSave"/> instance.
/// A save slot is identified by three files that share the same base name:
/// <list type="bullet">
///   <item><c>{slotName}.gsi</c> — save metadata</item>
///   <item><c>{slotName}.tfai</c> — archive index</item>
///   <item><c>{slotName}.tfaf</c> — archive data blob</item>
/// </list>
/// </summary>
public static class SaveGameLoader
{
    // ── Synchronous load ──────────────────────────────────────────────────────

    /// <summary>
    /// Loads a save slot from three explicit file paths.
    /// </summary>
    /// <param name="gsiPath">Path to the <c>.gsi</c> metadata file.</param>
    /// <param name="tfaiPath">Path to the <c>.tfai</c> index file.</param>
    /// <param name="tfafPath">Path to the <c>.tfaf</c> data blob.</param>
    public static LoadedSave Load(string gsiPath, string tfaiPath, string tfafPath)
    {
        var info = SaveInfoFormat.ParseFile(gsiPath);
        var index = SaveIndexFormat.ParseFile(tfaiPath);
        var tfafData = File.ReadAllBytes(tfafPath);
        return LoadFromParsed(info, index, tfafData);
    }

    /// <summary>
    /// Loads a save slot by folder and slot name.
    /// Resolves paths as <c>{saveFolder}/{slotName}.gsi</c>, <c>.tfai</c>, and <c>.tfaf</c>.
    /// </summary>
    /// <param name="saveFolder">Directory containing the three save slot files.</param>
    /// <param name="slotName">Base file name without extension (e.g. <c>"slot1"</c>).</param>
    public static LoadedSave Load(string saveFolder, string slotName) =>
        Load(
            Path.Combine(saveFolder, slotName + ".gsi"),
            Path.Combine(saveFolder, slotName + ".tfai"),
            Path.Combine(saveFolder, slotName + ".tfaf")
        );

    // ── Asynchronous load ─────────────────────────────────────────────────────

    /// <summary>
    /// Loads a save slot from three explicit file paths asynchronously.
    /// <paramref name="progress"/> is reported per inner file parsed, in the range [0, 1].
    /// </summary>
    /// <param name="gsiPath">Path to the <c>.gsi</c> metadata file.</param>
    /// <param name="tfaiPath">Path to the <c>.tfai</c> index file.</param>
    /// <param name="tfafPath">Path to the <c>.tfaf</c> data blob.</param>
    /// <param name="progress">
    /// Optional progress reporter. Receives values in [0, 1] as embedded files are parsed.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task<LoadedSave> LoadAsync(
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var info = await Task.Run(() => SaveInfoFormat.ParseFile(gsiPath), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var index = await Task.Run(() => SaveIndexFormat.ParseFile(tfaiPath), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var tfafData = await File.ReadAllBytesAsync(tfafPath, cancellationToken).ConfigureAwait(false);
        return await Task.Run(
                () => LoadFromParsed(info, index, tfafData, progress, cancellationToken),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a save slot by folder and slot name asynchronously.
    /// Resolves paths as <c>{saveFolder}/{slotName}.gsi</c>, <c>.tfai</c>, and <c>.tfaf</c>.
    /// <paramref name="progress"/> is reported per inner file parsed, in the range [0, 1].
    /// </summary>
    /// <param name="saveFolder">Directory containing the three save slot files.</param>
    /// <param name="slotName">Base file name without extension (e.g. <c>"slot1"</c>).</param>
    /// <param name="progress">
    /// Optional progress reporter. Receives values in [0, 1] as embedded files are parsed.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static Task<LoadedSave> LoadAsync(
        string saveFolder,
        string slotName,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    ) =>
        LoadAsync(
            Path.Combine(saveFolder, slotName + ".gsi"),
            Path.Combine(saveFolder, slotName + ".tfai"),
            Path.Combine(saveFolder, slotName + ".tfaf"),
            progress,
            cancellationToken
        );

    // ── Core parsing ──────────────────────────────────────────────────────────

    // Inner-archive file names that are not covered by file-extension dispatch.
    private const string DataSavFileName = "data.sav";
    private const string Data2SavFileName = "data2.sav";
    private const string MobileMdFileName = "mobile.md";
    private const string MobileMdyFileName = "mobile.mdy";

    internal static LoadedSave LoadFromParsed(
        SaveInfo info,
        SaveIndex index,
        byte[] tfafData,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var files = TfafFormat.ExtractAll(index, tfafData);

        var rawFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var mobiles = new Dictionary<string, MobData>(StringComparer.OrdinalIgnoreCase);
        var sectors = new Dictionary<string, Sector>(StringComparer.OrdinalIgnoreCase);
        var jumpFiles = new Dictionary<string, JmpFile>(StringComparer.OrdinalIgnoreCase);
        var mapProperties = new Dictionary<string, MapProperties>(StringComparer.OrdinalIgnoreCase);
        var messages = new Dictionary<string, MesFile>(StringComparer.OrdinalIgnoreCase);
        var townMapFogs = new Dictionary<string, TownMapFog>(StringComparer.OrdinalIgnoreCase);
        var dataSavFiles = new Dictionary<string, DataSavFile>(StringComparer.OrdinalIgnoreCase);
        var data2SavFiles = new Dictionary<string, Data2SavFile>(StringComparer.OrdinalIgnoreCase);
        var scripts = new Dictionary<string, ScrFile>(StringComparer.OrdinalIgnoreCase);
        var dialogs = new Dictionary<string, DlgFile>(StringComparer.OrdinalIgnoreCase);
        var mobileMds = new Dictionary<string, MobileMdFile>(StringComparer.OrdinalIgnoreCase);
        var mobileMdys = new Dictionary<string, MobileMdyFile>(StringComparer.OrdinalIgnoreCase);
        var parseErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var entries = files.ToList();
        var total = entries.Count;

        for (var i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (path, bytes) = (entries[i].Key, entries[i].Value);
            var mem = (ReadOnlyMemory<byte>)bytes;
            var format = FileFormatExtensions.FromPath(path);
            var fileName = Path.GetFileName(path);
            var hasTypedSurface = false;

            // Each format is parsed independently; a corrupt/stub file of one type
            // does not prevent the rest of the save from loading.
            try
            {
                if (fileName.Equals(DataSavFileName, StringComparison.OrdinalIgnoreCase))
                {
                    dataSavFiles[path] = DataSavFormat.ParseMemory(mem);
                    hasTypedSurface = true;
                }
                else if (fileName.Equals(Data2SavFileName, StringComparison.OrdinalIgnoreCase))
                {
                    data2SavFiles[path] = Data2SavFormat.ParseMemory(mem);
                    hasTypedSurface = true;
                }
                else
                {
                    switch (format)
                    {
                        case FileFormat.Mob:
                            mobiles[path] = MobFormat.ParseMemory(mem);
                            hasTypedSurface = true;
                            break;
                        case FileFormat.Sector:
                            sectors[path] = SectorFormat.ParseMemory(mem);
                            hasTypedSurface = true;
                            break;
                        case FileFormat.Jmp:
                            jumpFiles[path] = JmpFormat.ParseMemory(mem);
                            hasTypedSurface = true;
                            break;
                        case FileFormat.MapProperties:
                            mapProperties[path] = MapPropertiesFormat.ParseMemory(mem);
                            hasTypedSurface = true;
                            break;
                        case FileFormat.Message:
                            messages[path] = MessageFormat.ParseMemory(mem);
                            hasTypedSurface = true;
                            break;
                        case FileFormat.TownMapFog:
                            townMapFogs[path] = TownMapFogFormat.ParseMemory(mem);
                            hasTypedSurface = true;
                            break;
                        case FileFormat.Script:
                            scripts[path] = ScriptFormat.ParseMemory(mem);
                            hasTypedSurface = true;
                            break;
                        case FileFormat.Dialog:
                            dialogs[path] = DialogFormat.ParseMemory(mem);
                            hasTypedSurface = true;
                            break;
                        default:
                            // mobile.md and mobile.mdy are special inner-archive files not in the extension map.
                            if (fileName.Equals(MobileMdFileName, StringComparison.OrdinalIgnoreCase))
                            {
                                mobileMds[path] = MobileMdFormat.ParseMemory(mem);
                                hasTypedSurface = true;
                            }
                            else if (fileName.Equals(MobileMdyFileName, StringComparison.OrdinalIgnoreCase))
                            {
                                mobileMdys[path] = MobileMdyFormat.ParseMemory(mem);
                                hasTypedSurface = true;
                            }

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Record the error but continue — raw bytes are still accessible via Files.
                parseErrors[path] = $"{ex.GetType().Name}: {ex.Message}";
            }

            if (!hasTypedSurface)
                rawFiles[path] = bytes;

            progress?.Report((i + 1f) / total);
        }

        return new LoadedSave
        {
            Info = info,
            Index = index,
            Files = files,
            RawFiles = rawFiles,
            Mobiles = mobiles,
            Sectors = sectors,
            JumpFiles = jumpFiles,
            MapPropertiesList = mapProperties,
            Messages = messages,
            TownMapFogs = townMapFogs,
            DataSavFiles = dataSavFiles,
            Data2SavFiles = data2SavFiles,
            Scripts = scripts,
            Dialogs = dialogs,
            MobileMds = mobileMds,
            MobileMdys = mobileMdys,
            ParseErrors = parseErrors,
        };
    }
}
