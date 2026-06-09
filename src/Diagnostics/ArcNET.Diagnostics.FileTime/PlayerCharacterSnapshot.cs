namespace ArcNET.Diagnostics;

public sealed record PlayerCharacterSnapshot(
    string Path,
    string? Name,
    int Level,
    int Race,
    int RawBytesLength,
    bool HasCompleteData,
    bool IsSelectedPlayer,
    int QuestCount,
    int ReputationCount,
    int BlessingCount,
    int CurseCount,
    int SchematicsCount,
    int RumorsCount
);
