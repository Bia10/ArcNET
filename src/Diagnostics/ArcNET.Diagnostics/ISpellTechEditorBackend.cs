using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface ISpellTechEditorBackend : IHandleBackend
{
    SpellTechMutationExecutionResult AddSpell(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int spellId,
        TimeSpan timeout
    );

    SpellTechMutationExecutionResult GrantSchematic(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int schematicId,
        TimeSpan timeout
    );

    SpellTechMutationExecutionResult RemoveSchematic(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int schematicId,
        TimeSpan timeout
    );

    SpellTechMutationExecutionResult SetSpellCollegeLevel(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int collegeId,
        int level,
        TimeSpan timeout
    );

    SpellTechMutationExecutionResult SetTechDisciplineLevel(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int disciplineId,
        int level,
        TimeSpan timeout
    );

    SpellTechMutationExecutionResult SetTechSkillPoints(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int skillId,
        int points,
        TimeSpan timeout
    );
}
