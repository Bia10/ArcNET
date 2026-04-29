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
    public static string ResolvePath(string gameDir, string relativePath) =>
        PatchFileAccess.ResolvePath(gameDir, relativePath);

    private static PatchResult ApplyOne(IBinaryPatch patch, string gameDir, PatchOptions options) =>
        PatchApplyOperation.Apply(patch, gameDir, options);

    private static PatchResult RevertOne(IBinaryPatch patch, string gameDir) =>
        PatchRevertOperation.Revert(patch, gameDir);

    private static PatchVerifyResult VerifyOne(IBinaryPatch patch, string gameDir) =>
        PatchVerifyOperation.Verify(patch, gameDir);
}
