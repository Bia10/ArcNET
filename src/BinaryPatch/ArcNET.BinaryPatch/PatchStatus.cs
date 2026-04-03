namespace ArcNET.BinaryPatch;

/// <summary>Outcome of applying or reverting a single <see cref="IBinaryPatch"/>.</summary>
public enum PatchStatus : byte
{
    /// <summary>The patch was applied (or reverted) successfully and the file was written.</summary>
    Applied = 0,

    /// <summary>
    /// The file was already in the patched state; <see cref="IBinaryPatch.NeedsApply"/> returned
    /// <see langword="false"/>. Nothing was written.
    /// </summary>
    AlreadyApplied = 1,

    /// <summary>
    /// The patch was intentionally skipped. Common causes:
    /// <list type="bullet">
    ///   <item><see cref="PatchOptions.DryRun"/> is active.</item>
    ///   <item>No backup file exists for a revert operation.</item>
    ///   <item><see cref="IBinaryPatch.NeedsApply"/> returned <see langword="false"/> for unrecognised data.</item>
    /// </list>
    /// See <see cref="PatchResult.Reason"/> for details.
    /// </summary>
    Skipped = 2,

    /// <summary>
    /// An I/O error, parse error, or exception prevented the patch from completing.
    /// See <see cref="PatchResult.Reason"/> for the error message.
    /// </summary>
    Failed = 3,
}
