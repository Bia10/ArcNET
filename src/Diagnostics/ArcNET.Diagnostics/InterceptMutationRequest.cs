namespace ArcNET.Diagnostics;

public sealed record class InterceptMutationRequest(
    bool SkipOriginal,
    int CleanupBytes,
    uint? ReturnEax,
    uint? ReturnEdx,
    InterceptRegisterOverrideRequest Registers,
    IReadOnlyList<InterceptArgumentOverrideRequest> ArgumentOverrides
);
