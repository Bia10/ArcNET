using System.Runtime.Versioning;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class SheetEditorBackend : ISheetEditorBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public SheetMutationExecutionResult SetStat(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int statId,
        int value,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetSheetStat(memory, runtimeProfile, handle, statId, value, timeout);
    }

    public SheetMutationExecutionResult SetResistance(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int resistanceId,
        int value,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetSheetResistance(memory, runtimeProfile, handle, resistanceId, value, timeout);
    }

    public SheetMutationExecutionResult SetBasicSkill(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int skillId,
        int points,
        int? training,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetBasicSkill(memory, runtimeProfile, handle, skillId, points, training, timeout);
    }

    public SheetMutationExecutionResult SetTechSkill(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int skillId,
        int points,
        int? training,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetTechSkill(memory, runtimeProfile, handle, skillId, points, training, timeout);
    }

    public SheetMutationExecutionResult SetSpellCollegeLevel(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int collegeId,
        int level,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetSheetSpellCollegeLevel(
            memory,
            runtimeProfile,
            handle,
            collegeId,
            level,
            timeout
        );
    }

    public SheetMutationExecutionResult SetSpellMastery(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int masteryCollegeId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetSpellMastery(memory, runtimeProfile, handle, masteryCollegeId, timeout);
    }

    public SheetMutationExecutionResult SetTechDisciplineLevel(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int disciplineId,
        int level,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetSheetTechDisciplineLevel(
            memory,
            runtimeProfile,
            handle,
            disciplineId,
            level,
            timeout
        );
    }
}
