using System;

namespace ArcNET.DataTypes.GameObjects.Types
{
    public class KeyRing : Item
    {
        [Order(60)] public int ObjFKeyRingFlags { get; set; }
        [Order(61)] public Tuple<int[], int[]> ObjFKeyRingListIdx { get; set; }
        [Order(62)] public int ObjFKeyRingPadI1 { get; set; }
        [Order(63)] public int ObjFKeyRingPadI2 { get; set; }
        [Order(64)] public Unknown ObjFKeyRingPadIas1 { get; set; }
        [Order(65)] public Unknown ObjFKeyRingPadI64As1 { get; set; }
    }
}