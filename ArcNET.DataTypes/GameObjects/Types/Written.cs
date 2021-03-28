namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Written : Item
    {
        [Order(60)] public int ObjFWrittenFlags { get; set; }
        [Order(61)] public int ObjFWrittenSubtype { get; set; }
        [Order(62)] public int ObjFWrittenTextStartLine { get; set; }
        [Order(63)] public int ObjFWrittenTextEndLine { get; set; }
        [Order(64)] public int ObjFWrittenPadI1 { get; set; }
        [Order(65)] public int ObjFWrittenPadI2 { get; set; }
        [Order(66)] public Unknown ObjFWrittenPadIas1 { get; set; }
        [Order(67)] public Unknown ObjFWrittenPadI64As1 { get; set; }
    }
}