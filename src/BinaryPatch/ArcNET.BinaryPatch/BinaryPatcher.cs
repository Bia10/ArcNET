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

    // ── private helpers ────────────────────────────────────────────────────

    private static string ResolvePath(string gameDir, string relativePath) =>
        Path.GetFullPath(Path.Combine(gameDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static PatchResult ApplyOne(IBinaryPatch patch, string gameDir, PatchOptions options)
    {
        var path = ResolvePath(gameDir, patch.Target.RelativePath);
        var isDatSourced = patch.Target.SourceDatPath is not null;

        byte[] original;
        if (File.Exists(path))
        {
            try
            {
                original = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                return new PatchResult(patch.Id, PatchStatus.Failed, null, $"Read failed: {ex.Message}");
            }
        }
        else if (isDatSourced)
        {
            var result = ReadFromDat(patch, gameDir);
            if (result.Error is not null)
                return new PatchResult(patch.Id, PatchStatus.Failed, null, result.Error);
            original = result.Data!;
        }
        else
        {
            return new PatchResult(patch.Id, PatchStatus.Failed, null, $"File not found: {path}");
        }

        bool needs;
        try
        {
            needs = patch.NeedsApply(original);
        }
        catch (Exception ex)
        {
            return new PatchResult(patch.Id, PatchStatus.Failed, null, $"NeedsApply threw: {ex.Message}");
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
                return new PatchResult(patch.Id, PatchStatus.Failed, null, $"Backup failed: {ex.Message}");
            }
        }

        byte[] patched;
        try
        {
            patched = patch.Apply(original);
        }
        catch (Exception ex)
        {
            return new PatchResult(patch.Id, PatchStatus.Failed, backupPath, $"Apply threw: {ex.Message}");
        }

        try
        {
            if (isDatSourced)
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, patched);
        }
        catch (Exception ex)
        {
            return new PatchResult(patch.Id, PatchStatus.Failed, backupPath, $"Write failed: {ex.Message}");
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
                return new PatchResult(patch.Id, PatchStatus.Failed, null, ex.Message);
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
            return new PatchResult(patch.Id, PatchStatus.Failed, backupPath, ex.Message);
        }
    }

    private static PatchVerifyResult VerifyOne(IBinaryPatch patch, string gameDir)
    {
        var path = ResolvePath(gameDir, patch.Target.RelativePath);

        byte[] original;
        if (File.Exists(path))
        {
            try
            {
                original = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                return new PatchVerifyResult(patch.Id, false, true, $"Read failed: {ex.Message}");
            }
        }
        else if (patch.Target.SourceDatPath is not null)
        {
            var result = ReadFromDat(patch, gameDir);
            if (result.Error is not null)
                return new PatchVerifyResult(patch.Id, false, false, result.Error);
            original = result.Data!;
        }
        else
        {
            return new PatchVerifyResult(patch.Id, false, false, $"File not found: {path}");
        }

        try
        {
            var needs = patch.NeedsApply(original);
            return new PatchVerifyResult(patch.Id, needs, true, null);
        }
        catch (Exception ex)
        {
            return new PatchVerifyResult(patch.Id, false, true, $"NeedsApply threw: {ex.Message}");
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
            return (null, $"DAT read failed: {ex.Message}");
        }
    }
}
