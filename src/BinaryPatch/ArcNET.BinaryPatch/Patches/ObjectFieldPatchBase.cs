using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.BinaryPatch.Patches;

/// <summary>
/// Shared implementation for structured-format field patches
/// (<see cref="ProtoFieldPatch"/> and <see cref="MobFieldPatch"/>).
/// Handles patch identity, property lookup, and transform plumbing.
/// Subclasses provide the format-specific parse and serialize operations.
/// </summary>
public abstract class ObjectFieldPatchBase : IBinaryPatch
{
    private readonly ObjectField _field;
    private readonly Func<ObjectProperty, bool>? _predicate;
    private readonly Func<ObjectProperty, ObjectProperty> _transform;

    protected ObjectFieldPatchBase(
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

    /// <inheritdoc/>
    public bool NeedsApply(ReadOnlyMemory<byte> original)
    {
        var prop = ParseProperties(original).FirstOrDefault(p => p.Field == _field);
        return prop is not null && (_predicate is null || _predicate(prop));
    }

    /// <inheritdoc/>
    public byte[] Apply(ReadOnlyMemory<byte> original) =>
        ParseTransformSerialize(
            original,
            props =>
            {
                var updated = new ObjectProperty[props.Count];
                for (var i = 0; i < props.Count; i++)
                    updated[i] = props[i].Field == _field ? _transform(props[i]) : props[i];
                return updated;
            }
        );

    /// <summary>Parses <paramref name="data"/> and returns its property collection.</summary>
    protected abstract IReadOnlyList<ObjectProperty> ParseProperties(ReadOnlyMemory<byte> data);

    /// <summary>
    /// Parses <paramref name="original"/>, applies <paramref name="transform"/> to its property
    /// collection, and returns the re-serialized bytes.
    /// </summary>
    protected abstract byte[] ParseTransformSerialize(
        ReadOnlyMemory<byte> original,
        Func<IReadOnlyList<ObjectProperty>, IReadOnlyList<ObjectProperty>> transform
    );
}
