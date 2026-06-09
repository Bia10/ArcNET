using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

public sealed class TimelineServiceTests
{
    [Test]
    public async Task Create_WhenRuntimeIsValidated_ExposesRecommendedPresetsAndAdvancedProfiles()
    {
        var request = new TimelineRequest(
            new RuntimeProfileSnapshot(
                Id: "validated",
                DisplayName: "Validated classic profile",
                RuntimeKind: RuntimeKind.Classic,
                SupportLevel: RuntimeSupportLevel.Validated,
                SupportsCatalogRvas: true,
                Notes: "Validated.",
                ModuleSha256: "ABC",
                HashError: null
            ),
            HasModuleSymbols: false
        );

        var snapshot = TimelineService.Create(request);

        await Assert.That(snapshot.RecommendedPresets.Any(static preset => preset.Key == "session-core")).IsTrue();
        await Assert.That(snapshot.RecommendedPresets.Any(static preset => preset.Key == "render-core")).IsTrue();
        await Assert
            .That(snapshot.RecommendedPresets.First(static preset => preset.Key == "render-core").UsesHighVolumeHooks)
            .IsTrue();
        await Assert.That(snapshot.AdvancedProfiles.Any(static profile => profile.Key == "render")).IsTrue();
    }

    [Test]
    public async Task Create_WhenRuntimeIsNotWatchCapable_ReturnsUnavailableTimelineState()
    {
        var request = new TimelineRequest(
            new RuntimeProfileSnapshot(
                Id: null,
                DisplayName: "Community Edition executable (unvalidated profile)",
                RuntimeKind: RuntimeKind.CommunityEdition,
                SupportLevel: RuntimeSupportLevel.Exploratory,
                SupportsCatalogRvas: false,
                Notes: "Exploratory.",
                ModuleSha256: "ABC",
                HashError: null
            ),
            HasModuleSymbols: true
        );

        var snapshot = TimelineService.Create(request);

        await Assert.That(snapshot.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.WatchHooks)).IsFalse();
        await Assert.That(snapshot.RecommendedPresets).IsEmpty();
        await Assert.That(snapshot.AvailableProbePresets).IsEmpty();
        await Assert.That(snapshot.AdvancedProfiles).IsEmpty();
        await Assert
            .That(snapshot.Notes.Any(static note => note.Contains("unavailable", StringComparison.OrdinalIgnoreCase)))
            .IsTrue();
    }
}
