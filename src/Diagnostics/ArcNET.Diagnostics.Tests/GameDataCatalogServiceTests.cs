using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics.Tests;

public sealed class GameDataCatalogServiceTests
{
    [Test]
    public async Task Load_WhenBackendReturnsCatalog_ExposesAllCatalogs()
    {
        var service = new GameDataCatalogService(
            new FakeGameDataCatalogBackend
            {
                PrototypeEntries =
                [
                    new PrototypePaletteEntry(
                        14001,
                        "Npc",
                        "proto/critters/wolf.pro",
                        "Wolf",
                        "A hungry wolf.",
                        "Critters",
                        "art/critters/wolf.art"
                    ),
                ],
                WorldMapEntries =
                [
                    new WorldMapCatalogEntry(
                        10,
                        "Tarant",
                        91,
                        98,
                        true,
                        "Industrial city.",
                        "World (91, 98)",
                        "map01 @ (500, 475)",
                        ["map01"]
                    ),
                ],
                TileArtEntries =
                [
                    new TileArtCatalogEntry(
                        0x00000123u,
                        "0x00000123",
                        "drt0bse0a",
                        "Tile",
                        0,
                        0,
                        0,
                        "art/tile/drt0bse0a.art",
                        "Tile art #0 - frame 0 - palette 0"
                    ),
                ],
                StaticObjectEntries =
                [
                    new StaticObjectCatalogEntry(
                        "Sector object",
                        "Lamp Post",
                        "Scenery",
                        "mob:12345678... [12345678-1234-1234-1234-123456789abc]",
                        "12345678-1234-1234-1234-123456789abc",
                        2001,
                        "Lamp Post [2001]",
                        "maps/map01/00010001.sec",
                        "Tile (12, 34)",
                        "Sector object - maps/map01/00010001.sec"
                    ),
                ],
            }
        );

        var snapshot = await service.LoadAsync(CreateRequest());

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Game-data catalog loaded");
        await Assert.That(snapshot.PrototypeEntries.Count).IsEqualTo(1);
        await Assert.That(snapshot.WorldMapEntries.Count).IsEqualTo(1);
        await Assert.That(snapshot.TileArtEntries.Count).IsEqualTo(1);
        await Assert.That(snapshot.StaticObjectEntries.Count).IsEqualTo(1);
        await Assert.That(snapshot.Summary).Contains("prototype entries");
        await Assert.That(snapshot.Summary).Contains("tile-art ids");
    }

    [Test]
    public async Task Load_WhenSessionExposesProcessModulePath_UsesResolvedLocalWorkspacePath()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            var modulePath = Path.Combine(gameDirectory, "Arcanum.exe");

            Directory.CreateDirectory(gameDirectory);
            await File.WriteAllTextAsync(modulePath, "runtime");

            var backend = new FakeGameDataCatalogBackend();
            var service = new GameDataCatalogService(backend);

            var snapshot = await service.LoadAsync(new GameDataCatalogRequest(CreateAttachedSession(modulePath)));

            await Assert.That(snapshot.IsAvailable).IsTrue();
            await Assert.That(backend.LastWorkspacePath).IsEqualTo(gameDirectory);
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Load_WhenSessionExposesModuleScopedProcessPath_UsesResolvedModuleWorkspacePath()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var expectedWorkspacePath = Path.Combine(sandbox.FullName, "Arcanum", "modules", "co8");
            var modulePath = Path.Combine(expectedWorkspacePath, "Arcanum.exe");

            Directory.CreateDirectory(expectedWorkspacePath);
            await File.WriteAllTextAsync(modulePath, "runtime");

            var backend = new FakeGameDataCatalogBackend();
            var service = new GameDataCatalogService(backend);

            var snapshot = await service.LoadAsync(new GameDataCatalogRequest(CreateAttachedSession(modulePath)));

            await Assert.That(snapshot.IsAvailable).IsTrue();
            await Assert.That(backend.LastWorkspacePath).IsEqualTo(expectedWorkspacePath);
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Load_WhenInstallRootProcessPathHasOneModule_UsesPromotedModuleWorkspacePath()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            Directory.CreateDirectory(Path.Combine(gameDirectory, "modules"));
            var modulePath = Path.Combine(gameDirectory, "Arcanum.exe");
            await File.WriteAllTextAsync(modulePath, "runtime");
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "Arcanum.dat"), "module-archive");

            var backend = new FakeGameDataCatalogBackend();
            var service = new GameDataCatalogService(backend);

            var snapshot = await service.LoadAsync(new GameDataCatalogRequest(CreateAttachedSession(modulePath)));

            await Assert.That(snapshot.IsAvailable).IsTrue();
            await Assert.That(backend.LastWorkspacePath).IsEqualTo(Path.Combine(gameDirectory, "modules", "Arcanum"));
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Load_WhenSessionCarriesWorkspacePathHint_UsesHintBeforeProcessPathFallback()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            var modulePath = Path.Combine(gameDirectory, "Arcanum.exe");
            var workspacePathHint = Path.Combine(gameDirectory, "modules", "Vendigroth");

            Directory.CreateDirectory(workspacePathHint);
            await File.WriteAllTextAsync(modulePath, "runtime");

            var backend = new FakeGameDataCatalogBackend();
            var service = new GameDataCatalogService(backend);

            var snapshot = await service.LoadAsync(
                new GameDataCatalogRequest(CreateAttachedSession(modulePath, workspacePathHint))
            );

            await Assert.That(snapshot.IsAvailable).IsTrue();
            await Assert.That(backend.LastWorkspacePath).IsEqualTo(workspacePathHint);
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Load_WhenModulePathIsMissing_ReturnsUnavailableSnapshot()
    {
        var service = new GameDataCatalogService(new FakeGameDataCatalogBackend());
        var snapshot = await service.LoadAsync(
            new GameDataCatalogRequest(
                CreateAttachedSession() with
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
                }
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Game-data catalog unavailable");
        await Assert.That(snapshot.Summary).Contains("workspace path");
    }

    [Test]
    public async Task Load_WhenWorkspacePathOverrideIsProvided_UsesOverrideWhenSessionPathIsMissing()
    {
        var backend = new FakeGameDataCatalogBackend();
        var service = new GameDataCatalogService(backend);
        var workspacePath = @"C:\Games\Arcanum\modules\test-module";

        var snapshot = await service.LoadAsync(
            new GameDataCatalogRequest(
                CreateAttachedSession() with
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
                workspacePath
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(backend.LastWorkspacePath).IsEqualTo(workspacePath);
    }

    [Test]
    public async Task Load_WhenBackendThrows_ReturnsUnavailableSnapshot()
    {
        var service = new GameDataCatalogService(new ThrowingGameDataCatalogBackend());

        var snapshot = await service.LoadAsync(CreateRequest());

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Game-data catalog unavailable");
        await Assert.That(snapshot.Summary).Contains("InvalidOperationException");
    }

    private static GameDataCatalogRequest CreateRequest() => new(CreateAttachedSession());

    private static AttachedSessionSnapshot CreateAttachedSession(
        string modulePath = @"C:\Games\Arcanum\Arcanum.exe",
        string? WorkspacePathHint = null
    ) =>
        new(
            DateTimeOffset.UtcNow,
            SessionOrigin.Attach,
            "Arcanum.exe (PID 4242)",
            "Attached live session",
            $"{modulePath} @ 0x00400000",
            "Arcanum",
            4242,
            HasExited: false,
            new RuntimeFingerprint(
                "Arcanum",
                4242,
                RuntimeKind.Classic,
                Path.GetFileName(modulePath),
                modulePath,
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
            Notes: [],
            WorkspacePathHint: WorkspacePathHint
        );

    private sealed class FakeGameDataCatalogBackend : IGameDataCatalogBackend
    {
        public IReadOnlyList<PrototypePaletteEntry> PrototypeEntries { get; init; } = [];

        public IReadOnlyList<WorldMapCatalogEntry> WorldMapEntries { get; init; } = [];

        public IReadOnlyList<TileArtCatalogEntry> TileArtEntries { get; init; } = [];

        public IReadOnlyList<StaticObjectCatalogEntry> StaticObjectEntries { get; init; } = [];

        public string? LastWorkspacePath { get; private set; }

        public Task<IReadOnlyList<PrototypePaletteEntry>> LoadPrototypePaletteAsync(string workspacePath)
        {
            LastWorkspacePath = workspacePath;
            return Task.FromResult(PrototypeEntries);
        }

        public Task<IReadOnlyList<WorldMapCatalogEntry>> LoadWorldMapCatalogAsync(string workspacePath)
        {
            LastWorkspacePath = workspacePath;
            return Task.FromResult(WorldMapEntries);
        }

        public Task<IReadOnlyList<TileArtCatalogEntry>> LoadTileArtCatalogAsync(string workspacePath)
        {
            LastWorkspacePath = workspacePath;
            return Task.FromResult(TileArtEntries);
        }

        public Task<IReadOnlyList<StaticObjectCatalogEntry>> LoadStaticObjectCatalogAsync(string workspacePath)
        {
            LastWorkspacePath = workspacePath;
            return Task.FromResult(StaticObjectEntries);
        }
    }

    private sealed class ThrowingGameDataCatalogBackend : IGameDataCatalogBackend
    {
        public Task<IReadOnlyList<PrototypePaletteEntry>> LoadPrototypePaletteAsync(string workspacePath) =>
            throw new InvalidOperationException("Workspace loader failed.");

        public Task<IReadOnlyList<WorldMapCatalogEntry>> LoadWorldMapCatalogAsync(string workspacePath) =>
            throw new InvalidOperationException("Workspace loader failed.");

        public Task<IReadOnlyList<TileArtCatalogEntry>> LoadTileArtCatalogAsync(string workspacePath) =>
            throw new InvalidOperationException("Workspace loader failed.");

        public Task<IReadOnlyList<StaticObjectCatalogEntry>> LoadStaticObjectCatalogAsync(string workspacePath) =>
            throw new InvalidOperationException("Workspace loader failed.");
    }
}
