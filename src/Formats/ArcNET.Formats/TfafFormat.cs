namespace ArcNET.Formats;

/// <summary>
/// Helper for extracting individual file payloads from a TFAF save-game data blob,
/// using a parsed <see cref="SaveIndex"/> as the directory.
/// <para>
/// The TFAF blob is a raw concatenation of file payloads in the same depth-first traversal
/// order as the file entries in the companion TFAI index. Each entry's size is stored in
/// <see cref="TfaiFileEntry.Size"/>.
/// </para>
/// </summary>
public static class TfafFormat
{
    /// <summary>
    /// Extracts all file payloads from a TFAF data blob into a flat dictionary keyed by
    /// the virtual path within the save-game archive (e.g. <c>"maps/map01/mobile/G_….mob"</c>).
    /// </summary>
    /// <param name="index">The parsed TFAI index describing names, nesting, and sizes.</param>
    /// <param name="tfafData">The raw TFAF data blob (entire file contents).</param>
    /// <returns>
    /// A read-only dictionary mapping archive-relative virtual paths to their raw byte payloads.
    /// </returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when the TFAF data is shorter than the total size declared by the index.
    /// </exception>
    public static IReadOnlyDictionary<string, byte[]> ExtractAll(SaveIndex index, ReadOnlyMemory<byte> tfafData)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var offset = 0;
        TfaiTreeTraversal.Traverse(
            index.Root,
            string.Empty,
            onFile: (path, file) =>
            {
                var end = offset + file.Size;
                if (end > tfafData.Length)
                    throw new InvalidDataException(
                        $"TFAF blob is too short: entry '{file.Name}' at offset {offset} "
                            + $"requires {file.Size} bytes but only {tfafData.Length - offset} remain."
                    );

                result[path] = tfafData.Slice(offset, file.Size).ToArray();
                offset = end;
            }
        );
        return result;
    }

    /// <summary>
    /// Extracts the payload of a single named entry from the TFAF blob.
    /// The <paramref name="virtualPath"/> must use forward slashes and must match
    /// the case used in the TFAI index.
    /// </summary>
    /// <param name="index">The parsed TFAI index.</param>
    /// <param name="tfafData">The raw TFAF data blob.</param>
    /// <param name="virtualPath">
    /// Archive-relative path to the desired file, e.g. <c>"maps/map01/map.jmp"</c>.
    /// </param>
    /// <returns>The raw bytes of the requested file.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when <paramref name="virtualPath"/> is not found in the index.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Thrown when the TFAF blob is shorter than required to read the entry.
    /// </exception>
    public static byte[] Extract(SaveIndex index, ReadOnlyMemory<byte> tfafData, string virtualPath)
    {
        var all = ExtractAll(index, tfafData);
        if (!all.TryGetValue(virtualPath, out var payload))
            throw new KeyNotFoundException($"Entry '{virtualPath}' was not found in the TFAI index.");
        return payload;
    }

    /// <summary>
    /// Computes the total byte size of all file payloads declared in the index.
    /// Use this to validate that a TFAF blob is large enough before extraction.
    /// </summary>
    public static int TotalPayloadSize(SaveIndex index)
    {
        var total = 0;
        TfaiTreeTraversal.Traverse(index.Root, string.Empty, onFile: (_, file) => total += file.Size);
        return total;
    }

    /// <summary>
    /// Packs a set of file payloads into a TFAF data blob whose layout matches the
    /// provided index. Payloads are written in depth-first traversal order (same order
    /// as <see cref="ExtractAll"/>). The companion <paramref name="index"/> must be
    /// written separately with <see cref="SaveIndexFormat.WriteToArray"/>.
    /// </summary>
    /// <param name="index">The TFAI index describing names, nesting, and expected sizes.</param>
    /// <param name="payloads">
    /// Dictionary from archive-relative virtual paths (forward-slash separated) to their
    /// raw byte payloads. Must contain an entry for every file in the index.
    /// </param>
    /// <returns>Raw TFAF bytes ready to write to a <c>.tfaf</c> file.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when a file entry in the index has no matching entry in <paramref name="payloads"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a payload's length does not match the size recorded in the index.
    /// </exception>
    public static byte[] Pack(SaveIndex index, IReadOnlyDictionary<string, byte[]> payloads)
    {
        var ordered = new List<byte[]>();
        TfaiTreeTraversal.Traverse(
            index.Root,
            string.Empty,
            onFile: (path, file) =>
            {
                if (!payloads.TryGetValue(path, out var payload))
                    throw new KeyNotFoundException($"No payload provided for '{path}'.");
                if (payload.Length != file.Size)
                    throw new ArgumentException(
                        $"Payload for '{path}' is {payload.Length} bytes but index declares {file.Size}."
                    );

                ordered.Add(payload);
            }
        );
        var totalSize = 0;
        foreach (var p in ordered)
            totalSize += p.Length;
        var result = new byte[totalSize];
        var offset = 0;
        foreach (var p in ordered)
        {
            p.CopyTo(result, offset);
            offset += p.Length;
        }
        return result;
    }
}
