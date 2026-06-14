namespace ArcNET.Diagnostics;

public interface IDiagnosticsServices
{
    AuditService AuditService { get; }

    EnvironmentService EnvironmentService { get; }

    FunctionCallService FunctionCallService { get; }

    GameDataCatalogService GameDataCatalogService { get; }

    GuidedActionService GuidedActionService { get; }

    InterceptTargetResolver InterceptTargetResolver { get; }

    ModuleSymbolQueryService ModuleSymbolQueryService { get; }

    InventoryEditorService InventoryEditorService { get; }

    LogbookEditorService LogbookEditorService { get; }

    LogbookService LogbookService { get; }

    MobileEntityService MobileEntityService { get; }

    ObjectProbeService ObjectProbeService { get; }

    InterceptService InterceptService { get; }

    PrototypeResolutionService PrototypeResolutionService { get; }

    ReadService ReadService { get; }

    RuntimeStatusService RuntimeStatusService { get; }

    CrashDumpService CrashDumpService { get; }

    CrashDumpAnalysisService CrashDumpAnalysisService { get; }

    ScriptAttachmentService ScriptAttachmentService { get; }

    SessionService SessionService { get; }

    SheetService SheetService { get; }

    SheetEditorService SheetEditorService { get; }

    SpellTechEditorService SpellTechEditorService { get; }

    WatchService WatchService { get; }
}
