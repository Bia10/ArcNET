using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectPortal : ObjectCommon
{
    private int _portalPadI1Reserved;
    private int _portalPadI2Reserved;
    private int _portalPadIas1Reserved;
    private long _portalPadI64As1Reserved;

    public ObjFPortalFlags PortalFlags { get; internal set; }
    public int LockDifficulty { get; internal set; }
    public int KeyId { get; internal set; }
    public int NotifyNpc { get; internal set; }

    internal static ObjectPortal Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectPortal();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFPortalFlags))
            obj.PortalFlags = unchecked((ObjFPortalFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ObjFPortalLockDifficulty))
            obj.LockDifficulty = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalKeyId))
            obj.KeyId = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalNotifyNpc))
            obj.NotifyNpc = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalPadI1))
            obj._portalPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalPadI2))
            obj._portalPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalPadIas1))
            obj._portalPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPortalPadI64As1))
            obj._portalPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFPortalFlags))
            writer.WriteInt32(unchecked((int)PortalFlags));
        if (Bit(ObjectField.ObjFPortalLockDifficulty))
            writer.WriteInt32(LockDifficulty);
        if (Bit(ObjectField.ObjFPortalKeyId))
            writer.WriteInt32(KeyId);
        if (Bit(ObjectField.ObjFPortalNotifyNpc))
            writer.WriteInt32(NotifyNpc);
        if (Bit(ObjectField.ObjFPortalPadI1))
            writer.WriteInt32(_portalPadI1Reserved);
        if (Bit(ObjectField.ObjFPortalPadI2))
            writer.WriteInt32(_portalPadI2Reserved);
        if (Bit(ObjectField.ObjFPortalPadIas1))
            writer.WriteInt32(_portalPadIas1Reserved);
        if (Bit(ObjectField.ObjFPortalPadI64As1))
            writer.WriteInt64(_portalPadI64As1Reserved);
    }
}
