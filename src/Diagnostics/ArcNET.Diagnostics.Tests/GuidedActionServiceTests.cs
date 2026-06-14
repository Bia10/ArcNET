using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class GuidedActionServiceTests
{
    [Test]
    public async Task Execute_WhenSessionCannotInvokeFunctions_ReturnsUnavailableSnapshot()
    {
        var service = new GuidedActionService(new FakeGuidedActionBackend());

        var snapshot = await service.ExecuteAsync(
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

        var snapshot = await service.ExecuteAsync(
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

    [Test]
    public async Task Execute_WhenDiscoveringWorldMapLocations_LoadsCatalogAndReportsTraversal()
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
            DiscoveryResult = new WorldMapDiscoveryExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "area_set_known x2 · teleport_do x2 · wmap_load_worldmap_info x1",
                "Map 777 · start map 777 · town map 0 · EAX 0x00000001 (1) · EDX 0x00000000 (0)",
                ProcessedLocationCount: 2,
                VisitedLocationCount: 2,
                IsTravelerOnWorldMap: true,
                CurrentMapId: 777,
                StartMapId: 777,
                TownMapId: 0
            ),
        };
        var service = new GuidedActionService(
            backend,
            static _ =>
                Task.FromResult<IReadOnlyList<WorldMapLocationDescriptor>>([
                    new WorldMapLocationDescriptor(12, "Ashbury", 320, 448),
                    new WorldMapLocationDescriptor(35, "Dernholm", 512, 640),
                ])
        );

        var snapshot = await service.ExecuteAsync(
            new GuidedActionRequest(
                CreateSession(),
                "discover_world_map_locations",
                "player",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "1500"
            )
        );

        await Assert.That(backend.LocatePlayersCallCount).IsEqualTo(1);
        await Assert.That(backend.DiscoveryTravelerHandle).IsEqualTo(0x0000000201234567UL);
        await Assert
            .That(backend.DiscoveryLocations.Select(static location => location.AreaId))
            .IsEquivalentTo([12, 35]);
        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.FunctionKey).IsEqualTo("area_set_known");
        await Assert.That(snapshot.Summary).Contains("walked 2 anchors");
        await Assert.That(snapshot.ExecutionDetailText).Contains("teleport_do x2");
        await Assert.That(snapshot.ResultText).Contains("start map 777");
    }

    [Test]
    public async Task Execute_WhenWorldMapWorkspacePathOverrideIsProvided_UsesOverride()
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
            DiscoveryResult = new WorldMapDiscoveryExecutionResult(
                "main-thread-hook",
                "tig_window_display @ tig_window_display (Arcanum.exe+0x0011CF40)",
                "area_set_known x1",
                "Map 777 · start map 777 · town map 0 · EAX 0x00000001 (1) · EDX 0x00000000 (0)",
                ProcessedLocationCount: 1,
                VisitedLocationCount: 1,
                IsTravelerOnWorldMap: true,
                CurrentMapId: 777,
                StartMapId: 777,
                TownMapId: 0
            ),
        };
        var workspacePath = @"C:\Games\Arcanum\modules\test-module";
        var requestedWorkspacePath = string.Empty;
        var service = new GuidedActionService(
            backend,
            path =>
            {
                requestedWorkspacePath = path;
                return Task.FromResult<IReadOnlyList<WorldMapLocationDescriptor>>([
                    new WorldMapLocationDescriptor(12, "Ashbury", 320, 448),
                ]);
            }
        );

        var snapshot = await service.ExecuteAsync(
            new GuidedActionRequest(
                CreateSession() with
                {
                    Fingerprint = new RuntimeFingerprint(
                        "Arcanum",
                        4242,
                        RuntimeKind.Classic,
                        "Arcanum.exe",
                        "",
                        "0x00400000",
                        3_538_944,
                        2_048_000,
                        DateTime.UtcNow
                    ),
                },
                "discover_world_map_locations",
                "player",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "1500",
                workspacePath
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(requestedWorkspacePath).IsEqualTo(workspacePath);
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
        public WorldMapDiscoveryExecutionResult DiscoveryResult { get; init; } =
            new("dispatcher", "dispatcher-site", "detail", "result", 0, 0, false, 0, 0, 0);
        public ulong DiscoveryTravelerHandle { get; private set; }
        public IReadOnlyList<WorldMapLocationDescriptor> DiscoveryLocations { get; private set; } = [];

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

        public WorldMapDiscoveryExecutionResult DiscoverAllWorldMapLocations(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong travelerHandle,
            IReadOnlyList<WorldMapLocationDescriptor> locations,
            TimeSpan timeout
        )
        {
            DiscoveryTravelerHandle = travelerHandle;
            DiscoveryLocations = [.. locations];
            return DiscoveryResult;
        }
    }
}
