using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public interface ILogbookBackend : IHandleBackend
{
    LogbookReadResult ReadLogbook(int processId, RuntimeProfileSnapshot runtimeProfile, ulong handle, LogbookPage page);
}
