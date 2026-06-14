using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class FunctionCallBackend : IFunctionCallBackend, IGuidedActionBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live function invocation currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public FunctionCallExecutionResult InvokeCall(
        int processId,
        int targetRva,
        RuntimeProfileSnapshot runtimeProfile,
        StackCleanupMode cleanupMode,
        uint ecxValue,
        uint edxValue,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);
        ArgumentNullException.ThrowIfNull(stackArguments);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live function invocation currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var targetAddress = memory.ResolveRva(targetRva);
        var result = dispatcher.Invoke(
            checked((uint)(long)targetAddress),
            cleanupMode,
            ecxValue,
            edxValue,
            stackArguments,
            timeout
        );

        return new FunctionCallExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            ProcessMemory.FormatAddress(targetAddress),
            result.ResultEax,
            result.ResultEdx,
            result.State.ToString()
        );
    }

    public FunctionCallExecutionResult ExecuteTeleport(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong travelerHandle,
        int tileX,
        int tileY,
        int mapId,
        uint flags,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Guided runtime actions currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        var result = RuntimeActionInvoker.Teleport(
            memory,
            runtimeProfile,
            travelerHandle,
            tileX,
            tileY,
            mapId,
            flags,
            timeout
        );

        return new FunctionCallExecutionResult(
            result.DispatcherMode,
            result.DispatcherSite,
            result.TargetAddressText,
            result.ResultEax,
            result.ResultEdx,
            result.State
        );
    }

    public WorldMapDiscoveryExecutionResult DiscoverAllWorldMapLocations(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong travelerHandle,
        IReadOnlyList<WorldMapLocationDescriptor> locations,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);
        ArgumentNullException.ThrowIfNull(locations);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Guided runtime actions currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.DiscoverAllWorldMapLocations(
            memory,
            runtimeProfile,
            travelerHandle,
            locations,
            timeout
        );
    }
}
