using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using ArcNET.DataTypes.Common;
using ArcNET.DataTypes.GameObjects;

namespace ArcNET.DataTypes
{
    public static class BinaryReaderExtensions
    {
        public static Location ReadLocation(this BinaryReader reader, bool force = false)
        {
            var loc = new Location();
            if (!force)
            {
                var flag = reader.ReadByte();
                if (flag == 0x00) return null;
                if (flag != 0x01) throw new Exception();
            }

            loc.X = reader.ReadInt32();
            loc.Y = reader.ReadInt32();
            return loc;
        }

        public static GameObjectGuid ReadGameObjectGuid(this BinaryReader reader, bool force = false)
        {
            if (!force)
            {
                var flag = reader.ReadByte();
                if (flag == 0x00) return null;
                if (flag != 0x01) throw new Exception();
            }

            var result = new GameObjectGuid
            {
                Type = reader.ReadInt16(),
                Foo0 = reader.ReadInt16(),
                Foo2 = reader.ReadInt32()
            };
            var guidData = reader.ReadBytes(16);
            result.Guid = new Guid(guidData);

            return result;
        } 

        public static ArtId ReadArtId(this BinaryReader reader)
        {
            return new(reader.ReadInt32().ToString("X2"));
        }
    }

    public static class BitArrayUtils
    {
        public static bool Get(this BitArray bitArray, int index, bool isPrototype)
        {
            return bitArray.Get(index) || isPrototype;
        }
    }

    public class OrderAttribute : Attribute
    {
        public int Order { get; private set; }
        public OrderAttribute(int order)
        {
            Order = order;
        }
    }

    public static class PropertyInfoUtils
    {
        public static int PropertyOrder(this PropertyInfo propInfo)
        {
            var orderAttr = (OrderAttribute)propInfo.GetCustomAttributes(typeof(OrderAttribute), true).SingleOrDefault();
            var output = orderAttr?.Order ?? int.MaxValue;
            return output;
        }
    }
}