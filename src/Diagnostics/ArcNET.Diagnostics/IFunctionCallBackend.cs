using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface IFunctionCallBackend
{
    LivePlayerLocatorResult LocatePlayers(int processId);

    FunctionCallExecutionResult InvokeCall(
        int processId,
        int targetRva,
        RuntimeProfileSnapshot runtimeProfile,
        ArcNET.Diagnostics.StackCleanupMode cleanupMode,
        uint ecxValue,
        uint edxValue,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    );
}
