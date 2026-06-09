using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

public sealed class FunctionCallServiceTests
{
    [Test]
    public async Task Invoke_WhenSessionCannotInvokeFunctions_ReturnsUnavailableSnapshot()
    {
        var service = new FunctionCallService(new FakeFunctionCallBackend());

        var snapshot = service.Invoke(
            new FunctionCallRequest(
                CreateSession() with
                {
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "ui_start_dialog",
                "",
                "",
                "",
                UseSuggestedCleanup: true,
                StackCleanupMode.Cdecl,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Function call unavailable");
    }

    [Test]
    public async Task Invoke_WhenHandlePlayerMacrosAreUsed_ExpandsLowAndHighDwords()
    {
        var backend = new FakeFunctionCallBackend
        {
            PlayerResolution = new LivePlayerLocatorResult(
                AutoResolvedHandle: 0x0000000201234567,
                "SingleLivePcInstance",
                "Your likely live player is Live PC instance hero (0x0000000201234567).",
                [],
                [],
                []
            ),
            Result = new FunctionCallExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0008B9B0)",
                "0x004609E0",
                1,
                0,
                "Completed"
            ),
        };
        var service = new FunctionCallService(backend);

        var snapshot = service.Invoke(
            new FunctionCallRequest(
                CreateSession(),
                "ui_start_dialog",
                "handle(player), 0x20, -1",
                "handle_low(player)",
                "handle_high(player)",
                UseSuggestedCleanup: true,
                StackCleanupMode.StdCall,
                "1500"
            )
        );

        await Assert.That(backend.LocatePlayersCallCount).IsEqualTo(1);
        await Assert.That(backend.TargetRva).IsEqualTo(FunctionCatalog.GetDefinition("ui_start_dialog").Rva);
        await Assert
            .That(backend.CleanupMode)
            .IsEqualTo(FunctionCatalog.GetDefinition("ui_start_dialog").SuggestedCleanup);
        await Assert.That(backend.EcxValue).IsEqualTo(0x01234567u);
        await Assert.That(backend.EdxValue).IsEqualTo(0x00000002u);
        await Assert.That(backend.StackArguments).IsEquivalentTo([0x01234567u, 0x00000002u, 0x20u, 0xFFFFFFFFu]);
        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Call completed");
        await Assert.That(snapshot.Arguments).Count().IsEqualTo(4);
        await Assert.That(snapshot.Arguments[0].SourceText).IsEqualTo("handle(player) [low]");
        await Assert.That(snapshot.ResultEaxText).IsEqualTo("0x00000001 (1)");
    }

    [Test]
    public async Task Invoke_WhenRawRvaIsProvided_UsesManualCleanupAndCustomDwords()
    {
        var backend = new FakeFunctionCallBackend
        {
            Result = new FunctionCallExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0008B9B0)",
                "0x004609E0",
                0x89ABCDEF,
                0x00000007,
                "Completed"
            ),
        };
        var service = new FunctionCallService(backend);

        var snapshot = service.Invoke(
            new FunctionCallRequest(
                CreateSession(),
                "0x000609E0",
                "1, 2, 0xFFFFFFFF",
                "0x10",
                "0x20",
                UseSuggestedCleanup: false,
                StackCleanupMode.StdCall,
                "2500"
            )
        );

        await Assert.That(backend.LocatePlayersCallCount).IsEqualTo(0);
        await Assert.That(backend.TargetRva).IsEqualTo(unchecked((int)0x000609E0u));
        await Assert.That(backend.CleanupMode).IsEqualTo(StackCleanupMode.StdCall);
        await Assert.That(backend.EcxValue).IsEqualTo(0x10u);
        await Assert.That(backend.EdxValue).IsEqualTo(0x20u);
        await Assert.That(backend.StackArguments).IsEquivalentTo([1u, 2u, 0xFFFFFFFFu]);
        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.TargetKey).IsEqualTo("raw_rva_0x000609E0");
        await Assert.That(snapshot.CleanupModeText).IsEqualTo("Manual StdCall");
        await Assert.That(snapshot.ResultEaxText).IsEqualTo("0x89ABCDEF (-1985229329)");
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
                    | DiagnosticsCapability.ReadStructuredState
                    | DiagnosticsCapability.InvokeFunctions,
                []
            ),
            LaunchPreview: null,
            Notes: []
        );

    private sealed class FakeFunctionCallBackend : IFunctionCallBackend
    {
        public LivePlayerLocatorResult PlayerResolution { get; init; }
        public FunctionCallExecutionResult Result { get; init; } =
            new("dispatcher", "dispatcher-site", "0x00400000", 0, 0, "Completed");

        public int LocatePlayersCallCount { get; private set; }
        public int TargetRva { get; private set; }
        public StackCleanupMode CleanupMode { get; private set; }
        public uint EcxValue { get; private set; }
        public uint EdxValue { get; private set; }
        public IReadOnlyList<uint> StackArguments { get; private set; } = [];

        public LivePlayerLocatorResult LocatePlayers(int processId)
        {
            LocatePlayersCallCount++;
            return PlayerResolution;
        }

        public FunctionCallExecutionResult InvokeCall(
            int processId,
            int targetRva,
            RuntimeProfileSnapshot runtimeProfile,
            StackCleanupMode cleanupMode,
            uint ecxValue,
            uint edxValue,
            IReadOnlyList<uint> stackArguments,
            TimeSpan timeout
        )
        {
            TargetRva = targetRva;
            CleanupMode = cleanupMode;
            EcxValue = ecxValue;
            EdxValue = edxValue;
            StackArguments = [.. stackArguments];
            return Result;
        }
    }
}
