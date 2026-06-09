namespace ArcNET.Diagnostics;

public sealed record class InterceptStartRequest(
    AttachedSessionSnapshot Session,
    InterceptTarget Target,
    int StackCaptureDwordCount,
    InterceptMutationRequest Mutation,
    IReadOnlyList<InterceptDereferenceRequest> Dereferences
);
