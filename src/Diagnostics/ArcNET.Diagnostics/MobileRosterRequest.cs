namespace ArcNET.Diagnostics;

public sealed record class MobileRosterRequest(AttachedSessionSnapshot Session, int MaxEntries);
