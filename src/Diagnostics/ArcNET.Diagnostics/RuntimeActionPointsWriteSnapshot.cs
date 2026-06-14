namespace ArcNET.Diagnostics;

public sealed record class RuntimeActionPointsWriteSnapshot(
    ActionPointsMutationSnapshot Mutation,
    RuntimeStatusSnapshot Status
);
