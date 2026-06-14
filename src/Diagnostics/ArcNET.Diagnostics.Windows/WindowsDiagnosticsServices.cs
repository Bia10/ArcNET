using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;
using CoreCrashDumpAnalysisService = ArcNET.Diagnostics.CrashDumpAnalysisService;
using CoreCrashDumpService = ArcNET.Diagnostics.CrashDumpService;
using CoreInterceptTargetResolver = ArcNET.Diagnostics.InterceptTargetResolver;
using CoreModuleSymbolQueryService = ArcNET.Diagnostics.ModuleSymbolQueryService;
using CoreRuntimeStatusService = ArcNET.Diagnostics.RuntimeStatusService;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsDiagnosticsServices : IDiagnosticsServices
{
    private readonly IRuntimePlatformService runtimePlatformService;

    public WindowsDiagnosticsServices()
    {
        runtimePlatformService = new WindowsRuntimePlatformService();
        RuntimeStatusService = new CoreRuntimeStatusService(runtimePlatformService);
        ModuleSymbolQueryService = new CoreModuleSymbolQueryService(runtimePlatformService);
        InterceptTargetResolver = new CoreInterceptTargetResolver(runtimePlatformService);
        CrashDumpService = new CoreCrashDumpService(runtimePlatformService);
        CrashDumpAnalysisService = new CoreCrashDumpAnalysisService(runtimePlatformService);
    }

    public AuditService AuditService { get; } = new(new AuditBackend());

    public EnvironmentService EnvironmentService { get; } = new(new EnvironmentBackend());

    public FunctionCallService FunctionCallService { get; } = new(new FunctionCallBackend());

    public GameDataCatalogService GameDataCatalogService { get; } = new(new GameDataCatalogBackend());

    public GuidedActionService GuidedActionService { get; } =
        new(new FunctionCallBackend(), static modulePath => LoadDiscoverableWorldMapLocationsAsync(modulePath));

    public CoreInterceptTargetResolver InterceptTargetResolver { get; }

    public CoreModuleSymbolQueryService ModuleSymbolQueryService { get; }

    public InventoryEditorService InventoryEditorService { get; } = new(new InventoryEditorBackend());

    public InterceptService InterceptService { get; } = new(new InterceptBackend());

    public CoreCrashDumpAnalysisService CrashDumpAnalysisService { get; }

    public CoreCrashDumpService CrashDumpService { get; }

    public LogbookEditorService LogbookEditorService { get; } = new(new LogbookEditorBackend());

    public LogbookService LogbookService { get; } = new(new LogbookBackend());

    public MobileEntityService MobileEntityService { get; } = new(new MobileEntityBackend());

    public ObjectProbeService ObjectProbeService { get; } = new(new ObjectProbeBackend());

    public PrototypeResolutionService PrototypeResolutionService { get; } = new(new PrototypeResolutionBackend());

    public ReadService ReadService { get; } = new(new ReadBackend());

    public CoreRuntimeStatusService RuntimeStatusService { get; }

    public ScriptAttachmentService ScriptAttachmentService { get; } = new(new ScriptAttachmentBackend());

    public SessionService SessionService { get; } = new(new SessionBackend());

    public SheetService SheetService { get; } = new(new SheetBackend());

    public SheetEditorService SheetEditorService { get; } = new(new SheetEditorBackend());

    public SpellTechEditorService SpellTechEditorService { get; } = new(new SpellTechEditorBackend());

    public WatchService WatchService { get; } = new(new WatchBackend());

    private static async Task<IReadOnlyList<WorldMapLocationDescriptor>> LoadDiscoverableWorldMapLocationsAsync(
        string workspacePath
    )
    {
        var entries = WorkspaceGameDataCatalogProjector.ToWorldMapEntries(
            await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(workspacePath).ConfigureAwait(false)
        );
        return
        [
            .. entries
                .Where(static entry => entry.AreaId > 0 && entry.HasWorldCoordinates)
                .Select(static entry => new WorldMapLocationDescriptor(
                    entry.AreaId,
                    entry.DisplayName,
                    entry.WorldX,
                    entry.WorldY
                )),
        ];
    }
}
