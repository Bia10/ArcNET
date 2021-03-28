namespace ArcNET.DataTypes.GameObjects.Types
{
    public partial class Generic : Item
    {
        [Order(60)] public int ObjFGenericFlags { get; set; }
        [Order(61)] public int ObjFGenericUsageBonus { get; set; }
        [Order(62)] public int ObjFGenericUsageCountRemaining { get; set; }
        [Order(63)] public Unknown ObjFGenericPadIas1 { get; set; }
        [Order(64)] public Unknown ObjFGenericPadI64As1 { get; set; }
    }
}