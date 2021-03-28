namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Portal : Common
    {
        [Order(37)] public int ObjFPortalFlags { get; set; }
        [Order(38)] public int ObjFPortalLockDifficulty { get; set; }
        [Order(39)] public int ObjFPortalKeyId { get; set; }
        [Order(40)] public int ObjFPortalNotifyNpc { get; set; }
        [Order(41)] public int ObjFPortalPadI1 { get; set; }
        [Order(42)] public int ObjFPortalPadI2 { get; set; }
        [Order(43)] public Unknown ObjFPortalPadIas1 { get; set; }
        [Order(44)] public Unknown ObjFPortalPadI64As1 { get; set; }
    }
}