using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Patch;

namespace ArcNET.Diagnostics.Tests;

public sealed class SessionServiceTests
{
    [Test]
    public async Task Attach_WhenRuntimeIsSelected_CreatesAttachedSessionSnapshot()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var moduleDirectory = Path.Combine(sandbox.FullName, "Arcanum", "modules", "co8");
            Directory.CreateDirectory(moduleDirectory);
            var modulePath = Path.Combine(moduleDirectory, "Arcanum.exe");
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
            await Assert.That(handle.Snapshot.LocalWorkspacePath).IsEqualTo(moduleDirectory);
            await Assert
                .That(handle.Snapshot.Summary.Contains("Attached", StringComparison.OrdinalIgnoreCase))
                .IsTrue();
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Attach_WhenInstallRootRuntimeHasOneModule_PromotesLocalWorkspacePathToModuleDirectory()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            Directory.CreateDirectory(Path.Combine(gameDirectory, "modules"));
            var modulePath = Path.Combine(gameDirectory, "Arcanum.exe");
            await File.WriteAllTextAsync(modulePath, "attached-runtime");
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "Arcanum.dat"), "module-archive");

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

            await Assert
                .That(handle.Snapshot.LocalWorkspacePath)
                .IsEqualTo(Path.Combine(gameDirectory, "modules", "Arcanum"));
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Attach_WhenWorkspacePathHintIsProvided_PrefersHintOverInstallRootFallback()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            var hintedModuleDirectory = Path.Combine(gameDirectory, "modules", "Vendigroth");
            Directory.CreateDirectory(Path.Combine(gameDirectory, "modules"));
            Directory.CreateDirectory(hintedModuleDirectory);
            var modulePath = Path.Combine(gameDirectory, "Arcanum.exe");
            await File.WriteAllTextAsync(modulePath, "attached-runtime");

            var backend = new FakeSessionBackend
            {
                AttachConnection = new FakeSessionConnection("Arcanum", 4242, modulePath, (nint)0x00400000, 3538944),
            };
            var service = new SessionService(backend);

            using var handle = service.Attach(
                CreateLiveRuntime(modulePath, 4242),
                workspacePathHint: $"  {hintedModuleDirectory}  "
            );

            await Assert.That(handle.Snapshot.LocalWorkspacePath).IsEqualTo(hintedModuleDirectory);
            await Assert
                .That(
                    handle.Snapshot.Notes.Any(static note => note.Contains("Workspace hint", StringComparison.Ordinal))
                )
                .IsTrue();
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Attach_WhenBackendProvidesRuntimeWorkspaceHint_PrefersItOverInstallRootFallback()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            var hintedModuleDirectory = Path.Combine(gameDirectory, "modules", "Vendigroth");
            Directory.CreateDirectory(Path.Combine(gameDirectory, "modules"));
            Directory.CreateDirectory(hintedModuleDirectory);
            var modulePath = Path.Combine(gameDirectory, "Arcanum.exe");
            await File.WriteAllTextAsync(modulePath, "attached-runtime");

            var backend = new FakeSessionBackend
            {
                AttachConnection = new FakeSessionConnection("Arcanum", 4242, modulePath, (nint)0x00400000, 3538944),
                RuntimeWorkspacePathHint = hintedModuleDirectory,
            };
            var service = new SessionService(backend);

            using var handle = service.Attach(CreateLiveRuntime(modulePath, 4242));

            await Assert.That(handle.Snapshot.LocalWorkspacePath).IsEqualTo(hintedModuleDirectory);
            await Assert
                .That(
                    handle.Snapshot.Notes.Any(static note =>
                        note.Contains("Runtime workspace hint", StringComparison.Ordinal)
                    )
                )
                .IsTrue();
        }
        finally
        {
            sandbox.Delete(recursive: true);
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
            await Assert.That(handle.Snapshot.LocalWorkspacePath).IsEqualTo(moduleDirectory.FullName);
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
    public async Task SetWorkspacePathHint_WhenAppliedToActiveSession_RebuildsSnapshot()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            var hintedModuleDirectory = Path.Combine(gameDirectory, "modules", "Vendigroth");
            Directory.CreateDirectory(Path.Combine(gameDirectory, "modules"));
            Directory.CreateDirectory(hintedModuleDirectory);
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "Arcanum.dat"), "base-module");
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "co8.dat"), "secondary-module");
            var modulePath = Path.Combine(gameDirectory, "Arcanum.exe");
            await File.WriteAllTextAsync(modulePath, "refresh-runtime");

            var connection = new FakeSessionConnection("Arcanum", 6161, modulePath, (nint)0x00400000, 3538944);
            var backend = new FakeSessionBackend { AttachConnection = connection };
            var service = new SessionService(backend);

            using var handle = service.Attach(CreateLiveRuntime(modulePath, 6161));

            await Assert.That(handle.Snapshot.LocalWorkspacePath).IsEqualTo(gameDirectory);

            var updated = service.SetWorkspacePathHint(handle, hintedModuleDirectory);

            await Assert.That(updated.LocalWorkspacePath).IsEqualTo(hintedModuleDirectory);
            await Assert.That(handle.Snapshot.LocalWorkspacePath).IsEqualTo(hintedModuleDirectory);
        }
        finally
        {
            sandbox.Delete(recursive: true);
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

        public string? RuntimeWorkspacePathHint { get; init; }

        public ISessionConnection Attach(int processId) =>
            AttachConnection ?? throw new InvalidOperationException("No attach connection was configured.");

        public string? TryResolveWorkspacePathHint(
            ISessionConnection connection,
            RuntimeProfileSnapshot runtimeProfile
        ) => RuntimeWorkspacePathHint;

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

    private static LiveRuntimeSnapshot CreateLiveRuntime(string modulePath, int processId) =>
        new(
            $"live-Arcanum-{processId}",
            $"Arcanum.exe (PID {processId})",
            "Preview runtime",
            "Arcanum",
            processId,
            new ArcNET.Diagnostics.Contracts.RuntimeFingerprint(
                "Arcanum",
                processId,
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
        );
}
