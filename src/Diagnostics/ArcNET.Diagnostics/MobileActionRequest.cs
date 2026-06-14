namespace ArcNET.Diagnostics;

public sealed record class MobileActionRequest(
    AttachedSessionSnapshot Session,
    string TargetHandleToken,
    string TimeoutMillisecondsText
);
