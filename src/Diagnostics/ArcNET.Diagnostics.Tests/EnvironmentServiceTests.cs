using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Windows;
using ArcNET.Patch;

namespace ArcNET.Diagnostics.Tests;

public sealed class EnvironmentServiceTests
{
    [Test]
    public async Task Create_WhenSingleRuntimeIsRunning_ReportsAttachReadyAndLiveRuntime()
    {
        var moduleDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var modulePath = Path.Combine(moduleDirectory.FullName, "Arcanum.exe");
            await File.WriteAllTextAsync(modulePath, "classic-runtime");

            var backend = new FakeEnvironmentBackend
            {
                RunningProcesses =
                [
                    new RunningProcessInfo("Arcanum", 4242, "Arcanum.exe", modulePath, (nint)0x00400000, 3538944),
                ],
            };
            var service = new EnvironmentService(backend);

            var snapshot = service.Create(
                new EnvironmentRequest(["Arcanum"], InstallPath: null, ArcanumExecutableKind.Auto, false)
            );

            await Assert.That(snapshot.CanAttachSingleRuntime).IsTrue();
            await Assert.That(snapshot.LiveRuntimes.Count).IsEqualTo(1);
            await Assert.That(snapshot.LiveRuntimes[0].ProcessId).IsEqualTo(4242);
            await Assert.That(snapshot.ProcessCandidates[0].IsRunning).IsTrue();
            await Assert
                .That(snapshot.AttachSummary.Contains("Ready to attach", StringComparison.OrdinalIgnoreCase))
                .IsTrue();
        }
        finally
        {
            moduleDirectory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Create_WhenLaunchPreviewSucceeds_ProjectsResolvedPlan()
    {
        var backend = new FakeEnvironmentBackend
        {
            LaunchPlan = new ArcanumLaunchPlan(
                ArcanumExecutableKind.CommunityEdition,
                @"C:\Games\Arcanum\arcanum-ce.exe",
                @"C:\Games\Arcanum",
                ["-window"],
                new Dictionary<string, string> { ["SDL_RENDER_DRIVER"] = "direct3d11" }
            ),
        };
        var service = new EnvironmentService(backend);

        var snapshot = service.Create(
            new EnvironmentRequest(
                ["Arcanum", "arcanum-ce"],
                @"C:\Games\Arcanum",
                ArcanumExecutableKind.CommunityEdition,
                LaunchWindowed: true
            )
        );

        await Assert.That(snapshot.LaunchPreview).IsNotNull();
        await Assert.That(snapshot.LaunchPreview!.CanLaunch).IsTrue();
        await Assert.That(snapshot.LaunchPreview.ExecutableKind).IsEqualTo(ArcanumExecutableKind.CommunityEdition);
        await Assert.That(snapshot.LaunchPreview.Arguments).Contains("-window");
        await Assert.That(snapshot.LaunchPreview.EnvironmentVariables).Contains("SDL_RENDER_DRIVER=direct3d11");
    }

    [Test]
    public async Task Create_WhenLaunchPreviewFails_ReturnsErrorSummary()
    {
        var backend = new FakeEnvironmentBackend
        {
            LaunchError = new FileNotFoundException("No compatible executable was found."),
        };
        var service = new EnvironmentService(backend);

        var snapshot = service.Create(
            new EnvironmentRequest(
                ["Arcanum"],
                @"C:\Missing\Arcanum",
                ArcanumExecutableKind.Auto,
                LaunchWindowed: false
            )
        );

        await Assert.That(snapshot.LaunchPreview).IsNotNull();
        await Assert.That(snapshot.LaunchPreview!.CanLaunch).IsFalse();
        await Assert.That(snapshot.LaunchPreview.Error).IsEqualTo("No compatible executable was found.");
    }

    private sealed class FakeEnvironmentBackend : IEnvironmentBackend
    {
        public IReadOnlyList<RunningProcessInfo> RunningProcesses { get; init; } = [];

        public ArcanumLaunchPlan? LaunchPlan { get; init; }

        public Exception? LaunchError { get; init; }

        public IReadOnlyList<RunningProcessInfo> GetRunningProcesses(IReadOnlyList<string> processNames) =>
            RunningProcesses;

        public bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error)
        {
            moduleSha256 = null;
            error = null;
            return true;
        }

        public ArcanumLaunchPlan CreateLaunchPlan(string gamePath, ArcanumLaunchOptions options)
        {
            if (LaunchError is not null)
                throw LaunchError;

            return LaunchPlan ?? throw new InvalidOperationException("No fake launch plan was provided.");
        }
    }
}
