using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Typed blending-pane contract for one object/proto inspector target.
/// </summary>
public sealed class EditorObjectInspectorBlendingSummary
{
    public required EditorObjectInspectorSummary Inspector { get; init; }

    public ObjFBlitFlags BlitFlags { get; init; }

    public Color BlitColor { get; init; }

    public int BlitAlpha { get; init; }

    public int BlitScale { get; init; }

    public int Material { get; init; }

    internal static EditorObjectInspectorBlendingSummary Create(
        EditorObjectInspectorSummary inspector,
        IReadOnlyList<ObjectProperty> properties
    )
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(properties);

        return new EditorObjectInspectorBlendingSummary
        {
            Inspector = inspector,
            BlitFlags = (ObjFBlitFlags)unchecked((uint)ReadInt32(properties, ObjectField.ObjFBlitFlags)),
            BlitColor = ReadColor(properties, ObjectField.ObjFBlitColor),
            BlitAlpha = ReadInt32(properties, ObjectField.ObjFBlitAlpha),
            BlitScale = ReadInt32(properties, ObjectField.ObjFBlitScale),
            Material = ReadInt32(properties, ObjectField.ObjFMaterial),
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

    private static Color ReadColor(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field != field)
                continue;

            var bytes = property.RawBytes;
            return bytes.Length >= 3 ? new Color(bytes[0], bytes[1], bytes[2]) : default;
        }

        return default;
    }
}
