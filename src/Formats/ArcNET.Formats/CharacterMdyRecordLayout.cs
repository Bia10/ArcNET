namespace ArcNET.Formats;

internal sealed class CharacterMdyRecordLayout
{
    public int StatsDataOffset { get; set; }
    public int BasicSkillsDataOffset { get; set; } = -1;
    public int TechSkillsDataOffset { get; set; } = -1;
    public int SpellTechDataOffset { get; set; } = -1;
    public int GoldDataOffset { get; set; } = -1;
    public int ArrowsDataOffset { get; set; } = -1;
    public int TotalKillsDataOffset { get; set; } = -1;
    public int BulletsDataOffset { get; set; } = -1;
    public int PowerCellsDataOffset { get; set; } = -1;
    public int PortraitDataOffset { get; set; } = -1;
    public int NameLengthOffset { get; set; } = -1;
    public int PositionAiDataOffset { get; set; } = -1;
    public int HpDamageDataOffset { get; set; } = -1;
    public int FatigueDamageDataOffset { get; set; } = -1;
    public int EffectsDataOffset { get; set; } = -1;
    public int EffectsElementCount { get; set; }
    public int EffectCausesDataOffset { get; set; } = -1;
    public int EffectCausesElementCount { get; set; }
    public int QuestDataOffset { get; set; } = -1;
    public int QuestCount { get; set; }
    public int ReputationDataOffset { get; set; } = -1;
    public int RumorsDataOffset { get; set; } = -1;
    public int RumorsCount { get; set; }
    public int BlessingProtoDataOffset { get; set; } = -1;
    public int BlessingProtoElementCount { get; set; }
    public int BlessingTsDataOffset { get; set; } = -1;
    public int CurseProtoDataOffset { get; set; } = -1;
    public int CurseProtoElementCount { get; set; }
    public int CurseTsDataOffset { get; set; } = -1;
    public int SchematicsDataOffset { get; set; } = -1;
    public int SchematicsElementCount { get; set; }
}
