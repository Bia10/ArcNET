namespace ArcNET.BinaryPatch;

internal static class PatchVerifyOperation
{
    public static PatchVerifyResult Verify(IBinaryPatch patch, string gameDir)
    {
        var path = PatchFileAccess.ResolvePath(gameDir, patch.Target.RelativePath);
        var (original, readError) = PatchFileAccess.TryReadOriginalBytes(patch, path, gameDir);
        if (readError is not null)
            return new PatchVerifyResult(patch.Id, false, File.Exists(path), readError);

        try
        {
            var needsApply = patch.NeedsApply(original!);
            return new PatchVerifyResult(patch.Id, needsApply, true, null);
        }
        catch (Exception ex)
        {
            return new PatchVerifyResult(patch.Id, false, true, $"NeedsApply threw: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
