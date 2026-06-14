using System.Runtime.Versioning;
using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameData.Workspace;
using ArcNET.Patch;

namespace ArcanumDebugger.App.Tests;

[NotInParallel]
[SupportedOSPlatform("windows")]
public sealed class MainWindowViewModelWorkspaceOverrideTests
{
    [Test]
    public async Task WorkspaceOverridePath_UpdatesSourceTextAndInvalidatesLoadedGameDataCatalog()
    {
        var sandbox = Directory.CreateTempSubdirectory("arcnet-game-data-override-");
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            var baseWorkspacePath = Path.Combine(gameDirectory, "Arcanum.exe");
            var expectedBaseWorkspacePath = gameDirectory;
            var overrideWorkspacePath = Path.Combine(gameDirectory, "modules", "Vendigroth");

            Directory.CreateDirectory(overrideWorkspacePath);
            await File.WriteAllTextAsync(baseWorkspacePath, "test-runtime");
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "Arcanum.dat"), "base-module");
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "co8.dat"), "secondary-module");

            var gameDataBackend = new FakeGameDataCatalogBackend(overrideWorkspacePath);
            using var viewModel = new MainWindowViewModel(new FakeDiagnosticsServices(gameDataBackend));
            viewModel.ActiveSession = CreateSession(baseWorkspacePath);

            await viewModel.BrowseLookupPrototypeCatalogCommand.ExecuteAsync(null);

            await Assert.That(gameDataBackend.RequestedPrototypeWorkspacePaths.Count).IsEqualTo(1);
            await Assert
                .That(gameDataBackend.RequestedPrototypeWorkspacePaths[^1])
                .IsEqualTo(expectedBaseWorkspacePath);
            await Assert.That(viewModel.GameDataCatalogStatusText).IsEqualTo("Game-data catalog loaded");
            await Assert.That(viewModel.GameDataCatalogPresetSourceText).IsEqualTo("Local workspace: Arcanum");
            await Assert.That(viewModel.GameDataCatalogPrototypeEntries.Count).IsEqualTo(1);
            await Assert.That(viewModel.GameDataCatalogPrototypeEntries[0].DisplayName).IsEqualTo("Base Wolf");

            viewModel.WorkspaceOverridePathText = $"  {overrideWorkspacePath}  ";

            await Assert.That(viewModel.WorkspaceSourceText).IsEqualTo("Local workspace override: Vendigroth");
            await Assert
                .That(viewModel.GameDataCatalogPresetSourceText)
                .IsEqualTo("Local workspace override: Vendigroth");
            await Assert
                .That(viewModel.GameDataCatalogStatusText)
                .IsEqualTo("Local workspace data ready to load for Vendigroth.");
            await Assert.That(viewModel.GameDataCatalogPrototypeEntries).IsEmpty();

            await viewModel.BrowseLookupPrototypeCatalogCommand.ExecuteAsync(null);

            await Assert.That(gameDataBackend.RequestedPrototypeWorkspacePaths.Count).IsEqualTo(2);
            await Assert.That(gameDataBackend.RequestedPrototypeWorkspacePaths[^1]).IsEqualTo(overrideWorkspacePath);
            await Assert.That(viewModel.GameDataCatalogStatusText).IsEqualTo("Game-data catalog loaded");
            await Assert
                .That(viewModel.GameDataCatalogPresetSourceText)
                .IsEqualTo("Local workspace override: Vendigroth");
            await Assert.That(viewModel.GameDataCatalogPrototypeEntries.Count).IsEqualTo(1);
            await Assert.That(viewModel.GameDataCatalogPrototypeEntries[0].DisplayName).IsEqualTo("Override Wolf");

            viewModel.ClearWorkspaceOverridePathCommand.Execute(null);

            await Assert.That(viewModel.WorkspaceSourceText).IsEqualTo("Local workspace: Arcanum");
            await Assert.That(viewModel.GameDataCatalogPresetSourceText).IsEqualTo("Local workspace: Arcanum");
            await Assert
                .That(viewModel.GameDataCatalogStatusText)
                .IsEqualTo("Local workspace data ready to load for Arcanum.");
            await Assert.That(viewModel.GameDataCatalogPrototypeEntries).IsEmpty();
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task WorkspaceOverridePath_WhenSessionModulePathIsModuleScoped_UsesModuleWorkspaceByDefault()
    {
        var sandbox = Directory.CreateTempSubdirectory("arcnet-module-workspace-");
        try
        {
            var expectedBaseWorkspacePath = Path.Combine(sandbox.FullName, "Arcanum", "modules", "Vendigroth");
            var baseWorkspacePath = Path.Combine(expectedBaseWorkspacePath, "Arcanum.exe");

            Directory.CreateDirectory(expectedBaseWorkspacePath);
            await File.WriteAllTextAsync(baseWorkspacePath, "test-runtime");

            var gameDataBackend = new FakeGameDataCatalogBackend(expectedBaseWorkspacePath);
            using var viewModel = new MainWindowViewModel(new FakeDiagnosticsServices(gameDataBackend));
            viewModel.ActiveSession = CreateSession(baseWorkspacePath);

            await viewModel.BrowseLookupPrototypeCatalogCommand.ExecuteAsync(null);

            await Assert.That(gameDataBackend.RequestedPrototypeWorkspacePaths.Count).IsEqualTo(1);
            await Assert
                .That(gameDataBackend.RequestedPrototypeWorkspacePaths[^1])
                .IsEqualTo(expectedBaseWorkspacePath);
            await Assert.That(viewModel.GameDataCatalogPresetSourceText).IsEqualTo("Local workspace: Vendigroth");
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task WorkspaceOverridePath_InvalidatesLoadedLogbookCatalog()
    {
        var sandbox = Directory.CreateTempSubdirectory("arcnet-logbook-override-");
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            var baseWorkspacePath = Path.Combine(gameDirectory, "Arcanum.exe");
            var expectedBaseWorkspacePath = gameDirectory;
            var overrideWorkspacePath = Path.Combine(gameDirectory, "modules", "Vendigroth");

            Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
            Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "rules"));
            Directory.CreateDirectory(Path.Combine(overrideWorkspacePath, "mes"));
            Directory.CreateDirectory(Path.Combine(gameDirectory, "modules"));
            await File.WriteAllTextAsync(baseWorkspacePath, "test-runtime");
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "Arcanum.dat"), "base-module");
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "co8.dat"), "secondary-module");

            var logbookEditorBackend = new FakeLogbookEditorBackend(overrideWorkspacePath);
            using var viewModel = new MainWindowViewModel(
                new FakeDiagnosticsServices(logbookEditorBackend: logbookEditorBackend)
            );
            viewModel.ActiveSession = CreateSession(baseWorkspacePath);

            await viewModel.LoadLogbookCatalogCommand.ExecuteAsync(null);

            await Assert.That(logbookEditorBackend.RequestedWorkspacePaths.Count).IsEqualTo(1);
            await Assert.That(logbookEditorBackend.RequestedWorkspacePaths[^1]).IsEqualTo(expectedBaseWorkspacePath);
            await Assert.That(viewModel.LogbookCatalogStatusText).IsEqualTo("Logbook catalog loaded");
            await Assert.That(viewModel.LogbookCatalogEntries.Count).IsEqualTo(1);
            await Assert.That(viewModel.LogbookCatalogEntries[0].DisplayName).IsEqualTo("Base Quest");

            viewModel.WorkspaceOverridePathText = $"  {overrideWorkspacePath}  ";

            await Assert.That(viewModel.LogbookCatalogStatusText).IsEqualTo("Journal catalog not loaded.");
            await Assert.That(viewModel.LogbookCatalogEntries).IsEmpty();

            await viewModel.LoadLogbookCatalogCommand.ExecuteAsync(null);

            await Assert.That(logbookEditorBackend.RequestedWorkspacePaths.Count).IsEqualTo(2);
            await Assert.That(logbookEditorBackend.RequestedWorkspacePaths[^1]).IsEqualTo(overrideWorkspacePath);
            await Assert.That(viewModel.LogbookCatalogStatusText).IsEqualTo("Logbook catalog loaded");
            await Assert.That(viewModel.LogbookCatalogEntries.Count).IsEqualTo(1);
            await Assert.That(viewModel.LogbookCatalogEntries[0].DisplayName).IsEqualTo("Override Quest");

            viewModel.ClearWorkspaceOverridePathCommand.Execute(null);

            await Assert.That(viewModel.LogbookCatalogStatusText).IsEqualTo("Journal catalog not loaded.");
            await Assert.That(viewModel.LogbookCatalogEntries).IsEmpty();
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task LaunchAndAttach_WhenWorkspaceOverrideIsSet_PromotesHintIntoSessionIdentity()
    {
        var sandbox = Directory.CreateTempSubdirectory("arcnet-session-hint-");
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            var hintedModuleDirectory = Path.Combine(gameDirectory, "modules", "Vendigroth");
            Directory.CreateDirectory(gameDirectory);
            Directory.CreateDirectory(hintedModuleDirectory);
            Directory.CreateDirectory(Path.Combine(gameDirectory, "modules"));

            var modulePath = Path.Combine(gameDirectory, "Arcanum.exe");
            await File.WriteAllTextAsync(modulePath, "runtime");
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "Arcanum.dat"), "base-module");
            await File.WriteAllTextAsync(Path.Combine(gameDirectory, "modules", "co8.dat"), "secondary-module");

            var sessionService = new SessionService(
                new FakeSessionBackend
                {
                    LaunchPlan = new ArcanumLaunchPlan(
                        ArcanumExecutableKind.Classic,
                        modulePath,
                        gameDirectory,
                        [],
                        new Dictionary<string, string>()
                    ),
                    LaunchConnection = new FakeSessionConnection(
                        "Arcanum",
                        4242,
                        modulePath,
                        (nint)0x00400000,
                        3_538_944
                    ),
                }
            );
            using var viewModel = new MainWindowViewModel(new FakeDiagnosticsServices(sessionService: sessionService))
            {
                InstallPath = gameDirectory,
                WorkspaceOverridePathText = $"  {hintedModuleDirectory}  ",
            };

            await viewModel.LaunchAndAttachCommand.ExecuteAsync(null);

            await Assert.That(viewModel.ActiveSession).IsNotNull();
            await Assert.That(viewModel.ActiveSession!.LocalWorkspacePath).IsEqualTo(hintedModuleDirectory);
            await Assert.That(viewModel.WorkspaceSourceText).IsEqualTo("Local workspace override: Vendigroth");

            viewModel.ClearWorkspaceOverridePathCommand.Execute(null);

            await Assert.That(viewModel.ActiveSession.LocalWorkspacePath).IsEqualTo(gameDirectory);
            await Assert.That(viewModel.WorkspaceSourceText).IsEqualTo("Local workspace: Arcanum");
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    private static AttachedSessionSnapshot CreateSession(string modulePath) =>
        new(
            DateTimeOffset.UtcNow,
            SessionOrigin.Attach,
            "Arcanum.exe (PID 4242)",
            "Attached session",
            $"{modulePath} @ 0x00400000",
            "Arcanum",
            4242,
            HasExited: false,
            new RuntimeFingerprint(
                "Arcanum",
                4242,
                RuntimeKind.Classic,
                "Arcanum.exe",
                modulePath,
                "0x00400000",
                3_538_944,
                0,
                DateTime.MinValue
            ),
            new RuntimeProfileSnapshot(
                Id: "test-runtime",
                DisplayName: "Test runtime",
                RuntimeKind: RuntimeKind.Classic,
                SupportLevel: RuntimeSupportLevel.Unsupported,
                SupportsCatalogRvas: false,
                Notes: "Unit-test runtime.",
                ModuleSha256: null,
                HashError: null
            ),
            new RuntimeCapabilityReport(RuntimeSupportLevel.Unsupported, DiagnosticsCapability.None, []),
            null,
            []
        );

    private sealed class FakeDiagnosticsServices : IDiagnosticsServices
    {
        public FakeDiagnosticsServices(
            FakeGameDataCatalogBackend? gameDataCatalogBackend = null,
            FakeLogbookEditorBackend? logbookEditorBackend = null,
            SessionService? sessionService = null
        )
        {
            EnvironmentService = new EnvironmentService(new FakeEnvironmentBackend());
            GameDataCatalogService = gameDataCatalogBackend is null
                ? null!
                : new GameDataCatalogService(gameDataCatalogBackend);
            LogbookEditorService = logbookEditorBackend is null
                ? null!
                : new LogbookEditorService(logbookEditorBackend);
            SessionService = sessionService ?? null!;
        }

        public AuditService AuditService => null!;

        public EnvironmentService EnvironmentService { get; }

        public FunctionCallService FunctionCallService => null!;

        public GameDataCatalogService GameDataCatalogService { get; }

        public GuidedActionService GuidedActionService => null!;

        public InterceptTargetResolver InterceptTargetResolver => null!;

        public ModuleSymbolQueryService ModuleSymbolQueryService => null!;

        public InventoryEditorService InventoryEditorService => null!;

        public LogbookEditorService LogbookEditorService { get; }

        public LogbookService LogbookService => null!;

        public MobileEntityService MobileEntityService => null!;

        public ObjectProbeService ObjectProbeService => null!;

        public InterceptService InterceptService => null!;

        public PrototypeResolutionService PrototypeResolutionService => null!;

        public ReadService ReadService => null!;

        public RuntimeStatusService RuntimeStatusService => null!;

        public CrashDumpService CrashDumpService => null!;

        public CrashDumpAnalysisService CrashDumpAnalysisService => null!;

        public ScriptAttachmentService ScriptAttachmentService => null!;

        public SessionService SessionService { get; }

        public SheetService SheetService => null!;

        public SheetEditorService SheetEditorService => null!;

        public SpellTechEditorService SpellTechEditorService => null!;

        public WatchService WatchService => null!;
    }

    private sealed class FakeEnvironmentBackend : IEnvironmentBackend
    {
        public IReadOnlyList<RunningProcessInfo> GetRunningProcesses(IReadOnlyList<string> processNames) => [];

        public bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error)
        {
            moduleSha256 = null;
            error = null;
            return false;
        }

        public ArcanumLaunchPlan CreateLaunchPlan(string gamePath, ArcanumLaunchOptions options) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSessionBackend : ISessionBackend
    {
        public FakeSessionConnection? AttachConnection { get; init; }

        public FakeSessionConnection? LaunchConnection { get; init; }

        public ArcanumLaunchPlan? LaunchPlan { get; init; }

        public ISessionConnection Attach(int processId) =>
            AttachConnection ?? throw new InvalidOperationException("No attach connection was configured.");

        public string? TryResolveWorkspacePathHint(
            ISessionConnection connection,
            RuntimeProfileSnapshot runtimeProfile
        ) => null;

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

        public bool HasExited => false;

        public void Dispose() { }
    }

    private sealed class FakeGameDataCatalogBackend(string overrideWorkspacePath) : IGameDataCatalogBackend
    {
        public List<string> RequestedPrototypeWorkspacePaths { get; } = [];

        public Task<IReadOnlyList<PrototypePaletteEntry>> LoadPrototypePaletteAsync(string workspacePath)
        {
            RequestedPrototypeWorkspacePaths.Add(workspacePath);
            return Task.FromResult<IReadOnlyList<PrototypePaletteEntry>>([
                string.Equals(workspacePath, overrideWorkspacePath, StringComparison.OrdinalIgnoreCase)
                    ? new PrototypePaletteEntry(
                        14_002,
                        "Npc",
                        "proto/critters/override-wolf.pro",
                        "Override Wolf",
                        "Override workspace entry.",
                        "Critters",
                        "art/critters/override-wolf.art"
                    )
                    : new PrototypePaletteEntry(
                        14_001,
                        "Npc",
                        "proto/critters/base-wolf.pro",
                        "Base Wolf",
                        "Base workspace entry.",
                        "Critters",
                        "art/critters/base-wolf.art"
                    ),
            ]);
        }

        public Task<IReadOnlyList<WorldMapCatalogEntry>> LoadWorldMapCatalogAsync(string workspacePath) =>
            Task.FromResult<IReadOnlyList<WorldMapCatalogEntry>>([]);

        public Task<IReadOnlyList<TileArtCatalogEntry>> LoadTileArtCatalogAsync(string workspacePath) =>
            Task.FromResult<IReadOnlyList<TileArtCatalogEntry>>([]);

        public Task<IReadOnlyList<StaticObjectCatalogEntry>> LoadStaticObjectCatalogAsync(string workspacePath) =>
            Task.FromResult<IReadOnlyList<StaticObjectCatalogEntry>>([]);
    }

    private sealed class FakeLogbookEditorBackend(string overrideWorkspacePath) : ILogbookEditorBackend
    {
        public List<string> RequestedWorkspacePaths { get; } = [];

        public Task<IReadOnlyList<LogbookCatalogEntrySnapshot>> LoadCatalogAsync(string workspacePath)
        {
            RequestedWorkspacePaths.Add(workspacePath);
            return Task.FromResult<IReadOnlyList<LogbookCatalogEntrySnapshot>>([
                string.Equals(workspacePath, overrideWorkspacePath, StringComparison.OrdinalIgnoreCase)
                    ? new LogbookCatalogEntrySnapshot(
                        "quest",
                        1002,
                        0,
                        "Override Quest",
                        "Override workspace journal entry."
                    )
                    : new LogbookCatalogEntrySnapshot("quest", 1001, 0, "Base Quest", "Base workspace journal entry."),
            ]);
        }

        public LivePlayerLocatorResult LocatePlayers(int processId) => throw new NotSupportedException();

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) => throw new NotSupportedException();

        public LogbookMutationExecutionResult SetQuestState(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int questId,
            int state,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult SetQuestGlobalState(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            int questId,
            int state,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult SetRumorKnown(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int rumorId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult QuellRumor(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            int rumorId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult AddReputation(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int reputationId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult RemoveReputation(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int reputationId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult AddBlessing(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int blessingId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult RemoveBlessing(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int blessingId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult AddCurse(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int curseId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult RemoveCurse(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int curseId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult AddKey(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int keyId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult RemoveKey(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int keyId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult AddInjury(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int descriptionId,
            int injuryType,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult RemoveInjury(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int descriptionId,
            int injuryType,
            int slotIndex,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult AddKill(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            ulong victimHandle,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult SetKillSummary(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            LogbookMutationKind kind,
            int descriptionId,
            int value,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult SetBackground(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int backgroundId,
            int backgroundTextId,
            TimeSpan timeout
        ) => throw new NotSupportedException();

        public LogbookMutationExecutionResult ClearBackground(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            TimeSpan timeout
        ) => throw new NotSupportedException();
    }
}
