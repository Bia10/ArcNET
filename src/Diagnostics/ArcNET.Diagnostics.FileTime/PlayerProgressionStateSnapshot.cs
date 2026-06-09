namespace ArcNET.Diagnostics;

public sealed record PlayerProgressionStateSnapshot(
    string Path,
    string? Name,
    int Level,
    int Xp,
    int Alignment,
    int Fate,
    int MagicPoints,
    int TechPoints,
    int Gold,
    int QuestCount,
    int RumorsCount,
    int BlessingCount,
    int CurseCount,
    int SchematicsCount,
    int HpDamage,
    int FatigueDamage,
    int Bullets,
    int PowerCells,
    IReadOnlyList<PlayerQuestEntrySnapshot> Quests,
    IReadOnlyList<PlayerReputationEntrySnapshot> Reputation,
    IReadOnlyList<PlayerIndexedValueSnapshot> BaseStats,
    IReadOnlyList<PlayerIndexedValueSnapshot> BasicSkills,
    IReadOnlyList<PlayerIndexedValueSnapshot> SpellTech
);
