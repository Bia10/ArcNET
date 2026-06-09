using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Tests;

public sealed class CeSourceAuditServiceTests
{
    [Test]
    public async Task ResolveSourceRoot_WhenRepoRootIsProvided_ReturnsNestedSrcDirectory()
    {
        var repoRoot = CreateTemporaryRoot();
        var sourceRoot = Path.Combine(repoRoot, "src");
        Directory.CreateDirectory(Path.Combine(sourceRoot, "game"));
        Directory.CreateDirectory(Path.Combine(sourceRoot, "ui"));

        var resolved = CeSourceCatalogLoader.ResolveSourceRoot(repoRoot);

        await Assert.That(resolved).IsEqualTo(Path.GetFullPath(sourceRoot));
    }

    [Test]
    public async Task Create_WhenKnownAndUnknownFunctionsExist_ReportsCoverageParity()
    {
        var sourceRoot = CreateTemporaryRoot();
        Directory.CreateDirectory(Path.Combine(sourceRoot, "game"));
        Directory.CreateDirectory(Path.Combine(sourceRoot, "ui"));
        await File.WriteAllTextAsync(
            Path.Combine(sourceRoot, "game", "sample.c"),
            """
            int teleport_do(int value)
            {
                return value;
            }

            static void brand_new_function(void)
            {
            }
            """
        );

        var snapshot = CeSourceAuditService.Create(new CeSourceAuditRequest(sourceRoot, null, null));
        var teleport = snapshot.Functions.Single(function => function.Name == "teleport_do");
        var custom = snapshot.Functions.Single(function => function.Name == "brand_new_function");

        await Assert.That(teleport.Coverage.SignatureCoverage).IsTrue();
        await Assert.That(teleport.Coverage.DebuggerFunctionCoverage).IsTrue();
        await Assert.That(teleport.Coverage.AnyCatalogCoverage).IsTrue();
        await Assert.That(custom.Coverage.AnyCatalogCoverage).IsFalse();
        await Assert.That(snapshot.Summary.MissingCoverageCount).IsEqualTo(1);
    }

    private static string CreateTemporaryRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "arcnet-diagnostics-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
