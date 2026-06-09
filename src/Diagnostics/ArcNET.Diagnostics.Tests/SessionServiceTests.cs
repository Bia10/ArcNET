using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Windows;
using ArcNET.Patch;

namespace ArcNET.Diagnostics.Tests;

public sealed class SessionServiceTests
{
    [Test]
    public async Task Attach_WhenRuntimeIsSelected_CreatesAttachedSessionSnapshot()
    {
        var moduleDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var modulePath = Path.Combine(moduleDirectory.FullName, "Arcanum.exe");
            await File.WriteAllTextAsync(modulePath, "attached-runtime");

            var backend = new FakeSessionBackend
            {
                AttachConnection = new FakeSessionConnection("Arcanum", 4242, modulePath, (nint)0x00400000, 3538944),
            };
            var service = new SessionService(backend);

            using var handle = service.Attach(
                new LiveRuntimeSnapshot(
                    "live-Arcanum-4242",
                    "Arcanum.exe (PID 4242)",
                    "Preview runtime",
                    "Arcanum",
                    4242,
                    new ArcNET.Diagnostics.Contracts.RuntimeFingerprint(
                        "Arcanum",
                        4242,
                        ArcNET.Diagnostics.Contracts.RuntimeKind.Classic,
                        "Arcanum.exe",
                        modulePath,
                        "0x00400000",
                        3538944,
                        0,
                        DateTime.MinValue
                    ),
                    new ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot(
                        "preview",
                        "preview",
                        ArcNET.Diagnostics.Contracts.RuntimeKind.Classic,
                        ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Validated,
                        true,
                        "preview",
                        null,
                        null
                    ),
                    new ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport(
                        ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Validated,
                        ArcNET.Diagnostics.Contracts.DiagnosticsCapability.ReadMemory,
                        []
                    )
                )
            );

            await Assert.That(handle.Snapshot.Origin).IsEqualTo(SessionOrigin.Attach);
            await Assert.That(handle.Snapshot.ProcessId).IsEqualTo(4242);
            await Assert.That(handle.Snapshot.DisplayName).IsEqualTo("Arcanum.exe (PID 4242)");
            await Assert
                .That(handle.Snapshot.Summary.Contains("Attached", StringComparison.OrdinalIgnoreCase))
                .IsTrue();
        }
        finally
        {
            moduleDirectory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task LaunchAndAttach_WhenPlanResolves_TracksLaunchOriginAndPreview()
    {
        var moduleDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var modulePath = Path.Combine(moduleDirectory.FullName, "arcanum-ce.exe");
            await File.WriteAllTextAsync(modulePath, "launch-runtime");

            var backend = new FakeSessionBackend
            {
                LaunchPlan = new ArcanumLaunchPlan(
                    ArcanumExecutableKind.CommunityEdition,
                    modulePath,
                    moduleDirectory.FullName,
                    ["-window"],
                    new Dictionary<string, string> { ["SDL_RENDER_DRIVER"] = "direct3d11" }
                ),
                LaunchConnection = new FakeSessionConnection("arcanum-ce", 5252, modulePath, (nint)0x00600000, 4128768),
            };
            var service = new SessionService(backend);

            using var handle = service.LaunchAndAttach(
                new LaunchSessionRequest(
                    moduleDirectory.FullName,
                    ArcanumExecutableKind.CommunityEdition,
                    LaunchWindowed: true
                )
            );

            await Assert.That(handle.Snapshot.Origin).IsEqualTo(SessionOrigin.Launch);
            await Assert.That(handle.Snapshot.LaunchPreview).IsNotNull();
            await Assert
                .That(handle.Snapshot.LaunchPreview!.ExecutableKind)
                .IsEqualTo(ArcanumExecutableKind.CommunityEdition);
            await Assert.That(handle.Snapshot.LaunchPreview.Arguments).Contains("-window");
            await Assert
                .That(
                    handle.Snapshot.Notes.Any(static note => note.Contains("Launch origin", StringComparison.Ordinal))
                )
                .IsTrue();
        }
        finally
        {
            moduleDirectory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Refresh_WhenProcessHasExited_UpdatesSessionState()
    {
        var moduleDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var modulePath = Path.Combine(moduleDirectory.FullName, "Arcanum.exe");
            await File.WriteAllTextAsync(modulePath, "refresh-runtime");

            var connection = new FakeSessionConnection("Arcanum", 6161, modulePath, (nint)0x00400000, 3538944);
            var backend = new FakeSessionBackend { AttachConnection = connection };
            var service = new SessionService(backend);

            using var handle = service.Attach(
                new LiveRuntimeSnapshot(
                    "live-Arcanum-6161",
                    "Arcanum.exe (PID 6161)",
                    "Preview runtime",
                    "Arcanum",
                    6161,
                    new ArcNET.Diagnostics.Contracts.RuntimeFingerprint(
                        "Arcanum",
                        6161,
                        ArcNET.Diagnostics.Contracts.RuntimeKind.Classic,
                        "Arcanum.exe",
                        modulePath,
                        "0x00400000",
                        3538944,
                        0,
                        DateTime.MinValue
                    ),
                    new ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot(
                        "preview",
                        "preview",
                        ArcNET.Diagnostics.Contracts.RuntimeKind.Classic,
                        ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Validated,
                        true,
                        "preview",
                        null,
                        null
                    ),
                    new ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport(
                        ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Validated,
                        ArcNET.Diagnostics.Contracts.DiagnosticsCapability.ReadMemory,
                        []
                    )
                )
            );

            connection.HasExited = true;
            var refreshed = service.Refresh(handle);

            await Assert.That(refreshed.HasExited).IsTrue();
            await Assert.That(refreshed.Summary.Contains("exited", StringComparison.OrdinalIgnoreCase)).IsTrue();
            await Assert
                .That(
                    refreshed.Notes.Any(static note => note.Contains("has exited", StringComparison.OrdinalIgnoreCase))
                )
                .IsTrue();
        }
        finally
        {
            moduleDirectory.Delete(recursive: true);
        }
    }

    private sealed class FakeSessionBackend : ISessionBackend
    {
        public FakeSessionConnection? AttachConnection { get; init; }

        public FakeSessionConnection? LaunchConnection { get; init; }

        public ArcanumLaunchPlan? LaunchPlan { get; init; }

        public ISessionConnection Attach(int processId) =>
            AttachConnection ?? throw new InvalidOperationException("No attach connection was configured.");

        public bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error)
        {
            moduleSha256 = null;
            error = null;
            return true;
        }

        public ArcanumLaunchPlan CreateLaunchPlan(string gamePath, ArcanumLaunchOptions options) =>
            LaunchPlan ?? throw new InvalidOperationException("No launch plan was configured.");

        public ISessionConnection LaunchAndAttach(ArcanumLaunchPlan plan, TimeSpan attachTimeout) =>
            LaunchConnection ?? throw new InvalidOperationException("No launch connection was configured.");
    }

    private sealed class FakeSessionConnection(
        string processName,
        int processId,
        string modulePath,
        nint moduleBase,
        int moduleSize
    ) : ISessionConnection
    {
        public int ProcessId => processId;

        public string ProcessName => processName;

        public string ModulePath => modulePath;

        public nint ModuleBase => moduleBase;

        public int ModuleSize => moduleSize;

        public bool HasExited { get; set; }

        public void Dispose() { }
    }
}
