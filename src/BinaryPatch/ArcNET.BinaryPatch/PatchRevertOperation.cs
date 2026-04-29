namespace ArcNET.BinaryPatch;

internal static class PatchRevertOperation
{
    public static PatchResult Revert(IBinaryPatch patch, string gameDir)
    {
        var path = PatchFileAccess.ResolvePath(gameDir, patch.Target.RelativePath);
        if (patch.Target.SourceDatPath is not null)
            return RevertDatOverride(patch, path);

        var backupPath = PatchFileAccess.GetBackupPath(path);
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

    private static PatchResult RevertDatOverride(IBinaryPatch patch, string path)
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
}
