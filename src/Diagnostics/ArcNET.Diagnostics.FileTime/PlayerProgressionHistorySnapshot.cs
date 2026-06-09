namespace ArcNET.Diagnostics;

public sealed record PlayerProgressionHistorySnapshot(
    int FirstSlot,
    int LastSlot,
    QuestLabelCatalogSnapshot? QuestCatalog,
    IReadOnlyList<PlayerProgressionSlotSnapshot> Slots
);
