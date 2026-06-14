using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface IMobileEntityBackend : IHandleBackend
{
    IReadOnlyList<LiveObjectIdentity> ListLiveMobiles(int processId, int maxEntries);

    MobileMutationExecutionResult SetMobileStat(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int statId,
        int value,
        TimeSpan timeout
    );

    MobileMutationExecutionResult KillMobile(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        TimeSpan timeout
    );

    MobileMutationExecutionResult DespawnMobile(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        TimeSpan timeout
    );

    MobileMutationExecutionResult SpawnMobile(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong prototypeHandle,
        ulong anchorHandle,
        TimeSpan timeout
    );
}
