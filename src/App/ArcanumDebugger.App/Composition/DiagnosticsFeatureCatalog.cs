namespace ArcanumDebugger.App.Composition;

public static class DiagnosticsFeatureCatalog
{
    public static IReadOnlyList<DebuggerFeatureCard> Features => s_features;

    private static readonly HashSet<string> s_interactiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "workspace",
        "dashboard",
        "function-browser",
        "object-explorer",
        "timeline-guidance",
        "environment",
        "session",
        "runtime-profile",
        "runtime-status",
        "watch",
        "object-probe",
        "function-call",
        "guided-action",
        "audit",
        "prototype-resolution",
        "read",
        "sheet",
        "script-attachment",
        "logbook",
        "intercept",
        "module-symbol-query",
        "crash-dump",
        "ce-source-audit",
        "save-slot-load",
        "save-file-audit",
        "save-character-catalog",
        "save-character-sar-dump",
        "save-player-progression-history",
        "save-binary-diff",
        "character-sar-diff",
        "character-sar-full-dump",
        "player-sar-history",
        "player-sar-analysis",
        "player-sar-report",
        "save-global-analysis",
        "save-global-diff",
        "save-global-dump",
        "save-global-range-analysis",
        "save-embedded-file-analysis",
        "mob-item-analysis",
        "save-gold-item-inspection",
        "save-player-summary",
        "save-player-questbook",
        "save-structure-analysis",
        "save-typed-context",
        "save-typed-context-analysis",
    };

    private static readonly DebuggerFeatureCard[] s_features =
    [
        Create(
            "workspace",
            "Runtime workspace",
            "ArcNET.Diagnostics",
            "WorkspaceService",
            "Headless composition of dashboard, timeline, function-browser, and object-explorer guidance."
        ),
        Create(
            "dashboard",
            "Runtime workspace",
            "ArcNET.Diagnostics",
            "DashboardService",
            "Projects capability-aware panels and probe presets for the overall debugger workspace."
        ),
        Create(
            "function-browser",
            "Runtime workspace",
            "ArcNET.Diagnostics",
            "FunctionBrowserService",
            "Curates dispatcher candidates and known-function metadata for invocation-oriented workflows."
        ),
        Create(
            "object-explorer",
            "Runtime workspace",
            "ArcNET.Diagnostics",
            "ObjectExplorerService",
            "Builds object-field group guidance and structured-state explorer recommendations."
        ),
        Create(
            "timeline-guidance",
            "Runtime workspace",
            "ArcNET.Diagnostics",
            "TimelineService",
            "Recommends probe presets, watch profiles, and timeline notes for the active runtime capability set."
        ),
        Create(
            "environment",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "EnvironmentService",
            "Discovers candidate processes, live runtimes, and launch-preview posture from the local machine."
        ),
        Create(
            "session",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "SessionService",
            "Attaches to running processes or launches supported executables and establishes a process-backed session."
        ),
        Create(
            "runtime-profile",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "RuntimeProfileService",
            "Resolves module fingerprints, hashes, and support posture for a concrete runtime binary."
        ),
        Create(
            "runtime-status",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "RuntimeStatusService",
            "Reads capability-aware live runtime status such as action points and current character-sheet identity."
        ),
        Create(
            "watch",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "WatchService",
            "Streams live hook events through validated runtime watch presets."
        ),
        Create(
            "object-probe",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "ObjectProbeService",
            "Inspects runtime object handles and expands identity plus getter-backed object details."
        ),
        Create(
            "function-call",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "FunctionCallService",
            "Invokes known catalog functions or raw RVAs through the live dispatcher surface."
        ),
        Create(
            "guided-action",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "GuidedActionService",
            "Wraps common runtime mutations such as traveler teleport into safer guided workflows."
        ),
        Create(
            "audit",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "AuditService",
            "Audits dispatcher resolution, function metadata, and optional hook install/pass behavior on a live process."
        ),
        Create(
            "prototype-resolution",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "PrototypeResolutionService",
            "Resolves proto numbers, palette entries, and explicit prototype handles into live runtime objects."
        ),
        Create(
            "sheet",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "SheetService",
            "Reads typed sheet values and can capture or diff live sheet snapshots."
        ),
        Create(
            "script-attachment",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "ScriptAttachmentService",
            "Reads script attachment records by attachment point for live runtime objects."
        ),
        Create(
            "logbook",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "LogbookService",
            "Decodes rumors, quests, reputations, blessings, curses, injuries, background, and keyring pages."
        ),
        Create(
            "read",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "ReadService",
            "Performs getter-backed reads for quests, fields, stats, effects, and other structured runtime state."
        ),
        Create(
            "intercept",
            "Runtime live",
            "ArcNET.Diagnostics.Windows",
            "InterceptService",
            "Hooks one function with register overrides, argument mutation, dereference capture, and event polling."
        ),
        Create(
            "module-symbol-query",
            "Runtime research",
            "ArcNET.Diagnostics.Windows",
            "ModuleSymbolQueryService",
            "Queries file-based or live-loaded symbols to crosswalk names, RVAs, sizes, and duplicate matches."
        ),
        Create(
            "crash-dump",
            "Runtime research",
            "ArcNET.Diagnostics.Windows",
            "CrashDumpService",
            "Writes manual process dumps and manages automatic WER LocalDumps configuration."
        ),
        Create(
            "ce-source-audit",
            "File-time research",
            "ArcNET.Diagnostics",
            "CeSourceAuditService",
            "Audits CE source coverage against watch hooks, debugger functions, signatures, and optional symbol catalogs."
        ),
        Create(
            "save-slot-load",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveSlotLoadService",
            "Loads a save slot into the typed ArcNET save model from .gsi/.tfai/.tfaf inputs."
        ),
        Create(
            "save-file-audit",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveFileAuditService",
            "Produces a typed audit of asset counts, validation issues, object-field usage, and player-character SARs."
        ),
        Create(
            "save-character-catalog",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveCharacterCatalogService",
            "Catalogs every character record in one loaded save with level, race, resources, and non-zero skill summaries."
        ),
        Create(
            "save-character-sar-dump",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveCharacterSarDumpService",
            "Builds SAR-oriented per-character dumps for each character record discovered in one loaded save."
        ),
        Create(
            "save-player-summary",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SavePlayerCharacterSummaryService",
            "Summarizes player progression, stats, skills, reputations, quests, rumors, and ammunition resources."
        ),
        Create(
            "save-player-questbook",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SavePlayerQuestBookService",
            "Builds a player-centric quest, reputation, blessing, curse, and schematic view from save files."
        ),
        Create(
            "save-player-progression-history",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SavePlayerProgressionHistoryService",
            "Tracks progression changes for one player across multiple save slots."
        ),
        Create(
            "save-binary-diff",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveBinaryDiffService",
            "Diffs raw bytes or inner save files and produces preview-friendly changed regions."
        ),
        Create(
            "character-sar-diff",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "CharacterSarDiffService",
            "Compares two raw character SAR payloads and reports element, summary, add, and remove changes."
        ),
        Create(
            "character-sar-full-dump",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "CharacterSarFullDumpService",
            "Dumps every parsed SAR entry from one raw character payload with offsets and annotations."
        ),
        Create(
            "player-sar-history",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "PlayerSarHistoryService",
            "Builds SAR history across a slot range for one player character."
        ),
        Create(
            "player-sar-analysis",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "PlayerSarAnalysisService",
            "Creates lifecycle and transition analysis over SAR history snapshots."
        ),
        Create(
            "player-sar-report",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "PlayerSarReportService",
            "Composes higher-level reports over SAR history and transition data."
        ),
        Create(
            "save-global-analysis",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveGlobalAnalysisService",
            "Summarizes save-global files, parsed ranges, and typed file coverage."
        ),
        Create(
            "save-global-diff",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveGlobalDiffService",
            "Diffs save-global payloads with changed-int and changed-region reporting."
        ),
        Create(
            "save-global-dump",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveGlobalDumpService",
            "Renders dump-friendly snapshots of save-global content and quad previews."
        ),
        Create(
            "save-global-range-analysis",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveGlobalRangeAnalysisService",
            "Analyzes byte ranges and probable structures inside save-global payloads."
        ),
        Create(
            "save-embedded-file-analysis",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveEmbeddedFileAnalysisService",
            "Inspects embedded files such as dynamic mobiles and exposes file-level details."
        ),
        Create(
            "mob-item-analysis",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "MobItemAnalysisService",
            "Analyzes one parsed item mob and projects weight, worth, flags, discipline, spell effects, and type-specific fields."
        ),
        Create(
            "save-gold-item-inspection",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveGoldItemInspectionService",
            "Finds gold-bearing items and summarizes their placement across save content."
        ),
        Create(
            "save-structure-analysis",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveStructureAnalysisService",
            "Builds typed structural summaries over loaded save assets."
        ),
        Create(
            "save-typed-context",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveTypedContextService",
            "Creates a typed save context for higher-level offline diagnostics."
        ),
        Create(
            "save-typed-context-analysis",
            "File-time save",
            "ArcNET.Diagnostics.FileTime",
            "SaveTypedContextAnalysisService",
            "Analyzes typed save contexts and emits player-focused deltas."
        ),
    ];

    private static DebuggerFeatureCard Create(
        string key,
        string area,
        string packageName,
        string serviceName,
        string summary
    ) =>
        new(
            key,
            area,
            packageName,
            serviceName,
            summary,
            s_interactiveKeys.Contains(key) ? "Interactive in shell" : "Catalog only"
        );
}

public sealed record class DebuggerFeatureCard(
    string Key,
    string Area,
    string PackageName,
    string ServiceName,
    string Summary,
    string ShellStatus
);
