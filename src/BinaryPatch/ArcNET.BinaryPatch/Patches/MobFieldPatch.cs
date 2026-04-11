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
public sealed class MobFieldPatch : ObjectFieldPatchBase
{
    private MobFieldPatch(
        string id,
        string description,
        PatchTarget target,
        ObjectField field,
        Func<ObjectProperty, bool>? predicate,
        Func<ObjectProperty, ObjectProperty> transform
    )
        : base(id, description, target, field, predicate, transform) { }

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
    /// The current (unpatched) value expected in the field. <see cref="IBinaryPatch.NeedsApply"/> returns
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
        Span<byte> tmp = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(tmp, newValue);
        var replacement = tmp.ToArray();

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

    // ── ObjectFieldPatchBase ───────────────────────────────────────────────

    /// <inheritdoc/>
    protected override IReadOnlyList<ObjectProperty> ParseProperties(ReadOnlyMemory<byte> data) =>
        MobFormat.ParseMemory(data).Properties;

    /// <inheritdoc/>
    protected override byte[] ParseTransformSerialize(
        ReadOnlyMemory<byte> original,
        Func<IReadOnlyList<ObjectProperty>, IReadOnlyList<ObjectProperty>> transform
    )
    {
        var mob = MobFormat.ParseMemory(original);
        var patched = new MobData { Header = mob.Header, Properties = transform(mob.Properties) };
        return MobFormat.WriteToArray(in patched);
    }
}
