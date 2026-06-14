using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class TechSkillWriteRequest(
    AttachedSessionSnapshot Session,
    string TargetHandleToken,
    string SkillToken,
    string PointsText,
    string TimeoutMillisecondsText
);
