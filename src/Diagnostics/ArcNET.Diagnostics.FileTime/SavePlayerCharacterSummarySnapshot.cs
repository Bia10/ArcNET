namespace ArcNET.Diagnostics;

public sealed record SavePlayerCharacterSummarySnapshot(
    DateTimeOffset CapturedAt,
    string LeaderName,
    int LeaderLevel,
    string Path,
    bool HasCompleteData,
    IReadOnlyList<PlayerIndexedValueSnapshot> PrimaryAttributes,
    IReadOnlyList<PlayerIndexedValueSnapshot> DerivedStats,
    PlayerCharacterProgressionSnapshot Progression,
    IReadOnlyList<PlayerIndexedValueSnapshot> BasicSkills,
    IReadOnlyList<PlayerIndexedValueSnapshot> TechSkills,
    IReadOnlyList<PlayerIndexedValueSnapshot> SpellColleges,
    IReadOnlyList<PlayerIndexedValueSnapshot> TechDisciplines,
    SaveQuestLogSummarySnapshot QuestLog,
    IReadOnlyList<PlayerReputationEntrySnapshot> Reputation,
    IReadOnlyList<int> Blessings,
    IReadOnlyList<int> Curses,
    IReadOnlyList<int> Schematics,
    SaveRumorSummarySnapshot Rumors
);
