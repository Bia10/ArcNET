using ArcNET.BinaryPatch.Json;

namespace ArcNET.BinaryPatch;

/// <summary>
/// Discovers and loads <see cref="BinaryPatchSet"/> instances from a <c>patches/</c> directory
/// at runtime, enabling modders to drop new <c>.json</c> files without recompiling.
/// </summary>
/// <remarks>
/// <para>
/// The default patches folder is <c>patches/</c> next to the running executable
/// (<see cref="DefaultPatchesDir"/>). The App project copies its built-in patch files there
/// via <c>CopyToOutputDirectory</c>.
/// </para>
/// <para>Supported file extensions: <c>.json</c> (via <see cref="JsonPatchLoader"/>).</para>
/// </remarks>
public static class PatchDiscovery
{
    /// <summary>
    /// Absolute path to the <c>patches/</c> directory beside the running executable.
    /// </summary>
    public static string DefaultPatchesDir => Path.Combine(AppContext.BaseDirectory, "patches");

    /// <summary>
    /// Loads all patch files found in <paramref name="patchesDir"/> (non-recursive).
    /// Unknown file extensions are silently skipped.
    /// Files that fail to parse are skipped and the error is reported via
    /// <paramref name="onError"/> (if provided).
    /// </summary>
    /// <param name="patchesDir">
    /// Directory to scan. Defaults to <see cref="DefaultPatchesDir"/> when
    /// <see langword="null"/> or empty.
    /// </param>
    /// <param name="onError">
    /// Optional callback invoked with (filePath, exception) when a file fails to load.
    /// </param>
    /// <returns>
    /// All successfully loaded <see cref="BinaryPatchSet"/> instances, ordered by filename.
    /// </returns>
    public static IReadOnlyList<BinaryPatchSet> LoadAll(
        string? patchesDir = null,
        Action<string, Exception>? onError = null
    )
    {
        var dir = string.IsNullOrWhiteSpace(patchesDir) ? DefaultPatchesDir : patchesDir;

        if (!Directory.Exists(dir))
            return [];

        var files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        var results = new List<BinaryPatchSet>(files.Length);
        foreach (var file in files)
        {
            try
            {
                results.Add(JsonPatchLoader.LoadFile(file));
            }
            catch (Exception ex)
            {
                onError?.Invoke(file, ex);
            }
        }

        return results;
    }
}
