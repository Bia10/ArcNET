using ArcNET.Formats;

namespace ArcNET.Editor;

internal static class CharacterRecordMapper
{
    public static CharacterRecord From(CharacterMdyRecord rec) =>
        CharacterRecord.FromArrays(
            rec.Stats,
            rec.BasicSkills,
            rec.TechSkills,
            rec.SpellTech,
            rec.HasCompleteData,
            rec.Gold,
            rec.Arrows,
            rec.TotalKills,
            rec.PortraitIndex,
            rec.Name,
            rec.PositionAiRaw,
            rec.HpDamageRaw,
            rec.FatigueDamageRaw,
            rec.Bullets,
            rec.PowerCells,
            rec.QuestCount,
            rec.QuestDataRaw,
            rec.QuestBitsetRaw,
            rec.ReputationRaw,
            rec.BlessingRaw,
            rec.BlessingTsRaw,
            rec.CurseRaw,
            rec.CurseTsRaw,
            rec.SchematicsRaw,
            rec.RumorsCount,
            rec.RumorsRaw
        );

    public static CharacterMdyRecord ApplyTo(CharacterRecord character, CharacterMdyRecord original)
    {
        var rec = original
            .WithStats(character.ToStatArray())
            .WithBasicSkills(character.ToBasicSkillArray())
            .WithTechSkills(character.ToTechSkillArray())
            .WithSpellTech(character.ToSpellTechArray())
            .WithGold(character.Gold)
            .WithArrows(character.Arrows)
            .WithTotalKills(character.TotalKills)
            .WithPortraitIndex(
                character.PortraitIndex >= 0 ? character.PortraitIndex
                : original.PortraitIndex >= 0 ? original.PortraitIndex
                : 0
            )
            .WithPositionAi(character.PositionAiRaw ?? original.PositionAiRaw ?? [0, 0, 0])
            .WithHpDamage(character.HpDamageRaw ?? original.HpDamageRaw ?? [0, 0, 0, 0])
            .WithFatigueDamage(character.FatigueDamageRaw ?? original.FatigueDamageRaw ?? [0, 0, 0, 0])
            .WithName(character.Name ?? original.Name)
            .WithBullets(character.Bullets)
            .WithPowerCells(character.PowerCells);

        if (character.QuestDataRaw is { } questData)
        {
            if (character.QuestBitsetRaw is { } questBitset)
                rec = rec.WithQuestStateRaw(questData, questBitset);
            else
                rec = rec.WithQuestDataRaw(questData);
        }

        if (character.ReputationRaw is { } reputation)
            rec = rec.WithReputationRaw(reputation);
        if (character.BlessingRaw is { } blessings)
            rec = rec.WithBlessingRaw(blessings);
        if (character.CurseRaw is { } curses)
            rec = rec.WithCurseRaw(curses);
        if (character.SchematicsRaw is { } schematics)
            rec = rec.WithSchematicsRaw(schematics);
        if (character.RumorsRaw is { } rumors)
            rec = rec.WithRumorsRaw(rumors);

        return rec;
    }
}
