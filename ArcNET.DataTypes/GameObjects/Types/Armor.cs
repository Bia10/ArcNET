using System;

namespace ArcNET.DataTypes.GameObjects.Types
{
    public partial class Armor : Item
    {
        [Order(60)] public int ObjFArmorFlags { get; set; }
        [Order(61)] public int ObjFArmorPaperDollAid { get; set; }
        [Order(62)] public int ObjFArmorAcAdj { get; set; }
        [Order(63)] public int ObjFArmorMagicAcAdj { get; set; }
        [Order(64)] public Tuple<int[], int[]> ObjFArmorResistanceAdjIdx { get; set; }
        [Order(65)] public Tuple<int[], int[]> ObjFArmorMagicResistanceAdjIdx { get; set; }
        [Order(66)] public int ObjFArmorSilentMoveAdj { get; set; }
        [Order(67)] public int ObjFArmorMagicSilentMoveAdj { get; set; }
        [Order(68)] public int ObjFArmorUnarmedBonusDamage { get; set; }
        [Order(69)] public int ObjFArmorPadI2 { get; set; }
        [Order(70)] public Unknown ObjFArmorPadIas1 { get; set; }
        [Order(71)] public Unknown ObjFArmorPadI64As1 { get; set; }
    }
}