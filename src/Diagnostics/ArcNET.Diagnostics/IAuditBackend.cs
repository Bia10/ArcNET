using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface IAuditBackend
{
    DispatcherAuditSnapshot AuditDispatcher(int processId, RuntimeProfileSnapshot runtimeProfile);

    FunctionAuditSnapshot AuditFunctions(int processId);

    HookAuditSnapshot AuditHooks(
        int processId,
        IReadOnlyList<string> selectors,
        TimeSpan duration,
        bool includeWatch,
        bool includeIntercept,
        int stackCaptureDwordCount,
        bool stopOnFailure
    );
}
