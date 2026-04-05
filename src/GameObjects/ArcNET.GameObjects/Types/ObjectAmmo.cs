using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectAmmo : ObjectItem
{
    public int AmmoFlags { get; set; }
    public int AmmoQuantity { get; set; }
    public int AmmoType { get; set; }
    public int AmmoPadI1 { get; set; }
    public int AmmoPadI2 { get; set; }
    public int AmmoPadIas1 { get; set; }
    public long AmmoPadI64As1 { get; set; }

    internal static ObjectAmmo Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectAmmo();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFAmmoFlags))
            obj.AmmoFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFAmmoQuantity))
            obj.AmmoQuantity = reader.ReadInt32();
        if (Bit(ObjectField.ObjFAmmoType))
            obj.AmmoType = reader.ReadInt32();
        if (Bit(ObjectField.ObjFAmmoPadI1))
            obj.AmmoPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFAmmoPadI2))
            obj.AmmoPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFAmmoPadIas1))
            obj.AmmoPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFAmmoPadI64As1))
            obj.AmmoPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFAmmoFlags))
            writer.WriteInt32(AmmoFlags);
        if (Bit(ObjectField.ObjFAmmoQuantity))
            writer.WriteInt32(AmmoQuantity);
        if (Bit(ObjectField.ObjFAmmoType))
            writer.WriteInt32(AmmoType);
        if (Bit(ObjectField.ObjFAmmoPadI1))
            writer.WriteInt32(AmmoPadI1);
        if (Bit(ObjectField.ObjFAmmoPadI2))
            writer.WriteInt32(AmmoPadI2);
        if (Bit(ObjectField.ObjFAmmoPadIas1))
            writer.WriteInt32(AmmoPadIas1);
        if (Bit(ObjectField.ObjFAmmoPadI64As1))
            writer.WriteInt64(AmmoPadI64As1);
    }
}
