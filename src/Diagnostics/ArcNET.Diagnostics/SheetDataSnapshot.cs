namespace ArcNET.Diagnostics;

public sealed record class SheetDataSnapshot(
    IReadOnlyList<SheetScalarSnapshot> PrimaryStats,
    IReadOnlyList<SheetScalarSnapshot> Progression,
    IReadOnlyList<SheetScalarSnapshot> DerivedStats,
    IReadOnlyList<SheetScalarSnapshot> Resistances,
    IReadOnlyList<SheetSkillSnapshot> BasicSkills,
    IReadOnlyList<SheetSkillSnapshot> TechSkills,
    IReadOnlyList<SheetScalarSnapshot> SpellColleges,
    SheetScalarSnapshot SpellMastery,
    IReadOnlyList<SheetScalarSnapshot> TechDisciplines
);
