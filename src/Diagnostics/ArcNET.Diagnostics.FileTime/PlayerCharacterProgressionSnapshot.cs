namespace ArcNET.Diagnostics;

public sealed record PlayerCharacterProgressionSnapshot(
    string? Name,
    int Level,
    int ExperiencePoints,
    int Alignment,
    int FatePoints,
    int Race,
    int Gender,
    int Age,
    int PoisonLevel,
    int UnspentPoints,
    int MagickPoints,
    int TechPoints,
    int Gold,
    int Arrows,
    int TotalKills,
    int Bullets,
    int PowerCells,
    int HpDamage,
    int FatigueDamage
);
