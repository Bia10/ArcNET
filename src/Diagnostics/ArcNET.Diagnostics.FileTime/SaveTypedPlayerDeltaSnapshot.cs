namespace ArcNET.Diagnostics;

public sealed record SaveTypedPlayerDeltaSnapshot(
    SaveTypedPlayerDeltaKind Kind,
    int QuestDelta,
    int RumorsDelta,
    int BlessingsDelta,
    int CursesDelta,
    int SchematicsDelta,
    SaveTypedReputationDeltaSnapshot Reputation
);
