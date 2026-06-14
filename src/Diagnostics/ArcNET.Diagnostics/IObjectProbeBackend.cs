using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface IObjectProbeBackend
{
    LivePlayerLocatorResult LocatePlayers(int processId);

    IReadOnlyList<LiveObjectInspection> InspectHandles(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        bool includeExtendedDetails,
        IReadOnlyList<ulong> handles
    );
}
