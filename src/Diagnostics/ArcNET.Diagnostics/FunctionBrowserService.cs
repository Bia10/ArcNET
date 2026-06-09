using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class FunctionBrowserService
{
    public static FunctionBrowserSnapshot Create(FunctionBrowserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var capabilities = DiagnosticsCapabilityPolicy.Create(request.RuntimeProfile, request.HasModuleSymbols);
        var notes = CreateNotes(capabilities);

        return new FunctionBrowserSnapshot(
            capabilities,
            FunctionCatalog.DispatcherCandidates,
            FunctionCatalog.KnownFunctions,
            notes
        );
    }

    private static IReadOnlyList<string> CreateNotes(RuntimeCapabilityReport capabilities)
    {
        List<string> notes = [.. capabilities.Warnings];
        if (capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions))
        {
            notes.Add("This runtime can use named-function targets for live invocation and dispatcher workflows.");
        }
        else if (capabilities.Capabilities.HasFlag(DiagnosticsCapability.LoadModuleSymbols))
        {
            notes.Add(
                "Use the browser as a symbol-assisted research surface; direct invocation should stay capability-gated."
            );
        }
        else
        {
            notes.Add(
                "Use the browser as reference metadata until the runtime reaches validated or symbol-assisted support."
            );
        }

        return notes;
    }
}
