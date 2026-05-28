using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Typed container-pane contract for one object/proto inspector target.
/// </summary>
public sealed class EditorObjectInspectorContainerSummary
{
    public required EditorObjectInspectorSummary Inspector { get; init; }

    public ContainerFlags ContainerFlags { get; init; }

    public int LockDifficulty { get; init; }

    public int KeyId { get; init; }

    public IReadOnlyList<Guid> Inventory { get; init; } = [];

    public bool IsContainerTarget => Inspector.TargetObjectType is ObjectType.Container;

    internal static EditorObjectInspectorContainerSummary Create(
        EditorObjectInspectorSummary inspector,
        IReadOnlyList<ObjectProperty> properties
    )
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(properties);

        if (inspector.TargetObjectType is not ObjectType.Container)
            return new EditorObjectInspectorContainerSummary { Inspector = inspector };

        return new EditorObjectInspectorContainerSummary
        {
            Inspector = inspector,
            ContainerFlags = (ContainerFlags)unchecked((uint)ReadInt32(properties, ObjectField.ContainerFlags)),
            LockDifficulty = ReadInt32(properties, ObjectField.ContainerLockDifficulty),
            KeyId = ReadInt32(properties, ObjectField.ContainerKeyId),
            Inventory = ReadObjectIdArray(properties, ObjectField.ContainerInventoryListIdx),
        };
    }

    private static int ReadInt32(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field == field)
            {
                try
                {
                    return property.GetInt32();
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }
        }

        return 0;
    }

    private static Guid[] ReadObjectIdArray(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field == field)
            {
                try
                {
                    return property.GetObjectIdArray();
                }
                catch (InvalidOperationException)
                {
                    return [];
                }
            }
        }

        return [];
    }
}
