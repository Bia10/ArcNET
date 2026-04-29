using ArcNET.Core;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

/// <summary>
/// Extension methods for working with <see cref="MobData"/> and <see cref="ProtoData"/>
/// at the property level without manually indexing the raw property list.
/// </summary>
public static class MobDataExtensions
{
    // ── MobData ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the property for <paramref name="field"/>, or <see langword="null"/> if not present.
    /// </summary>
    public static ObjectProperty? GetProperty(this MobData mob, ObjectField field) =>
        GetPropertyCore(mob.Properties, field);

    /// <summary>
    /// Returns a new <see cref="MobData"/> with <paramref name="property"/> replacing any existing
    /// entry for the same field, or appended if not present. The header bitmap and
    /// <c>PropCollectionItems</c> are kept in sync via <see cref="RebuildHeader(MobData)"/>.
    /// </summary>
    public static MobData WithProperty(this MobData mob, ObjectProperty property) =>
        new MobData
        {
            Header = mob.Header,
            Properties = ReplaceOrAppendPropertyCore(mob.Properties, property),
        }.RebuildHeader();

    /// <summary>
    /// Returns a new <see cref="MobData"/> with the property for <paramref name="field"/> removed.
    /// The header bitmap and <c>PropCollectionItems</c> are kept in sync.
    /// </summary>
    public static MobData WithoutProperty(this MobData mob, ObjectField field) =>
        new MobData { Header = mob.Header, Properties = RemovePropertyCore(mob.Properties, field) }.RebuildHeader();

    /// <summary>
    /// Returns a new <see cref="MobData"/> whose header bitmap and <c>PropCollectionItems</c>
    /// are rebuilt from the current property list.
    /// </summary>
    public static MobData RebuildHeader(this MobData mob)
    {
        var header = RebuildHeader(mob.Header, mob.Properties);
        return new MobData { Header = header, Properties = mob.Properties };
    }

    // ── ProtoData ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the property for <paramref name="field"/>, or <see langword="null"/> if not present.
    /// </summary>
    public static ObjectProperty? GetProperty(this ProtoData proto, ObjectField field) =>
        GetPropertyCore(proto.Properties, field);

    /// <summary>
    /// Returns a new <see cref="ProtoData"/> with <paramref name="property"/> replacing any existing
    /// entry for the same field, or appended if not present. The header bitmap is kept in sync.
    /// </summary>
    public static ProtoData WithProperty(this ProtoData proto, ObjectProperty property) =>
        new ProtoData
        {
            Header = proto.Header,
            Properties = ReplaceOrAppendPropertyCore(proto.Properties, property),
        }.RebuildHeader();

    /// <summary>
    /// Returns a new <see cref="ProtoData"/> with the property for <paramref name="field"/> removed.
    /// The header bitmap is kept in sync.
    /// </summary>
    public static ProtoData WithoutProperty(this ProtoData proto, ObjectField field) =>
        new ProtoData
        {
            Header = proto.Header,
            Properties = RemovePropertyCore(proto.Properties, field),
        }.RebuildHeader();

    /// <summary>
    /// Returns a new <see cref="ProtoData"/> whose header bitmap and <c>PropCollectionItems</c>
    /// are rebuilt from the current property list.
    /// </summary>
    public static ProtoData RebuildHeader(this ProtoData proto)
    {
        var header = RebuildHeader(proto.Header, proto.Properties);
        return new ProtoData { Header = header, Properties = proto.Properties };
    }

    // ── GameObject bridge ─────────────────────────────────────────────────────

    /// <summary>
    /// Converts a raw <see cref="MobData"/> property bag into a fully typed
    /// <see cref="GameObject"/>.  The conversion serialises the <see cref="MobData"/> to its
    /// binary representation and re-parses it through <see cref="GameObject.Read"/>.
    /// </summary>
    /// <param name="data">The raw mob data to convert.</param>
    /// <returns>A typed game object with all fields extracted from the property collection.</returns>
    /// <remarks>
    /// This bridge performs a full binary round-trip and allocates a temporary payload for the
    /// serialised mob bytes. It is correct but intentionally not a zero-copy conversion; if this
    /// becomes a hot path, replace it with a direct structural bridge instead of widening the
    /// binary codec dependency.
    /// </remarks>
    public static GameObject ToGameObject(this MobData data)
    {
        var bytes = MobFormat.WriteToArray(in data);
        var reader = new SpanReader(bytes);
        return GameObject.Read(ref reader);
    }

    /// <summary>
    /// Converts a typed <see cref="GameObject"/> back into a raw <see cref="MobData"/>
    /// property bag.  The conversion serialises the <see cref="GameObject"/> to its binary
    /// representation and re-parses it through <see cref="MobFormat.Parse"/>.
    /// </summary>
    /// <param name="obj">The typed game object to convert.</param>
    /// <returns>
    /// A <see cref="MobData"/> with <see cref="ObjectProperty"/> entries for every set bit
    /// in the object's bitmap.
    /// </returns>
    /// <remarks>
    /// This bridge also relies on a full binary round-trip, so it allocates a temporary payload
    /// and stays coupled to the current mob binary codec. Keep it as a convenience API, but move
    /// to a direct property-to-object mapping if conversion throughput ever matters.
    /// </remarks>
    public static MobData ToMobData(this GameObject obj)
    {
        var bytes = obj.WriteToArray();
        var reader = new SpanReader(bytes);
        return MobFormat.Parse(ref reader);
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    private static ObjectProperty? GetPropertyCore(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        foreach (var property in properties)
            if (property.Field == field)
                return property;

        return null;
    }

    private static List<ObjectProperty> ReplaceOrAppendPropertyCore(
        IReadOnlyList<ObjectProperty> properties,
        ObjectProperty property
    )
    {
        var list = new List<ObjectProperty>(properties.Count + 1);
        var replaced = false;
        foreach (var existing in properties)
        {
            if (existing.Field == property.Field)
            {
                list.Add(property);
                replaced = true;
            }
            else
            {
                list.Add(existing);
            }
        }

        if (!replaced)
            list.Add(property);

        return list;
    }

    private static List<ObjectProperty> RemovePropertyCore(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        var list = new List<ObjectProperty>(properties.Count);
        foreach (var property in properties)
            if (property.Field != field)
                list.Add(property);

        return list;
    }

    private static GameObjectHeader RebuildHeader(GameObjectHeader existing, IReadOnlyList<ObjectProperty> properties)
    {
        var type = existing.GameObjectType;
        var bitmapByteLength = ObjectFieldBitmapSize.For(type);
        var bitmap = new byte[bitmapByteLength];

        foreach (var prop in properties)
        {
            var f = (int)prop.Field;
            bitmap[f >> 3] |= (byte)(1 << (f & 7));
        }

        return new GameObjectHeader
        {
            Version = existing.Version,
            ProtoId = existing.ProtoId,
            ObjectId = existing.ObjectId,
            GameObjectType = type,
            PropCollectionItems = existing.IsPrototype ? (short)0 : (short)properties.Count,
            Bitmap = bitmap,
        };
    }
}
