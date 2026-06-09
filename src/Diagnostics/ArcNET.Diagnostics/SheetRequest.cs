namespace ArcNET.Diagnostics;

public sealed record class SheetRequest(AttachedSessionSnapshot Session, string HandleToken, string SheetLabel);
