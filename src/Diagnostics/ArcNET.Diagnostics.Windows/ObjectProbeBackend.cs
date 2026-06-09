using System.Runtime.Versioning;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class ObjectProbeBackend : IObjectProbeBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live object probing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public IReadOnlyList<LiveObjectInspection> InspectHandles(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        bool includeExtendedDetails,
        IReadOnlyList<ulong> handles
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);
        ArgumentNullException.ThrowIfNull(handles);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live object probing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        if (!includeExtendedDetails)
        {
            return
            [
                .. handles.Select(handle => new LiveObjectInspection(LiveObjectInspector.Inspect(memory, handle), [])),
            ];
        }

        using var reader = new LiveObjectDetailsReader(memory, runtimeProfile);
        return [.. handles.Select(reader.Inspect)];
    }
}
