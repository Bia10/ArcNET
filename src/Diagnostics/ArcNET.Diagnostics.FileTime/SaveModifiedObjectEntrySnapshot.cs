using ArcNET.Formats;

namespace ArcNET.Diagnostics;

public sealed record SaveModifiedObjectEntrySnapshot(
    int Index,
    string FileObjectId,
    MobData? Mob,
    string? ParseError,
    string? Warning
);
