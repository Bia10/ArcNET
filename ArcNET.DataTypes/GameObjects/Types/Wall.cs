namespace ArcNET.DataTypes.GameObjects.Types;

public class Wall : Common
{
    [Order(37)] public int ObjFWallFlags { get; set; }
    [Order(38)] public int ObjFWallPadI1 { get; set; }
    [Order(39)] public int ObjFWallPadI2 { get; set; }
    [Order(40)] public Unknown ObjFWallPadIas1 { get; set; }
    [Order(41)] public Unknown ObjFWallPadI64As1 { get; set; }
}