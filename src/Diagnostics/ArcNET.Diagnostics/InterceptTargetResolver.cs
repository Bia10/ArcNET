namespace ArcNET.Diagnostics;

public sealed class InterceptTargetResolver(IRuntimePlatformService platformService)
{
    public InterceptTarget Resolve(int processId, string targetText)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetText);
        return platformService.ResolveInterceptTarget(processId, targetText);
    }
}
