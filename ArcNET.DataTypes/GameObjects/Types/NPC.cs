using ArcNET.DataTypes.Common;
using System;

namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Npc : Critter
    {
        [Order(70)] public int ObjFNpcFlags { get; set; }
        [Order(71)] public GameObjectGuid ObjFNpcLeader { get; set; }
        [Order(72)] public int ObjFNpcAiData { get; set; }
        [Order(73)] public GameObjectGuid ObjFNpcCombatFocus { get; set; }
        [Order(74)] public GameObjectGuid ObjFNpcWhoHitMeLast { get; set; }
        [Order(75)] public int ObjFNpcExperienceWorth { get; set; }
        [Order(76)] public int ObjFNpcExperiencePool { get; set; }
        [Order(77)] public Tuple<Location[], int[]> ObjFNpcWaypointsIdx { get; set; }
        [Order(78)] public int ObjFNpcWaypointCurrent { get; set; }
        [Order(79)] public Location ObjFNpcStandpointDay { get; set; }
        [Order(80)] public Location ObjFNpcStandpointNight { get; set; }
        [Order(81)] public int ObjFNpcOrigin { get; set; }
        [Order(82)] public int ObjFNpcFaction { get; set; }
        [Order(83)] public int ObjFNpcRetailPriceMultiplier { get; set; }
        [Order(84)] public GameObjectGuid ObjFNpcSubstituteInventory { get; set; }
        [Order(85)] public int ObjFNpcReactionBase { get; set; }
        [Order(86)] public int ObjFNpcSocialClass { get; set; }
        [Order(87)] public Tuple<int[], int[]> ObjFNpcReactionPcIdx { get; set; }
        [Order(88)] public Tuple<int[], int[]> ObjFNpcReactionLevelIdx { get; set; }
        [Order(89)] public Tuple<int[], int[]> ObjFNpcReactionTimeIdx { get; set; }
        [Order(90)] public int ObjFNpcWait { get; set; }
        [Order(91)] public int ObjFNpcGeneratorData { get; set; }
        [Order(92)] public int ObjFNpcPadI1 { get; set; }
        [Order(93)] public Tuple<int[], int[]> ObjFNpcDamageIdx { get; set; }
        [Order(94)] public Tuple<int[], int[]> ObjFNpcShitListIdx { get; set; }
    }
}