namespace ArcNET.BinaryPatch;

/// <summary>The binary format that a <see cref="PatchTarget"/> file is stored in.</summary>
public enum PatchTargetFormat
{
    /// <summary>Arcanum object prototype (<c>.pro</c>) file — parsed via <c>ProtoFormat</c>.</summary>
    Proto,

    /// <summary>Arcanum mobile save-state (<c>.mob</c>) file — parsed via <c>MobFormat</c>.</summary>
    Mob,

    /// <summary>
    /// Raw bytes — no structured parsing. The patcher passes file bytes directly to the patch.
    /// Suitable for EXE patches, opaque configs, and formats that have no dedicated parser.
    /// </summary>
    Raw,
}

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
