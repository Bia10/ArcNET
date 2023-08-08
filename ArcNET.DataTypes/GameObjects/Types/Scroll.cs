namespace ArcNET.DataTypes.GameObjects.Types;

public class Scroll : Item
{
    [Order(60)] public int ObjFScrollFlags { get; set; }
    [Order(61)] public int ObjFScrollPadI1 { get; set; }
    [Order(62)] public int ObjFScrollPadI2 { get; set; }
    [Order(63)] public Unknown ObjFScrollPadIas1 { get; set; }
    [Order(64)] public Unknown ObjFScrollPadI64As1 { get; set; }
}