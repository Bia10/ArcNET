namespace ArcNET.Diagnostics;

public sealed record PlayerQuestBookSnapshot(
    DateTimeOffset CapturedAt,
    string LeaderName,
    int LeaderLevel,
    QuestLabelCatalogSnapshot? QuestCatalog,
    PlayerCharacterSnapshot? Player,
    IReadOnlyList<PlayerCharacterSnapshot> QuestCharacters,
    IReadOnlyList<PlayerQuestEntrySnapshot> Quests,
    IReadOnlyList<PlayerReputationEntrySnapshot> Reputation,
    IReadOnlyList<int> Blessings,
    IReadOnlyList<int> Curses,
    IReadOnlyList<int> Schematics
);
