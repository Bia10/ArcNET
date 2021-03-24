namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Scenery : Common
    {
		[Order(37)] public int ObjFSceneryFlags { get; set; }
        [Order(38)] public GameObjectGuid ObjFSceneryWhosInMe { get; set; }
        [Order(39)] public int ObjFSceneryRespawnDelay { get; set; }
        [Order(40)] public int ObjFSceneryPadI2 { get; set; }

        [Order(41)] public Unknown ObjFSceneryPadIas1 { get; set; } //byte
        [Order(42)] public Unknown ObjFSceneryPadI64As1 { get; set; } //byte
	}
}