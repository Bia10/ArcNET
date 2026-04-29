using System.Buffers;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects.Types;

namespace ArcNET.GameObjects;

/// <summary>
/// A fully parsed game object: header + type-specific data.  Construct via
/// <see cref="Read(ref SpanReader)"/>.
/// </summary>
public sealed class GameObject : IGameObject
{
    private GameObjectHeader? _header;
    private ObjectCommon? _common;

    public required GameObjectHeader Header
    {
        get => _header ?? throw new InvalidOperationException("GameObject header is not initialized.");
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _header = value;
            ValidateHeaderAndBody(_header, _common);
        }
    }

    public required ObjectCommon Common
    {
        get => _common ?? throw new InvalidOperationException("GameObject body is not initialized.");
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _common = value;
            ValidateHeaderAndBody(_header, _common);
        }
    }

    public ObjectType Type => Header.GameObjectType;
    public GameObjectGuid ObjectId => Header.ObjectId;
    public GameObjectGuid ProtoId => Header.ProtoId;
    public bool IsPrototype => Header.IsPrototype;

    /// <summary>
    /// Reads a complete game object (header + type body) from <paramref name="reader"/>.
    /// Prototype objects (where <see cref="GameObjectHeader.IsPrototype"/> is true) include all
    /// fields; non-prototype objects inherit absent fields from the prototype.
    /// </summary>
    public static GameObject Read(ref SpanReader reader)
    {
        var header = GameObjectHeader.Read(ref reader);
        var bitmap = header.Bitmap;
        var isProto = header.IsPrototype;

        var common = ObjectTypeRegistry.Read(header.GameObjectType, ref reader, bitmap, isProto);

        return new GameObject { Header = header, Common = common };
    }

    /// <summary>
    /// Serialises this game object back to its binary on-disk representation.
    /// The format is identical to the OFF format used by <c>MobFormat</c> and <c>ProtoFormat</c>:
    /// a <see cref="GameObjectHeader"/> followed by type-specific field data in bitmap order.
    /// </summary>
    public byte[] WriteToArray()
    {
        var header = Header;
        var common = Common;
        ObjectTypeRegistry.Validate(header.GameObjectType, common);

        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        header.Write(ref writer);
        var bitmap = header.Bitmap;
        var isProto = header.IsPrototype;
        ObjectTypeRegistry.Write(header.GameObjectType, common, ref writer, bitmap, isProto);
        return buf.WrittenSpan.ToArray();
    }

    private static void ValidateHeaderAndBody(GameObjectHeader? header, ObjectCommon? common)
    {
        if (header is null || common is null)
            return;

        ObjectTypeRegistry.Validate(header.GameObjectType, common);
    }
}
