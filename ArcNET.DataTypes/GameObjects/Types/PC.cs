using System;
using ArcNET.DataTypes.Common;

namespace ArcNET.DataTypes.GameObjects.Types
{
    public class Pc : Critter
    {
        [Order(70)] public int ObjFPcFlags { get; set; }
        [Order(71)] public int ObjFPcFlagsFate { get; set; }
        [Order(72)] public Tuple<int[], int[]> ObjFPcReputationIdx { get; set; }
        [Order(73)] public Tuple<int[], int[]> ObjFPcReputationTsIdx { get; set; }
        [Order(74)] public int ObjFPcBackground { get; set; }
        [Order(75)] public int ObjFPcBackgroundText { get; set; }
        [Order(76)] public Tuple<int[], int[]> ObjFPcQuestIdx { get; set; }
        [Order(77)] public Tuple<int[], int[]> ObjFPcBlessingIdx { get; set; }
        [Order(78)] public Tuple<int[], int[]> ObjFPcBlessingTsIdx { get; set; }
        [Order(79)] public Tuple<int[], int[]> ObjFPcCurseIdx { get; set; }
        [Order(80)] public Tuple<int[], int[]> ObjFPcCurseTsIdx { get; set; }
        [Order(81)] public int ObjFPcPartyId { get; set; }
        [Order(82)] public Tuple<int[], int[]> ObjFPcRumorIdx { get; set; }
        [Order(83)] public Unknown ObjFPcPadIas2 { get; set; }
        [Order(84)] public Tuple<int[], int[]> ObjFPcSchematicsFoundIdx { get; set; }
        [Order(85)] public Tuple<int[], int[]> ObjFPcLogbookEgoIdx { get; set; }
        [Order(86)] public int ObjFPcFogMask { get; set; }
        [Order(87)] public PrefixedString ObjFPcPlayerName { get; set; }
        [Order(88)] public int ObjFPcBankMoney { get; set; }
        [Order(89)] public Tuple<int[], int[]> ObjFPcGlobalFlags { get; set; }
        [Order(90)] public Tuple<int[], int[]> ObjFPcGlobalVariables { get; set; }
        [Order(91)] public int ObjFPcPadI1 { get; set; }
        [Order(92)] public int ObjFPcPadI2 { get; set; }
        [Order(93)] public Unknown ObjFPcPadIas1 { get; set; }
        [Order(94)] public Unknown ObjFPcPadI64As1 { get; set; }
        [Order(95)] public Unknown ObjFPcPadI64As2 { get; set; }
    }
}