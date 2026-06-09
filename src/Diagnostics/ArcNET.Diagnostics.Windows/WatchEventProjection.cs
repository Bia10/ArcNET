using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Windows;

internal readonly record struct WatchEventProjection(
    string SemanticEvent,
    string Signature,
    string Summary,
    string? SuggestedHandleHex,
    IReadOnlyList<string> CandidateHandles
);
