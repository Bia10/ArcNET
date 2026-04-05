using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectPortal : ObjectCommon
{
    public int PortalFlags { get; set; }
    public int PortalLockDifficulty { get; set; }
    public int PortalKeyId { get; set; }
    public int PortalNotifyNpc { get; set; }
    public int PortalPadI1 { get; set; }
    public int PortalPadI2 { get; set; }
    public int PortalPadIas1 { get; set; }
    public long PortalPadI64As1 { get; set; }

    internal static ObjectPortal Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectPortal();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFPortalFlags))
            obj.PortalFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalLockDifficulty))
            obj.PortalLockDifficulty = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalKeyId))
            obj.PortalKeyId = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalNotifyNpc))
            obj.PortalNotifyNpc = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalPadI1))
            obj.PortalPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalPadI2))
            obj.PortalPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalPadIas1))
            obj.PortalPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalPadI64As1))
            obj.PortalPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFPortalFlags))
            writer.WriteInt32(PortalFlags);
        if (Bit(ObjectField.ObjFPortalLockDifficulty))
            writer.WriteInt32(PortalLockDifficulty);
        if (Bit(ObjectField.ObjFPortalKeyId))
            writer.WriteInt32(PortalKeyId);
        if (Bit(ObjectField.ObjFPortalNotifyNpc))
            writer.WriteInt32(PortalNotifyNpc);
        if (Bit(ObjectField.ObjFPortalPadI1))
            writer.WriteInt32(PortalPadI1);
        if (Bit(ObjectField.ObjFPortalPadI2))
            writer.WriteInt32(PortalPadI2);
        if (Bit(ObjectField.ObjFPortalPadIas1))
            writer.WriteInt32(PortalPadIas1);
        if (Bit(ObjectField.ObjFPortalPadI64As1))
            writer.WriteInt64(PortalPadI64As1);
    }
}
