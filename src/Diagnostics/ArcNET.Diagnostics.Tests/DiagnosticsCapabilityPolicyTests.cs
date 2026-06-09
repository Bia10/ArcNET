using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class DiagnosticsCapabilityPolicyTests
{
    [Test]
    public async Task Create_WhenValidatedProfile_EnablesFullRuntimeCapabilitySet()
    {
        var profile = new RuntimeProfileSnapshot(
            Id: "classic-uap-2021-05-28",
            DisplayName: "Validated classic profile",
            RuntimeKind: RuntimeKind.Classic,
            SupportLevel: RuntimeSupportLevel.Validated,
            SupportsCatalogRvas: true,
            Notes: "Validated.",
            ModuleSha256: "ABC",
            HashError: null
        );

        var report = DiagnosticsCapabilityPolicy.Create(profile, hasModuleSymbols: false);

        await Assert.That(report.SupportLevel).IsEqualTo(RuntimeSupportLevel.Validated);
        await Assert.That(report.Capabilities.HasFlag(DiagnosticsCapability.MutateRuntime)).IsTrue();
        await Assert.That(report.Capabilities.HasFlag(DiagnosticsCapability.WatchHooks)).IsTrue();
        await Assert.That(report.Capabilities.HasFlag(DiagnosticsCapability.InterceptFunctions)).IsTrue();
    }

    [Test]
    public async Task Create_WhenCommunityEditionHasSymbols_PromotesSupportLevel()
    {
        var profile = new RuntimeProfileSnapshot(
            Id: null,
            DisplayName: "Community Edition executable (unvalidated profile)",
            RuntimeKind: RuntimeKind.CommunityEdition,
            SupportLevel: RuntimeSupportLevel.Exploratory,
            SupportsCatalogRvas: false,
            Notes: "No validated Community Edition profile is registered yet.",
            ModuleSha256: "ABC",
            HashError: null
        );

        var report = DiagnosticsCapabilityPolicy.Create(profile, hasModuleSymbols: true);

        await Assert.That(report.SupportLevel).IsEqualTo(RuntimeSupportLevel.SymbolAssisted);
        await Assert.That(report.Capabilities.HasFlag(DiagnosticsCapability.LoadModuleSymbols)).IsTrue();
        await Assert.That(report.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions)).IsTrue();
        await Assert.That(report.Capabilities.HasFlag(DiagnosticsCapability.MutateRuntime)).IsFalse();
    }
}
