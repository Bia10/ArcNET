namespace ArcNET.Diagnostics;

public sealed record SaveCharacterSarRecordSnapshot(
    string SourcePath,
    bool HasCompleteData,
    string? Name,
    int Level,
    int RawBytesLength,
    int Gold,
    int Arrows,
    int Bullets,
    int PowerCells,
    int TotalKills,
    int PortraitIndex,
    int MaxFollowers,
    int HpDamage,
    int FatigueDamage,
    int QuestCount,
    int? QuestDataRawBytesLength,
    int? QuestBitsetWordCount,
    IReadOnlyList<int> QuestSlotIds,
    IReadOnlyList<int> Reputation,
    IReadOnlyList<int> Blessings,
    IReadOnlyList<int> Curses,
    IReadOnlyList<int> Schematics,
    IReadOnlyList<CharacterSarDumpEntrySnapshot> Sars
);
