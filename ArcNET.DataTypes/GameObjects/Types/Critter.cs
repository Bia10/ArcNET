using ArcNET.DataTypes.Common;
using System;

namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Critter : Common
	{
		[Order(37)] public int ObjFCritterFlags { get; set; }
		[Order(38)] public int ObjFCritterFlags2 { get; set; }
		[Order(39)] public Tuple<int[], int[]> ObjFCritterStatBaseIdx { get; set; }
		[Order(40)] public Tuple<int[], int[]> ObjFCritterBasicSkillIdx { get; set; }
		[Order(41)] public Tuple<int[], int[]> ObjFCritterTechSkillIdx { get; set; }
		[Order(42)] public Tuple<int[], int[]> ObjFCritterSpellTechIdx { get; set; }
		[Order(43)] public int ObjFCritterFatiguePts { get; set; }
		[Order(44)] public int ObjFCritterFatigueAdj { get; set; }
		[Order(45)] public int ObjFCritterFatigueDamage { get; set; }
		[Order(46)] public int ObjFCritterCritHitChart { get; set; }
		[Order(47)] public Tuple<int[], int[]> ObjFCritterEffectsIdx { get; set; }
		[Order(48)] public Tuple<int[], int[]> ObjFCritterEffectCauseIdx { get; set; }
		[Order(49)] public GameObjectGuid ObjFCritterFleeingFrom { get; set; }
		[Order(50)] public int ObjFCritterPortrait { get; set; } //strinf aid
		[Order(51)] public GameObjectGuid ObjFCritterGold { get; set; }
		[Order(52)] public GameObjectGuid ObjFCritterArrows { get; set; }
		[Order(53)] public GameObjectGuid ObjFCritterBullets { get; set; }
		[Order(54)] public GameObjectGuid ObjFCritterPowerCells { get; set; }
		[Order(55)] public GameObjectGuid ObjFCritterFuel { get; set; }
		[Order(56)] public int ObjFCritterInventoryNum { get; set; }
		[Order(57)] public Tuple<GameObjectGuid[], int[]> ObjFCritterInventoryListIdx { get; set; }
		[Order(58)] public int ObjFCritterInventorySource { get; set; }
		[Order(59)] public int ObjFCritterDescriptionUnknown { get; set; }
		[Order(60)] public Tuple<GameObjectGuid[], int[]> ObjFCritterFollowerIdx { get; set; }
		[Order(61)] public Location ObjFCritterTeleportDest { get; set; }
		[Order(62)] public int ObjFCritterTeleportMap { get; set; }
		[Order(63)] public int ObjFCritterDeathTime { get; set; }
		[Order(64)] public int ObjFCritterAutoLevelScheme { get; set; }
        [Order(65)] public int ObjFCritterPadI1 { get; set; }
		[Order(66)] public int ObjFCritterPadI2 { get; set; }
		[Order(67)] public int ObjFCritterPadI3 { get; set; }
		[Order(68)] public Unknown ObjFCritterPadIas1 { get; set; }
		[Order(69)] public Unknown ObjFCritterPadI64As1 { get; set; }
	}
}