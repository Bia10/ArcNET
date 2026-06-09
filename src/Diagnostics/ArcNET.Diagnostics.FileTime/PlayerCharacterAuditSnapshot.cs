namespace ArcNET.Diagnostics;

public sealed record PlayerCharacterAuditSnapshot(
    string SourcePath,
    int RecordSize,
    bool HasCompleteData,
    string? Name,
    int Level,
    int Gold,
    int Arrows,
    int Bullets,
    int PowerCells,
    int TotalKills,
    int QuestCount,
    int RumorsCount,
    int BlessingCount,
    int CurseCount,
    int SchematicsCount,
    int ReputationCount,
    int EffectsCount,
    int? PositionAid,
    int? PositionLocation,
    int? PositionOffsetX,
    int HpDamage,
    int FatigueDamage,
    IReadOnlyList<CharacterSarAuditSnapshot> Sars
);
