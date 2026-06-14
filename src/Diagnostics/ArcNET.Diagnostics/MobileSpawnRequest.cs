namespace ArcNET.Diagnostics;

public sealed record class MobileSpawnRequest(
    AttachedSessionSnapshot Session,
    string AnchorHandleToken,
    ulong PrototypeHandle,
    string TimeoutMillisecondsText
);
