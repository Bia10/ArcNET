using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Typed light-pane contract for one object/proto inspector target.
/// </summary>
public sealed class EditorObjectInspectorLightSummary
{
    public required EditorObjectInspectorSummary Inspector { get; init; }

    public int LightFlags { get; init; }

    public ArtId LightArtId { get; init; }

    public Color LightColor { get; init; }

    public int OverlayLightFlags { get; init; }

    public required IReadOnlyList<int> OverlayLightArtIds { get; init; }

    public int OverlayLightColor { get; init; }

    public bool HasOverlayLights => OverlayLightFlags != 0 || OverlayLightArtIds.Count > 0 || OverlayLightColor != 0;

    internal static EditorObjectInspectorLightSummary Create(
        EditorObjectInspectorSummary inspector,
        IReadOnlyList<ObjectProperty> properties
    )
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(properties);

        return new EditorObjectInspectorLightSummary
        {
            Inspector = inspector,
            LightFlags = ReadInt32(properties, ObjectField.ObjFLightFlags),
            LightArtId = new ArtId(unchecked((uint)ReadInt32(properties, ObjectField.ObjFLightAid))),
            LightColor = ReadColor(properties, ObjectField.ObjFLightColor),
            OverlayLightFlags = ReadInt32(properties, ObjectField.ObjFOverlayLightFlags),
            OverlayLightArtIds = ReadInt32Array(properties, ObjectField.ObjFOverlayLightAid),
            OverlayLightColor = ReadInt32(properties, ObjectField.ObjFOverlayLightColor),
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

    private static int[] ReadInt32Array(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field == field)
                return property.GetInt32Array();
        }

        return [];
    }
}
