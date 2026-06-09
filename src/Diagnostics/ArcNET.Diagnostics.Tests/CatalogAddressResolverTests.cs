using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class CatalogAddressResolverTests
{
    [Test]
    public async Task Resolve_WhenValidatedProfileAndRvaAvailable_PrefersCatalogRva()
    {
        var profile = new RuntimeProfileSnapshot(
            Id: "validated",
            DisplayName: "Validated",
            RuntimeKind: RuntimeKind.Classic,
            SupportLevel: RuntimeSupportLevel.Validated,
            SupportsCatalogRvas: true,
            Notes: "Validated",
            ModuleSha256: "ABC",
            HashError: null
        );
        var request = new CatalogAddressResolveRequest(
            Key: "level_recalc",
            PreferredRva: 0x1234,
            Operation: "test operation",
            ModuleFileName: "Arcanum.exe",
            ModuleSize: 0x4000,
            ModuleBytes: [0x90, 0x90, 0x90],
            RuntimeProfile: profile,
            ForceSignatureFallback: false,
            SignaturesByNormalizedKey: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            AddressCacheKey: "test"
        );

        var resolution = CatalogAddressResolver.Resolve(request);

        await Assert.That(resolution.Rva).IsEqualTo((uint)0x1234);
        await Assert.That(resolution.Resolution).IsEqualTo("catalog-rva");
    }

    [Test]
    public async Task Resolve_WhenSignatureFallbackMatches_ReturnsSignatureResolution()
    {
        var profile = new RuntimeProfileSnapshot(
            Id: null,
            DisplayName: "Exploratory",
            RuntimeKind: RuntimeKind.CommunityEdition,
            SupportLevel: RuntimeSupportLevel.Exploratory,
            SupportsCatalogRvas: false,
            Notes: "Exploratory",
            ModuleSha256: "ABC",
            HashError: null
        );
        var request = new CatalogAddressResolveRequest(
            Key: "level_recalc",
            PreferredRva: 0,
            Operation: "test operation",
            ModuleFileName: "arcanum-ce.exe",
            ModuleSize: 0x4000,
            ModuleBytes: [0x90, 0x8B, 0xFF, 0x24, 0x90],
            RuntimeProfile: profile,
            ForceSignatureFallback: false,
            SignaturesByNormalizedKey: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [CatalogAddressResolver.NormalizeKey("level_recalc")] = "8B FF 24",
            },
            AddressCacheKey: "test-fallback"
        );

        var resolution = CatalogAddressResolver.Resolve(request);

        await Assert.That(resolution.Rva).IsEqualTo((uint)1);
        await Assert.That(resolution.Resolution).IsEqualTo("catalog-signature");
    }
}
