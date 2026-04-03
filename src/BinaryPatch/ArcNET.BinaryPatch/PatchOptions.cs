namespace ArcNET.BinaryPatch;

/// <summary>Controls how <see cref="BinaryPatcher"/> applies a <see cref="BinaryPatchSet"/>.</summary>
public sealed record PatchOptions
{
    /// <summary>Default options: create backups, do not dry-run.</summary>
    public static readonly PatchOptions Default = new();

    /// <summary>
    /// When <see langword="true"/> (default), a <c>.bak</c> copy of the original file is written
    /// beside the target file before any bytes are overwritten.
    /// <see cref="BinaryPatcher.Revert"/> uses the backup to restore the original.
    /// </summary>
    public bool CreateBackup { get; init; } = true;

    /// <summary>
    /// When <see langword="true"/>, the patcher checks every patch and reports results but writes
    /// nothing to disk and creates no backups. Useful for pre-flight verification.
    /// </summary>
    public bool DryRun { get; init; }
}
