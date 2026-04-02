using System.Collections;
using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects;

/// <summary>Header block present at the start of every serialized game object.</summary>
public sealed class GameObjectHeader
{
    /// <summary>File format version (expected 0x77).</summary>
    public required int Version { get; init; }

    /// <summary>Prototype ID (all-bits-set Type field indicates this IS a prototype).</summary>
    public required GameObjectGuid ProtoId { get; init; }

    /// <summary>Unique object instance ID.</summary>
    public required GameObjectGuid ObjectId { get; init; }

    /// <summary>The major type category of this object.</summary>
    public required ObjectType GameObjectType { get; init; }

    /// <summary>Number of property collection items (present only for non-prototype objects).</summary>
    public short PropCollectionItems { get; init; }

    /// <summary>Property bitmap indicating which fields are present.</summary>
    public required BitArray Bitmap { get; init; }

    /// <summary>Returns <see langword="true"/> when this header describes a prototype.</summary>
    public bool IsPrototype => ProtoId.IsProto;

    internal static GameObjectHeader Read(ref SpanReader reader)
    {
        var version = reader.ReadInt32();
        if (version != 0x77)
            throw new InvalidDataException($"Unknown object file version: 0x{version:X2}");

        var protoId = GameObjectGuid.Read(ref reader);
        var objectId = GameObjectGuid.Read(ref reader);
        var objectType = (ObjectType)reader.ReadUInt32();

        short propCollectionItems = 0;
        if (!protoId.IsProto)
            propCollectionItems = reader.ReadInt16();

        var bitmapLength = ObjectFieldBitmapSize.For(objectType);
        var bitmapBytes = reader.ReadBytes(bitmapLength).ToArray();
        var bitmap = new BitArray(bitmapBytes);

        return new GameObjectHeader
        {
            Version = version,
            ProtoId = protoId,
            ObjectId = objectId,
            GameObjectType = objectType,
            PropCollectionItems = propCollectionItems,
            Bitmap = bitmap,
        };
    }
}
