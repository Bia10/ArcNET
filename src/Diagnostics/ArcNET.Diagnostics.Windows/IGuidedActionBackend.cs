using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public interface IGuidedActionBackend
{
    LivePlayerLocatorResult LocatePlayers(int processId);

    FunctionCallExecutionResult ExecuteTeleport(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong travelerHandle,
        int tileX,
        int tileY,
        int mapId,
        uint flags,
        TimeSpan timeout
    );
}
