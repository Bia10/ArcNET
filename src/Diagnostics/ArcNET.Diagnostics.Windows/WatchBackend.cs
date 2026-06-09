using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class WatchBackend : IWatchBackend
{
    public IWatchSession StartWatch(int processId, IReadOnlyList<RuntimeWatchHookDefinition> hooks)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live watch hooks currently require Windows.");

        var memory = ProcessMemory.Attach(processId);
        try
        {
            var watchSession = RuntimeWatchSession.Install(memory, hooks);
            return new WatchSessionLease(memory, watchSession);
        }
        catch
        {
            memory.Dispose();
            throw;
        }
    }

    private sealed class WatchSessionLease(ProcessMemory memory, RuntimeWatchSession watchSession) : IWatchSession
    {
        private readonly ProcessMemory _memory = memory;
        private readonly RuntimeWatchSession _watchSession = watchSession;

        public RuntimeWatchReadResult ReadSince(uint lastSequence) => _watchSession.ReadSince(lastSequence);

        public void Dispose()
        {
            _watchSession.Dispose();
            _memory.Dispose();
        }
    }
}
