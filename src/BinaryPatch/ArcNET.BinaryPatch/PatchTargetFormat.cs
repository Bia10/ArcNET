namespace ArcNET.BinaryPatch;

/// <summary>The binary format that a <see cref="PatchTarget"/> file is stored in.</summary>
public enum PatchTargetFormat : byte
{
    /// <summary>Arcanum object prototype (<c>.pro</c>) file — parsed via <c>ProtoFormat</c>.</summary>
    Proto = 0,

    /// <summary>Arcanum mobile save-state (<c>.mob</c>) file — parsed via <c>MobFormat</c>.</summary>
    Mob = 1,

    /// <summary>
    /// Raw bytes — no structured parsing. The patcher passes file bytes directly to the patch.
    /// Suitable for EXE patches, opaque configs, and formats that have no dedicated parser.
    /// </summary>
    Raw = 2,
}
