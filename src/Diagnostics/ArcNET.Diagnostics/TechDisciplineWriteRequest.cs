using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class TechDisciplineWriteRequest(
    AttachedSessionSnapshot Session,
    string TargetHandleToken,
    string DisciplineToken,
    string DegreeText,
    string TimeoutMillisecondsText
);
