namespace ArcNET.BinaryPatch;

/// <summary>Identifies the file a patch operates on.</summary>
/// <param name="RelativePath">
/// File path relative to the game root directory, using forward slashes as separators,
/// e.g. <c>data/proto/containers/00000025.pro</c>.
/// </param>
/// <param name="Format">
/// Binary format of the target file. Used by <see cref="BinaryPatcher"/> to select the right
/// structured parser when needed.
/// </param>
public sealed record PatchTarget(string RelativePath, PatchTargetFormat Format)
{
    /// <summary>
    /// When set, indicates that the target file originates from a DAT archive rather than
    /// being an existing loose file. <see cref="BinaryPatcher"/> will extract the entry from
    /// this DAT (relative to the game root) when the loose file does not yet exist.
    /// </summary>
    public string? SourceDatPath { get; init; }

    /// <summary>
    /// The virtual path of the entry inside <see cref="SourceDatPath"/>
    /// (backslash-separated, as stored in the DAT directory).
    /// Required when <see cref="SourceDatPath"/> is set.
    /// </summary>
    public string? DatEntryPath { get; init; }
};
