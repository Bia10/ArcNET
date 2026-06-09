using ArcNET.Formats;

namespace ArcNET.Diagnostics;

public sealed record SaveDynamicMobileEntrySnapshot(int Index, int Offset, MobData? Mob, string? ParseError);
