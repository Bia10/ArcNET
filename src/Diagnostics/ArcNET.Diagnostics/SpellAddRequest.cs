using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class SpellAddRequest(
    AttachedSessionSnapshot Session,
    string TargetHandleToken,
    string SpellToken,
    string TimeoutMillisecondsText
);
