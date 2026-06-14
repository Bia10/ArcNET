namespace ArcNET.Diagnostics;

public sealed class RuntimeStatusService(IRuntimePlatformService platformService)
{
    public RuntimeStatusSnapshot Inspect(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        return platformService.InspectRuntimeStatus(processId);
    }

    public RuntimeActionPointsWriteSnapshot WriteActionPoints(int processId, int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        return platformService.WriteRuntimeActionPoints(processId, value);
    }
}
