using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcanumDebugger.App.Composition;

public static class ArcanumDebuggerPreviewCatalog
{
    public static IReadOnlyList<ArcanumDebuggerPreviewScenario> Scenarios => s_scenarios;

    private static readonly ArcanumDebuggerPreviewScenario[] s_scenarios =
    [
        new(
            "validated-classic",
            "Validated Classic Build",
            "Full diagnostics posture for the original executable when the runtime profile is validated and catalog RVAs are trusted.",
            new WorkspaceRequest(
                new RuntimeProfileSnapshot(
                    Id: "validated-classic",
                    DisplayName: "Arcanum.exe validated runtime profile",
                    RuntimeKind: RuntimeKind.Classic,
                    SupportLevel: RuntimeSupportLevel.Validated,
                    SupportsCatalogRvas: true,
                    Notes: "Classic executable with validated catalog-backed offsets.",
                    ModuleSha256: "preview-classic-validated",
                    HashError: null
                ),
                HasModuleSymbols: false,
                RequestedProcessNames: ["Arcanum"]
            )
        ),
        new(
            "ce-symbol-assisted",
            "CE With Symbols",
            "Function-browser and research-first posture for community-edition sessions where symbols improve visibility before full watch support exists.",
            new WorkspaceRequest(
                new RuntimeProfileSnapshot(
                    Id: null,
                    DisplayName: "arcanum-ce.exe exploratory profile",
                    RuntimeKind: RuntimeKind.CommunityEdition,
                    SupportLevel: RuntimeSupportLevel.Exploratory,
                    SupportsCatalogRvas: false,
                    Notes: "Community edition executable with symbol-assisted fallback.",
                    ModuleSha256: "preview-ce-symbol-assisted",
                    HashError: null
                ),
                HasModuleSymbols: true,
                RequestedProcessNames: ["arcanum-ce"]
            )
        ),
        new(
            "unsupported-readonly",
            "Unsupported Read-Only Probe",
            "Honest degraded posture for unknown builds where the shell should surface warnings and keep mutation-heavy panels closed by default.",
            new WorkspaceRequest(
                new RuntimeProfileSnapshot(
                    Id: null,
                    DisplayName: "Unknown executable build",
                    RuntimeKind: RuntimeKind.Unknown,
                    SupportLevel: RuntimeSupportLevel.Unsupported,
                    SupportsCatalogRvas: false,
                    Notes: "Unknown runtime fingerprint. Read-mostly diagnostics only.",
                    ModuleSha256: null,
                    HashError: "Preview scenario: hash not captured."
                ),
                HasModuleSymbols: false,
                RequestedProcessNames: ["Arcanum", "arcanum-ce"]
            )
        ),
    ];
}
