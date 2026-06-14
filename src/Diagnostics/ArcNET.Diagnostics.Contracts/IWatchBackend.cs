namespace ArcNET.Diagnostics;

public interface IWatchBackend
{
    IWatchSession StartWatch(int processId, IReadOnlyList<RuntimeWatchHookDefinition> hooks);
}
