using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class LogbookEditorBackend : ILogbookEditorBackend
{
    public async Task<IReadOnlyList<LogbookCatalogEntrySnapshot>> LoadCatalogAsync(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        var catalog = await WorkspaceTextCatalog.LoadFromModulePathAsync(workspacePath).ConfigureAwait(false);
        List<LogbookCatalogEntrySnapshot> entries =
        [
            .. catalog
                .EnumerateQuests()
                .Select(static quest => new LogbookCatalogEntrySnapshot(
                    "quest",
                    quest.QuestId,
                    0,
                    quest.SummaryLabel,
                    FirstNonEmpty(quest.Description, quest.DumbDescription, $"Quest {quest.QuestId}")!
                )),
            .. catalog
                .EnumerateRumors()
                .Select(static rumor => new LogbookCatalogEntrySnapshot(
                    "rumor",
                    rumor.RumorId,
                    0,
                    rumor.SummaryText,
                    CreateRumorDetail(rumor)
                )),
            .. catalog
                .EnumerateReputations()
                .Select(static reputation => new LogbookCatalogEntrySnapshot(
                    "reputation",
                    reputation.Id,
                    0,
                    reputation.Name,
                    $"Reputation {reputation.Id.ToString(CultureInfo.InvariantCulture)}"
                )),
            .. catalog
                .EnumerateBlessings()
                .Select(static blessing => new LogbookCatalogEntrySnapshot(
                    "blessing",
                    blessing.Id,
                    0,
                    blessing.Name,
                    $"Blessing {blessing.Id.ToString(CultureInfo.InvariantCulture)}"
                )),
            .. catalog
                .EnumerateCurses()
                .Select(static curse => new LogbookCatalogEntrySnapshot(
                    "curse",
                    curse.Id,
                    0,
                    curse.Name,
                    $"Curse {curse.Id.ToString(CultureInfo.InvariantCulture)}"
                )),
            .. catalog
                .EnumerateKeys()
                .Select(static key => new LogbookCatalogEntrySnapshot(
                    "key",
                    key.Id,
                    0,
                    key.Name,
                    $"Key {key.Id.ToString(CultureInfo.InvariantCulture)}"
                )),
            .. catalog
                .EnumerateDescriptions()
                .Select(static description => new LogbookCatalogEntrySnapshot(
                    "injury",
                    description.Id,
                    0,
                    description.Name,
                    $"Description {description.Id.ToString(CultureInfo.InvariantCulture)}"
                )),
            .. catalog
                .EnumerateDescriptions()
                .Select(static description => new LogbookCatalogEntrySnapshot(
                    "description",
                    description.Id,
                    0,
                    description.Name,
                    $"Description {description.Id.ToString(CultureInfo.InvariantCulture)}"
                )),
            .. catalog
                .EnumerateBackgrounds()
                .Select(static background => new LogbookCatalogEntrySnapshot(
                    "background",
                    background.BackgroundId,
                    background.TextId,
                    background.SummaryLabel,
                    $"Text {background.TextId.ToString(CultureInfo.InvariantCulture)} · {FirstNonEmpty(background.Body, background.SummaryLabel)}"
                )),
        ];
        return
        [
            .. entries
                .OrderBy(static entry => entry.CategoryToken, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.EntryId),
        ];
    }

    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public LogbookMutationExecutionResult SetQuestState(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int questId,
        int state,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentQuestState = ReadPcQuestState(memory, dispatcher, handle, questId, timeout);
        var currentDisplayState = FormatQuestPcState(currentQuestState.RawState);
        var requestedDisplayState = FormatQuestPcState(state);
        var requestedLogicalState = QuestLogicalState(state);
        var desiredRawState = ResolveRequestedQuestRawState(currentQuestState.RawState, state);
        if (currentQuestState.RawState == desiredRawState)
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"quest_state_get @ {s_questStateGet.Site} · obj_array_field_pc_quest_get @ {s_objectArrayFieldPcQuestGet.Site}",
                $"Quest {questId.ToString(CultureInfo.InvariantCulture)} is already {currentDisplayState} on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        if (CanUseNativeQuestStatePath(state))
        {
            var nativeResult = Invoke(
                memory,
                dispatcher,
                s_questStateSet,
                [
                    TargetResolver.ToLow32(handle),
                    TargetResolver.ToHigh32(handle),
                    unchecked((uint)questId),
                    unchecked((uint)state),
                    0,
                    0,
                ],
                timeout
            );
            var updatedState = InvokeInt32(
                memory,
                dispatcher,
                s_questStateGet,
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)questId)],
                timeout
            );
            var updatedQuestState = ReadPcQuestState(memory, dispatcher, handle, questId, timeout);
            if (
                updatedState == requestedLogicalState
                && currentQuestState.RawState != updatedQuestState.RawState
                && updatedQuestState.RawState == desiredRawState
            )
            {
                return new LogbookMutationExecutionResult(
                    dispatcher.ModeDescription,
                    dispatcher.SiteDescription,
                    $"quest_state_get @ {s_questStateGet.Site} · quest_state_set @ {s_questStateSet.Site} · obj_array_field_pc_quest_get @ {s_objectArrayFieldPcQuestGet.Site}",
                    $"Quest {questId.ToString(CultureInfo.InvariantCulture)} {currentDisplayState} -> {FormatQuestPcState(updatedQuestState.RawState)} through the native quest path · EAX {FormatUInt32Result(nativeResult.ResultEax)}"
                );
            }

            if (updatedState == requestedLogicalState && state != QuestStateBotched)
            {
                return new LogbookMutationExecutionResult(
                    dispatcher.ModeDescription,
                    dispatcher.SiteDescription,
                    $"quest_state_get @ {s_questStateGet.Site} · quest_state_set @ {s_questStateSet.Site} · obj_array_field_pc_quest_get @ {s_objectArrayFieldPcQuestGet.Site}",
                    $"Quest {questId.ToString(CultureInfo.InvariantCulture)} {currentDisplayState} -> {FormatQuestPcState(updatedQuestState.RawState)} through the native quest path · EAX {FormatUInt32Result(nativeResult.ResultEax)}"
                );
            }
        }

        var desiredQuestState = currentQuestState with { RawState = desiredRawState };
        var directResult = WritePcQuestState(memory, dispatcher, handle, questId, desiredQuestState, timeout);
        var finalQuestState = ReadPcQuestState(memory, dispatcher, handle, questId, timeout);
        var finalState = InvokeInt32(
            memory,
            dispatcher,
            s_questStateGet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)questId)],
            timeout
        );
        if (finalQuestState.RawState != desiredRawState || finalState != requestedLogicalState)
        {
            throw new InvalidOperationException(
                $"Quest {questId.ToString(CultureInfo.InvariantCulture)} remained {FormatQuestPcState(finalQuestState.RawState)} after requesting {requestedDisplayState}. ArcNET tried the native quest path and a direct PC-quest overwrite, but the runtime did not preserve the requested raw quest record."
            );
        }

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"quest_state_get @ {s_questStateGet.Site} · obj_array_field_pc_quest_get @ {s_objectArrayFieldPcQuestGet.Site} · obj_array_field_pc_quest_set @ {s_objectArrayFieldPcQuestSet.Site}",
            $"Quest {questId.ToString(CultureInfo.InvariantCulture)} {currentDisplayState} -> {FormatQuestPcState(finalQuestState.RawState)} through a direct PC-quest overwrite · EAX {FormatUInt32Result(directResult.ResultEax)}"
        );
    }

    public LogbookMutationExecutionResult SetQuestGlobalState(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        int questId,
        int state,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentState = InvokeInt32(memory, dispatcher, s_questGlobalStateGet, [unchecked((uint)questId)], timeout);
        if (currentState == state)
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"quest_global_state_get @ {s_questGlobalStateGet.Site}",
                $"Quest {questId.ToString(CultureInfo.InvariantCulture)} global state is already {RuntimeWatchValueCatalog.QuestStateName(state)}.",
                NoMutation: true
            );
        }

        var result = Invoke(
            memory,
            dispatcher,
            s_questGlobalStateSet,
            [unchecked((uint)questId), unchecked((uint)state)],
            timeout
        );
        var updatedState = InvokeInt32(memory, dispatcher, s_questGlobalStateGet, [unchecked((uint)questId)], timeout);
        if (updatedState != state)
        {
            throw new InvalidOperationException(
                $"Quest {questId.ToString(CultureInfo.InvariantCulture)} global state remained {RuntimeWatchValueCatalog.QuestStateName(updatedState)} after requesting {RuntimeWatchValueCatalog.QuestStateName(state)}. The native global quest path only accepts accepted, completed, or botched transitions and will not rewrite completed or botched entries backwards."
            );
        }

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"quest_global_state_get @ {s_questGlobalStateGet.Site} · quest_global_state_set @ {s_questGlobalStateSet.Site}",
            $"Quest {questId.ToString(CultureInfo.InvariantCulture)} global {RuntimeWatchValueCatalog.QuestStateName(currentState)} -> {RuntimeWatchValueCatalog.QuestStateName(updatedState)} · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    public LogbookMutationExecutionResult SetRumorKnown(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int rumorId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var known =
            InvokeInt32(
                memory,
                dispatcher,
                s_rumorKnownGet,
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)rumorId)],
                timeout
            ) != 0;
        if (known)
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"rumor_known_get @ {s_rumorKnownGet.Site}",
                $"Rumor {rumorId.ToString(CultureInfo.InvariantCulture)} is already known by {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var result = Invoke(
            memory,
            dispatcher,
            s_rumorKnownSet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)rumorId)],
            timeout
        );
        var updatedKnown =
            InvokeInt32(
                memory,
                dispatcher,
                s_rumorKnownGet,
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)rumorId)],
                timeout
            ) != 0;
        if (!updatedKnown)
            throw new InvalidOperationException(
                $"Failed to mark rumor {rumorId.ToString(CultureInfo.InvariantCulture)} known."
            );

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"rumor_known_get @ {s_rumorKnownGet.Site} · rumor_known_set @ {s_rumorKnownSet.Site}",
            $"Rumor {rumorId.ToString(CultureInfo.InvariantCulture)} is now known by {RuntimeSemanticCatalog.FormatHandle(handle)} · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    public LogbookMutationExecutionResult QuellRumor(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        int rumorId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var quelled = InvokeInt32(memory, dispatcher, s_rumorQstateGet, [unchecked((uint)rumorId)], timeout) != 0;
        if (quelled)
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"rumor_qstate_get @ {s_rumorQstateGet.Site}",
                $"Rumor {rumorId.ToString(CultureInfo.InvariantCulture)} is already quelled.",
                NoMutation: true
            );
        }

        var result = Invoke(memory, dispatcher, s_rumorQstateSet, [unchecked((uint)rumorId)], timeout);
        var updatedQuelled =
            InvokeInt32(memory, dispatcher, s_rumorQstateGet, [unchecked((uint)rumorId)], timeout) != 0;
        if (!updatedQuelled)
            throw new InvalidOperationException(
                $"Failed to quell rumor {rumorId.ToString(CultureInfo.InvariantCulture)}."
            );

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"rumor_qstate_get @ {s_rumorQstateGet.Site} · rumor_qstate_set @ {s_rumorQstateSet.Site}",
            $"Rumor {rumorId.ToString(CultureInfo.InvariantCulture)} is now quelled · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    public LogbookMutationExecutionResult AddReputation(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int reputationId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return MutateArrayEntry(
            memory,
            runtimeProfile,
            handle,
            reputationId,
            s_pcReputationFieldId,
            s_reputationAdd,
            "Reputation",
            add: true,
            timeout
        );
    }

    public LogbookMutationExecutionResult RemoveReputation(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int reputationId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return MutateArrayEntry(
            memory,
            runtimeProfile,
            handle,
            reputationId,
            s_pcReputationFieldId,
            s_reputationRemove,
            "Reputation",
            add: false,
            timeout
        );
    }

    public LogbookMutationExecutionResult AddBlessing(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int blessingId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return MutateArrayEntry(
            memory,
            runtimeProfile,
            handle,
            blessingId,
            s_pcBlessingFieldId,
            s_blessAdd,
            "Blessing",
            add: true,
            timeout
        );
    }

    public LogbookMutationExecutionResult RemoveBlessing(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int blessingId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return MutateArrayEntry(
            memory,
            runtimeProfile,
            handle,
            blessingId,
            s_pcBlessingFieldId,
            s_blessRemove,
            "Blessing",
            add: false,
            timeout
        );
    }

    public LogbookMutationExecutionResult AddCurse(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int curseId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return MutateArrayEntry(
            memory,
            runtimeProfile,
            handle,
            curseId,
            s_pcCurseFieldId,
            s_curseAdd,
            "Curse",
            add: true,
            timeout
        );
    }

    public LogbookMutationExecutionResult RemoveCurse(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int curseId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return MutateArrayEntry(
            memory,
            runtimeProfile,
            handle,
            curseId,
            s_pcCurseFieldId,
            s_curseRemove,
            "Curse",
            add: false,
            timeout
        );
    }

    public LogbookMutationExecutionResult AddKey(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int keyId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        if (keyId <= 0)
            throw new InvalidOperationException("Key id must be a positive integer.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var keyRingHandle = FindKeyRingHandle(memory, dispatcher, handle, timeout);
        if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(keyRingHandle))
        {
            throw new InvalidOperationException(
                $"No key ring is currently available on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        if (ArrayFieldContainsValue(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, keyId, timeout))
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"Key {keyId.ToString(CultureInfo.InvariantCulture)} is already present on {RuntimeSemanticCatalog.FormatHandle(keyRingHandle)}.",
                NoMutation: true
            );
        }

        var beforeLength = ArrayFieldLength(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, timeout);
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldUInt32Setter,
            [
                TargetResolver.ToLow32(keyRingHandle),
                TargetResolver.ToHigh32(keyRingHandle),
                unchecked((uint)s_keyRingListFieldId),
                unchecked((uint)beforeLength),
                unchecked((uint)keyId),
            ],
            timeout
        );
        UpdateKeyRingInventoryArt(memory, dispatcher, keyRingHandle, timeout);
        var afterLength = ArrayFieldLength(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, timeout);
        if (
            afterLength <= beforeLength
            || !ArrayFieldContainsValue(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, keyId, timeout)
        )
        {
            throw new InvalidOperationException(
                $"Failed to add key {keyId.ToString(CultureInfo.InvariantCulture)} to {RuntimeSemanticCatalog.FormatHandle(keyRingHandle)}."
            );
        }

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_uint32_set @ {s_objectArrayFieldUInt32Setter.Site} · obj_field_int32_set @ {s_objectFieldInt32Setter.Site}",
            $"Key {keyId.ToString(CultureInfo.InvariantCulture)} added to {RuntimeSemanticCatalog.FormatHandle(keyRingHandle)} · length {beforeLength.ToString(CultureInfo.InvariantCulture)} -> {afterLength.ToString(CultureInfo.InvariantCulture)}"
        );
    }

    public LogbookMutationExecutionResult RemoveKey(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int keyId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        if (keyId <= 0)
            throw new InvalidOperationException("Key id must be a positive integer.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var keyRingHandle = FindKeyRingHandle(memory, dispatcher, handle, timeout);
        if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(keyRingHandle))
        {
            throw new InvalidOperationException(
                $"No key ring is currently available on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        var keyIndex = FindArrayFieldIndex(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, keyId, timeout);
        if (keyIndex < 0)
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"Key {keyId.ToString(CultureInfo.InvariantCulture)} is not currently present on {RuntimeSemanticCatalog.FormatHandle(keyRingHandle)}.",
                NoMutation: true
            );
        }

        var beforeLength = ArrayFieldLength(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, timeout);
        for (var index = keyIndex + 1; index < beforeLength; index++)
        {
            var shiftedValue = ReadArrayFieldValue(
                memory,
                dispatcher,
                keyRingHandle,
                s_keyRingListFieldId,
                index,
                timeout
            );
            _ = Invoke(
                memory,
                dispatcher,
                s_objectArrayFieldUInt32Setter,
                [
                    TargetResolver.ToLow32(keyRingHandle),
                    TargetResolver.ToHigh32(keyRingHandle),
                    unchecked((uint)s_keyRingListFieldId),
                    unchecked((uint)(index - 1)),
                    unchecked((uint)shiftedValue),
                ],
                timeout
            );
        }

        SetArrayFieldLength(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, beforeLength - 1, timeout);
        UpdateKeyRingInventoryArt(memory, dispatcher, keyRingHandle, timeout);
        var afterLength = ArrayFieldLength(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, timeout);
        if (
            afterLength != beforeLength - 1
            || ArrayFieldContainsValue(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, keyId, timeout)
        )
        {
            throw new InvalidOperationException(
                $"Failed to remove key {keyId.ToString(CultureInfo.InvariantCulture)} from {RuntimeSemanticCatalog.FormatHandle(keyRingHandle)}."
            );
        }

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_uint32_set @ {s_objectArrayFieldUInt32Setter.Site} · obj_array_field_length_set @ {s_objectArrayFieldLengthSetter.Site} · obj_field_int32_set @ {s_objectFieldInt32Setter.Site}",
            $"Key {keyId.ToString(CultureInfo.InvariantCulture)} removed from {RuntimeSemanticCatalog.FormatHandle(keyRingHandle)} · length {beforeLength.ToString(CultureInfo.InvariantCulture)} -> {afterLength.ToString(CultureInfo.InvariantCulture)}"
        );
    }

    public LogbookMutationExecutionResult AddInjury(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int descriptionId,
        int injuryType,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        if (descriptionId <= 0)
            throw new InvalidOperationException("Injury source description id must be a positive integer.");

        ValidateInjuryType(injuryType);

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var targetSlot = FindNextAvailableInjurySlot(memory, dispatcher, handle, timeout);
        if (targetSlot < 0)
        {
            throw new InvalidOperationException(
                $"No free injury-history slot remains on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Setter,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)s_logbookFieldId),
                unchecked((uint)targetSlot),
                unchecked((uint)descriptionId),
            ],
            timeout
        );
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Setter,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)s_logbookFieldId),
                unchecked((uint)(targetSlot + 1)),
                unchecked((uint)injuryType),
            ],
            timeout
        );

        var storedDescriptionId = ReadArrayFieldValue(
            memory,
            dispatcher,
            handle,
            s_logbookFieldId,
            targetSlot,
            timeout
        );
        var storedInjuryType = ReadArrayFieldValue(
            memory,
            dispatcher,
            handle,
            s_logbookFieldId,
            targetSlot + 1,
            timeout
        );
        if (storedDescriptionId != descriptionId || storedInjuryType != injuryType)
        {
            throw new InvalidOperationException(
                $"Failed to record {FormatInjuryType(injuryType).ToLowerInvariant()} history from description {descriptionId.ToString(CultureInfo.InvariantCulture)} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        var totalEntries = CountInjuryEntries(memory, dispatcher, handle, timeout);
        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_int32_set @ {s_objectArrayFieldInt32Setter.Site}",
            $"Recorded {FormatInjuryType(injuryType)} history from description {descriptionId.ToString(CultureInfo.InvariantCulture)} in slot {targetSlot.ToString(CultureInfo.InvariantCulture)} on {RuntimeSemanticCatalog.FormatHandle(handle)} · total entries {totalEntries.ToString(CultureInfo.InvariantCulture)}"
        );
    }

    public LogbookMutationExecutionResult RemoveInjury(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int descriptionId,
        int injuryType,
        int slotIndex,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        if (descriptionId <= 0)
            throw new InvalidOperationException("Injury source description id must be a positive integer.");

        ValidateInjuryType(injuryType);
        ValidateInjurySlotIndex(slotIndex);

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var records = ReadInjuryHistoryRecords(memory, dispatcher, handle, timeout);
        var targetIndex = records.FindIndex(record => record.SlotIndex == slotIndex);
        if (targetIndex < 0)
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"Injury history slot {slotIndex.ToString(CultureInfo.InvariantCulture)} is already empty on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var targetRecord = records[targetIndex];
        if (targetRecord.DescriptionId != descriptionId || targetRecord.InjuryType != injuryType)
        {
            throw new InvalidOperationException(
                $"Injury history slot {slotIndex.ToString(CultureInfo.InvariantCulture)} no longer matches description {descriptionId.ToString(CultureInfo.InvariantCulture)} / {FormatInjuryType(injuryType)} on {RuntimeSemanticCatalog.FormatHandle(handle)}. Reload the journal page to refresh the live shortcut list."
            );
        }

        if (
            IsInjuryTypeActive(memory, dispatcher, handle, injuryType, timeout)
            && records.Last(record => record.InjuryType == injuryType).SlotIndex == slotIndex
        )
        {
            throw new InvalidOperationException(
                $"Cannot remove the active {FormatInjuryType(injuryType).ToLowerInvariant()} row from slot {slotIndex.ToString(CultureInfo.InvariantCulture)} on {RuntimeSemanticCatalog.FormatHandle(handle)}. Heal the condition first, then remove the healed history row."
            );
        }

        for (var index = targetIndex; index < records.Count - 1; index++)
        {
            var nextRecord = records[index + 1];
            WriteInjuryFieldPair(
                memory,
                dispatcher,
                handle,
                records[index].SlotIndex,
                nextRecord.DescriptionId,
                nextRecord.InjuryType,
                timeout
            );
        }

        var tailSlot = records[^1].SlotIndex;
        WriteInjuryFieldPair(memory, dispatcher, handle, tailSlot, 0, 0, timeout);

        var remainingRecords = ReadInjuryHistoryRecords(memory, dispatcher, handle, timeout);
        var expectedRecords = records.Where((_, index) => index != targetIndex).ToArray();
        if (remainingRecords.Count != expectedRecords.Length)
        {
            throw new InvalidOperationException(
                $"Failed to remove injury history slot {slotIndex.ToString(CultureInfo.InvariantCulture)} from {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        for (var index = 0; index < expectedRecords.Length; index++)
        {
            if (
                remainingRecords[index].DescriptionId != expectedRecords[index].DescriptionId
                || remainingRecords[index].InjuryType != expectedRecords[index].InjuryType
            )
            {
                throw new InvalidOperationException(
                    $"Failed to compact injury history after removing slot {slotIndex.ToString(CultureInfo.InvariantCulture)} from {RuntimeSemanticCatalog.FormatHandle(handle)}."
                );
            }
        }

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_int32_set @ {s_objectArrayFieldInt32Setter.Site} · obj_field_int32_get @ {s_objectFieldInt32Getter.Site} · effect_count_effects_of_type @ {s_effectCountEffectsOfType.Site}",
            $"Removed healed {FormatInjuryType(injuryType).ToLowerInvariant()} history from description {descriptionId.ToString(CultureInfo.InvariantCulture)} in slot {slotIndex.ToString(CultureInfo.InvariantCulture)} on {RuntimeSemanticCatalog.FormatHandle(handle)} · total entries {records.Count.ToString(CultureInfo.InvariantCulture)} -> {remainingRecords.Count.ToString(CultureInfo.InvariantCulture)}"
        );
    }

    public LogbookMutationExecutionResult AddKill(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        ulong victimHandle,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        ValidateKillParticipant(memory, handle, "Logbook target");
        ValidateKillParticipant(memory, victimHandle, "Kill victim");
        var beforeValues = ReadKillSummaryValues(memory, dispatcher, handle, timeout);
        var result = Invoke(
            memory,
            dispatcher,
            s_logbookAddKill,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                TargetResolver.ToLow32(victimHandle),
                TargetResolver.ToHigh32(victimHandle),
            ],
            timeout
        );
        var afterValues = ReadKillSummaryValues(memory, dispatcher, handle, timeout);
        if (afterValues[LbkTotalKills] != beforeValues[LbkTotalKills] + 1)
        {
            throw new InvalidOperationException(
                $"Failed to record a kill from {RuntimeSemanticCatalog.FormatHandle(victimHandle)} on {RuntimeSemanticCatalog.FormatHandle(handle)}. Total kills stayed at {afterValues[LbkTotalKills].ToString(CultureInfo.InvariantCulture)} instead of advancing from {beforeValues[LbkTotalKills].ToString(CultureInfo.InvariantCulture)} to {(beforeValues[LbkTotalKills] + 1).ToString(CultureInfo.InvariantCulture)}."
            );
        }

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"logbook_get_kills @ {s_logbookGetKills.Site} · logbook_add_kill @ {s_logbookAddKill.Site}",
            $"Recorded one kill on {RuntimeSemanticCatalog.FormatHandle(handle)} from {RuntimeSemanticCatalog.FormatHandle(victimHandle)} · total kills {beforeValues[LbkTotalKills].ToString(CultureInfo.InvariantCulture)} -> {afterValues[LbkTotalKills].ToString(CultureInfo.InvariantCulture)} · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    public LogbookMutationExecutionResult SetKillSummary(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        LogbookMutationKind kind,
        int descriptionId,
        int value,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        if (!KillLogbookSummaryCatalog.TryGetDefinition(kind, out var definition))
            throw new InvalidOperationException($"Unsupported kill-summary mutation '{kind}'.");

        ValidateKillSummaryValue(definition, descriptionId, value);

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        ValidateKillParticipant(memory, handle, "Logbook target");

        var beforeComputed = ReadKillSummaryValues(memory, dispatcher, handle, timeout);
        var beforeBacking = ReadKillSummaryBackingValues(memory, dispatcher, handle, timeout);
        EnsureKillSummaryBackingMatches(handle, beforeComputed, beforeBacking);

        int[] expectedBacking = [.. beforeBacking];
        if (definition.RequiresDescription)
            expectedBacking[definition.DescriptionIndex!.Value] = descriptionId;

        expectedBacking[definition.ValueIndex] = value;

        if (expectedBacking.SequenceEqual(beforeBacking))
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"logbook_get_kills @ {s_logbookGetKills.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"{definition.SummaryLabel} already records {FormatKillSummarySelection(definition, descriptionId, value)} on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        WriteKillSummaryBackingValues(memory, dispatcher, handle, definition, descriptionId, value, timeout);
        ValidateKillSummaryWrite(memory, dispatcher, handle, expectedBacking, timeout);

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"logbook_get_kills @ {s_logbookGetKills.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_int32_set @ {s_objectArrayFieldInt32Setter.Site}",
            $"{definition.SummaryLabel} {FormatKillSummaryTransition(definition, beforeBacking, expectedBacking)} on {RuntimeSemanticCatalog.FormatHandle(handle)}"
        );
    }

    public LogbookMutationExecutionResult SetBackground(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int backgroundId,
        int backgroundTextId,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentBackground = InvokeInt32(
            memory,
            dispatcher,
            s_backgroundGet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
            timeout
        );
        var currentTextId = InvokeInt32(
            memory,
            dispatcher,
            s_backgroundTextGet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
            timeout
        );
        if (currentBackground == backgroundId && currentTextId == backgroundTextId)
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"background_get @ {s_backgroundGet.Site} · background_text_get @ {s_backgroundTextGet.Site}",
                $"Background {backgroundId.ToString(CultureInfo.InvariantCulture)} / text {backgroundTextId.ToString(CultureInfo.InvariantCulture)} is already applied to {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var result = Invoke(
            memory,
            dispatcher,
            s_backgroundSet,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)backgroundId),
                unchecked((uint)backgroundTextId),
            ],
            timeout
        );
        var updatedBackground = InvokeInt32(
            memory,
            dispatcher,
            s_backgroundGet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
            timeout
        );
        var updatedTextId = InvokeInt32(
            memory,
            dispatcher,
            s_backgroundTextGet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
            timeout
        );
        if (updatedBackground != backgroundId || updatedTextId != backgroundTextId)
        {
            throw new InvalidOperationException(
                $"Failed to apply background {backgroundId.ToString(CultureInfo.InvariantCulture)} / text {backgroundTextId.ToString(CultureInfo.InvariantCulture)}."
            );
        }

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"background_get @ {s_backgroundGet.Site} · background_text_get @ {s_backgroundTextGet.Site} · background_set @ {s_backgroundSet.Site}",
            $"Background {currentBackground.ToString(CultureInfo.InvariantCulture)} / text {currentTextId.ToString(CultureInfo.InvariantCulture)} -> {updatedBackground.ToString(CultureInfo.InvariantCulture)} / text {updatedTextId.ToString(CultureInfo.InvariantCulture)} · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    public LogbookMutationExecutionResult ClearBackground(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentBackground = InvokeInt32(
            memory,
            dispatcher,
            s_backgroundGet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
            timeout
        );
        var currentTextId = InvokeInt32(
            memory,
            dispatcher,
            s_backgroundTextGet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
            timeout
        );
        if (currentBackground == 0 && currentTextId == 0)
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"background_get @ {s_backgroundGet.Site} · background_text_get @ {s_backgroundTextGet.Site}",
                $"No background is currently applied to {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var result = Invoke(
            memory,
            dispatcher,
            s_backgroundClear,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
            timeout
        );
        var updatedBackground = InvokeInt32(
            memory,
            dispatcher,
            s_backgroundGet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
            timeout
        );
        var updatedTextId = InvokeInt32(
            memory,
            dispatcher,
            s_backgroundTextGet,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
            timeout
        );
        if (updatedBackground != 0 || updatedTextId != 0)
            throw new InvalidOperationException("Failed to clear the current background.");

        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"background_get @ {s_backgroundGet.Site} · background_text_get @ {s_backgroundTextGet.Site} · background_clear @ {s_backgroundClear.Site}",
            $"Background {currentBackground.ToString(CultureInfo.InvariantCulture)} / text {currentTextId.ToString(CultureInfo.InvariantCulture)} cleared on {RuntimeSemanticCatalog.FormatHandle(handle)} · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    private static LogbookMutationExecutionResult MutateArrayEntry(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int entryId,
        int fieldId,
        FunctionDefinition function,
        string label,
        bool add,
        TimeSpan timeout
    )
    {
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var hadValue = ArrayFieldContainsValue(memory, dispatcher, handle, fieldId, entryId, timeout);
        if (hadValue == add)
        {
            return new LogbookMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                add
                    ? $"{label} {entryId.ToString(CultureInfo.InvariantCulture)} is already present on {RuntimeSemanticCatalog.FormatHandle(handle)}."
                    : $"{label} {entryId.ToString(CultureInfo.InvariantCulture)} is not currently present on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var beforeLength = ArrayFieldLength(memory, dispatcher, handle, fieldId, timeout);
        var result = Invoke(
            memory,
            dispatcher,
            function,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)entryId)],
            timeout
        );
        var hasValue = ArrayFieldContainsValue(memory, dispatcher, handle, fieldId, entryId, timeout);
        if (hasValue != add)
        {
            throw new InvalidOperationException(
                $"Failed to {(add ? "add" : "remove")} {label.ToLowerInvariant()} {entryId.ToString(CultureInfo.InvariantCulture)} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        var afterLength = ArrayFieldLength(memory, dispatcher, handle, fieldId, timeout);
        return new LogbookMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · {function.Key} @ {function.Site}",
            $"{label} {entryId.ToString(CultureInfo.InvariantCulture)} {(add ? "added" : "removed")} on {RuntimeSemanticCatalog.FormatHandle(handle)} · length {beforeLength.ToString(CultureInfo.InvariantCulture)} -> {afterLength.ToString(CultureInfo.InvariantCulture)} · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    private static PcQuestStateRecord ReadPcQuestState(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int questId,
        TimeSpan timeout
    )
    {
        using var remoteBuffer = new RemoteAllocation(memory, PcQuestStateByteSize);
        memory.WriteBytes(remoteBuffer.Address, new byte[PcQuestStateByteSize]);
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldPcQuestGet,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)s_pcQuestFieldId),
                unchecked((uint)questId),
                remoteBuffer.Address32,
            ],
            timeout
        );
        var bytes = memory.ReadBytes(remoteBuffer.Address, PcQuestStateByteSize);
        var span = bytes.AsSpan();
        return new PcQuestStateRecord(
            BinaryPrimitives.ReadUInt32LittleEndian(span[..sizeof(uint)]),
            BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint), sizeof(uint))),
            BinaryPrimitives.ReadInt32LittleEndian(span.Slice(PcQuestStateRawStateOffset, sizeof(int)))
        );
    }

    private static NativeInvocationResult WritePcQuestState(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int questId,
        PcQuestStateRecord questState,
        TimeSpan timeout
    )
    {
        Span<byte> bytes = stackalloc byte[PcQuestStateByteSize];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[..sizeof(uint)], questState.Days);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(sizeof(uint), sizeof(uint)), questState.Milliseconds);
        BinaryPrimitives.WriteInt32LittleEndian(
            bytes.Slice(PcQuestStateRawStateOffset, sizeof(int)),
            questState.RawState
        );
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(PcQuestStatePaddingOffset, sizeof(int)), 0);
        using var remoteBuffer = new RemoteAllocation(memory, PcQuestStateByteSize);
        memory.WriteBytes(remoteBuffer.Address, bytes);
        return Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldPcQuestSet,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)s_pcQuestFieldId),
                unchecked((uint)questId),
                remoteBuffer.Address32,
            ],
            timeout
        );
    }

    private static bool CanUseNativeQuestStatePath(int requestedState) =>
        !RuntimeWatchValueCatalog.QuestHasBotchedModifier(requestedState);

    private static int ResolveRequestedQuestRawState(int currentRawState, int requestedState)
    {
        if (RuntimeWatchValueCatalog.QuestHasBotchedModifier(requestedState))
            return requestedState;

        if (requestedState != QuestStateBotched)
            return requestedState;

        var baseState = RuntimeWatchValueCatalog.QuestBaseState(currentRawState);
        if (baseState is QuestStateUnknown or QuestStateBotched)
            baseState = QuestStateAccepted;

        return baseState | RuntimeWatchValueCatalog.QuestBotchedModifier;
    }

    private static int QuestLogicalState(int rawState) =>
        RuntimeWatchValueCatalog.QuestHasBotchedModifier(rawState)
            ? QuestStateBotched
            : RuntimeWatchValueCatalog.QuestBaseState(rawState);

    private static string FormatQuestPcState(int rawState) => RuntimeWatchValueCatalog.QuestPcStateName(rawState);

    private static int ArrayFieldLength(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        TimeSpan timeout
    ) =>
        Math.Max(
            0,
            InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldLengthGetter,
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)fieldId)],
                timeout
            )
        );

    private static bool ArrayFieldContainsValue(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        int expectedValue,
        TimeSpan timeout
    )
    {
        var length = ArrayFieldLength(memory, dispatcher, handle, fieldId, timeout);
        for (var index = 0; index < length; index++)
        {
            var currentValue = InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldInt32Getter,
                [
                    TargetResolver.ToLow32(handle),
                    TargetResolver.ToHigh32(handle),
                    unchecked((uint)fieldId),
                    unchecked((uint)index),
                ],
                timeout
            );
            if (currentValue == expectedValue)
                return true;
        }

        return false;
    }

    private static int FindArrayFieldIndex(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        int expectedValue,
        TimeSpan timeout
    )
    {
        var length = ArrayFieldLength(memory, dispatcher, handle, fieldId, timeout);
        for (var index = 0; index < length; index++)
        {
            if (ReadArrayFieldValue(memory, dispatcher, handle, fieldId, index, timeout) == expectedValue)
                return index;
        }

        return -1;
    }

    private static int ReadArrayFieldValue(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        int index,
        TimeSpan timeout
    ) =>
        InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)fieldId),
                unchecked((uint)index),
            ],
            timeout
        );

    private static void SetArrayFieldLength(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        int length,
        TimeSpan timeout
    )
    {
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldLengthSetter,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)fieldId),
                unchecked((uint)length),
            ],
            timeout
        );
    }

    private static ulong FindKeyRingHandle(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong targetHandle,
        TimeSpan timeout
    )
    {
        var target = LiveObjectInspector.Inspect(memory, targetHandle);
        var objectTypeName = target.Header?.ObjectTypeName;
        if (string.Equals(objectTypeName, KeyRingObjectTypeName, StringComparison.Ordinal))
            return targetHandle;

        if (
            !string.Equals(objectTypeName, PcObjectTypeName, StringComparison.Ordinal)
            && !string.Equals(objectTypeName, NpcObjectTypeName, StringComparison.Ordinal)
        )
        {
            throw new InvalidOperationException(
                $"Keyring edits expect a PC, NPC, or key ring handle, but {RuntimeSemanticCatalog.FormatHandle(targetHandle)} resolved as {objectTypeName ?? "an unknown object"}."
            );
        }

        var inventoryLength = ArrayFieldLength(memory, dispatcher, targetHandle, s_critterInventoryFieldId, timeout);
        ulong fallbackKeyRingHandle = 0;
        for (var index = 0; index < inventoryLength; index++)
        {
            var inventoryHandle = ReadArrayFieldHandleValue(
                memory,
                dispatcher,
                targetHandle,
                s_critterInventoryFieldId,
                index,
                timeout
            );
            if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(inventoryHandle))
                continue;

            var inventoryIdentity = LiveObjectInspector.Inspect(memory, inventoryHandle);
            if (
                !string.Equals(
                    inventoryIdentity.Header?.ObjectTypeName,
                    KeyRingObjectTypeName,
                    StringComparison.Ordinal
                )
            )
                continue;

            fallbackKeyRingHandle = inventoryHandle;
            if (ArrayFieldLength(memory, dispatcher, inventoryHandle, s_keyRingListFieldId, timeout) > 0)
                return inventoryHandle;
        }

        return fallbackKeyRingHandle;
    }

    private static ulong ReadArrayFieldHandleValue(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        int index,
        TimeSpan timeout
    )
    {
        var result = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldHandleGetter,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)fieldId),
                unchecked((uint)index),
            ],
            timeout
        );
        return ComposeWideResult(result.ResultEax, result.ResultEdx);
    }

    private static void UpdateKeyRingInventoryArt(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong keyRingHandle,
        TimeSpan timeout
    )
    {
        var hasKeys = ArrayFieldLength(memory, dispatcher, keyRingHandle, s_keyRingListFieldId, timeout) > 0;
        var inventoryArtId = hasKeys ? KeyRingInventoryArtId : EmptyKeyRingInventoryArtId;
        _ = Invoke(
            memory,
            dispatcher,
            s_objectFieldInt32Setter,
            [
                TargetResolver.ToLow32(keyRingHandle),
                TargetResolver.ToHigh32(keyRingHandle),
                unchecked((uint)s_itemInventoryArtFieldId),
                unchecked((uint)inventoryArtId),
            ],
            timeout
        );
        var storedInventoryArtId = InvokeInt32(
            memory,
            dispatcher,
            s_objectFieldInt32Getter,
            [
                TargetResolver.ToLow32(keyRingHandle),
                TargetResolver.ToHigh32(keyRingHandle),
                unchecked((uint)s_itemInventoryArtFieldId),
            ],
            timeout
        );
        if (storedInventoryArtId != inventoryArtId)
        {
            throw new InvalidOperationException(
                $"Failed to refresh key ring inventory art on {RuntimeSemanticCatalog.FormatHandle(keyRingHandle)}."
            );
        }
    }

    private static int FindNextAvailableInjurySlot(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        TimeSpan timeout
    )
    {
        for (var slot = FirstInjuryRecordIndex; slot < MaxLogbookFieldIndex - 1; slot += 2)
        {
            if (ReadArrayFieldValue(memory, dispatcher, handle, s_logbookFieldId, slot, timeout) == 0)
                return slot;
        }

        return -1;
    }

    private static int CountInjuryEntries(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        TimeSpan timeout
    )
    {
        var count = 0;
        for (var slot = FirstInjuryRecordIndex; slot < MaxLogbookFieldIndex - 1; slot += 2)
        {
            if (ReadArrayFieldValue(memory, dispatcher, handle, s_logbookFieldId, slot, timeout) == 0)
                break;

            count++;
        }

        return count;
    }

    private static List<InjuryHistoryRecord> ReadInjuryHistoryRecords(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        TimeSpan timeout
    )
    {
        List<InjuryHistoryRecord> records = [];
        for (var slot = FirstInjuryRecordIndex; slot < MaxLogbookFieldIndex - 1; slot += 2)
        {
            var descriptionId = ReadArrayFieldValue(memory, dispatcher, handle, s_logbookFieldId, slot, timeout);
            if (descriptionId == 0)
                break;

            records.Add(
                new InjuryHistoryRecord(
                    slot,
                    descriptionId,
                    ReadArrayFieldValue(memory, dispatcher, handle, s_logbookFieldId, slot + 1, timeout)
                )
            );
        }

        return records;
    }

    private static void WriteInjuryFieldPair(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int slotIndex,
        int descriptionId,
        int injuryType,
        TimeSpan timeout
    )
    {
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Setter,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)s_logbookFieldId),
                unchecked((uint)slotIndex),
                unchecked((uint)descriptionId),
            ],
            timeout
        );
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Setter,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)s_logbookFieldId),
                unchecked((uint)(slotIndex + 1)),
                unchecked((uint)injuryType),
            ],
            timeout
        );
    }

    private static void ValidateInjurySlotIndex(int slotIndex)
    {
        if (slotIndex < FirstInjuryRecordIndex || slotIndex >= MaxLogbookFieldIndex - 1 || (slotIndex & 1) != 0)
        {
            throw new InvalidOperationException(
                $"Injury history slot '{slotIndex.ToString(CultureInfo.InvariantCulture)}' is invalid. Use one live injury shortcut so ArcNET can target an exact even-numbered injury slot."
            );
        }
    }

    private static bool IsInjuryTypeActive(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int injuryType,
        TimeSpan timeout
    )
    {
        var critterFlags = InvokeInt32(
            memory,
            dispatcher,
            s_objectFieldInt32Getter,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)s_critterFlagsFieldId)],
            timeout
        );
        return injuryType switch
        {
            LbiBlinded => (critterFlags & OcfBlinded) != 0,
            LbiCrippledArm => (critterFlags & (OcfCrippledArmsOne | OcfCrippledArmsBoth)) != 0,
            LbiCrippledLeg => (critterFlags & OcfCrippledLegsBoth) != 0,
            LbiScarred => InvokeInt32(
                memory,
                dispatcher,
                s_effectCountEffectsOfType,
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), EffectScarring],
                timeout
            ) > 0,
            _ => false,
        };
    }

    private static int[] ReadKillSummaryValues(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        TimeSpan timeout
    )
    {
        using var remoteBuffer = new RemoteAllocation(memory, KillStatCount * sizeof(int));
        memory.WriteBytes(remoteBuffer.Address, new byte[KillStatCount * sizeof(int)]);
        _ = Invoke(
            memory,
            dispatcher,
            s_logbookGetKills,
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), remoteBuffer.Address32],
            timeout
        );
        var bytes = memory.ReadBytes(remoteBuffer.Address, KillStatCount * sizeof(int));
        return
        [
            .. Enumerable
                .Range(0, KillStatCount)
                .Select(index =>
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(index * sizeof(int), sizeof(int)))
                ),
        ];
    }

    private static int[] ReadKillSummaryBackingValues(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        TimeSpan timeout
    )
    {
        var length = ArrayFieldLength(memory, dispatcher, handle, s_logbookFieldId, timeout);
        if (length < KillStatCount)
        {
            throw new InvalidOperationException(
                $"Kill-summary backing on {RuntimeSemanticCatalog.FormatHandle(handle)} exposes only {length.ToString(CultureInfo.InvariantCulture)} logbook slots. ArcNET expected at least {KillStatCount.ToString(CultureInfo.InvariantCulture)} slots before patching raw kill-ledger values."
            );
        }

        return
        [
            .. Enumerable
                .Range(0, KillStatCount)
                .Select(index => ReadArrayFieldValue(memory, dispatcher, handle, s_logbookFieldId, index, timeout)),
        ];
    }

    private static void EnsureKillSummaryBackingMatches(ulong handle, int[] computedValues, int[] backingValues)
    {
        if (computedValues.SequenceEqual(backingValues))
            return;

        throw new InvalidOperationException(
            $"Kill-summary backing on {RuntimeSemanticCatalog.FormatHandle(handle)} does not currently mirror logbook_get_kills. ArcNET will not patch raw kill-ledger slots until the runtime mapping is confirmed on this build."
        );
    }

    private static void WriteKillSummaryBackingValues(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        KillLogbookSummaryDefinition definition,
        int descriptionId,
        int value,
        TimeSpan timeout
    )
    {
        if (definition.RequiresDescription)
            WriteKillSummaryBackingValue(
                memory,
                dispatcher,
                handle,
                definition.DescriptionIndex!.Value,
                descriptionId,
                timeout
            );

        WriteKillSummaryBackingValue(memory, dispatcher, handle, definition.ValueIndex, value, timeout);
    }

    private static void WriteKillSummaryBackingValue(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int index,
        int value,
        TimeSpan timeout
    )
    {
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Setter,
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)s_logbookFieldId),
                unchecked((uint)index),
                unchecked((uint)value),
            ],
            timeout
        );

        var storedValue = ReadArrayFieldValue(memory, dispatcher, handle, s_logbookFieldId, index, timeout);
        if (storedValue != value)
        {
            throw new InvalidOperationException(
                $"Failed to persist kill-ledger slot {index.ToString(CultureInfo.InvariantCulture)} on {RuntimeSemanticCatalog.FormatHandle(handle)}. Stored {storedValue.ToString(CultureInfo.InvariantCulture)} instead of {value.ToString(CultureInfo.InvariantCulture)}."
            );
        }
    }

    private static void ValidateKillSummaryWrite(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int[] expectedBacking,
        TimeSpan timeout
    )
    {
        var actualBacking = ReadKillSummaryBackingValues(memory, dispatcher, handle, timeout);
        var actualComputed = ReadKillSummaryValues(memory, dispatcher, handle, timeout);
        if (actualBacking.SequenceEqual(expectedBacking) && actualComputed.SequenceEqual(expectedBacking))
            return;

        throw new InvalidOperationException(
            $"Kill-summary verification failed on {RuntimeSemanticCatalog.FormatHandle(handle)} after a raw kill-ledger edit. ArcNET expected both the direct logbook array and logbook_get_kills to match the requested values."
        );
    }

    private static void ValidateKillSummaryValue(KillLogbookSummaryDefinition definition, int descriptionId, int value)
    {
        if (definition.RequiresDescription && descriptionId < 0)
        {
            throw new InvalidOperationException(
                $"{definition.OperationLabel} requires a non-negative description id. Use 0 to clear the name slot or pick one description from the local catalog."
            );
        }

        if (value < 0)
        {
            throw new InvalidOperationException(
                $"{definition.OperationLabel} requires a non-negative {definition.ValueLabel.ToLowerInvariant()}."
            );
        }
    }

    private static string FormatKillSummarySelection(
        KillLogbookSummaryDefinition definition,
        int descriptionId,
        int value
    ) =>
        definition.RequiresDescription
            ? $"{FormatKillSummaryDescription(descriptionId)} / {FormatKillSummaryMetric(definition, value)}"
            : FormatKillSummaryMetric(definition, value);

    private static string FormatKillSummaryTransition(
        KillLogbookSummaryDefinition definition,
        int[] beforeBacking,
        int[] afterBacking
    ) =>
        definition.RequiresDescription
            ? $"{FormatKillSummaryDescription(beforeBacking[definition.DescriptionIndex!.Value])} / {FormatKillSummaryMetric(definition, beforeBacking[definition.ValueIndex])} -> {FormatKillSummaryDescription(afterBacking[definition.DescriptionIndex.Value])} / {FormatKillSummaryMetric(definition, afterBacking[definition.ValueIndex])}"
            : $"{FormatKillSummaryMetric(definition, beforeBacking[definition.ValueIndex])} -> {FormatKillSummaryMetric(definition, afterBacking[definition.ValueIndex])}";

    private static string FormatKillSummaryDescription(int descriptionId) =>
        descriptionId > 0 ? $"description {descriptionId.ToString(CultureInfo.InvariantCulture)}" : "no description";

    private static string FormatKillSummaryMetric(KillLogbookSummaryDefinition definition, int value) =>
        definition.MutationKind == LogbookMutationKind.SetTotalKills
            ? $"{value.ToString(CultureInfo.InvariantCulture)} total kills"
            : $"{definition.ValueLabel} {value.ToString(CultureInfo.InvariantCulture)}";

    private static void ValidateKillParticipant(ProcessMemory memory, ulong handle, string subject)
    {
        var identity = LiveObjectInspector.Inspect(memory, handle);
        var objectTypeName = identity.Header?.ObjectTypeName;
        if (
            string.Equals(objectTypeName, PcObjectTypeName, StringComparison.Ordinal)
            || string.Equals(objectTypeName, NpcObjectTypeName, StringComparison.Ordinal)
        )
        {
            return;
        }

        throw new InvalidOperationException(
            $"{subject} expects a PC or NPC handle, but {RuntimeSemanticCatalog.FormatHandle(handle)} resolved as {objectTypeName ?? "an unknown object"}."
        );
    }

    private static void ValidateInjuryType(int injuryType)
    {
        if (injuryType is LbiBlinded or LbiCrippledArm or LbiCrippledLeg or LbiScarred)
            return;

        throw new InvalidOperationException(
            $"Unknown injury type '{injuryType.ToString(CultureInfo.InvariantCulture)}'. Use blinded, crippled-arm, crippled-leg, or scarred."
        );
    }

    private static string FormatInjuryType(int injuryType) =>
        injuryType switch
        {
            LbiBlinded => "Blinded",
            LbiCrippledArm => "Crippled arm",
            LbiCrippledLeg => "Crippled leg",
            LbiScarred => "Scarred",
            _ => $"Injury {injuryType.ToString(CultureInfo.InvariantCulture)}",
        };

    private static NativeInvocationResult Invoke(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        FunctionDefinition function,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    ) => NativeInvoker.Invoke(dispatcher, memory, function.Key, stackArguments, timeout);

    private static int InvokeInt32(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        FunctionDefinition function,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    ) => unchecked((int)Invoke(memory, dispatcher, function, stackArguments, timeout).ResultEax);

    private static ulong ComposeWideResult(uint eax, uint edx) => eax | ((ulong)edx << 32);

    private static string FormatUInt32Result(ulong value) => $"0x{unchecked((uint)value):X8} ({unchecked((int)value)})";

    private static string CreateRumorDetail(WorkspaceTextCatalog.ResolvedRumor rumor)
    {
        if (!string.IsNullOrWhiteSpace(rumor.NormalText) && !string.IsNullOrWhiteSpace(rumor.DumbText))
            return $"Normal: {rumor.NormalText} · Dumb: {rumor.DumbText}";

        return FirstNonEmpty(rumor.NormalText, rumor.DumbText, $"Rumor {rumor.RumorId}")!;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

    private static readonly FunctionDefinition s_objectArrayFieldInt32Getter = FunctionCatalog.GetDefinition(
        "obj_array_field_int32_get"
    );
    private static readonly FunctionDefinition s_objectArrayFieldInt32Setter = FunctionCatalog.GetDefinition(
        "obj_array_field_int32_set"
    );
    private static readonly FunctionDefinition s_objectArrayFieldHandleGetter = FunctionCatalog.GetDefinition(
        "obj_array_field_handle_get"
    );
    private static readonly FunctionDefinition s_objectArrayFieldUInt32Setter = FunctionCatalog.GetDefinition(
        "obj_array_field_uint32_set"
    );
    private static readonly FunctionDefinition s_objectArrayFieldPcQuestGet = FunctionCatalog.GetDefinition(
        "obj_array_field_pc_quest_get"
    );
    private static readonly FunctionDefinition s_objectArrayFieldPcQuestSet = FunctionCatalog.GetDefinition(
        "obj_array_field_pc_quest_set"
    );
    private static readonly FunctionDefinition s_objectArrayFieldLengthGetter = FunctionCatalog.GetDefinition(
        "obj_array_field_length_get"
    );
    private static readonly FunctionDefinition s_objectArrayFieldLengthSetter = FunctionCatalog.GetDefinition(
        "obj_array_field_length_set"
    );
    private static readonly FunctionDefinition s_objectFieldInt32Getter = FunctionCatalog.GetDefinition(
        "obj_field_int32_get"
    );
    private static readonly FunctionDefinition s_objectFieldInt32Setter = FunctionCatalog.GetDefinition(
        "obj_field_int32_set"
    );
    private static readonly FunctionDefinition s_questStateGet = FunctionCatalog.GetDefinition("quest_state_get");
    private static readonly FunctionDefinition s_questStateSet = FunctionCatalog.GetDefinition("quest_state_set");
    private static readonly FunctionDefinition s_questGlobalStateGet = FunctionCatalog.GetDefinition(
        "quest_global_state_get"
    );
    private static readonly FunctionDefinition s_questGlobalStateSet = FunctionCatalog.GetDefinition(
        "quest_global_state_set"
    );
    private static readonly FunctionDefinition s_rumorKnownGet = FunctionCatalog.GetDefinition("rumor_known_get");
    private static readonly FunctionDefinition s_rumorKnownSet = FunctionCatalog.GetDefinition("rumor_known_set");
    private static readonly FunctionDefinition s_rumorQstateGet = FunctionCatalog.GetDefinition("rumor_qstate_get");
    private static readonly FunctionDefinition s_rumorQstateSet = FunctionCatalog.GetDefinition("rumor_qstate_set");
    private static readonly FunctionDefinition s_reputationAdd = FunctionCatalog.GetDefinition("reputation_add");
    private static readonly FunctionDefinition s_reputationRemove = FunctionCatalog.GetDefinition("reputation_remove");
    private static readonly FunctionDefinition s_blessAdd = FunctionCatalog.GetDefinition("bless_add");
    private static readonly FunctionDefinition s_blessRemove = FunctionCatalog.GetDefinition("bless_remove");
    private static readonly FunctionDefinition s_curseAdd = FunctionCatalog.GetDefinition("curse_add");
    private static readonly FunctionDefinition s_curseRemove = FunctionCatalog.GetDefinition("curse_remove");
    private static readonly FunctionDefinition s_logbookAddKill = FunctionCatalog.GetDefinition("logbook_add_kill");
    private static readonly FunctionDefinition s_logbookGetKills = FunctionCatalog.GetDefinition("logbook_get_kills");
    private static readonly FunctionDefinition s_effectCountEffectsOfType = FunctionCatalog.GetDefinition(
        "effect_count_effects_of_type"
    );
    private static readonly FunctionDefinition s_backgroundGet = FunctionCatalog.GetDefinition("background_get");
    private static readonly FunctionDefinition s_backgroundTextGet = FunctionCatalog.GetDefinition(
        "background_text_get"
    );
    private static readonly FunctionDefinition s_backgroundSet = FunctionCatalog.GetDefinition("background_set");
    private static readonly FunctionDefinition s_backgroundClear = FunctionCatalog.GetDefinition("background_clear");
    private static readonly int s_pcQuestFieldId = ResolveFieldId("OBJ_F_PC_QUEST_IDX");
    private static readonly int s_pcReputationFieldId = ResolveFieldId("OBJ_F_PC_REPUTATION_IDX");
    private static readonly int s_pcBlessingFieldId = ResolveFieldId("OBJ_F_PC_BLESSING_IDX");
    private static readonly int s_pcCurseFieldId = ResolveFieldId("OBJ_F_PC_CURSE_IDX");
    private static readonly int s_critterInventoryFieldId = ResolveFieldId("OBJ_F_CRITTER_INVENTORY_LIST_IDX");
    private static readonly int s_itemInventoryArtFieldId = ResolveFieldId("OBJ_F_ITEM_INV_AID");
    private static readonly int s_keyRingListFieldId = ResolveFieldId("OBJ_F_KEY_RING_LIST_IDX");
    private static readonly int s_logbookFieldId = ResolveFieldId("OBJ_F_PC_LOGBOOK_EGO_IDX");
    private static readonly int s_critterFlagsFieldId = ResolveFieldId("OBJ_F_CRITTER_FLAGS");
    private const int PcQuestStateByteSize = 0x10;
    private const int PcQuestStateRawStateOffset = 0x08;
    private const int PcQuestStatePaddingOffset = 0x0C;
    private const int QuestStateUnknown = 0;
    private const int QuestStateAccepted = 2;
    private const int QuestStateBotched = 6;
    private const int KillStatCount = 13;
    private const int LbkTotalKills = 0;
    private const int LbkMostPowerfulName = 1;
    private const int LbkMostPowerfulLevel = 2;
    private const int LbkLeastPowerfulName = 3;
    private const int LbkLeastPowerfulLevel = 4;
    private const int LbkMostGoodName = 5;
    private const int LbkMostGoodValue = 6;
    private const int LbkMostEvilName = 7;
    private const int LbkMostEvilValue = 8;
    private const int LbkMostMagicalName = 9;
    private const int LbkMostMagicalValue = 10;
    private const int LbkMostTechName = 11;
    private const int LbkMostTechValue = 12;
    private const int FirstInjuryRecordIndex = 64;
    private const int MaxLogbookFieldIndex = 1024;
    private const int LbiBlinded = 0;
    private const int LbiCrippledArm = 1;
    private const int LbiCrippledLeg = 2;
    private const int LbiScarred = 3;
    private const int OcfBlinded = 0x00000080;
    private const int OcfCrippledArmsOne = 0x00000100;
    private const int OcfCrippledArmsBoth = 0x00000200;
    private const int OcfCrippledLegsBoth = 0x00000400;
    private const uint EffectScarring = 50;
    private const string PcObjectTypeName = "Pc";
    private const string NpcObjectTypeName = "Npc";
    private const string KeyRingObjectTypeName = "KeyRing";
    private const int KeyRingInventoryArtId = 0x60001007;
    private const int EmptyKeyRingInventoryArtId = 0x60021007;

    private static int ResolveFieldId(string rawName)
    {
        if (ObjectFieldCatalog.TryGetFieldId(rawName, out var fieldId))
            return fieldId;

        throw new InvalidOperationException($"Unable to resolve runtime object field '{rawName}'.");
    }

    private readonly record struct PcQuestStateRecord(uint Days, uint Milliseconds, int RawState);

    private readonly record struct InjuryHistoryRecord(int SlotIndex, int DescriptionId, int InjuryType);
}
