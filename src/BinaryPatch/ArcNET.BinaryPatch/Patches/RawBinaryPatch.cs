namespace ArcNET.BinaryPatch.Patches;

/// <summary>
/// Replaces a fixed-length byte sequence at a known file offset.
/// </summary>
/// <remarks>
/// <para>
/// This is the lowest-level patch type — use it for formats that have no structured parser
/// (e.g. executable patches, opaque binary configs, unknown format escape hatches).
/// </para>
/// <para>
/// Both the expected and replacement byte arrays must have the same length; the patch is
/// always in-place and does not grow or shrink the file.
/// </para>
/// <para>
/// <see cref="NeedsApply"/> returns <see langword="true"/> only when the bytes at the target
/// offset exactly match the expected bytes, providing idempotency and a basic sanity-check that
/// the file is the expected version.
/// </para>
/// </remarks>
public sealed class RawBinaryPatch : IBinaryPatch
{
    private readonly int _offset;
    private readonly byte[] _expectedBytes;
    private readonly byte[] _newBytes;

    private RawBinaryPatch(
        string id,
        string description,
        PatchTarget target,
        int offset,
        byte[] expectedBytes,
        byte[] newBytes
    )
    {
        if (expectedBytes.Length != newBytes.Length)
            throw new ArgumentException(
                $"expectedBytes.Length ({expectedBytes.Length}) must equal newBytes.Length ({newBytes.Length})."
            );

        Id = id;
        Description = description;
        Target = target;
        _offset = offset;
        _expectedBytes = expectedBytes;
        _newBytes = newBytes;
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Description { get; }

    /// <inheritdoc/>
    public PatchTarget Target { get; }

    /// <inheritdoc/>
    public string PatchSummary =>
        $"offset 0x{_offset:X}: {Convert.ToHexString(_expectedBytes)} → {Convert.ToHexString(_newBytes)}";

    /// <summary>
    /// Creates a <see cref="RawBinaryPatch"/> that replaces <paramref name="expectedBytes"/> at
    /// <paramref name="offset"/> with <paramref name="newBytes"/>.
    /// </summary>
    /// <param name="id">Patch identifier.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="relativePath">
    /// File path relative to the game directory, using forward slashes.
    /// </param>
    /// <param name="offset">Zero-based byte offset inside the target file.</param>
    /// <param name="expectedBytes">
    /// Bytes expected at <paramref name="offset"/> in the unpatched file.
    /// <see cref="NeedsApply"/> returns <see langword="true"/> only when these bytes are present,
    /// which prevents the patch from being applied to an already-patched or unsupported file.
    /// </param>
    /// <param name="newBytes">
    /// Replacement bytes. Must have the same length as <paramref name="expectedBytes"/>.
    /// </param>
    public static RawBinaryPatch AtOffset(
        string id,
        string description,
        string relativePath,
        int offset,
        byte[] expectedBytes,
        byte[] newBytes
    ) => new(id, description, new PatchTarget(relativePath, PatchTargetFormat.Raw), offset, expectedBytes, newBytes);

    /// <summary>
    /// Creates a <see cref="RawBinaryPatch"/> using a fully-specified <see cref="PatchTarget"/>.
    /// Use this overload when the target file must be extracted from a DAT archive by setting
    /// <see cref="PatchTarget.SourceDatPath"/> and <see cref="PatchTarget.DatEntryPath"/>.
    /// </summary>
    internal static RawBinaryPatch AtOffset(
        string id,
        string description,
        PatchTarget target,
        int offset,
        byte[] expectedBytes,
        byte[] newBytes
    ) => new(id, description, target, offset, expectedBytes, newBytes);

    /// <inheritdoc/>
    /// <remarks>
    /// Returns <see langword="false"/> if <paramref name="original"/> is too short to contain
    /// the expected bytes at <see cref="_offset"/>, or if the bytes differ.
    /// </remarks>
    public bool NeedsApply(ReadOnlyMemory<byte> original)
    {
        var span = original.Span;
        if (_offset + _expectedBytes.Length > span.Length)
            return false;

        return span.Slice(_offset, _expectedBytes.Length).SequenceEqual(_expectedBytes);
    }

    /// <inheritdoc/>
    public byte[] Apply(ReadOnlyMemory<byte> original)
    {
        var result = original.ToArray();
        _newBytes.CopyTo(result, _offset);
        return result;
    }
}
