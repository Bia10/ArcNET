using ArcNET.Utilities;
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
                header.PropCollectionItems = reader.ReadInt16();
            }

            var bitmapLength = (int)Enum.Parse(typeof(Enums.ObjectFieldBitmap), header.GameObjectType.ToString());
            header.Bitmap = new BitArray(reader.ReadBytes(bitmapLength));

            AnsiConsoleExtensions.Log($"Parsed GameOjb headerVersion: {header.Version} " 
                                      + $"\n ProtoId: {header.ProtoId}"
                                      + $"\n ObjectId: {header.ObjectId}"
                                      + $"\n GameObjectType: {header.GameObjectType}"
                                      + $"\n bitmapLength: {bitmapLength}"
                                      + $"\n Bitmap: {header.Bitmap}", "warn");
            return header;
        }
    }
}