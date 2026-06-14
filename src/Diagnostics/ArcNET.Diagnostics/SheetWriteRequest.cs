namespace ArcNET.Diagnostics;

public sealed record class SheetWriteRequest(
    AttachedSessionSnapshot Session,
    string TargetHandleToken,
    string FieldToken,
    string ValueText,
    string TrainingText,
    string TimeoutMillisecondsText
);
