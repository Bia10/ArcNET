using System;
using System.Collections;
using System.IO;

namespace ArcNET.DataTypes.GameObjects
{
    public static class GameObjectHeaderReader
    {
        public static GameObjectHeader Read(this BinaryReader reader)
        {
            var header = new GameObjectHeader
            {
                Version = reader.ReadInt32()
            };

            if (header.Version != 0x77)
            {
                throw new InvalidDataException("Unknown object file version: " + header.Version);
            }

            header.ProtoId = reader.ReadGameObjectGuid(true);
            header.ObjectId = reader.ReadGameObjectGuid(true);
            header.GameObjectType = (Enums.ObjectType)reader.ReadUInt32();

            if (!header.ProtoId.IsProto())
            {
                header.PropCollectionItems = reader.ReadInt16(); // Actually not really used anymore
            }

            var bitmapLength = (int)Enum.Parse(typeof(Enums.ObjectFieldBitmap), header.GameObjectType.ToString());
            header.Bitmap = new BitArray(reader.ReadBytes(bitmapLength));

            return header;
        }
    }
}