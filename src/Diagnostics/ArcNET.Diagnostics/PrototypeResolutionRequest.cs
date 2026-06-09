namespace ArcNET.Diagnostics;

public sealed record class PrototypeResolutionRequest(AttachedSessionSnapshot Session, string PrototypeText);
