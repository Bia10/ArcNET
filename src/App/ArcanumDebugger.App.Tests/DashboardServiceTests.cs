using ArcanumDebugger.App.Composition;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcanumDebugger.App.Tests;

public sealed class DashboardServiceTests
{
    [Test]
    public async Task Create_WhenRuntimeIsValidated_RecommendsTimelineAndProbeProfiles()
    {
        var request = new DashboardRequest(
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
            HasModuleSymbols: false,
            RequestedProcessNames: ["Arcanum"]
        );

        var snapshot = DashboardService.Create(request);

        await Assert.That(snapshot.RecommendedPanels.Any(static panel => panel.Key == "timeline")).IsTrue();
        await Assert.That(snapshot.RecommendedPanels.Any(static panel => panel.Key == "scripts")).IsTrue();
        await Assert.That(snapshot.RecommendedPanels.Any(static panel => panel.Key == "logbook")).IsTrue();
        await Assert
            .That(snapshot.RecommendedProbeProfiles.Any(static profile => profile.Key == "session-core"))
            .IsTrue();
    }

    [Test]
    public async Task Create_WhenRuntimeIsSymbolAssisted_RecommendsFunctionsWithoutTimeline()
    {
        var request = new DashboardRequest(
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
            HasModuleSymbols: true,
            RequestedProcessNames: ["arcanum-ce"]
        );

        var snapshot = DashboardService.Create(request);

        await Assert.That(snapshot.Capabilities.SupportLevel).IsEqualTo(RuntimeSupportLevel.SymbolAssisted);
        await Assert.That(snapshot.RecommendedPanels.Any(static panel => panel.Key == "functions")).IsTrue();
        await Assert.That(snapshot.RecommendedPanels.Any(static panel => panel.Key == "timeline")).IsFalse();
        await Assert.That(snapshot.RecommendedProbeProfiles).IsEmpty();
    }
}
