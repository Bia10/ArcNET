namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Ammo : Item
    {
        [Order(60)] public int ObjFAmmoFlags { get; set; }
        [Order(61)] public int ObjFAmmoQuantity { get; set; }
        [Order(62)] public int ObjFAmmoType { get; set; }
        [Order(63)] public int ObjFAmmoPadI1 { get; set; }
        [Order(64)] public int ObjFAmmoPadI2 { get; set; }
        [Order(65)] public Unknown ObjFAmmoPadIas1 { get; set; }
        [Order(66)] public Unknown ObjFAmmoPadI64As1 { get; set; }
    }
}