using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

public sealed class GuidedActionServiceTests
{
    [Test]
    public async Task Execute_WhenSessionCannotInvokeFunctions_ReturnsUnavailableSnapshot()
    {
        var service = new GuidedActionService(new FakeGuidedActionBackend());

        var snapshot = service.Execute(
            new GuidedActionRequest(
                CreateSession() with
                {
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "teleport_traveler",
                "player",
                "480",
                "512",
                "-1",
                "0",
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Guided action unavailable");
    }

    [Test]
    public async Task Execute_WhenTeleportUsesPlayerToken_ParsesUserFacingInputs()
    {
        var backend = new FakeGuidedActionBackend
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
                "0x004D3380",
                1,
                0,
                "Completed"
            ),
        };
        var service = new GuidedActionService(backend);

        var snapshot = service.Execute(
            new GuidedActionRequest(CreateSession(), "teleport_traveler", "player", "480", "512", "-1", "0x4", "1500")
        );

        await Assert.That(backend.LocatePlayersCallCount).IsEqualTo(1);
        await Assert.That(backend.TravelerHandle).IsEqualTo(0x0000000201234567UL);
        await Assert.That(backend.TileX).IsEqualTo(480);
        await Assert.That(backend.TileY).IsEqualTo(512);
        await Assert.That(backend.MapId).IsEqualTo(-1);
        await Assert.That(backend.Flags).IsEqualTo(0x4u);
        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Action completed");
        await Assert.That(snapshot.FunctionKey).IsEqualTo("teleport_do");
        await Assert.That(snapshot.ResultText).Contains("0x00000001 (1)");
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
                3_538_944,
                2_048_000,
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

    private sealed class FakeGuidedActionBackend : IGuidedActionBackend
    {
        public LivePlayerLocatorResult PlayerResolution { get; init; }
        public FunctionCallExecutionResult Result { get; init; } =
            new("dispatcher", "dispatcher-site", "0x00400000", 0, 0, "Completed");

        public int LocatePlayersCallCount { get; private set; }
        public ulong TravelerHandle { get; private set; }
        public int TileX { get; private set; }
        public int TileY { get; private set; }
        public int MapId { get; private set; }
        public uint Flags { get; private set; }

        public LivePlayerLocatorResult LocatePlayers(int processId)
        {
            LocatePlayersCallCount++;
            return PlayerResolution;
        }

        public FunctionCallExecutionResult ExecuteTeleport(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong travelerHandle,
            int tileX,
            int tileY,
            int mapId,
            uint flags,
            TimeSpan timeout
        )
        {
            TravelerHandle = travelerHandle;
            TileX = tileX;
            TileY = tileY;
            MapId = mapId;
            Flags = flags;
            return Result;
        }
    }
}
