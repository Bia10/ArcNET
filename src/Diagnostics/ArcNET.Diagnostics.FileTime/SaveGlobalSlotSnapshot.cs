namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalSlotSnapshot(
    int Slot,
    string SlotStem,
    string LeaderName,
    int LeaderLevel,
    IReadOnlyDictionary<string, SaveGlobalFileSnapshot> Files,
    SaveTypedPlayerStateSnapshot? Player,
    SaveTownMapFogSnapshot TownMapFogs
);
