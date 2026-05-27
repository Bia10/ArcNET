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
            LightFlags = ReadInt32(properties, ObjectField.LightFlags),
            LightArtId = new ArtId(unchecked((uint)ReadInt32(properties, ObjectField.LightAid))),
            LightColor = ReadColor(properties, ObjectField.LightColor),
            OverlayLightFlags = ReadFirstInt32Value(properties, ObjectField.OverlayLightFlags),
            OverlayLightArtIds = ReadInt32Array(properties, ObjectField.OverlayLightAid),
            OverlayLightColor = ReadFirstInt32Value(properties, ObjectField.OverlayLightColor),
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

    private static int ReadFirstInt32Value(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field != field)
                continue;

            try
            {
                return property.GetInt32();
            }
            catch (InvalidOperationException)
            {
                var values = property.GetInt32Array();
                return values.Length == 0 ? 0 : values[0];
            }
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

            return property.GetPackedRgbColor();
        }

        return default;
    }

    private static int[] ReadInt32Array(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field == field)
            {
                try
                {
                    return property.GetInt32Array();
                }
                catch (InvalidOperationException)
                {
                    return [property.GetInt32()];
                }
            }
        }

        return [];
    }
}
