using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

public sealed class ObjectExplorerServiceTests
{
    [Test]
    public async Task Create_WhenRuntimeIsValidated_ExposesRecommendedGroupsAndTransientNoise()
    {
        var request = new ObjectExplorerRequest(
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

        var snapshot = ObjectExplorerService.Create(request);

        await Assert.That(snapshot.RecommendedGroups.Any(static group => group.Key == "critter")).IsTrue();
        await Assert.That(snapshot.RecommendedGroups.Any(static group => group.Key == "pc")).IsTrue();
        await Assert
            .That(
                snapshot
                    .AllGroups.First(static group => group.Key == "core")
                    .Fields.Any(static field => field.RawName == "OBJ_F_LOCATION")
            )
            .IsTrue();
        await Assert
            .That(snapshot.AllGroups.First(static group => group.Key == "transient").NoiseFieldCount)
            .IsGreaterThan(0);
    }

    [Test]
    public async Task Create_WhenRuntimeCannotReadStructuredState_ReturnsUnavailableState()
    {
        var request = new ObjectExplorerRequest(
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

        var snapshot = ObjectExplorerService.Create(request);

        await Assert
            .That(snapshot.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.ReadStructuredState))
            .IsFalse();
        await Assert.That(snapshot.RecommendedGroups).IsEmpty();
        await Assert.That(snapshot.AllGroups).IsEmpty();
        await Assert
            .That(snapshot.Notes.Any(static note => note.Contains("unavailable", StringComparison.OrdinalIgnoreCase)))
            .IsTrue();
    }
}
