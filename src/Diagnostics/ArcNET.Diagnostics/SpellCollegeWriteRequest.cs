using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class SpellCollegeWriteRequest(
    AttachedSessionSnapshot Session,
    string TargetHandleToken,
    string CollegeToken,
    string LevelText,
    string TimeoutMillisecondsText
);
