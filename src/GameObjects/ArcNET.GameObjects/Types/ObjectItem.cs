using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

/// <summary>Item fields, shared by Weapon, Ammo, Armor, Gold, Food, Scroll, Key, KeyRing, Written, Generic.</summary>
public class ObjectItem : ObjectCommon
{
    private int _itemPadI1Reserved;
    private int _itemPadIas1Reserved;
    private long _itemPadI64As1Reserved;

    public ItemFlags ItemFlags { get; internal set; }
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

    internal static ObjectItem ReadItem(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectItem();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
    }

    protected void ReadItemFields(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ItemFlags))
            ItemFlags = unchecked((ItemFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ItemParent))
            ItemParent = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ItemWeight))
            ItemWeight = reader.ReadInt32();
        if (Bit(ObjectField.ItemMagicWeightAdj))
            ItemMagicWeightAdj = reader.ReadInt32();
        if (Bit(ObjectField.ItemWorth))
            ItemWorth = reader.ReadInt32();
        if (Bit(ObjectField.ItemManaStore))
            ItemManaStore = reader.ReadInt32();
        if (Bit(ObjectField.ItemInvAid))
            ItemInvAid = reader.ReadInt32();
        if (Bit(ObjectField.ItemInvLocation))
            ItemInvLocation = reader.ReadInt32();
        if (Bit(ObjectField.ItemUseAidFragment))
            ItemUseAidFragment = reader.ReadInt32();
        if (Bit(ObjectField.ItemMagicTechComplexity))
            ItemMagicTechComplexity = reader.ReadInt32();
        if (Bit(ObjectField.ItemDiscipline))
            ItemDiscipline = reader.ReadInt32();
        if (Bit(ObjectField.ItemDescriptionUnknown))
            ItemDescriptionUnknown = reader.ReadInt32();
        if (Bit(ObjectField.ItemDescriptionEffects))
            ItemDescriptionEffects = reader.ReadInt32();
        if (Bit(ObjectField.ItemSpell1))
            ItemSpell1 = reader.ReadInt32();
        if (Bit(ObjectField.ItemSpell2))
            ItemSpell2 = reader.ReadInt32();
        if (Bit(ObjectField.ItemSpell3))
            ItemSpell3 = reader.ReadInt32();
        if (Bit(ObjectField.ItemSpell4))
            ItemSpell4 = reader.ReadInt32();
        if (Bit(ObjectField.ItemSpell5))
            ItemSpell5 = reader.ReadInt32();
        if (Bit(ObjectField.ItemSpellManaStore))
            ItemSpellManaStore = reader.ReadInt32();
        if (Bit(ObjectField.ItemAiAction))
            ItemAiAction = reader.ReadInt32();
        if (Bit(ObjectField.ItemPadI1))
            _itemPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ItemPadIas1))
            _itemPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ItemPadI64As1))
            _itemPadI64As1Reserved = reader.ReadInt64();
    }

    protected void WriteItemFields(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ItemFlags))
            writer.WriteInt32(unchecked((int)ItemFlags));
        if (Bit(ObjectField.ItemParent))
            ItemParent.Write(ref writer);
        if (Bit(ObjectField.ItemWeight))
            writer.WriteInt32(ItemWeight);
        if (Bit(ObjectField.ItemMagicWeightAdj))
            writer.WriteInt32(ItemMagicWeightAdj);
        if (Bit(ObjectField.ItemWorth))
            writer.WriteInt32(ItemWorth);
        if (Bit(ObjectField.ItemManaStore))
            writer.WriteInt32(ItemManaStore);
        if (Bit(ObjectField.ItemInvAid))
            writer.WriteInt32(ItemInvAid);
        if (Bit(ObjectField.ItemInvLocation))
            writer.WriteInt32(ItemInvLocation);
        if (Bit(ObjectField.ItemUseAidFragment))
            writer.WriteInt32(ItemUseAidFragment);
        if (Bit(ObjectField.ItemMagicTechComplexity))
            writer.WriteInt32(ItemMagicTechComplexity);
        if (Bit(ObjectField.ItemDiscipline))
            writer.WriteInt32(ItemDiscipline);
        if (Bit(ObjectField.ItemDescriptionUnknown))
            writer.WriteInt32(ItemDescriptionUnknown);
        if (Bit(ObjectField.ItemDescriptionEffects))
            writer.WriteInt32(ItemDescriptionEffects);
        if (Bit(ObjectField.ItemSpell1))
            writer.WriteInt32(ItemSpell1);
        if (Bit(ObjectField.ItemSpell2))
            writer.WriteInt32(ItemSpell2);
        if (Bit(ObjectField.ItemSpell3))
            writer.WriteInt32(ItemSpell3);
        if (Bit(ObjectField.ItemSpell4))
            writer.WriteInt32(ItemSpell4);
        if (Bit(ObjectField.ItemSpell5))
            writer.WriteInt32(ItemSpell5);
        if (Bit(ObjectField.ItemSpellManaStore))
            writer.WriteInt32(ItemSpellManaStore);
        if (Bit(ObjectField.ItemAiAction))
            writer.WriteInt32(ItemAiAction);
        if (Bit(ObjectField.ItemPadI1))
            writer.WriteInt32(_itemPadI1Reserved);
        if (Bit(ObjectField.ItemPadIas1))
            writer.WriteInt32(_itemPadIas1Reserved);
        if (Bit(ObjectField.ItemPadI64As1))
            writer.WriteInt64(_itemPadI64As1Reserved);
    }
}
