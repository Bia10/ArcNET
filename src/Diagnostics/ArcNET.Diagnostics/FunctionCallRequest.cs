using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics;

public sealed record class FunctionCallRequest(
    AttachedSessionSnapshot Session,
    string TargetText,
    string StackArgumentsText,
    string EcxValueText,
    string EdxValueText,
    bool UseSuggestedCleanup,
    StackCleanupMode OverrideCleanupMode,
    string TimeoutMillisecondsText
);
