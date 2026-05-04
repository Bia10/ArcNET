using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Typed generator-pane contract for one object/proto inspector target.
/// </summary>
public sealed class EditorObjectInspectorGeneratorSummary
{
    public required EditorObjectInspectorSummary Inspector { get; init; }

    public int GeneratorData { get; init; }

    public bool IsNpcTarget => Inspector.TargetObjectType is ObjectType.Npc;

    internal static EditorObjectInspectorGeneratorSummary Create(
        EditorObjectInspectorSummary inspector,
        IReadOnlyList<ObjectProperty> properties
    )
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(properties);

        return new EditorObjectInspectorGeneratorSummary
        {
            Inspector = inspector,
            GeneratorData = ReadInt32(properties, ObjectField.ObjFNpcGeneratorData),
        };
    }

    private static int ReadInt32(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field == field)
                return property.GetInt32();
        }

        return 0;
    }
}
