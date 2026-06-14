using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface ILogbookBackend : IHandleBackend
{
    Task<LogbookReadResult> ReadLogbookAsync(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        LogbookPage page,
        string workspacePath
    );
}
