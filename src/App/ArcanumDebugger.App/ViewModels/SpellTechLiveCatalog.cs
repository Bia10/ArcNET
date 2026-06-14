using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;

namespace ArcanumDebugger.App.ViewModels;

public enum SpellTechLiveEntryKind
{
    SpellCollege,
    TechDiscipline,
    TechSkill,
}

public sealed record class DebuggerSpellTechLiveEntry(
    SpellTechLiveEntryKind Kind,
    string EntryKey,
    string CategoryText,
    string TitleText,
    string ValueText,
    string DetailText,
    string TokenText,
    string NumericValueText
);

public sealed record class DebuggerSpellTechSchematicEntry(
    string EntryKey,
    int SlotIndex,
    int SchematicId,
    string TitleText,
    string SubtitleText,
    string DetailText,
    string ValueText
);

public sealed record class DebuggerSpellTechKnownSpellEntry(
    string EntryKey,
    int SpellId,
    int CollegeId,
    int SpellLevel,
    string TitleText,
    string SubtitleText,
    string DetailText,
    string TokenText
);

public static class SpellTechLiveCatalog
{
    public static IReadOnlyList<DebuggerSpellTechLiveEntry> CreateProgressionEntries(SheetDataSnapshot data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return
        [
            .. data.SpellColleges.Select(CreateSpellCollegeEntry),
            .. data.TechDisciplines.Select(CreateTechDisciplineEntry),
            .. data.TechSkills.Select(CreateTechSkillEntry),
        ];
    }

    public static IReadOnlyList<DebuggerSpellTechKnownSpellEntry> CreateKnownSpellEntries(SheetDataSnapshot data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var spellLevelsByCollegeId = data.SpellColleges.ToDictionary(
            static college => college.Id,
            static college => college.Value
        );
        return
        [
            .. SpellTechCatalog
                .EnumerateSpells()
                .Where(spell =>
                    spellLevelsByCollegeId.TryGetValue(spell.CollegeId, out var knownLevel) && knownLevel >= spell.Level
                )
                .Select(CreateKnownSpellEntry),
        ];
    }

    public static IReadOnlyList<DebuggerSpellTechSchematicEntry> CreateSchematicEntries(
        IReadOnlyList<int> schematicIds,
        IReadOnlyList<PrototypePaletteEntry> prototypeEntries
    )
    {
        ArgumentNullException.ThrowIfNull(schematicIds);
        ArgumentNullException.ThrowIfNull(prototypeEntries);

        var prototypesByNumber = prototypeEntries
            .GroupBy(static entry => entry.ProtoNumber)
            .ToDictionary(static group => group.Key, static group => group.First());
        return
        [
            .. schematicIds.Select(
                (schematicId, slotIndex) =>
                    CreateSchematicEntry(
                        slotIndex,
                        schematicId,
                        prototypesByNumber.TryGetValue(schematicId, out var prototype) ? prototype : null
                    )
            ),
        ];
    }

    private static DebuggerSpellTechLiveEntry CreateSpellCollegeEntry(SheetScalarSnapshot college)
    {
        var tokenText = SpellTechCatalog.SpellCollegeName(college.Id);
        return new DebuggerSpellTechLiveEntry(
            SpellTechLiveEntryKind.SpellCollege,
            $"college:{college.Id.ToString(CultureInfo.InvariantCulture)}",
            "Spell College",
            college.Name,
            $"Rank {college.Value.ToString(CultureInfo.InvariantCulture)}",
            DescribeKnownSpells(college.Id, college.Value),
            tokenText,
            college.Value.ToString(CultureInfo.InvariantCulture)
        );
    }

    private static DebuggerSpellTechLiveEntry CreateTechDisciplineEntry(SheetScalarSnapshot discipline) =>
        new(
            SpellTechLiveEntryKind.TechDiscipline,
            $"discipline:{discipline.Id.ToString(CultureInfo.InvariantCulture)}",
            "Tech Discipline",
            discipline.Name,
            $"Degree {discipline.Value.ToString(CultureInfo.InvariantCulture)}",
            discipline.Value == 0
                ? "No live degree invested in this discipline yet."
                : "Current live technology discipline degree from the selected target.",
            discipline.Name,
            discipline.Value.ToString(CultureInfo.InvariantCulture)
        );

    private static DebuggerSpellTechLiveEntry CreateTechSkillEntry(SheetSkillSnapshot skill) =>
        new(
            SpellTechLiveEntryKind.TechSkill,
            $"skill:{skill.Id.ToString(CultureInfo.InvariantCulture)}",
            "Tech Skill",
            skill.Name,
            $"{skill.Value.ToString(CultureInfo.InvariantCulture)} point(s)",
            $"{skill.TrainingName} training · encoded {skill.Encoded.ToString(CultureInfo.InvariantCulture)}",
            skill.Name,
            skill.Value.ToString(CultureInfo.InvariantCulture)
        );

    private static DebuggerSpellTechKnownSpellEntry CreateKnownSpellEntry(SpellDescriptor spell) =>
        new(
            $"spell:{spell.Id.ToString(CultureInfo.InvariantCulture)}",
            spell.Id,
            spell.CollegeId,
            spell.Level,
            spell.Name,
            $"{spell.CollegeName} · rank {spell.Level.ToString(CultureInfo.InvariantCulture)}",
            $"Known live spell from the selected target's {spell.CollegeName} college.",
            spell.Name
        );

    private static DebuggerSpellTechSchematicEntry CreateSchematicEntry(
        int slotIndex,
        int schematicId,
        PrototypePaletteEntry? prototype
    )
    {
        List<string> detailParts = [];
        if (!string.IsNullOrWhiteSpace(prototype?.Description))
            detailParts.Add(prototype.Description);

        if (!string.IsNullOrWhiteSpace(prototype?.AssetPath))
            detailParts.Add(prototype.AssetPath);

        if (!string.IsNullOrWhiteSpace(prototype?.ArtAssetPath))
            detailParts.Add(prototype.ArtAssetPath);

        return new DebuggerSpellTechSchematicEntry(
            $"schematic:{slotIndex.ToString(CultureInfo.InvariantCulture)}:{schematicId.ToString(CultureInfo.InvariantCulture)}",
            slotIndex,
            schematicId,
            prototype?.DisplayName
                ?? prototype?.AssetPath
                ?? $"Schematic {schematicId.ToString(CultureInfo.InvariantCulture)}",
            $"Slot {slotIndex.ToString(CultureInfo.InvariantCulture)} · id {schematicId.ToString(CultureInfo.InvariantCulture)}",
            detailParts.Count == 0
                ? "Known discovered schematic id from the live runtime."
                : string.Join(" · ", detailParts),
            schematicId.ToString(CultureInfo.InvariantCulture)
        );
    }

    private static string DescribeKnownSpells(int collegeId, int level)
    {
        if (level <= 0)
            return "No known spells in this college yet.";

        var spells = SpellTechCatalog
            .EnumerateSpells()
            .Where(spell => spell.CollegeId == collegeId && spell.Level <= level)
            .Select(static spell => spell.Name)
            .ToArray();
        return spells.Length == 0
            ? "No known spells in this college yet."
            : $"Known spells: {string.Join(", ", spells)}";
    }
}
