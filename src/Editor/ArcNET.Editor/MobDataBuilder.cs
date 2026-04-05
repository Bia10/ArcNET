using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Fluent mutable builder for <see cref="MobData"/> instances.
/// Use the <see cref="MobDataBuilder(ObjectType, GameObjectGuid, GameObjectGuid)"/> constructor
/// to create a new object from scratch, or
/// <see cref="MobDataBuilder(MobData)"/> to start editing an existing one.
/// Call <see cref="Build"/> to produce a new <see cref="MobData"/> with a rebuilt header bitmap.
/// </summary>
public sealed class MobDataBuilder
{
    private readonly ObjectType _type;
    private readonly GameObjectGuid _protoId;
    private readonly GameObjectGuid _objectId;
    private readonly List<ObjectProperty> _properties;

    /// <summary>
    /// Starts a builder from an existing <see cref="MobData"/>.
    /// All properties are copied; modifications do not affect the original.
    /// </summary>
    public MobDataBuilder(MobData existing)
    {
        _type = existing.Header.GameObjectType;
        _protoId = existing.Header.ProtoId;
        _objectId = existing.Header.ObjectId;
        _properties = new List<ObjectProperty>(existing.Properties);
    }

    /// <summary>
    /// Starts a builder for a brand-new object of the given type with no properties set.
    /// </summary>
    /// <param name="type">The <see cref="ObjectType"/> of the new object.</param>
    /// <param name="objectId">Unique instance identifier.</param>
    /// <param name="protoId">Prototype reference identifier.</param>
    public MobDataBuilder(ObjectType type, GameObjectGuid objectId, GameObjectGuid protoId)
    {
        _type = type;
        _objectId = objectId;
        _protoId = protoId;
        _properties = [];
    }

    // ── Property mutations ────────────────────────────────────────────────────

    /// <summary>
    /// Adds or replaces the property for <see cref="ObjectProperty.Field"/>.
    /// </summary>
    public MobDataBuilder WithProperty(ObjectProperty property)
    {
        for (var i = 0; i < _properties.Count; i++)
        {
            if (_properties[i].Field != property.Field)
                continue;
            _properties[i] = property;
            return this;
        }

        _properties.Add(property);
        return this;
    }

    /// <summary>Removes the property for <paramref name="field"/> if present.</summary>
    public MobDataBuilder WithoutProperty(ObjectField field)
    {
        _properties.RemoveAll(p => p.Field == field);
        return this;
    }

    /// <summary>
    /// Sets the <see cref="ObjectField.ObjFLocation"/> property from tile coordinates.
    /// Location is packed as <c>LOCATION_MAKE(x, y)</c>: lower 32 bits = X, upper 32 bits = Y.
    /// </summary>
    public MobDataBuilder WithLocation(int tileX, int tileY)
    {
        var packed = (long)tileX | ((long)tileY << 32);
        // ObjFLocation wire type is Int64 — presence byte (1) + 8-byte value
        var bytes = new byte[9];
        bytes[0] = 1; // presence = present
        BitConverter.TryWriteBytes(bytes.AsSpan(1), packed);
        return WithProperty(new ObjectProperty { Field = ObjectField.ObjFLocation, RawBytes = bytes });
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Produces a new <see cref="MobData"/> with a freshly rebuilt header bitmap
    /// matching the current property list.
    /// </summary>
    public MobData Build()
    {
        var bitmapByteLength = ObjectFieldBitmapSizeHelper.For(_type);
        var bitmap = new byte[bitmapByteLength];
        foreach (var prop in _properties)
        {
            var f = (int)prop.Field;
            bitmap[f >> 3] |= (byte)(1 << (f & 7));
        }

        var header = new GameObjectHeader
        {
            Version = 0x77,
            ProtoId = _protoId,
            ObjectId = _objectId,
            GameObjectType = _type,
            PropCollectionItems = (short)_properties.Count,
            Bitmap = bitmap,
        };

        // Properties must be ordered by bit index to match the order ReadProperties uses
        // when parsing: it iterates bitmap bits ascending and reads one field per set bit.
        var sorted = new List<ObjectProperty>(_properties);
        sorted.Sort(static (a, b) => ((int)a.Field).CompareTo((int)b.Field));

        return new MobData { Header = header, Properties = sorted.AsReadOnly() };
    }
}
