using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class ObjectExplorerSnapshot(
    RuntimeCapabilityReport Capabilities,
    IReadOnlyList<ObjectFieldGroupDescriptor> RecommendedGroups,
    IReadOnlyList<ObjectFieldGroupDescriptor> AllGroups,
    IReadOnlyList<string> Notes
);
