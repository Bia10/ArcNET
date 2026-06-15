using System.Globalization;
using ArcNET.GameObjects.Metadata;

namespace ArcNET.Diagnostics;

public static class SheetCatalog
{
    public static SheetReference ResolveReference(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var normalized = Normalize(token);

        if (DerivedStatAliases.TryGetValue(normalized, out var derivedStatId))
        {
            return new SheetReference(
                SheetRoute.DerivedStat,
                derivedStatId,
                CharacterSheetMetadata.StatName(derivedStatId)
            );
        }

        if (BasicSkillAliases.TryGetValue(normalized, out var basicSkillId))
            return new(SheetRoute.BasicSkill, basicSkillId, CharacterSheetMetadata.BasicSkillName(basicSkillId));

        if (TechSkillAliases.TryGetValue(normalized, out var techSkillId))
            return new(SheetRoute.TechSkill, techSkillId, CharacterSheetMetadata.TechSkillName(techSkillId));

        for (var index = 0; index < SpellCollegeCount; index++)
        {
            var collegeName = CharacterSheetMetadata.SpellCollegeName(index);
            if (Normalize(collegeName) == normalized)
                return new(SheetRoute.SpellCollege, index, collegeName);
        }

        if (SpellMasteryAliases.TryGetValue(normalized, out _))
            return new(SheetRoute.SpellMastery, SpellCollegeCount, "Spell Mastery");

        if (TechDisciplineAliases.TryGetValue(normalized, out var disciplineId))
            return new(
                SheetRoute.TechDiscipline,
                disciplineId,
                CharacterSheetMetadata.TechDisciplineName(disciplineId)
            );

        if (ResistanceAliases.TryGetValue(normalized, out var resistanceId))
            return new(SheetRoute.Resistance, resistanceId, CharacterSheetMetadata.ResistanceName(resistanceId));

        for (var index = 0; index < RuntimeStatCount; index++)
        {
            if (Normalize(RuntimeSemanticCatalog.StatName(index)) != normalized)
                continue;

            return index >= DerivedStatBaseIndex && index < DerivedStatBaseIndex + DerivedStatCount
                ? new(SheetRoute.DerivedStat, index, CharacterSheetMetadata.StatName(index))
                : new(SheetRoute.Stat, index, RuntimeSemanticCatalog.StatName(index));
        }

        if (StatAliases.TryGetValue(normalized, out var statId))
        {
            return statId >= DerivedStatBaseIndex && statId < DerivedStatBaseIndex + DerivedStatCount
                ? new(SheetRoute.DerivedStat, statId, CharacterSheetMetadata.StatName(statId))
                : new(SheetRoute.Stat, statId, RuntimeSemanticCatalog.StatName(statId));
        }

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId))
        {
            return numericId >= DerivedStatBaseIndex && numericId < DerivedStatBaseIndex + DerivedStatCount
                ? new(SheetRoute.DerivedStat, numericId, CharacterSheetMetadata.StatName(numericId))
                : new(SheetRoute.Stat, numericId, RuntimeSemanticCatalog.StatName(numericId));
        }

        throw new InvalidOperationException(
            $"Unknown sheet label '{token}'. Examples: strength, level, alignment, haggle, repair, herbology, speed, spell-mastery, max-followers."
        );
    }

    private static string Normalize(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (!char.IsLetterOrDigit(ch))
                continue;

            buffer[count++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..count]);
    }

    private const int SpellCollegeCount = 16;
    private const int RuntimeStatCount = 28;
    private const int DerivedStatBaseIndex = 8;
    private const int DerivedStatCount = 9;

    private static readonly Dictionary<string, int> BasicSkillAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bow"] = 0,
        ["dodge"] = 1,
        ["melee"] = 2,
        ["throw"] = 3,
        ["backstab"] = 4,
        ["pickpocket"] = 5,
        ["pickpocketing"] = 5,
        ["prowling"] = 6,
        ["prowl"] = 6,
        ["spottrap"] = 7,
        ["spottraps"] = 7,
        ["gambling"] = 8,
        ["gamble"] = 8,
        ["haggle"] = 9,
        ["haggling"] = 9,
        ["heal"] = 10,
        ["persuasion"] = 11,
        ["persuade"] = 11,
    };

    private static readonly Dictionary<string, int> TechSkillAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["repair"] = 0,
        ["firearms"] = 1,
        ["firearm"] = 1,
        ["picklocks"] = 2,
        ["picklock"] = 2,
        ["disarmtraps"] = 3,
        ["disarmtrap"] = 3,
    };

    private static readonly Dictionary<string, int> StatAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["level"] = 17,
        ["xp"] = 18,
        ["experience"] = 18,
        ["experiencepoints"] = 18,
        ["alignment"] = 19,
        ["align"] = 19,
        ["fate"] = 20,
        ["skillpoints"] = 21,
        ["unspentpoints"] = 21,
        ["unspent"] = 21,
        ["mp"] = 22,
        ["magick"] = 22,
        ["magickpoints"] = 22,
        ["tp"] = 23,
        ["tech"] = 23,
        ["techpoints"] = 23,
        ["ac"] = 10,
    };

    private static readonly Dictionary<string, int> DerivedStatAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["carryweight"] = 8,
        ["carry"] = 8,
        ["damagebonus"] = 9,
        ["dmgbonus"] = 9,
        ["acadjustment"] = 10,
        ["acadj"] = 10,
        ["speed"] = 11,
        ["healrate"] = 12,
        ["poisonrecovery"] = 13,
        ["reactionmodifier"] = 14,
        ["reactionmod"] = 14,
        ["maxfollowers"] = 15,
        ["followers"] = 15,
        ["magicktechaptitude"] = 16,
        ["magickaptitude"] = 16,
        ["techaptitude"] = 16,
    };

    private static readonly Dictionary<string, int> ResistanceAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["normalresistance"] = 0,
        ["physicalresistance"] = 0,
        ["fireresistance"] = 1,
        ["electricalresistance"] = 2,
        ["electricresistance"] = 2,
        ["poisonresistance"] = 3,
        ["magicresistance"] = 4,
    };

    private static readonly Dictionary<string, int> TechDisciplineAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["herbology"] = 0,
        ["chemistry"] = 1,
        ["electric"] = 2,
        ["explosives"] = 3,
        ["gunsmithy"] = 4,
        ["gunsmith"] = 4,
        ["mechanical"] = 5,
        ["smithy"] = 6,
        ["therapeutics"] = 7,
        ["therapeutic"] = 7,
    };

    private static readonly Dictionary<string, int> SpellMasteryAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["spellmastery"] = SpellCollegeCount,
        ["mastery"] = SpellCollegeCount,
        ["spellfocus"] = SpellCollegeCount,
        ["spellcollegemastery"] = SpellCollegeCount,
    };
}
