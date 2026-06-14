namespace ArcNET.Diagnostics;

public sealed record class SpellTechMutationExecutionResult(
    string DispatcherMode,
    string DispatcherSite,
    string ExecutionDetailText,
    string ResultText,
    bool NoMutation = false,
    int RelatedIndex = -1
);
