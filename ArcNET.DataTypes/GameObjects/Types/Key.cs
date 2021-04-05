namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Key : Item
    {
        [Order(60)] public int ObjFKeyKeyId { get; set; }
        [Order(61)] public int ObjFKeyPadI1 { get; set; }
        [Order(62)] public int ObjFKeyPadI2 { get; set; }
        [Order(63)] public Unknown ObjFKeyPadIas1 { get; set; }
        [Order(64)] public Unknown ObjFKeyPadI64As1 { get; set; }
    }
}