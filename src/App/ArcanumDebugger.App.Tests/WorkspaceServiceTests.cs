using ArcanumDebugger.App.Composition;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcanumDebugger.App.Tests;

public sealed class WorkspaceServiceTests
{
    [Test]
    public async Task Create_WhenRuntimeIsValidated_ComposesAllHeadlessPanels()
    {
        var request = new WorkspaceRequest(
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

        var snapshot = WorkspaceService.Create(request);

        await Assert.That(snapshot.Dashboard.Capabilities.SupportLevel).IsEqualTo(RuntimeSupportLevel.Validated);
        await Assert.That(snapshot.PanelWorkflows.Count).IsEqualTo(snapshot.Dashboard.RecommendedPanels.Count);
        await Assert
            .That(snapshot.PanelWorkflows.Select(static workflow => workflow.PanelKey))
            .IsEquivalentTo(snapshot.Dashboard.RecommendedPanels.Select(static panel => panel.Key));
        await Assert.That(snapshot.Timeline.RecommendedPresets.Any()).IsTrue();
        await Assert.That(snapshot.FunctionBrowser.Functions.Any()).IsTrue();
        await Assert.That(snapshot.ObjectExplorer.RecommendedGroups.Any()).IsTrue();
    }

    [Test]
    public async Task Create_WhenRuntimeIsValidated_MapsRecommendedPanelsToExplicitShellWorkflows()
    {
        var request = new WorkspaceRequest(
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

        var snapshot = WorkspaceService.Create(request);

        await Assert
            .That(snapshot.PanelWorkflows.Single(static workflow => workflow.PanelKey == "home").ShellSurfaceText)
            .IsEqualTo("Workspace");
        await Assert
            .That(snapshot.PanelWorkflows.Single(static workflow => workflow.PanelKey == "timeline").ShellSurfaceText)
            .IsEqualTo("Timeline");
        await Assert
            .That(snapshot.PanelWorkflows.Single(static workflow => workflow.PanelKey == "functions").ShellSurfaceText)
            .IsEqualTo("Functions");
        await Assert
            .That(snapshot.PanelWorkflows.Single(static workflow => workflow.PanelKey == "sheets").WorkflowTitle)
            .IsEqualTo("Prototype, Read, and Sheet");
        await Assert
            .That(snapshot.PanelWorkflows.Single(static workflow => workflow.PanelKey == "inventory").ShellSurfaceText)
            .IsEqualTo("Objects + Timeline");
        await Assert
            .That(snapshot.PanelWorkflows.Single(static workflow => workflow.PanelKey == "dumps").ShellSurfaceText)
            .IsEqualTo("Diagnostics");
        await Assert
            .That(snapshot.PanelWorkflows.Any(static workflow => workflow.ShellSurfaceText == "Coverage"))
            .IsFalse();
    }

    [Test]
    public async Task PreviewCatalog_ExposesRunnableScenariosForShellWork()
    {
        await Assert.That(ArcanumDebuggerPreviewCatalog.Scenarios.Count).IsGreaterThanOrEqualTo(3);
        await Assert
            .That(ArcanumDebuggerPreviewCatalog.Scenarios.Any(static scenario => scenario.Key == "validated-classic"))
            .IsTrue();
        await Assert
            .That(
                ArcanumDebuggerPreviewCatalog.Scenarios.Any(static scenario => scenario.Key == "unsupported-readonly")
            )
            .IsTrue();
    }

    [Test]
    public async Task CreateForRuntime_UsesLiveProcessNameAsAttachTarget()
    {
        var runtime = new LiveRuntimeSnapshot(
            "live-Arcanum-27748",
            "Arcanum.exe (PID 27748)",
            "Live runtime",
            "Arcanum",
            27748,
            new RuntimeFingerprint(
                "Arcanum",
                27748,
                RuntimeKind.Classic,
                "Arcanum.exe",
                @"C:\Games\Arcanum\Arcanum.exe",
                "0x00400000",
                3538944,
                0,
                DateTime.MinValue
            ),
            new RuntimeProfileSnapshot(
                Id: "live-classic",
                DisplayName: "Live classic runtime",
                RuntimeKind: RuntimeKind.Classic,
                SupportLevel: RuntimeSupportLevel.Validated,
                SupportsCatalogRvas: true,
                Notes: "Validated runtime.",
                ModuleSha256: "ABC",
                HashError: null
            ),
            new RuntimeCapabilityReport(
                RuntimeSupportLevel.Validated,
                DiagnosticsCapability.ReadMemory | DiagnosticsCapability.WatchHooks,
                []
            )
        );

        var snapshot = WorkspaceService.CreateForRuntime(runtime);

        await Assert.That(snapshot.RuntimeProfile.DisplayName).IsEqualTo("Live classic runtime");
        await Assert.That(snapshot.Dashboard.RequestedProcessNames).IsEquivalentTo(["Arcanum"]);
    }

    [Test]
    public async Task CreateForSession_UsesAttachedProcessNameAsAttachTarget()
    {
        var session = new AttachedSessionSnapshot(
            DateTimeOffset.UtcNow,
            SessionOrigin.Attach,
            "Arcanum.exe (PID 27748)",
            "Attached session",
            @"C:\Games\Arcanum\Arcanum.exe @ 0x00400000",
            "Arcanum",
            27748,
            false,
            new RuntimeFingerprint(
                "Arcanum",
                27748,
                RuntimeKind.Classic,
                "Arcanum.exe",
                @"C:\Games\Arcanum\Arcanum.exe",
                "0x00400000",
                3538944,
                0,
                DateTime.MinValue
            ),
            new RuntimeProfileSnapshot(
                Id: "session-classic",
                DisplayName: "Session classic runtime",
                RuntimeKind: RuntimeKind.Classic,
                SupportLevel: RuntimeSupportLevel.Unsupported,
                SupportsCatalogRvas: false,
                Notes: "Unsupported runtime.",
                ModuleSha256: null,
                HashError: "No hash"
            ),
            new RuntimeCapabilityReport(
                RuntimeSupportLevel.Unsupported,
                DiagnosticsCapability.ReadMemory,
                ["Read-only only."]
            ),
            null,
            ["Read-only only."]
        );

        var snapshot = WorkspaceService.CreateForSession(session);

        await Assert.That(snapshot.RuntimeProfile.DisplayName).IsEqualTo("Session classic runtime");
        await Assert.That(snapshot.Dashboard.RequestedProcessNames).IsEquivalentTo(["Arcanum"]);
        await Assert.That(snapshot.Dashboard.Capabilities.SupportLevel).IsEqualTo(RuntimeSupportLevel.Unsupported);
    }
}
