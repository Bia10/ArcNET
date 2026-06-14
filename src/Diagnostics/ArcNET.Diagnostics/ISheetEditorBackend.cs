using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface ISheetEditorBackend : IHandleBackend
{
    SheetMutationExecutionResult SetStat(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int statId,
        int value,
        TimeSpan timeout
    );

    SheetMutationExecutionResult SetResistance(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int resistanceId,
        int value,
        TimeSpan timeout
    );

    SheetMutationExecutionResult SetBasicSkill(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int skillId,
        int points,
        int? training,
        TimeSpan timeout
    );

    SheetMutationExecutionResult SetTechSkill(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int skillId,
        int points,
        int? training,
        TimeSpan timeout
    );

    SheetMutationExecutionResult SetSpellCollegeLevel(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int collegeId,
        int level,
        TimeSpan timeout
    );

    SheetMutationExecutionResult SetSpellMastery(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int masteryCollegeId,
        TimeSpan timeout
    );

    SheetMutationExecutionResult SetTechDisciplineLevel(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int disciplineId,
        int level,
        TimeSpan timeout
    );
}
