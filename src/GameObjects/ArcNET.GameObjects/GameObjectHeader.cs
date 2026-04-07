using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects;

/// <summary>Header block present at the start of every serialized game object.</summary>
public sealed class GameObjectHeader
{
    /// <summary>
    /// File format version.
    /// <list type="bullet">
    ///   <item><c>0x08</c> — original Arcanum retail binary.</item>
    ///   <item><c>0x77</c> — arcanum-ce reimplementation.</item>
    /// </list>
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> if the version came from an original Arcanum save
    /// (<c>0x08</c>) rather than an arcanum-ce artefact (<c>0x77</c>).
    /// </summary>
    public bool IsOriginalVersion => Version == 0x08;

    /// <summary>Prototype ID (all-bits-set Type field indicates this IS a prototype).</summary>
    public required GameObjectGuid ProtoId { get; init; }

    /// <summary>Unique object instance ID.</summary>
    public required GameObjectGuid ObjectId { get; init; }

    /// <summary>The major type category of this object.</summary>
    public required ObjectType GameObjectType { get; init; }

    /// <summary>Number of property collection items (present only for non-prototype objects).</summary>
    public short PropCollectionItems { get; init; }

    /// <summary>Property bitmap indicating which fields are present (one bit per <see cref="ObjectField"/>).</summary>
    public required byte[] Bitmap { get; init; }

    /// <summary>Returns <see langword="true"/> when this header describes a prototype.</summary>
    public bool IsPrototype => ProtoId.IsProto;

    internal void Write(ref SpanWriter writer)
    {
        writer.WriteInt32(Version);
        ProtoId.Write(ref writer);
        ObjectId.Write(ref writer);
        writer.WriteUInt32((uint)GameObjectType);

        if (!IsPrototype)
            writer.WriteInt16(PropCollectionItems);

        writer.WriteBytes(Bitmap);
    }

    internal static GameObjectHeader Read(ref SpanReader reader)
    {
        var version = reader.ReadInt32();
        if (version != 0x77 && version != 0x08)
            throw new InvalidDataException($"Unknown object file version: 0x{version:X2} (expected 0x08 or 0x77)");

        var protoId = GameObjectGuid.Read(ref reader);
        var objectId = GameObjectGuid.Read(ref reader);
        var objectType = (ObjectType)reader.ReadUInt32();

        short propCollectionItems = 0;
        if (!protoId.IsProto)
            propCollectionItems = reader.ReadInt16();

        var bitmapLength = ObjectFieldBitmapSize.For(objectType);
        var bitmap = reader.ReadBytes(bitmapLength).ToArray();

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

    /// <summary>
    /// Reads a <em>compact</em> game-object header — used by <c>mobile.md</c> records that store
    /// only a single OID in the body (no separate ObjectId field).
    /// Wire layout: <c>[version 4B][protoId 24B][objectType 4B][propCollItems 2B?][bitmap N B]</c>.
    /// The caller provides <paramref name="objectId"/> (typically the file-level MapObjectId).
    /// </summary>
    internal static GameObjectHeader ReadCompact(ref SpanReader reader, GameObjectGuid objectId)
    {
        var version = reader.ReadInt32();
        if (version != 0x77 && version != 0x08)
            throw new InvalidDataException($"Unknown object file version: 0x{version:X2} (expected 0x08 or 0x77)");

        // Compact format: only ONE OID in the body (serves as both proto and instance reference).
        var protoId = GameObjectGuid.Read(ref reader);
        var objectType = (ObjectType)reader.ReadUInt32();

        short propCollectionItems = 0;
        if (!protoId.IsProto)
            propCollectionItems = reader.ReadInt16();

        var bitmapLength = ObjectFieldBitmapSize.For(objectType);
        var bitmap = reader.ReadBytes(bitmapLength).ToArray();

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

    /// <summary>
    /// Writes this header in <em>compact</em> form — emits only <c>ProtoId</c> (no <c>ObjectId</c>).
    /// Used together with <see cref="ReadCompact"/> to round-trip compact <c>mobile.md</c> records.
    /// </summary>
    internal void WriteCompact(ref SpanWriter writer)
    {
        writer.WriteInt32(Version);
        ProtoId.Write(ref writer);
        // ObjectId is NOT written — compact format has only one OID.
        writer.WriteUInt32((uint)GameObjectType);

        if (!IsPrototype)
            writer.WriteInt16(PropCollectionItems);

        writer.WriteBytes(Bitmap);
    }
}
