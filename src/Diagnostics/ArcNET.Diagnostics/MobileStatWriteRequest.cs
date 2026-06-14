namespace ArcNET.Diagnostics;

public sealed record class MobileStatWriteRequest(
    AttachedSessionSnapshot Session,
    string TargetHandleToken,
    string StatToken,
    string ValueText,
    string TimeoutMillisecondsText
);
