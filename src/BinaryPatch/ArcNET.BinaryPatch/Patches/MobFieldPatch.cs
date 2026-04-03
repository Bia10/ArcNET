using System.Buffers.Binary;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.BinaryPatch.Patches;

/// <summary>
/// Mutates a single typed property inside an Arcanum mobile save-state (<c>.mob</c>) file.
/// </summary>
/// <remarks>
/// <para>
/// Structurally identical to <see cref="ProtoFieldPatch"/> but targets <c>.mob</c> files, which
/// store live object instances (as opposed to prototypes). Mobile objects carry the same
/// <see cref="ObjectField"/> property system but include an additional <c>PropCollectionItems</c>
/// field in the header and only store fields whose bitmap bit is set.
/// </para>
/// <para>
/// Use the static factories (<see cref="SetInt32"/>, <see cref="Custom"/>) to create instances.
/// </para>
/// </remarks>
public sealed class MobFieldPatch : IBinaryPatch
{
    private readonly ObjectField _field;
    private readonly Func<ObjectProperty, bool>? _predicate;
    private readonly Func<ObjectProperty, ObjectProperty> _transform;

    private MobFieldPatch(
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
    /// Creates a <see cref="MobFieldPatch"/> that replaces a single <see cref="int"/> property.
    /// </summary>
    /// <param name="id">Patch identifier.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="relativePath">
    /// Path of the <c>.mob</c> file relative to the game directory, using forward slashes.
    /// </param>
    /// <param name="field">The <see cref="ObjectField"/> to modify.</param>
    /// <param name="expectedValue">
    /// The current (unpatched) value expected in the field. <see cref="NeedsApply"/> returns
    /// <see langword="true"/> only when the field holds this value.
    /// </param>
    /// <param name="newValue">The replacement value to write.</param>
    public static MobFieldPatch SetInt32(
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

        return new MobFieldPatch(
            id,
            description,
            new PatchTarget(relativePath, PatchTargetFormat.Mob),
            field,
            prop => prop.GetInt32() == expectedValue,
            prop => new ObjectProperty { Field = prop.Field, RawBytes = replacement }
        );
    }

    /// <summary>
    /// Creates a <see cref="MobFieldPatch"/> with fully custom predicate and transform delegates.
    /// </summary>
    /// <param name="id">Patch identifier.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="relativePath">
    /// Path of the <c>.mob</c> file relative to the game directory, using forward slashes.
    /// </param>
    /// <param name="field">The <see cref="ObjectField"/> to modify.</param>
    /// <param name="needsApplyPredicate">
    /// Returns <see langword="true"/> when the property should be transformed. Pass
    /// <see langword="null"/> to skip the idempotency check.
    /// </param>
    /// <param name="transform">Receives the current property and returns the replacement.</param>
    public static MobFieldPatch Custom(
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
            new PatchTarget(relativePath, PatchTargetFormat.Mob),
            field,
            needsApplyPredicate,
            transform
        );

    // ── IBinaryPatch ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool NeedsApply(ReadOnlyMemory<byte> original)
    {
        var mob = MobFormat.ParseMemory(original);
        var prop = FindProperty(mob.Properties);

        if (prop is null)
            return false;

        return _predicate is null || _predicate(prop);
    }

    /// <inheritdoc/>
    public byte[] Apply(ReadOnlyMemory<byte> original)
    {
        var mob = MobFormat.ParseMemory(original);

        var props = mob.Properties;
        var updatedProps = new ObjectProperty[props.Count];
        for (var i = 0; i < props.Count; i++)
            updatedProps[i] = props[i].Field == _field ? _transform(props[i]) : props[i];

        var patched = new MobData { Header = mob.Header, Properties = updatedProps };
        return MobFormat.WriteToArray(in patched);
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
