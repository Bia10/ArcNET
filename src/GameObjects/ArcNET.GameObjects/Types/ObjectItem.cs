using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

/// <summary>Item fields, shared by Weapon, Ammo, Armor, Gold, Food, Scroll, Key, KeyRing, Written, Generic.</summary>
public class ObjectItem : ObjectCommon
{
    public int ItemFlags { get; internal set; }
    public GameObjectGuid ItemParent { get; internal set; }
    public int ItemWeight { get; internal set; }
    public int ItemMagicWeightAdj { get; internal set; }
    public int ItemWorth { get; internal set; }
    public int ItemManaStore { get; internal set; }
    public int ItemInvAid { get; internal set; }
    public int ItemInvLocation { get; internal set; }
    public int ItemUseAidFragment { get; internal set; }
    public int ItemMagicTechComplexity { get; internal set; }
    public int ItemDiscipline { get; internal set; }
    public int ItemDescriptionUnknown { get; internal set; }
    public int ItemDescriptionEffects { get; internal set; }
    public int ItemSpell1 { get; internal set; }
    public int ItemSpell2 { get; internal set; }
    public int ItemSpell3 { get; internal set; }
    public int ItemSpell4 { get; internal set; }
    public int ItemSpell5 { get; internal set; }
    public int ItemSpellManaStore { get; internal set; }
    public int ItemAiAction { get; internal set; }
    public int ItemPadI1 { get; internal set; }
    public int ItemPadIas1 { get; internal set; }
    public long ItemPadI64As1 { get; internal set; }

    internal static ObjectItem ReadItem(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectItem();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        return obj;
    }

    protected void ReadItemFields(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;

        if (Bit(ObjectField.ObjFItemFlags))
            ItemFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemParent))
            ItemParent = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFItemWeight))
            ItemWeight = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemMagicWeightAdj))
            ItemMagicWeightAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemWorth))
            ItemWorth = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemManaStore))
            ItemManaStore = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemInvAid))
            ItemInvAid = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemInvLocation))
            ItemInvLocation = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemUseAidFragment))
            ItemUseAidFragment = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemMagicTechComplexity))
            ItemMagicTechComplexity = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemDiscipline))
            ItemDiscipline = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemDescriptionUnknown))
            ItemDescriptionUnknown = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemDescriptionEffects))
            ItemDescriptionEffects = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemSpell1))
            ItemSpell1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemSpell2))
            ItemSpell2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemSpell3))
            ItemSpell3 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemSpell4))
            ItemSpell4 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemSpell5))
            ItemSpell5 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemSpellManaStore))
            ItemSpellManaStore = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemAiAction))
            ItemAiAction = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemPadI1))
            ItemPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemPadIas1))
            ItemPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFItemPadI64As1))
            ItemPadI64As1 = reader.ReadInt64();
    }

    protected void WriteItemFields(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;

        if (Bit(ObjectField.ObjFItemFlags))
            writer.WriteInt32(ItemFlags);
        if (Bit(ObjectField.ObjFItemParent))
            ItemParent.Write(ref writer);
        if (Bit(ObjectField.ObjFItemWeight))
            writer.WriteInt32(ItemWeight);
        if (Bit(ObjectField.ObjFItemMagicWeightAdj))
            writer.WriteInt32(ItemMagicWeightAdj);
        if (Bit(ObjectField.ObjFItemWorth))
            writer.WriteInt32(ItemWorth);
        if (Bit(ObjectField.ObjFItemManaStore))
            writer.WriteInt32(ItemManaStore);
        if (Bit(ObjectField.ObjFItemInvAid))
            writer.WriteInt32(ItemInvAid);
        if (Bit(ObjectField.ObjFItemInvLocation))
            writer.WriteInt32(ItemInvLocation);
        if (Bit(ObjectField.ObjFItemUseAidFragment))
            writer.WriteInt32(ItemUseAidFragment);
        if (Bit(ObjectField.ObjFItemMagicTechComplexity))
            writer.WriteInt32(ItemMagicTechComplexity);
        if (Bit(ObjectField.ObjFItemDiscipline))
            writer.WriteInt32(ItemDiscipline);
        if (Bit(ObjectField.ObjFItemDescriptionUnknown))
            writer.WriteInt32(ItemDescriptionUnknown);
        if (Bit(ObjectField.ObjFItemDescriptionEffects))
            writer.WriteInt32(ItemDescriptionEffects);
        if (Bit(ObjectField.ObjFItemSpell1))
            writer.WriteInt32(ItemSpell1);
        if (Bit(ObjectField.ObjFItemSpell2))
            writer.WriteInt32(ItemSpell2);
        if (Bit(ObjectField.ObjFItemSpell3))
            writer.WriteInt32(ItemSpell3);
        if (Bit(ObjectField.ObjFItemSpell4))
            writer.WriteInt32(ItemSpell4);
        if (Bit(ObjectField.ObjFItemSpell5))
            writer.WriteInt32(ItemSpell5);
        if (Bit(ObjectField.ObjFItemSpellManaStore))
            writer.WriteInt32(ItemSpellManaStore);
        if (Bit(ObjectField.ObjFItemAiAction))
            writer.WriteInt32(ItemAiAction);
        if (Bit(ObjectField.ObjFItemPadI1))
            writer.WriteInt32(ItemPadI1);
        if (Bit(ObjectField.ObjFItemPadIas1))
            writer.WriteInt32(ItemPadIas1);
        if (Bit(ObjectField.ObjFItemPadI64As1))
            writer.WriteInt64(ItemPadI64As1);
    }
}
