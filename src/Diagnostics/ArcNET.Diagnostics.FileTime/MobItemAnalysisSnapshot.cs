using ArcNET.GameObjects;

namespace ArcNET.Diagnostics;

public sealed record MobItemAnalysisSnapshot(
    ObjectType ObjectType,
    int? Weight,
    int? Worth,
    int? ItemFlags,
    IReadOnlyList<string> ItemFlagNames,
    int? Discipline,
    string? DisciplineLabel,
    int? Complexity,
    IReadOnlyList<MobItemSpellEffectSnapshot> SpellEffects,
    MobItemSpecificAnalysisSnapshot? Specific
);
