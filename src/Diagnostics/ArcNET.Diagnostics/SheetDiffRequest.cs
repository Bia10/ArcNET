namespace ArcNET.Diagnostics;

public sealed record class SheetDiffRequest(AttachedSessionSnapshot Session, string HandleToken, int DelayMilliseconds);
