namespace ArcNET.BinaryPatch;

/// <summary>Outcome of applying or reverting a single <see cref="IBinaryPatch"/>.</summary>
public enum PatchStatus
{
    /// <summary>The patch was applied (or reverted) successfully and the file was written.</summary>
    Applied,

    /// <summary>
    /// The file was already in the patched state; <see cref="IBinaryPatch.NeedsApply"/> returned
    /// <see langword="false"/>. Nothing was written.
    /// </summary>
    AlreadyApplied,

    /// <summary>
    /// The patch was intentionally skipped. Common causes:
    /// <list type="bullet">
    ///   <item><see cref="PatchOptions.DryRun"/> is active.</item>
    ///   <item>No backup file exists for a revert operation.</item>
    ///   <item><see cref="IBinaryPatch.NeedsApply"/> returned <see langword="false"/> for unrecognised data.</item>
    /// </list>
    /// See <see cref="PatchResult.Reason"/> for details.
    /// </summary>
    Skipped,

    /// <summary>
    /// An I/O error, parse error, or exception prevented the patch from completing.
    /// See <see cref="PatchResult.Reason"/> for the error message.
    /// </summary>
    Failed,
}

/// <summary>Result of applying or reverting a single <see cref="IBinaryPatch"/>.</summary>
/// <param name="PatchId"><see cref="IBinaryPatch.Id"/> of the patch.</param>
/// <param name="Status">Outcome of the operation.</param>
/// <param name="BackupPath">
/// Absolute path to the <c>.bak</c> file created during apply, or <see langword="null"/> when
/// no backup was requested or the patch was not applied.
/// </param>
/// <param name="Reason">
/// Human-readable explanation for <see cref="PatchStatus.Skipped"/> and
/// <see cref="PatchStatus.Failed"/> outcomes, or <see langword="null"/> for success.
/// </param>
public sealed record PatchResult(string PatchId, PatchStatus Status, string? BackupPath, string? Reason);

/// <summary>Result of verifying a single <see cref="IBinaryPatch"/> without modifying any file.</summary>
/// <param name="PatchId"><see cref="IBinaryPatch.Id"/> of the patch.</param>
/// <param name="NeedsApply">
/// <see langword="true"/> when the target file exists and the patch has not yet been applied.
/// </param>
/// <param name="FileExists">
/// <see langword="false"/> when the target file could not be found in the game directory.
/// </param>
/// <param name="Reason">
/// Diagnostic detail when <see cref="NeedsApply"/> is <see langword="false"/> due to an error,
/// or <see langword="null"/> on success.
/// </param>
public sealed record PatchVerifyResult(string PatchId, bool NeedsApply, bool FileExists, string? Reason);
