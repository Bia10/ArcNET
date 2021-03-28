namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Trap : Common
    {
        [Order(37)] public int ObjFTrapFlags { get; set; }
        [Order(38)] public int ObjFTrapDifficulty { get; set; }
        [Order(39)] public int ObjFTrapPadI2 { get; set; }
        [Order(40)] public Unknown ObjFTrapPadIas1 { get; set; }
        [Order(41)] public Unknown ObjFTrapPadI64As1 { get; set; }
    }
}