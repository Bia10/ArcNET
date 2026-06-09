namespace ArcNET.Diagnostics;

public sealed record PlayerSarTransitionSnapshot(
    int FromSlot,
    int ToSlot,
    int FromLevel,
    int ToLevel,
    int FromRawBytesLength,
    int ToRawBytesLength,
    bool IsDiscontinuous,
    IReadOnlyList<PlayerSarTransitionChangeSnapshot> Changes
);
