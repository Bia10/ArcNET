namespace ArcNET.Diagnostics;

public sealed record class SheetScanRequest(AttachedSessionSnapshot Session, string HandleToken);
