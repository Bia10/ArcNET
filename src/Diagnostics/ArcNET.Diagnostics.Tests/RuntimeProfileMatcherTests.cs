using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class RuntimeProfileMatcherTests
{
    [Test]
    public async Task Match_WhenFingerprintMatchesValidatedClassicBuild_ReturnsValidatedProfile()
    {
        var fingerprint = new RuntimeFingerprint(
            ProcessName: "Arcanum",
            ProcessId: 42,
            RuntimeKind: RuntimeKind.Classic,
            ModuleFileName: "Arcanum.exe",
            ModulePath: @"C:\Games\Arcanum\Arcanum.exe",
            ModuleBase: "0x00400000",
            ModuleSize: 3_538_944,
            ModuleFileSize: 2_048_000,
            ModuleLastWriteTimeUtc: new DateTime(2021, 05, 28, 0, 0, 0, DateTimeKind.Utc)
        );

        var profile = RuntimeProfileMatcher.Match(
            fingerprint,
            "D7A16B8C29141E6E834ED2647506059BC482F7AE63EB2CB5E6F1761358FD038F"
        );

        await Assert.That(profile.SupportLevel).IsEqualTo(RuntimeSupportLevel.Validated);
        await Assert.That(profile.SupportsCatalogRvas).IsTrue();
        await Assert.That(profile.RuntimeKind).IsEqualTo(RuntimeKind.Classic);
    }

    [Test]
    public async Task Match_WhenCommunityEditionFingerprintIsUnknown_ReturnsExploratoryProfile()
    {
        var fingerprint = new RuntimeFingerprint(
            ProcessName: "arcanum-ce",
            ProcessId: 77,
            RuntimeKind: RuntimeKind.CommunityEdition,
            ModuleFileName: "arcanum-ce.exe",
            ModulePath: @"C:\Games\Arcanum\arcanum-ce.exe",
            ModuleBase: "0x00400000",
            ModuleSize: 4_000_000,
            ModuleFileSize: 2_300_000,
            ModuleLastWriteTimeUtc: new DateTime(2026, 06, 08, 0, 0, 0, DateTimeKind.Utc)
        );

        var profile = RuntimeProfileMatcher.Match(fingerprint, "1234");

        await Assert.That(profile.SupportLevel).IsEqualTo(RuntimeSupportLevel.Exploratory);
        await Assert.That(profile.RuntimeKind).IsEqualTo(RuntimeKind.CommunityEdition);
        await Assert.That(profile.SupportsCatalogRvas).IsFalse();
    }

    [Test]
    public async Task ClassifyRuntimeKind_WhenNamesUseArcanumCasing_ReturnsClassic()
    {
        var runtimeKind = RuntimeProfileMatcher.ClassifyRuntimeKind("Arcanum.exe", "Arcanum");

        await Assert.That(runtimeKind).IsEqualTo(RuntimeKind.Classic);
    }
}
