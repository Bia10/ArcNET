namespace ArcNET.DataTypes.GameObjects.Types;

public class Gold : Item
{
    [Order(60)] public int ObjFGoldFlags { get; set; }
    [Order(61)] public int ObjFGoldQuantity { get; set; }
    [Order(62)] public int ObjFGoldPadI1 { get; set; }
    [Order(63)] public int ObjFGoldPadI2 { get; set; }
    [Order(64)] public Unknown ObjFGoldPadIas1 { get; set; }
    [Order(65)] public Unknown ObjFGoldPadI64As1 { get; set; }
}