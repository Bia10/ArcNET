using System.Collections;

namespace ArcNET.DataTypes.GameObjects;

public class GameObjectHeader
{
    public string Filename;
    public int Version;
    public GameObjectGuid ProtoId { get; set; }
    public GameObjectGuid ObjectId { get; set; }
    public Enums.ObjectType GameObjectType { get; set; }
    public short PropCollectionItems;
    public BitArray Bitmap;

    public bool IsPrototype()
        => ProtoId.IsProto();
}