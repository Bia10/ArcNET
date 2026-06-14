using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface ILogbookEditorBackend : IHandleBackend
{
    Task<IReadOnlyList<LogbookCatalogEntrySnapshot>> LoadCatalogAsync(string workspacePath);

    LogbookMutationExecutionResult SetQuestState(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int questId,
        int state,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult SetQuestGlobalState(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        int questId,
        int state,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult SetRumorKnown(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int rumorId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult QuellRumor(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        int rumorId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult AddReputation(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int reputationId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult RemoveReputation(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int reputationId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult AddBlessing(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int blessingId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult RemoveBlessing(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int blessingId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult AddCurse(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int curseId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult RemoveCurse(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int curseId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult AddKey(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int keyId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult RemoveKey(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int keyId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult AddInjury(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int descriptionId,
        int injuryType,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult RemoveInjury(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int descriptionId,
        int injuryType,
        int slotIndex,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult AddKill(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        ulong victimHandle,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult SetKillSummary(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        LogbookMutationKind kind,
        int descriptionId,
        int value,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult SetBackground(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int backgroundId,
        int backgroundTextId,
        TimeSpan timeout
    );

    LogbookMutationExecutionResult ClearBackground(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        TimeSpan timeout
    );
}
