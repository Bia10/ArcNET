namespace ArcNET.BinaryPatch;

/// <summary>
/// A named, versioned collection of related <see cref="IBinaryPatch"/> entries.
/// </summary>
/// <remarks>
/// Build a <see cref="BinaryPatchSet"/> once as a static field or via a factory method, then pass
/// it to <see cref="BinaryPatcher.Apply"/>, <see cref="BinaryPatcher.Revert"/>, or
/// <see cref="BinaryPatcher.Verify"/>.
/// </remarks>
public sealed class BinaryPatchSet
{
    /// <summary>
    /// Human-readable name identifying this patch set,
    /// e.g. <c>ArcNET Game Data Fixes — Vanilla Bug Corrections</c>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>SemVer string identifying this patch set release, e.g. <c>1.0.0</c>.</summary>
    public required string Version { get; init; }

    /// <summary>
    /// Ordered list of patches. <see cref="BinaryPatcher"/> applies them in declaration order;
    /// patches that target different files are independent of each other.
    /// </summary>
    public required IReadOnlyList<IBinaryPatch> Patches { get; init; }
}
