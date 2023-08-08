namespace ArcNET.DataTypes.GameObjects.Types;

public class Item : Common
{
    [Order(37)] public int ObjFItemFlags { get; set; }
    [Order(38)] public GameObjectGuid ObjFItemParent { get; set; }
    [Order(39)] public int ObjFItemWeight { get; set; }
    [Order(40)] public int ObjFItemMagicWeightAdj { get; set; }
    [Order(41)] public int ObjFItemWorth { get; set; }
    [Order(42)] public int ObjFItemManaStore { get; set; }
    [Order(43)] public int ObjFItemInvAid { get; set; } //string aid
    [Order(44)] public int ObjFItemInvLocation { get; set; }
    [Order(45)] public int ObjFItemUseAidFragment { get; set; } //string aid
    [Order(46)] public int ObjFItemMagicTechComplexity { get; set; }
    [Order(47)] public int ObjFItemDiscipline { get; set; }
    [Order(48)] public int ObjFItemDescriptionUnknown { get; set; }
    [Order(49)] public int ObjFItemDescriptionEffects { get; set; }
    [Order(50)] public int ObjFItemSpell1 { get; set; }
    [Order(51)] public int ObjFItemSpell2 { get; set; }
    [Order(52)] public int ObjFItemSpell3 { get; set; }
    [Order(53)] public int ObjFItemSpell4 { get; set; }
    [Order(54)] public int ObjFItemSpell5 { get; set; }
    [Order(55)] public int ObjFItemSpellManaStore { get; set; }
    [Order(56)] public int ObjFItemAiAction { get; set; }
    [Order(57)] public int ObjFItemPadI1 { get; set; }
    [Order(58)] public Unknown ObjFItemPadIas1 { get; set; }
    [Order(59)] public Unknown ObjFItemPadI64As1 { get; set; }
}