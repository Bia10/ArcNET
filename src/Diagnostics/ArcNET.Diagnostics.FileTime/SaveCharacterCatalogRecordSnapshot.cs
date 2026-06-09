namespace ArcNET.Diagnostics;

public sealed record SaveCharacterCatalogRecordSnapshot(
    string SourcePath,
    int RecordIndex,
    bool HasCompleteData,
    string? Name,
    int Level,
    int ExperiencePoints,
    int Alignment,
    int RaceId,
    string RaceName,
    int GenderId,
    string GenderName,
    int MagickPoints,
    int TechPoints,
    int Gold,
    int Bullets,
    int PowerCells,
    int HpDamage,
    int FatigueDamage,
    int RawBytesLength,
    IReadOnlyList<PlayerIndexedValueSnapshot> NonZeroBasicSkills
);
