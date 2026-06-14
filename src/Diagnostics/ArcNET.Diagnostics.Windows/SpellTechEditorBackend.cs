using System.Runtime.Versioning;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class SpellTechEditorBackend : ISpellTechEditorBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live spell and tech editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live spell and tech editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public SpellTechMutationExecutionResult AddSpell(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int spellId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live spell and tech editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.AddSpell(memory, runtimeProfile, handle, spellId, timeout);
    }

    public SpellTechMutationExecutionResult GrantSchematic(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int schematicId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live spell and tech editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.GrantSchematic(memory, runtimeProfile, handle, schematicId, timeout);
    }

    public SpellTechMutationExecutionResult RemoveSchematic(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int schematicId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live spell and tech editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.RemoveSchematic(memory, runtimeProfile, handle, schematicId, timeout);
    }

    public SpellTechMutationExecutionResult SetSpellCollegeLevel(
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
            throw new PlatformNotSupportedException("Live spell and tech editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetSpellCollegeLevel(memory, runtimeProfile, handle, collegeId, level, timeout);
    }

    public SpellTechMutationExecutionResult SetTechDisciplineLevel(
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
            throw new PlatformNotSupportedException("Live spell and tech editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetTechDisciplineLevel(
            memory,
            runtimeProfile,
            handle,
            disciplineId,
            level,
            timeout
        );
    }

    public SpellTechMutationExecutionResult SetTechSkillPoints(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int skillId,
        int points,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live spell and tech editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetTechSkillPoints(memory, runtimeProfile, handle, skillId, points, timeout);
    }
}
