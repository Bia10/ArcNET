using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

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

    WorldMapDiscoveryExecutionResult DiscoverAllWorldMapLocations(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong travelerHandle,
        IReadOnlyList<WorldMapLocationDescriptor> locations,
        TimeSpan timeout
    );
}
