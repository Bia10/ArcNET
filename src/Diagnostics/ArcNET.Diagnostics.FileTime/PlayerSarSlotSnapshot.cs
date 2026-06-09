namespace ArcNET.Diagnostics;

public sealed record PlayerSarSlotSnapshot(
    int Slot,
    int Level,
    int RawBytesLength,
    IReadOnlyList<CharacterSarEntrySnapshot> Sars,
    string SaveName
);
