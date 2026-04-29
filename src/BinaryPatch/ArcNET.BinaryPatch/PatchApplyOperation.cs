namespace ArcNET.BinaryPatch;

internal static class PatchApplyOperation
{
    public static PatchResult Apply(IBinaryPatch patch, string gameDir, PatchOptions options)
    {
        var path = PatchFileAccess.ResolvePath(gameDir, patch.Target.RelativePath);
        var isDatSourced = patch.Target.SourceDatPath is not null;

        var (original, readError) = PatchFileAccess.TryReadOriginalBytes(patch, path, gameDir);
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

        string? backupPath = null;
        if (options.CreateBackup && !isDatSourced)
        {
            backupPath = PatchFileAccess.GetBackupPath(path);
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

        var writeError = PatchFileAccess.WritePatchedBytes(path, patched, ensureDirectory: isDatSourced);
        if (writeError is not null)
            return new PatchResult(patch.Id, PatchStatus.Failed, backupPath, writeError);

        return new PatchResult(patch.Id, PatchStatus.Applied, backupPath, null);
    }
}
