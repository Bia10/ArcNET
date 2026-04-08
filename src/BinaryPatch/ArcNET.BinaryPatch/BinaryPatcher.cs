using ArcNET.Archive;

namespace ArcNET.BinaryPatch;

/// <summary>
/// Applies, reverts, and verifies <see cref="BinaryPatchSet"/> instances against a game directory.
/// </summary>
/// <remarks>
/// <para>
/// All three operations are synchronous; I/O is local-file-system only. Each patch is processed
/// independently — a single patch failure does not abort the remaining patches in the set.
/// </para>
/// <para>
/// <b>Apply:</b> for each patch, the patcher reads the target file, calls
/// <see cref="IBinaryPatch.NeedsApply"/>, optionally creates a <c>.bak</c> backup, calls
/// <see cref="IBinaryPatch.Apply"/>, and writes the patched bytes back in place.
/// </para>
/// <para>
/// <b>Revert:</b> restores the original bytes from the <c>.bak</c> file created during apply,
/// then removes the backup. Patches for which no backup exists are reported as
/// <see cref="PatchStatus.Skipped"/>.
/// </para>
/// <para>
/// <b>Verify:</b> calls <see cref="IBinaryPatch.NeedsApply"/> for every patch and returns the
/// results without writing anything to disk.
/// </para>
/// </remarks>
public static class BinaryPatcher
{
    private const string BackupExtension = ".bak";

    // ── Apply ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies every patch in <paramref name="patchSet"/> to the files inside
    /// <paramref name="gameDir"/>.
    /// </summary>
    /// <param name="patchSet">The set of patches to apply.</param>
    /// <param name="gameDir">Root directory of the Arcanum installation.</param>
    /// <param name="options">
    /// Tuning options; defaults to <see cref="PatchOptions.Default"/> (backup enabled, no dry-run).
    /// </param>
    /// <returns>One <see cref="PatchResult"/> per patch, in declaration order.</returns>
    public static IReadOnlyList<PatchResult> Apply(
        BinaryPatchSet patchSet,
        string gameDir,
        PatchOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(patchSet);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        options ??= PatchOptions.Default;

        var results = new List<PatchResult>(patchSet.Patches.Count);
        foreach (var patch in patchSet.Patches)
            results.Add(ApplyOne(patch, gameDir, options));

        return results;
    }

    // ── Revert ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Reverts every patch in <paramref name="patchSet"/> by restoring the <c>.bak</c> backup
    /// files created during the previous <see cref="Apply"/> call.
    /// </summary>
    /// <remarks>
    /// If a backup is absent the patch is recorded as <see cref="PatchStatus.Skipped"/>.
    /// The backup file is removed after a successful restore.
    /// </remarks>
    public static IReadOnlyList<PatchResult> Revert(BinaryPatchSet patchSet, string gameDir)
    {
        ArgumentNullException.ThrowIfNull(patchSet);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);

        var results = new List<PatchResult>(patchSet.Patches.Count);
        foreach (var patch in patchSet.Patches)
            results.Add(RevertOne(patch, gameDir));

        return results;
    }

    // ── Verify ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks whether each patch in <paramref name="patchSet"/> needs to be applied, without
    /// modifying any file.
    /// </summary>
    public static IReadOnlyList<PatchVerifyResult> Verify(BinaryPatchSet patchSet, string gameDir)
    {
        ArgumentNullException.ThrowIfNull(patchSet);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);

        var results = new List<PatchVerifyResult>(patchSet.Patches.Count);
        foreach (var patch in patchSet.Patches)
            results.Add(VerifyOne(patch, gameDir));

        return results;
    }

    // ── public helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Resolves an absolute file path from <paramref name="gameDir"/> and a
    /// forward-slash-separated <paramref name="relativePath"/>.
    /// Throws if the resolved path would escape <paramref name="gameDir"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="relativePath"/> contains path-traversal segments (e.g. <c>../</c>)
    /// that would place the target outside <paramref name="gameDir"/>.
    /// </exception>
    public static string ResolvePath(string gameDir, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(gameDir, normalized));
        // Append the separator so that a game dir of "/foo" does not match "/foobar".
        var gameRoot = Path.GetFullPath(gameDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Patch target '{relativePath}' resolves to '{fullPath}' which is outside the game directory '{gameDir}'."
            );
        return fullPath;
    }

    // Returns (Data: non-null, Error: null) on success; (Data: null, Error: non-null) on failure.
    private static (byte[]? Data, string? Error) TryReadOriginalBytes(IBinaryPatch patch, string path, string gameDir)
    {
        if (File.Exists(path))
        {
            try
            {
                return (File.ReadAllBytes(path), null);
            }
            catch (Exception ex)
            {
                return (null, $"Read failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        if (patch.Target.SourceDatPath is not null)
            return ReadFromDat(patch, gameDir);

        return (null, $"File not found: {path}");
    }

    private static PatchResult ApplyOne(IBinaryPatch patch, string gameDir, PatchOptions options)
    {
        var path = ResolvePath(gameDir, patch.Target.RelativePath);
        var isDatSourced = patch.Target.SourceDatPath is not null;

        var (original, readError) = TryReadOriginalBytes(patch, path, gameDir);
        if (readError is not null)
            return new PatchResult(patch.Id, PatchStatus.Failed, null, readError);

        var originalBytes = original!;

        bool needs;
        try
        {
            needs = patch.NeedsApply(originalBytes);
        }
        catch (Exception ex)
        {
            return new PatchResult(
                patch.Id,
                PatchStatus.Failed,
                null,
                $"NeedsApply threw: {ex.GetType().Name}: {ex.Message}"
            );
        }

        if (!needs)
            return new PatchResult(patch.Id, PatchStatus.AlreadyApplied, null, null);

        if (options.DryRun)
            return new PatchResult(patch.Id, PatchStatus.Skipped, null, "Dry run — no file written.");

        // DAT-sourced files write to a loose override path — no backup is needed because
        // the original bytes remain safely in the DAT; revert simply deletes the loose file.
        string? backupPath = null;
        if (options.CreateBackup && !isDatSourced)
        {
            backupPath = path + BackupExtension;
            try
            {
                File.Copy(path, backupPath, overwrite: true);
            }
            catch (Exception ex)
            {
                return new PatchResult(
                    patch.Id,
                    PatchStatus.Failed,
                    null,
                    $"Backup failed: {ex.GetType().Name}: {ex.Message}"
                );
            }
        }

        byte[] patched;
        try
        {
            patched = patch.Apply(originalBytes);
        }
        catch (Exception ex)
        {
            return new PatchResult(
                patch.Id,
                PatchStatus.Failed,
                backupPath,
                $"Apply threw: {ex.GetType().Name}: {ex.Message}"
            );
        }

        // Write to a temp file and atomically move it into place so that a crash or
        // cancellation during the write cannot leave the target in a half-written state.
        var tempPath = path + ".tmp";
        try
        {
            if (isDatSourced)
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(tempPath, patched);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            TryDeleteSilently(tempPath);
            return new PatchResult(
                patch.Id,
                PatchStatus.Failed,
                backupPath,
                $"Write failed: {ex.GetType().Name}: {ex.Message}"
            );
        }

        return new PatchResult(patch.Id, PatchStatus.Applied, backupPath, null);
    }

    private static PatchResult RevertOne(IBinaryPatch patch, string gameDir)
    {
        var path = ResolvePath(gameDir, patch.Target.RelativePath);

        // DAT-sourced files revert by deleting the loose override so the game falls back to the DAT.
        if (patch.Target.SourceDatPath is not null)
        {
            if (!File.Exists(path))
                return new PatchResult(patch.Id, PatchStatus.Skipped, null, "No loose override to delete.");

            try
            {
                File.Delete(path);
                return new PatchResult(patch.Id, PatchStatus.Applied, null, null);
            }
            catch (Exception ex)
            {
                return new PatchResult(patch.Id, PatchStatus.Failed, null, $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        var backupPath = path + BackupExtension;
        if (!File.Exists(backupPath))
            return new PatchResult(patch.Id, PatchStatus.Skipped, null, "No backup found.");

        try
        {
            File.Copy(backupPath, path, overwrite: true);
            File.Delete(backupPath);
            return new PatchResult(patch.Id, PatchStatus.Applied, null, null);
        }
        catch (Exception ex)
        {
            return new PatchResult(patch.Id, PatchStatus.Failed, backupPath, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static PatchVerifyResult VerifyOne(IBinaryPatch patch, string gameDir)
    {
        var path = ResolvePath(gameDir, patch.Target.RelativePath);

        var (original, readError) = TryReadOriginalBytes(patch, path, gameDir);
        if (readError is not null)
        {
            var fileExists = File.Exists(path);
            return new PatchVerifyResult(patch.Id, false, fileExists, readError);
        }

        try
        {
            var needs = patch.NeedsApply(original!);
            return new PatchVerifyResult(patch.Id, needs, true, null);
        }
        catch (Exception ex)
        {
            return new PatchVerifyResult(patch.Id, false, true, $"NeedsApply threw: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void TryDeleteSilently(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        { /* best effort */
        }
    }

    private static (byte[]? Data, string? Error) ReadFromDat(IBinaryPatch patch, string gameDir)
    {
        var datPath = ResolvePath(gameDir, patch.Target.SourceDatPath!);
        if (!File.Exists(datPath))
            return (null, $"DAT archive not found: {datPath}");

        try
        {
            using var dat = DatArchive.Open(datPath);
            var entry = dat.FindEntry(patch.Target.DatEntryPath!);
            if (entry is null)
                return (null, $"Entry '{patch.Target.DatEntryPath}' not found in '{Path.GetFileName(datPath)}'.");
            return (dat.ReadEntry(entry), null);
        }
        catch (Exception ex)
        {
            return (null, $"DAT read failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
