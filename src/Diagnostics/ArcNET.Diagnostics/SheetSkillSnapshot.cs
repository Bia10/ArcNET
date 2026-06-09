namespace ArcNET.Diagnostics;

public readonly record struct SheetSkillSnapshot(
    int Id,
    string Name,
    int Value,
    int Training,
    string TrainingName,
    int Encoded
);
