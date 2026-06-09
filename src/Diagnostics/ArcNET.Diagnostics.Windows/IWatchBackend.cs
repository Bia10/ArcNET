using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

public interface IWatchBackend
{
    IWatchSession StartWatch(int processId, IReadOnlyList<RuntimeWatchHookDefinition> hooks);
}
