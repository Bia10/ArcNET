using System;

namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Container : Common
    {
        [Order(37)] public int ObjFContainerFlags { get; set; }
        [Order(38)] public int ObjFContainerLockDifficulty { get; set; }
        [Order(39)] public int ObjFContainerKeyId { get; set; }
        [Order(40)] public int ObjFContainerInventoryNum { get; set; }
        [Order(41)] public Tuple<GameObjectGuid[], int[]> ObjFContainerInventoryListIdx { get; set; }
        [Order(42)] public int ObjFContainerInventorySource { get; set; }
        [Order(43)] public int ObjFContainerNotifyNpc { get; set; }
        [Order(44)] public int ObjFContainerPadI1 { get; set; }
        [Order(45)] public int ObjFContainerPadI2 { get; set; }
        [Order(46)] public Unknown ObjFContainerPadIas1 { get; set; }
        [Order(47)] public Unknown ObjFContainerPadI64As1 { get; set; }
    }
}