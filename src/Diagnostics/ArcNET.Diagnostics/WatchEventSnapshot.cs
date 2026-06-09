namespace ArcNET.Diagnostics;

public sealed record class WatchEventSnapshot(
    uint Sequence,
    string HookKey,
    string EventName,
    string SemanticEvent,
    string Area,
    string Site,
    string CallerAddress,
    string CallerRva,
    string Signature,
    string Summary,
    string? SuggestedHandleHex,
    IReadOnlyList<string> CandidateHandles,
    string StackPreview
);
