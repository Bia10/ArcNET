using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class TimelineSnapshot(
    RuntimeCapabilityReport Capabilities,
    IReadOnlyList<TimelinePresetDescriptor> RecommendedPresets,
    IReadOnlyList<TimelinePresetDescriptor> AvailableProbePresets,
    IReadOnlyList<RuntimeWatchProfileDescriptor> AdvancedProfiles,
    IReadOnlyList<string> Notes
);
