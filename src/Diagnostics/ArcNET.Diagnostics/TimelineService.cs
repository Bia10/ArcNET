using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class TimelineService
{
    public static TimelineSnapshot Create(TimelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var capabilities = DiagnosticsCapabilityPolicy.Create(request.RuntimeProfile, request.HasModuleSymbols);
        if (!capabilities.Capabilities.HasFlag(DiagnosticsCapability.WatchHooks))
        {
            return new TimelineSnapshot(capabilities, [], [], [], CreateUnavailableNotes(capabilities));
        }

        var availableProbePresets = CreateProbePresets(ProbeCatalog.Profiles);
        var recommendedPresets = CreateProbePresets(ResolveRecommendedProbeProfiles());
        var notes = CreateNotes(capabilities, availableProbePresets);

        return new TimelineSnapshot(
            capabilities,
            recommendedPresets,
            availableProbePresets,
            RuntimeWatchCatalog.Profiles,
            notes
        );
    }

    private static IReadOnlyList<ProbeProfile> ResolveRecommendedProbeProfiles() =>
        [
            .. new[] { "session-core", "progression-core", "inventory-core", "world-core", "render-core" }.Select(
                static key => ProbeCatalog.Profiles.First(profile => profile.Key == key)
            ),
        ];

    private static IReadOnlyList<TimelinePresetDescriptor> CreateProbePresets(IReadOnlyList<ProbeProfile> profiles) =>
        [.. profiles.Select(CreateProbePreset)];

    private static TimelinePresetDescriptor CreateProbePreset(ProbeProfile profile)
    {
        var hooks = RuntimeWatchCatalog.ResolveSelectors(profile.Selectors);
        return new TimelinePresetDescriptor(
            profile.Key,
            profile.DisplayName,
            profile.Description,
            profile.Selectors,
            [.. hooks.Select(static hook => hook.Key)],
            [.. hooks.Select(static hook => hook.Area).Distinct(StringComparer.OrdinalIgnoreCase)],
            RuntimeWatchCatalog.UsesHighVolumeHooks(hooks)
        );
    }

    private static IReadOnlyList<string> CreateNotes(
        RuntimeCapabilityReport capabilities,
        IReadOnlyList<TimelinePresetDescriptor> presets
    )
    {
        List<string> notes = [.. capabilities.Warnings];
        notes.Add(
            "Timeline-capable runtimes should start with probe presets and only fall back to advanced selector sets when needed."
        );

        if (presets.Any(static preset => preset.UsesHighVolumeHooks))
        {
            notes.Add(
                "Some presets include high-volume render or mutation hooks and should be sampled with tighter guardrails."
            );
        }

        notes.Add(
            "Advanced watch profiles map closely to legacy selector sets, which helps parity testing during transition."
        );
        return notes;
    }

    private static IReadOnlyList<string> CreateUnavailableNotes(RuntimeCapabilityReport capabilities)
    {
        List<string> notes = [.. capabilities.Warnings];
        notes.Add("Timeline workflows are unavailable until the runtime reaches validated watch-hook support.");
        return notes;
    }
}
