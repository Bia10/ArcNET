using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public interface IReadBackend : IHandleBackend
{
    NativeReadSnapshot InvokeInt32(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        string functionKey,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    );
}
