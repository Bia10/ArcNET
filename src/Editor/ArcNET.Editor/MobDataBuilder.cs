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

    // Preserved from the original header so that bits for fields we could not decode
    // (because their wire type is unknown) are not silently dropped when Build() is called.
    // Build() seeds the new bitmap from this array and then ORs in bits for every
    // property in _properties, so known + unknown bits are both retained.
    private readonly byte[]? _originalBitmap;

    // Preserved from the original header when building from an existing MobData.
    // null means "new from scratch" — Build() will use 0x77 / _properties.Count in that case.
    private readonly int? _originalVersion;
    private readonly short? _originalPropCollItems;

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
        _originalBitmap = (byte[])existing.Header.Bitmap.Clone();
        _originalVersion = existing.Header.Version;
        _originalPropCollItems = existing.Header.PropCollectionItems;
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
        _originalBitmap = null;
        _originalVersion = null;
        _originalPropCollItems = null;
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

        // Seed from the original bitmap so that bits for fields whose wire type is unknown
        // (and therefore excluded from _properties) are not silently discarded.
        if (_originalBitmap is not null)
        {
            var copyLen = Math.Min(_originalBitmap.Length, bitmapByteLength);
            _originalBitmap.AsSpan(0, copyLen).CopyTo(bitmap);
        }

        foreach (var prop in _properties)
        {
            var f = (int)prop.Field;
            bitmap[f >> 3] |= (byte)(1 << (f & 7));
        }

        var header = new GameObjectHeader
        {
            // Preserve the source version (0x08 = retail Arcanum) when editing an existing object.
            // For new-from-scratch objects use 0x77 (arcanum-ce convention).
            Version = _originalVersion ?? 0x77,
            ProtoId = _protoId,
            ObjectId = _objectId,
            GameObjectType = _type,
            // Preserve the original PropCollectionItems when editing an existing object so that
            // the game-internal counter is not inadvertently changed by our decoded property count.
            PropCollectionItems = _originalPropCollItems ?? (short)_properties.Count,
            Bitmap = bitmap,
        };

        // Properties must be ordered by bit index to match the order ReadProperties uses
        // when parsing: it iterates bitmap bits ascending and reads one field per set bit.
        var sorted = new List<ObjectProperty>(_properties);
        sorted.Sort(static (a, b) => ((int)a.Field).CompareTo((int)b.Field));

        return new MobData { Header = header, Properties = sorted.AsReadOnly() };
    }
}
