namespace ArcNET.Diagnostics;

internal readonly record struct WatchEventProjection(
    string SemanticEvent,
    string Signature,
    string Summary,
    string? SuggestedHandleHex,
    IReadOnlyList<string> CandidateHandles
);
