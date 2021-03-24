using System;

namespace ArcNET.DataTypes.GameObjects
{
    public class GameObjectGuid
    {
        public short Type;
        public short Foo0;
        public int Foo2;
        public Guid Guid;

        public GameObjectGuid()
        {
        }

        public GameObjectGuid(short type, short foo0, int foo2)
        {
            Type = type;
            Foo0 = foo0;
            Foo2 = foo2;
        }

        public bool IsProto()
        {
            return Type == -1;
        }

        public int GetId()
        {
            return BitConverter.ToInt32(Guid.ToByteArray(), 0);
        }

        public override string ToString()
        {
            return $"{Type} {Foo0:x8} {Foo2:x8} {Guid}";
        }
    }
}