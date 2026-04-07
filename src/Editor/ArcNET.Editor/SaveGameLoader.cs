using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Loads an Arcanum save slot from disk into a <see cref="SaveGame"/> instance.
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
    public static SaveGame Load(string gsiPath, string tfaiPath, string tfafPath)
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
    public static SaveGame Load(string saveFolder, string slotName) =>
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
    public static async Task<SaveGame> LoadAsync(
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
    public static Task<SaveGame> LoadAsync(
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

    internal static SaveGame LoadFromParsed(
        SaveInfo info,
        SaveIndex index,
        byte[] tfafData,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var files = TfafFormat.ExtractAll(index, tfafData);

        var mobiles = new Dictionary<string, MobData>(StringComparer.OrdinalIgnoreCase);
        var sectors = new Dictionary<string, Sector>(StringComparer.OrdinalIgnoreCase);
        var jumpFiles = new Dictionary<string, JmpFile>(StringComparer.OrdinalIgnoreCase);
        var mapProperties = new Dictionary<string, MapProperties>(StringComparer.OrdinalIgnoreCase);
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
            var ext = Path.GetExtension(path);

            // Each format is parsed independently; a corrupt/stub file of one type
            // does not prevent the rest of the save from loading.
            try
            {
                if (ext.Equals(".mob", StringComparison.OrdinalIgnoreCase))
                    mobiles[path] = MobFormat.ParseMemory(mem);
                else if (ext.Equals(".sec", StringComparison.OrdinalIgnoreCase))
                    sectors[path] = SectorFormat.ParseMemory(mem);
                else if (ext.Equals(".jmp", StringComparison.OrdinalIgnoreCase))
                    jumpFiles[path] = JmpFormat.ParseMemory(mem);
                else if (ext.Equals(".prp", StringComparison.OrdinalIgnoreCase))
                    mapProperties[path] = MapPropertiesFormat.ParseMemory(mem);
                else if (ext.Equals(".scr", StringComparison.OrdinalIgnoreCase))
                    scripts[path] = ScriptFormat.ParseMemory(mem);
                else if (ext.Equals(".dlg", StringComparison.OrdinalIgnoreCase))
                    dialogs[path] = DialogFormat.ParseMemory(mem);
                else if (
                    ext.Equals(".md", StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(path).Equals("mobile.md", StringComparison.OrdinalIgnoreCase)
                )
                    mobileMds[path] = MobileMdFormat.ParseMemory(mem);
                else if (
                    ext.Equals(".mdy", StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(path).Equals("mobile.mdy", StringComparison.OrdinalIgnoreCase)
                )
                    mobileMdys[path] = MobileMdyFormat.ParseMemory(mem);
            }
            catch (Exception ex)
            {
                // Record the error but continue — raw bytes are still accessible via Files.
                parseErrors[path] = $"{ex.GetType().Name}: {ex.Message}";
            }

            progress?.Report((i + 1f) / total);
        }

        return new SaveGame
        {
            Info = info,
            Index = index,
            Files = files,
            Mobiles = mobiles,
            Sectors = sectors,
            JumpFiles = jumpFiles,
            MapPropertiesList = mapProperties,
            Scripts = scripts,
            Dialogs = dialogs,
            MobileMds = mobileMds,
            MobileMdys = mobileMdys,
            ParseErrors = parseErrors,
        };
    }
}
