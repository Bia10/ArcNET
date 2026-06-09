using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

[SupportedOSPlatform("windows")]
public sealed class AuditServiceTests
{
    [Test]
    public async Task Run_WhenAllSectionsRequested_DelegatesToBackendAndIncludesHookAuditNotes()
    {
        var backend = new FakeAuditBackend();
        var service = new AuditService(backend);

        var snapshot = service.Run(
            new AuditRequest(
                CreateSession(),
                IncludeDispatcher: true,
                IncludeFunctions: true,
                IncludeHooks: true,
                ["session-core"],
                TimeSpan.FromMilliseconds(50),
                IncludeWatchPass: true,
                IncludeInterceptPass: true,
                StackCaptureDwordCount: 8,
                StopOnFailure: false
            )
        );

        await Assert.That(backend.DispatcherCallCount).IsEqualTo(1);
        await Assert.That(backend.FunctionsCallCount).IsEqualTo(1);
        await Assert.That(backend.HooksCallCount).IsEqualTo(1);
        await Assert.That(snapshot.Dispatcher).IsNotNull();
        await Assert.That(snapshot.Functions).IsNotNull();
        await Assert.That(snapshot.Hooks).IsNotNull();
        await Assert.That(snapshot.Notes.Count).IsGreaterThan(2);
        await Assert.That(snapshot.Notes.Last()).Contains("sample window");
    }

    [Test]
    public async Task Run_WhenSectionIsNotRequested_SkipsThatBackendCall()
    {
        var backend = new FakeAuditBackend();
        var service = new AuditService(backend);

        var snapshot = service.Run(
            new AuditRequest(
                CreateSession(),
                IncludeDispatcher: false,
                IncludeFunctions: true,
                IncludeHooks: false,
                [],
                TimeSpan.FromMilliseconds(50),
                IncludeWatchPass: true,
                IncludeInterceptPass: true,
                StackCaptureDwordCount: 8,
                StopOnFailure: false
            )
        );

        await Assert.That(backend.DispatcherCallCount).IsEqualTo(0);
        await Assert.That(backend.FunctionsCallCount).IsEqualTo(1);
        await Assert.That(backend.HooksCallCount).IsEqualTo(0);
        await Assert.That(snapshot.Dispatcher).IsNull();
        await Assert.That(snapshot.Hooks).IsNull();
        await Assert.That(snapshot.Functions).IsNotNull();
    }

    [Test]
    public async Task Run_WhenHookAuditDisablesBothPasses_Throws()
    {
        var service = new AuditService(new FakeAuditBackend());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Task.Run(() =>
                service.Run(
                    new AuditRequest(
                        CreateSession(),
                        IncludeDispatcher: false,
                        IncludeFunctions: false,
                        IncludeHooks: true,
                        ["all"],
                        TimeSpan.FromMilliseconds(50),
                        IncludeWatchPass: false,
                        IncludeInterceptPass: false,
                        StackCaptureDwordCount: 8,
                        StopOnFailure: false
                    )
                )
            )
        );
    }

    private static AttachedSessionSnapshot CreateSession() =>
        new(
            DateTimeOffset.UtcNow,
            SessionOrigin.Attach,
            "Arcanum.exe (PID 4242)",
            "Attached live session",
            @"C:\Games\Arcanum\Arcanum.exe @ 0x00400000",
            "Arcanum",
            4242,
            HasExited: false,
            new RuntimeFingerprint(
                "Arcanum",
                4242,
                RuntimeKind.Classic,
                "Arcanum.exe",
                @"C:\Games\Arcanum\Arcanum.exe",
                "0x00400000",
                3538944,
                3538944,
                DateTime.UtcNow
            ),
            new RuntimeProfileSnapshot(
                "validated-classic",
                "Arcanum.exe validated runtime profile",
                RuntimeKind.Classic,
                RuntimeSupportLevel.Validated,
                SupportsCatalogRvas: true,
                "Validated classic profile.",
                ModuleSha256: null,
                HashError: null
            ),
            new RuntimeCapabilityReport(
                RuntimeSupportLevel.Validated,
                DiagnosticsCapability.ReadMemory
                    | DiagnosticsCapability.ResolveRuntimeProfile
                    | DiagnosticsCapability.WatchHooks
                    | DiagnosticsCapability.InvokeFunctions
                    | DiagnosticsCapability.InterceptFunctions,
                []
            ),
            LaunchPreview: null,
            Notes: ["Existing session note."]
        );

    private sealed class FakeAuditBackend : IAuditBackend
    {
        public int DispatcherCallCount { get; private set; }

        public int FunctionsCallCount { get; private set; }

        public int HooksCallCount { get; private set; }

        public DispatcherAuditSnapshot AuditDispatcher(int processId, RuntimeProfileSnapshot runtimeProfile)
        {
            DispatcherCallCount++;
            return new DispatcherAuditSnapshot(true, "main-thread-hook", "tig_window_display", null);
        }

        public FunctionAuditSnapshot AuditFunctions(int processId)
        {
            FunctionsCallCount++;
            return new FunctionAuditSnapshot(
                1,
                1,
                0,
                [
                    new FunctionAuditResultSnapshot(
                        "teleport_do",
                        true,
                        "Arcanum.exe+0x00123456",
                        "catalog-rva",
                        "Teleport entrypoint",
                        null
                    ),
                ]
            );
        }

        public HookAuditSnapshot AuditHooks(
            int processId,
            IReadOnlyList<string> selectors,
            TimeSpan duration,
            bool includeWatch,
            bool includeIntercept,
            int stackCaptureDwordCount,
            bool stopOnFailure
        )
        {
            HooksCallCount++;
            return new HookAuditSnapshot(
                [.. selectors],
                checked((int)duration.TotalMilliseconds),
                includeWatch,
                includeIntercept,
                stackCaptureDwordCount,
                1,
                1,
                0,
                includeWatch ? 1 : 0,
                0,
                includeIntercept ? 1 : 0,
                0,
                includeWatch ? 1 : 0,
                includeIntercept ? 1 : 0,
                0,
                0,
                ProcessExited: false,
                AbortedAtHook: null,
                [
                    new HookAuditResultSnapshot(
                        "teleport-do",
                        "World",
                        new HookBindAuditSnapshot(true, "Arcanum.exe+0x00123456", null),
                        includeWatch
                            ? new HookPassAuditSnapshot(true, true, 1, 0, 0, 0, "Arcanum.exe+0x00432100", null)
                            : null,
                        includeIntercept
                            ? new HookPassAuditSnapshot(true, true, 1, 0, 0, 0, "Arcanum.exe+0x00432100", null)
                            : null
                    ),
                ]
            );
        }
    }
}
