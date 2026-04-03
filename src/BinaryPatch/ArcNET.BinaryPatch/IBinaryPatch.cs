namespace ArcNET.BinaryPatch;

/// <summary>
/// Represents a single binary change applied to one game data file.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are typically created via static factory methods on concrete types such as
/// <c>ProtoFieldPatch.SetInt32</c> or <c>RawBinaryPatch.AtOffset</c>, then grouped into a
/// <see cref="BinaryPatchSet"/> and executed through <see cref="BinaryPatcher"/>.
/// </para>
/// <para>
/// The interface works at the byte level: <see cref="BinaryPatcher"/> handles file I/O and backup
/// creation; implementations only need to inspect and transform bytes.
/// </para>
/// </remarks>
public interface IBinaryPatch
{
    /// <summary>
    /// Unique, URL-safe identifier for this patch, e.g.
    /// <c>issue-2-bangellian-chest-inventory-source</c>.
    /// </summary>
    string Id { get; }

    /// <summary>Human-readable description of what the patch changes and why.</summary>
    string Description { get; }

    /// <summary>Target file, expressed as a path relative to the game root directory.</summary>
    PatchTarget Target { get; }

    /// <summary>
    /// A concise human-readable summary of the specific change this patch makes, suitable for
    /// display in tables and logs. Examples:
    /// <list type="bullet">
    ///   <item><c>offset 0x64: FFFFFFFF → 00000000</c> (raw offset patch)</item>
    ///   <item><c>field ObjFContainerInventorySource: 0 → -1</c> (field patch)</item>
    /// </list>
    /// </summary>
    string PatchSummary { get; }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="original"/> is in the pre-patch state
    /// and <see cref="Apply"/> should be called, <see langword="false"/> when the patch is already
    /// applied or the file content is unrecognised (incompatible build, wrong file).
    /// </summary>
    /// <remarks>
    /// Implementations must be side-effect-free and deterministic. They are called by
    /// <see cref="BinaryPatcher"/> before every <see cref="Apply"/> and also during
    /// <see cref="BinaryPatcher.Verify"/>.
    /// <para>
    /// The parameter is <see cref="ReadOnlyMemory{T}"/> rather than
    /// <see cref="ReadOnlySpan{T}"/> so that structured patch implementations can forward it
    /// directly to format parsers (e.g. <c>ProtoFormat.ParseMemory</c>) without an extra heap
    /// copy. <see cref="BinaryPatcher"/> passes the <c>byte[]</c> returned by
    /// <c>File.ReadAllBytes</c> which converts to <see cref="ReadOnlyMemory{T}"/> implicitly
    /// and zero-allocation.
    /// </para>
    /// </remarks>
    bool NeedsApply(ReadOnlyMemory<byte> original);

    /// <summary>
    /// Applies the patch to <paramref name="original"/> and returns the modified bytes.
    /// </summary>
    /// <remarks>
    /// Only called by <see cref="BinaryPatcher"/> when <see cref="NeedsApply"/> returned
    /// <see langword="true"/>. The returned array is written directly to disk.
    /// </remarks>
    byte[] Apply(ReadOnlyMemory<byte> original);
}
