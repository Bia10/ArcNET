namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Food : Item
    {
        [Order(60)] public int ObjFFoodFlags { get; set; }
        [Order(61)] public int ObjFFoodPadI1 { get; set; }
        [Order(62)] public int ObjFFoodPadI2 { get; set; }
        [Order(63)] public Unknown ObjFFoodPadIas1 { get; set; }
        [Order(64)] public Unknown ObjFFoodPadI64As1 { get; set; }
    }
}