using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class DashboardSnapshot(
    IReadOnlyList<string> RequestedProcessNames,
    RuntimeCapabilityReport Capabilities,
    IReadOnlyList<ProbeProfile> RecommendedProbeProfiles,
    IReadOnlyList<PanelDescriptor> RecommendedPanels,
    IReadOnlyList<string> Notes
);
