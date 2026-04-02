namespace ArcNET.Archive;

/// <summary>Extracts entries from an open <see cref="DatArchive"/> to the filesystem.</summary>
public static class DatExtractor
{
    /// <summary>
    /// Extracts all entries from <paramref name="archive"/> into <paramref name="outputDir"/>.
    /// Directory structure embedded in entry paths is recreated automatically.
    /// </summary>
    /// <param name="archive">The open archive to extract from.</param>
    /// <param name="outputDir">Root directory to write extracted files to.</param>
    /// <param name="progress">Optional progress callback (value in [0, 1]).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ExtractAllAsync(
        DatArchive archive,
        string outputDir,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        var entries = archive.Entries;
        var total = entries.Count;
        var i = 0;
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExtractEntryAsync(archive, entry.Path, outputDir, cancellationToken).ConfigureAwait(false);
            progress?.Report((float)(++i) / total);
        }
    }

    /// <summary>
    /// Extracts a single entry identified by <paramref name="entryName"/> into <paramref name="outputDir"/>.
    /// </summary>
    /// <param name="archive">The open archive to extract from.</param>
    /// <param name="entryName">Virtual path of the entry inside the archive.</param>
    /// <param name="outputDir">Root directory to write the extracted file to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ExtractEntryAsync(
        DatArchive archive,
        string entryName,
        string outputDir,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        var entry = archive.FindEntry(entryName) ?? throw new FileNotFoundException($"Entry not found: {entryName}");

        // Sanitize path separators and strip any leading directory traversal
        var relativePath = entry.Path.Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var destPath = Path.GetFullPath(Path.Combine(outputDir, relativePath));

        // Guard against path traversal
        var root = Path.GetFullPath(outputDir);
        if (!destPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Path traversal detected for entry: {entryName}");

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        var data = archive.ReadEntry(entry);
        await File.WriteAllBytesAsync(destPath, data, cancellationToken).ConfigureAwait(false);
    }
}
