using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class FunctionBrowserServiceTests
{
    [Test]
    public async Task Create_WhenRuntimeIsValidated_ExposesDispatcherCandidatesAndInvocationGuidance()
    {
        var request = new FunctionBrowserRequest(
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

        var snapshot = FunctionBrowserService.Create(request);

        await Assert
            .That(snapshot.DispatcherCandidates.Any(static candidate => candidate.Key == "tig_window_display"))
            .IsTrue();
        await Assert.That(snapshot.Functions.Any(static function => function.Key == "level_recalc")).IsTrue();
        await Assert.That(snapshot.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions)).IsTrue();
        await Assert
            .That(snapshot.Notes.Any(static note => note.Contains("live invocation", StringComparison.Ordinal)))
            .IsTrue();
    }

    [Test]
    public async Task Create_WhenRuntimeIsExploratoryWithoutSymbols_StaysReferenceOnly()
    {
        var request = new FunctionBrowserRequest(
            new RuntimeProfileSnapshot(
                Id: null,
                DisplayName: "Unknown runtime",
                RuntimeKind: RuntimeKind.CommunityEdition,
                SupportLevel: RuntimeSupportLevel.Exploratory,
                SupportsCatalogRvas: false,
                Notes: "Exploratory.",
                ModuleSha256: "ABC",
                HashError: null
            ),
            HasModuleSymbols: false
        );

        var snapshot = FunctionBrowserService.Create(request);

        await Assert.That(snapshot.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions)).IsFalse();
        await Assert
            .That(snapshot.Notes.Any(static note => note.Contains("reference metadata", StringComparison.Ordinal)))
            .IsTrue();
    }
}
