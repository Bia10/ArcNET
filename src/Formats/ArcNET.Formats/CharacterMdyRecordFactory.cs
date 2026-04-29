namespace ArcNET.Formats;

internal static class CharacterMdyRecordFactory
{
    public static CharacterMdyRecord Create(
        byte[] rawBytes,
        int[] stats,
        int[] basicSkills,
        int[] techSkills,
        int[] spellTech,
        bool hasCompleteData,
        CharacterMdyRecordLayout layout
    )
    {
        return new CharacterMdyRecord
        {
            RawBytes = rawBytes,
            Stats = stats,
            BasicSkills = basicSkills,
            TechSkills = techSkills,
            SpellTech = spellTech,
            HasCompleteData = hasCompleteData,
            StatsDataOffset = layout.StatsDataOffset,
            BasicSkillsDataOffset = layout.BasicSkillsDataOffset,
            TechSkillsDataOffset = layout.TechSkillsDataOffset,
            SpellTechDataOffset = layout.SpellTechDataOffset,
            GoldDataOffset = layout.GoldDataOffset,
            ArrowsDataOffset = layout.ArrowsDataOffset,
            TotalKillsDataOffset = layout.TotalKillsDataOffset,
            BulletsDataOffset = layout.BulletsDataOffset,
            PowerCellsDataOffset = layout.PowerCellsDataOffset,
            PortraitDataOffset = layout.PortraitDataOffset,
            NameLengthOffset = layout.NameLengthOffset,
            PositionAiDataOffset = layout.PositionAiDataOffset,
            HpDamageDataOffset = layout.HpDamageDataOffset,
            FatigueDamageDataOffset = layout.FatigueDamageDataOffset,
            EffectsDataOffset = layout.EffectsDataOffset,
            EffectsElementCount = layout.EffectsElementCount,
            EffectCausesDataOffset = layout.EffectCausesDataOffset,
            EffectCausesElementCount = layout.EffectCausesElementCount,
            QuestDataOffset = layout.QuestDataOffset,
            QuestCount = layout.QuestCount,
            ReputationDataOffset = layout.ReputationDataOffset,
            RumorsDataOffset = layout.RumorsDataOffset,
            RumorsCount = layout.RumorsCount,
            BlessingProtoDataOffset = layout.BlessingProtoDataOffset,
            BlessingProtoElementCount = layout.BlessingProtoElementCount,
            BlessingTsDataOffset = layout.BlessingTsDataOffset,
            CurseProtoDataOffset = layout.CurseProtoDataOffset,
            CurseProtoElementCount = layout.CurseProtoElementCount,
            CurseTsDataOffset = layout.CurseTsDataOffset,
            SchematicsDataOffset = layout.SchematicsDataOffset,
            SchematicsElementCount = layout.SchematicsElementCount,
        };
    }
}
