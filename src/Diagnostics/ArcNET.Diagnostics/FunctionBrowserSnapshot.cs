using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class FunctionBrowserSnapshot(
    RuntimeCapabilityReport Capabilities,
    IReadOnlyList<DispatcherCandidateDefinition> DispatcherCandidates,
    IReadOnlyList<FunctionDefinition> Functions,
    IReadOnlyList<string> Notes
);
