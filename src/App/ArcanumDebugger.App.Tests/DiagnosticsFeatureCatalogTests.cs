using ArcanumDebugger.App.Composition;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.Tests;

public sealed class DiagnosticsFeatureCatalogTests
{
    [Test]
    public async Task Features_CoverRuntimeAndFileTimeDiagnostics()
    {
        var features = DiagnosticsFeatureCatalog.Features;

        await Assert.That(features.Any(static feature => feature.ServiceName == "AuditService")).IsTrue();
        await Assert.That(features.Any(static feature => feature.ServiceName == "LogbookService")).IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "TimelineService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "ReadService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert.That(features.Any(static feature => feature.ServiceName == "SaveFileAuditService")).IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "SaveCharacterCatalogService"
                    && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "SaveGlobalAnalysisService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert.That(features.Any(static feature => feature.ServiceName == "SaveBinaryDiffService")).IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "RuntimeProfileService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "SaveGlobalDiffService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "PlayerSarReportService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "MobItemAnalysisService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "RuntimeStatusService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "InterceptService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert
            .That(
                features.Any(static feature =>
                    feature.ServiceName == "CrashDumpService" && feature.ShellStatus == "Interactive in shell"
                )
            )
            .IsTrue();
        await Assert.That(features.Any(static feature => feature.ServiceName == "CeSourceAuditService")).IsTrue();
        await Assert.That(features.Any(static feature => feature.Area == "File-time save")).IsTrue();
        await Assert.That(features.Any(static feature => feature.ShellStatus == "Interactive in shell")).IsTrue();
    }

    [Test]
    public async Task Features_CatalogEveryPublicDiagnosticsServiceAsInteractive()
    {
        var features = DiagnosticsFeatureCatalog.Features;
        var serviceNames = features
            .Select(static feature => feature.ServiceName)
            .OrderBy(static name => name)
            .ToArray();
        var expectedServiceNames = GetPublicDiagnosticsServiceNames();

        await Assert.That(serviceNames).IsEquivalentTo(expectedServiceNames);
        await Assert.That(features.All(static feature => feature.ShellStatus == "Interactive in shell")).IsTrue();
    }

    private static string[] GetPublicDiagnosticsServiceNames() =>
        [
            .. new[]
            {
                typeof(WorkspaceService).Assembly,
                typeof(SaveSlotLoadService).Assembly,
                typeof(SessionService).Assembly,
            }
                .Distinct()
                .SelectMany(static assembly => assembly.GetExportedTypes())
                .Where(static type =>
                    type is { IsClass: true, IsPublic: true }
                    && type.Name.EndsWith("Service", StringComparison.Ordinal)
                    && type.Namespace is not null
                    && type.Namespace.Equals("ArcNET.Diagnostics", StringComparison.Ordinal)
                )
                .Select(static type => type.Name)
                .Concat(
                    typeof(IDiagnosticsServices)
                        .GetProperties()
                        .Select(static property => property.PropertyType)
                        .Where(static type =>
                            type is { IsClass: true } && type.Name.EndsWith("Service", StringComparison.Ordinal)
                        )
                        .Select(static type => type.Name)
                )
                .Append(nameof(RuntimeProfileService))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static name => name),
        ];
}
