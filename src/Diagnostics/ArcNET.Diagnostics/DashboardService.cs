using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class DashboardService
{
    public static DashboardSnapshot Create(DashboardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var capabilities = DiagnosticsCapabilityPolicy.Create(request.RuntimeProfile, request.HasModuleSymbols);
        var recommendedPanels = CreateRecommendedPanels(capabilities);
        var recommendedProbeProfiles = CreateRecommendedProbeProfiles(capabilities);
        var notes = CreateNotes(request, capabilities);

        return new DashboardSnapshot(
            request.RequestedProcessNames,
            capabilities,
            recommendedProbeProfiles,
            recommendedPanels,
            notes
        );
    }

    private static IReadOnlyList<PanelDescriptor> CreateRecommendedPanels(RuntimeCapabilityReport capabilities)
    {
        List<PanelDescriptor> panels =
        [
            new("home", "Home", "Launch, attach, and environment overview."),
            new("diagnostics", "Diagnostics", "Warnings, unsupported operations, and runtime health."),
        ];

        if (capabilities.Capabilities.HasFlag(DiagnosticsCapability.WatchHooks))
        {
            panels.Add(new("timeline", "Timeline", "Live hook event stream with filtering and summaries."));
        }

        if (capabilities.Capabilities.HasFlag(DiagnosticsCapability.ReadStructuredState))
        {
            panels.Add(new("objects", "Objects", "Resolved live object state and handles."));
            panels.Add(new("sheets", "Sheets", "Character sheet and progression diagnostics."));
            panels.Add(new("inventory", "Inventory", "Resources, equipment, and key-ring diagnostics."));

            if (capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions))
            {
                panels.Add(new("scripts", "Scripts", "Attachment records, locals, and runtime script diagnostics."));
                panels.Add(
                    new(
                        "logbook",
                        "Logbook",
                        "Rumors, quests, reputations, background, injuries, and key-ring history."
                    )
                );
            }
        }

        if (
            capabilities.Capabilities.HasFlag(DiagnosticsCapability.LoadModuleSymbols)
            || capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions)
        )
        {
            panels.Add(new("functions", "Functions", "Named-function targets, symbols, and resolution metadata."));
        }

        if (capabilities.Capabilities.HasFlag(DiagnosticsCapability.CaptureDump))
            panels.Add(new("dumps", "Dumps", "Crash-dump capture and recording workflows."));

        return panels;
    }

    private static IReadOnlyList<ProbeProfile> CreateRecommendedProbeProfiles(RuntimeCapabilityReport capabilities)
    {
        if (!capabilities.Capabilities.HasFlag(DiagnosticsCapability.WatchHooks))
            return [];

        string[] keys = ["session-core", "world-core", "inventory-core", "render-core"];
        return [.. keys.Select(static key => ProbeCatalog.Profiles.First(profile => profile.Key == key))];
    }

    private static IReadOnlyList<string> CreateNotes(DashboardRequest request, RuntimeCapabilityReport capabilities)
    {
        List<string> notes = [.. capabilities.Warnings];
        if (request.RequestedProcessNames.Count > 0)
        {
            notes.Add(
                $"Default attach targets: {string.Join(", ", request.RequestedProcessNames.Select(static name => $"{name}.exe"))}."
            );
        }

        if (capabilities.Capabilities.HasFlag(DiagnosticsCapability.WatchHooks))
            notes.Add("Watch-capable runtimes should default to probe presets rather than raw hook-name entry.");

        if (
            capabilities.SupportLevel == RuntimeSupportLevel.SymbolAssisted
            && !capabilities.Capabilities.HasFlag(DiagnosticsCapability.WatchHooks)
        )
        {
            notes.Add(
                "Symbol-assisted sessions should emphasize function browsing and controlled invocation before live watch workflows."
            );
        }

        return notes;
    }
}
