using System.Buffers.Binary;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.BinaryPatch.Patches;

/// <summary>
/// Mutates a single typed property inside an Arcanum object prototype (<c>.pro</c>) file.
/// </summary>
/// <remarks>
/// <para>
/// The target file is fully parsed via <see cref="ProtoFormat"/>, the nominated property is
/// replaced via the caller-supplied <c>transform</c> delegate, and the result is re-serialised.
/// This means the patch is resilient to layout shifts in fields that precede the target field —
/// a guarantee that raw-offset patches cannot provide.
/// </para>
/// <para>
/// Use the static factories (<see cref="SetInt32"/>, <see cref="Custom"/>) to create instances.
/// </para>
/// </remarks>
public sealed class ProtoFieldPatch : IBinaryPatch
{
    private readonly ObjectField _field;
    private readonly Func<ObjectProperty, bool>? _predicate;
    private readonly Func<ObjectProperty, ObjectProperty> _transform;

    private ProtoFieldPatch(
        string id,
        string description,
        PatchTarget target,
        ObjectField field,
        Func<ObjectProperty, bool>? predicate,
        Func<ObjectProperty, ObjectProperty> transform
    )
    {
        Id = id;
        Description = description;
        Target = target;
        _field = field;
        _predicate = predicate;
        _transform = transform;
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Description { get; }

    /// <inheritdoc/>
    public PatchTarget Target { get; }

    /// <inheritdoc/>
    public string PatchSummary => $"field {_field}";

    // ── Factories ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="ProtoFieldPatch"/> that replaces a single <see cref="int"/> property.
    /// </summary>
    /// <param name="id">Patch identifier.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="relativePath">
    /// Path of the <c>.pro</c> file relative to the game directory, using forward slashes.
    /// </param>
    /// <param name="field">The <see cref="ObjectField"/> to modify.</param>
    /// <param name="expectedValue">
    /// The current (unpatched) value expected in the field.
    /// <see cref="NeedsApply"/> returns <see langword="true"/> only when the field holds this
    /// value, providing idempotency and version-checking in one step.
    /// </param>
    /// <param name="newValue">The replacement value to write.</param>
    public static ProtoFieldPatch SetInt32(
        string id,
        string description,
        string relativePath,
        ObjectField field,
        int expectedValue,
        int newValue
    )
    {
        var replacement = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(replacement, newValue);

        return new ProtoFieldPatch(
            id,
            description,
            new PatchTarget(relativePath, PatchTargetFormat.Proto),
            field,
            prop => prop.GetInt32() == expectedValue,
            prop => new ObjectProperty { Field = prop.Field, RawBytes = replacement }
        );
    }

    /// <summary>
    /// Creates a <see cref="ProtoFieldPatch"/> with fully custom predicate and transform
    /// delegates, for changes that cannot be expressed as a direct scalar replacement.
    /// </summary>
    /// <param name="id">Patch identifier.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="relativePath">
    /// Path of the <c>.pro</c> file relative to the game directory, using forward slashes.
    /// </param>
    /// <param name="field">The <see cref="ObjectField"/> to modify.</param>
    /// <param name="needsApplyPredicate">
    /// Returns <see langword="true"/> when the property should be transformed. Pass
    /// <see langword="null"/> to skip the idempotency check (patch always applies when the field
    /// is present).
    /// </param>
    /// <param name="transform">
    /// Receives the current <see cref="ObjectProperty"/> and returns the replacement.
    /// </param>
    public static ProtoFieldPatch Custom(
        string id,
        string description,
        string relativePath,
        ObjectField field,
        Func<ObjectProperty, bool>? needsApplyPredicate,
        Func<ObjectProperty, ObjectProperty> transform
    ) =>
        new(
            id,
            description,
            new PatchTarget(relativePath, PatchTargetFormat.Proto),
            field,
            needsApplyPredicate,
            transform
        );

    // ── IBinaryPatch ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Returns <see langword="false"/> when the target field is absent from the proto
    /// (bit not set in the header bitmap) or when the predicate reports the value is
    /// already in the patched state.
    /// </remarks>
    public bool NeedsApply(ReadOnlyMemory<byte> original)
    {
        var proto = ProtoFormat.ParseMemory(original);
        var prop = FindProperty(proto.Properties);

        if (prop is null)
            return false;

        return _predicate is null || _predicate(prop);
    }

    /// <inheritdoc/>
    public byte[] Apply(ReadOnlyMemory<byte> original)
    {
        var proto = ProtoFormat.ParseMemory(original);

        var props = proto.Properties;
        var updatedProps = new ObjectProperty[props.Count];
        for (var i = 0; i < props.Count; i++)
            updatedProps[i] = props[i].Field == _field ? _transform(props[i]) : props[i];

        var patched = new ProtoData { Header = proto.Header, Properties = updatedProps };
        return ProtoFormat.WriteToArray(in patched);
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private ObjectProperty? FindProperty(IReadOnlyList<ObjectProperty> properties)
    {
        foreach (var p in properties)
            if (p.Field == _field)
                return p;
        return null;
    }
}
