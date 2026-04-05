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
    public static ObjectProperty? GetProperty(this MobData mob, ObjectField field)
    {
        foreach (var prop in mob.Properties)
            if (prop.Field == field)
                return prop;
        return null;
    }

    /// <summary>
    /// Returns a new <see cref="MobData"/> with <paramref name="property"/> replacing any existing
    /// entry for the same field, or appended if not present. The header bitmap and
    /// <c>PropCollectionItems</c> are kept in sync via <see cref="RebuildHeader(MobData)"/>.
    /// </summary>
    public static MobData WithProperty(this MobData mob, ObjectProperty property)
    {
        var list = new List<ObjectProperty>(mob.Properties.Count + 1);
        var replaced = false;
        foreach (var p in mob.Properties)
        {
            if (p.Field == property.Field)
            {
                list.Add(property);
                replaced = true;
            }
            else
            {
                list.Add(p);
            }
        }

        if (!replaced)
            list.Add(property);

        return new MobData { Header = mob.Header, Properties = list }.RebuildHeader();
    }

    /// <summary>
    /// Returns a new <see cref="MobData"/> with the property for <paramref name="field"/> removed.
    /// The header bitmap and <c>PropCollectionItems</c> are kept in sync.
    /// </summary>
    public static MobData WithoutProperty(this MobData mob, ObjectField field)
    {
        var list = new List<ObjectProperty>(mob.Properties.Count);
        foreach (var p in mob.Properties)
            if (p.Field != field)
                list.Add(p);

        return new MobData { Header = mob.Header, Properties = list }.RebuildHeader();
    }

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
    public static ObjectProperty? GetProperty(this ProtoData proto, ObjectField field)
    {
        foreach (var prop in proto.Properties)
            if (prop.Field == field)
                return prop;
        return null;
    }

    /// <summary>
    /// Returns a new <see cref="ProtoData"/> with <paramref name="property"/> replacing any existing
    /// entry for the same field, or appended if not present. The header bitmap is kept in sync.
    /// </summary>
    public static ProtoData WithProperty(this ProtoData proto, ObjectProperty property)
    {
        var list = new List<ObjectProperty>(proto.Properties.Count + 1);
        var replaced = false;
        foreach (var p in proto.Properties)
        {
            if (p.Field == property.Field)
            {
                list.Add(property);
                replaced = true;
            }
            else
            {
                list.Add(p);
            }
        }

        if (!replaced)
            list.Add(property);

        return new ProtoData { Header = proto.Header, Properties = list }.RebuildHeader();
    }

    /// <summary>
    /// Returns a new <see cref="ProtoData"/> with the property for <paramref name="field"/> removed.
    /// The header bitmap is kept in sync.
    /// </summary>
    public static ProtoData WithoutProperty(this ProtoData proto, ObjectField field)
    {
        var list = new List<ObjectProperty>(proto.Properties.Count);
        foreach (var p in proto.Properties)
            if (p.Field != field)
                list.Add(p);

        return new ProtoData { Header = proto.Header, Properties = list }.RebuildHeader();
    }

    /// <summary>
    /// Returns a new <see cref="ProtoData"/> whose header bitmap and <c>PropCollectionItems</c>
    /// are rebuilt from the current property list.
    /// </summary>
    public static ProtoData RebuildHeader(this ProtoData proto)
    {
        var header = RebuildHeader(proto.Header, proto.Properties);
        return new ProtoData { Header = header, Properties = proto.Properties };
    }

    // ── Shared ────────────────────────────────────────────────────────────────

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
