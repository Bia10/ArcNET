using System.Buffers.Binary;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class LogbookBackend : ILogbookBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook diagnostics currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook diagnostics currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public Task<LogbookReadResult> ReadLogbookAsync(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        LogbookPage page,
        string workspacePath
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live logbook diagnostics currently require Windows.");

        return Task.Run(async () =>
        {
            using var memory = ProcessMemory.Attach(processId);
            var catalog = await WorkspaceTextCatalog
                .LoadFromModulePathAsync(string.IsNullOrWhiteSpace(workspacePath) ? memory.ModulePath : workspacePath)
                .ConfigureAwait(false);
            using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
            List<string> notes = [];
            if (!string.IsNullOrWhiteSpace(catalog.AvailabilityNote))
                notes.Add(catalog.AvailabilityNote);

            var intelligence = page is LogbookPage.All or LogbookPage.RumorsAndNotes or LogbookPage.Quests
                ? ReadStatValue(dispatcher, memory, handle, IntelligenceStatId)
                : 0;

            var data = page switch
            {
                LogbookPage.All => new LogbookPayload(
                    ReadRumorsAndNotes(dispatcher, memory, catalog, handle, intelligence),
                    ReadQuests(dispatcher, memory, catalog, handle, intelligence),
                    ReadReputations(dispatcher, memory, catalog, handle),
                    ReadBlessingsAndCurses(dispatcher, memory, catalog, handle),
                    ReadKillsAndInjuries(dispatcher, memory, catalog, handle),
                    ReadBackground(dispatcher, memory, catalog, handle),
                    ReadKeyring(dispatcher, memory, catalog, handle)
                ),
                LogbookPage.RumorsAndNotes => new LogbookPayload(
                    ReadRumorsAndNotes(dispatcher, memory, catalog, handle, intelligence),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                ),
                LogbookPage.Quests => new LogbookPayload(
                    null,
                    ReadQuests(dispatcher, memory, catalog, handle, intelligence),
                    null,
                    null,
                    null,
                    null,
                    null
                ),
                LogbookPage.Reputations => new LogbookPayload(
                    null,
                    null,
                    ReadReputations(dispatcher, memory, catalog, handle),
                    null,
                    null,
                    null,
                    null
                ),
                LogbookPage.BlessingsAndCurses => new LogbookPayload(
                    null,
                    null,
                    null,
                    ReadBlessingsAndCurses(dispatcher, memory, catalog, handle),
                    null,
                    null,
                    null
                ),
                LogbookPage.KillsAndInjuries => new LogbookPayload(
                    null,
                    null,
                    null,
                    null,
                    ReadKillsAndInjuries(dispatcher, memory, catalog, handle),
                    null,
                    null
                ),
                LogbookPage.Background => new LogbookPayload(
                    null,
                    null,
                    null,
                    null,
                    null,
                    ReadBackground(dispatcher, memory, catalog, handle),
                    null
                ),
                LogbookPage.KeyringContents => new LogbookPayload(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    ReadKeyring(dispatcher, memory, catalog, handle)
                ),
                _ => throw new InvalidOperationException($"Unsupported logbook page '{page}'."),
            };

            return new LogbookReadResult(data, notes);
        });
    }

    private static RumorLogbookPageSnapshot ReadRumorsAndNotes(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        WorkspaceTextCatalog catalog,
        ulong handle,
        int intelligence
    )
    {
        var useDumbText = intelligence <= LowIntelligence;
        using var remoteBuffer = new RemoteAllocation(memory, MaxRumorLogbookEntries * RumorLogbookEntrySize);
        memory.WriteBytes(remoteBuffer.Address, new byte[MaxRumorLogbookEntries * RumorLogbookEntrySize]);
        var read = NativeInvoker
            .Invoke(
                dispatcher,
                memory,
                "rumor_get_logbook_data",
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), remoteBuffer.Address32],
                ReadTimeout
            )
            .Snapshot;
        var count = Math.Clamp(read.Int32Value, 0, MaxRumorLogbookEntries);
        var bytes = memory.ReadBytes(remoteBuffer.Address, count * RumorLogbookEntrySize);
        var entries = Enumerable
            .Range(0, count)
            .Select(index =>
            {
                var span = bytes.AsSpan(index * RumorLogbookEntrySize, RumorLogbookEntrySize);
                var rumorId = BinaryPrimitives.ReadInt32LittleEndian(span[..sizeof(int)]);
                var normalText = NullIfWhiteSpace(catalog.ResolveRumorText(rumorId));
                var dumbText = NullIfWhiteSpace(catalog.ResolveRumorText(rumorId, dumb: true));
                return new RumorLogbookEntrySnapshot(
                    rumorId,
                    ReadDateTime(span, RumorDateTimeOffset),
                    Quelled: span[RumorQuelledOffset] != 0,
                    useDumbText && !string.IsNullOrWhiteSpace(dumbText) ? dumbText : normalText,
                    normalText,
                    dumbText
                );
            })
            .ToArray();

        return new RumorLogbookPageSnapshot(intelligence, useDumbText, entries, read);
    }

    private static QuestLogbookPageSnapshot ReadQuests(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        WorkspaceTextCatalog catalog,
        ulong handle,
        int intelligence
    )
    {
        var useDumbText = intelligence <= LowIntelligence;
        using var remoteBuffer = new RemoteAllocation(memory, MaxQuestLogbookEntries * QuestLogbookEntrySize);
        memory.WriteBytes(remoteBuffer.Address, new byte[MaxQuestLogbookEntries * QuestLogbookEntrySize]);
        var read = NativeInvoker
            .Invoke(
                dispatcher,
                memory,
                "quest_get_logbook_data",
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), remoteBuffer.Address32],
                ReadTimeout
            )
            .Snapshot;
        var count = Math.Clamp(read.Int32Value, 0, MaxQuestLogbookEntries);
        var bytes = memory.ReadBytes(remoteBuffer.Address, count * QuestLogbookEntrySize);
        var entries = Enumerable
            .Range(0, count)
            .Select(index =>
            {
                var span = bytes.AsSpan(index * QuestLogbookEntrySize, QuestLogbookEntrySize);
                var questId = BinaryPrimitives.ReadInt32LittleEndian(span[..sizeof(int)]);
                var state = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(QuestStateOffset, sizeof(int)));
                var quest = catalog.ResolveQuest(questId);
                return new QuestLogbookEntrySnapshot(
                    questId,
                    ReadDateTime(span, QuestDateTimeOffset),
                    state,
                    RuntimeWatchValueCatalog.QuestPcStateName(state),
                    quest.SummaryLabel,
                    useDumbText && !string.IsNullOrWhiteSpace(quest.DumbDescription)
                        ? quest.DumbDescription
                        : quest.Description,
                    quest.Description,
                    quest.DumbDescription
                );
            })
            .ToArray();

        return new QuestLogbookPageSnapshot(intelligence, useDumbText, entries, read);
    }

    private static ReputationLogbookPageSnapshot ReadReputations(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        WorkspaceTextCatalog catalog,
        ulong handle
    )
    {
        using var remoteBuffer = new RemoteAllocation(memory, MaxTimedLogbookEntries * TimedLogbookEntrySize);
        memory.WriteBytes(remoteBuffer.Address, new byte[MaxTimedLogbookEntries * TimedLogbookEntrySize]);
        var read = NativeInvoker
            .Invoke(
                dispatcher,
                memory,
                "reputation_get_logbook_data",
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), remoteBuffer.Address32],
                ReadTimeout
            )
            .Snapshot;
        var count = Math.Clamp(read.Int32Value, 0, MaxTimedLogbookEntries);
        var bytes = memory.ReadBytes(remoteBuffer.Address, count * TimedLogbookEntrySize);
        var entries = Enumerable
            .Range(0, count)
            .Select(index =>
            {
                var span = bytes.AsSpan(index * TimedLogbookEntrySize, TimedLogbookEntrySize);
                var reputationId = BinaryPrimitives.ReadInt32LittleEndian(span[..sizeof(int)]);
                var name = FirstNonEmpty(catalog.ResolveReputationName(reputationId), $"Reputation {reputationId}")!;
                return new ReputationLogbookEntrySnapshot(
                    reputationId,
                    ReadDateTime(span, TimedLogbookDateTimeOffset),
                    name
                );
            })
            .ToArray();

        return new ReputationLogbookPageSnapshot(entries, read);
    }

    private static BlessingCurseLogbookPageSnapshot ReadBlessingsAndCurses(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        WorkspaceTextCatalog catalog,
        ulong handle
    )
    {
        var blessings = ReadTimedIdEntries(dispatcher, memory, handle, "bless_get_logbook_data");
        var curses = ReadTimedIdEntries(dispatcher, memory, handle, "curse_get_logbook_data");
        List<BlessingCurseLogbookEntrySnapshot> entries =
        [
            .. blessings.Entries.Select(entry => new BlessingCurseLogbookEntrySnapshot(
                "Blessing",
                entry.Id,
                entry.DateTime,
                FirstNonEmpty(catalog.ResolveBlessingName(entry.Id), $"Blessing {entry.Id}")!
            )),
            .. curses.Entries.Select(entry => new BlessingCurseLogbookEntrySnapshot(
                "Curse",
                entry.Id,
                entry.DateTime,
                FirstNonEmpty(catalog.ResolveCurseName(entry.Id), $"Curse {entry.Id}")!
            )),
        ];
        entries.Sort(static (left, right) => left.DateTime.SortKey.CompareTo(right.DateTime.SortKey));

        return new BlessingCurseLogbookPageSnapshot(entries, [blessings.NativeRead, curses.NativeRead]);
    }

    private static KillsAndInjuriesLogbookPageSnapshot ReadKillsAndInjuries(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        WorkspaceTextCatalog catalog,
        ulong handle
    )
    {
        using var remoteBuffer = new RemoteAllocation(memory, KillStatCount * sizeof(int));
        memory.WriteBytes(remoteBuffer.Address, new byte[KillStatCount * sizeof(int)]);
        var read = NativeInvoker
            .Invoke(
                dispatcher,
                memory,
                "logbook_get_kills",
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), remoteBuffer.Address32],
                ReadTimeout
            )
            .Snapshot;
        var bytes = memory.ReadBytes(remoteBuffer.Address, KillStatCount * sizeof(int));
        var values = Enumerable
            .Range(0, KillStatCount)
            .Select(index => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(index * sizeof(int), sizeof(int))))
            .ToArray();

        var summary = new[]
        {
            new KillLogbookSummaryEntrySnapshot("total_kills", "Total Kills", 0, null, values[LbkTotalKills]),
            new KillLogbookSummaryEntrySnapshot(
                "most_powerful",
                "Most Powerful",
                values[LbkMostPowerfulName],
                ResolveDescriptionBestEffort(catalog, values[LbkMostPowerfulName]),
                values[LbkMostPowerfulLevel]
            ),
            new KillLogbookSummaryEntrySnapshot(
                "least_powerful",
                "Least Powerful",
                values[LbkLeastPowerfulName],
                ResolveDescriptionBestEffort(catalog, values[LbkLeastPowerfulName]),
                values[LbkLeastPowerfulLevel]
            ),
            new KillLogbookSummaryEntrySnapshot(
                "most_good",
                "Most Good",
                values[LbkMostGoodName],
                ResolveDescriptionBestEffort(catalog, values[LbkMostGoodName]),
                values[LbkMostGoodValue]
            ),
            new KillLogbookSummaryEntrySnapshot(
                "most_evil",
                "Most Evil",
                values[LbkMostEvilName],
                ResolveDescriptionBestEffort(catalog, values[LbkMostEvilName]),
                values[LbkMostEvilValue]
            ),
            new KillLogbookSummaryEntrySnapshot(
                "most_magical",
                "Most Magical",
                values[LbkMostMagicalName],
                ResolveDescriptionBestEffort(catalog, values[LbkMostMagicalName]),
                values[LbkMostMagicalValue]
            ),
            new KillLogbookSummaryEntrySnapshot(
                "most_tech",
                "Most Tech",
                values[LbkMostTechName],
                ResolveDescriptionBestEffort(catalog, values[LbkMostTechName]),
                values[LbkMostTechValue]
            ),
        };

        List<InjuryLogbookEntrySnapshot> injuries = [];
        for (var slot = FirstInjuryRecordIndex; slot < MaxLogbookFieldIndex; slot += 2)
        {
            var descriptionId = ReadArrayInt32Value(dispatcher, memory, handle, s_logbookFieldId, slot);
            if (descriptionId == 0)
                break;

            var injuryType = ReadArrayInt32Value(dispatcher, memory, handle, s_logbookFieldId, slot + 1);
            var injuryTypeName = InjuryTypeName(injuryType);
            var sourceName = ResolveDescriptionBestEffort(catalog, descriptionId);
            injuries.Add(
                new InjuryLogbookEntrySnapshot(
                    slot,
                    descriptionId,
                    sourceName,
                    injuryType,
                    injuryTypeName,
                    Active: false,
                    StateText: "Healed",
                    SummaryText: $"{injuryTypeName} by {sourceName}"
                )
            );
        }

        var critterFlags = ReadObjectFieldValue(dispatcher, memory, handle, s_critterFlagsFieldId);
        MarkLatestActiveInjury(injuries, LbiBlinded, (critterFlags & OcfBlinded) != 0);
        MarkLatestActiveInjury(injuries, LbiCrippledLeg, (critterFlags & OcfCrippledLegsBoth) != 0);
        MarkLatestActiveInjury(
            injuries,
            LbiCrippledArm,
            (critterFlags & (OcfCrippledArmsOne | OcfCrippledArmsBoth)) != 0
        );
        var scarringCount = ReadInt32Value(
            dispatcher,
            memory,
            "effect_count_effects_of_type",
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), EffectScarring]
        );
        MarkLatestActiveInjury(injuries, LbiScarred, scarringCount > 0);

        return new KillsAndInjuriesLogbookPageSnapshot(summary, injuries, read);
    }

    private static BackgroundLogbookPageSnapshot ReadBackground(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        WorkspaceTextCatalog catalog,
        ulong handle
    )
    {
        var backgroundRead = NativeInvoker
            .Invoke(
                dispatcher,
                memory,
                "background_get",
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
                ReadTimeout
            )
            .Snapshot;
        var backgroundTextRead = NativeInvoker
            .Invoke(
                dispatcher,
                memory,
                "background_text_get",
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle)],
                ReadTimeout
            )
            .Snapshot;
        var background = catalog.ResolveBackground(backgroundTextRead.Int32Value);
        var nativeName = NativeInvoker.ReadAsciiPointerResult(
            dispatcher,
            memory,
            "background_description_get_name",
            unchecked((uint)backgroundTextRead.Int32Value),
            ReadTimeout
        );
        var nativeBody = NativeInvoker.ReadAsciiPointerResult(
            dispatcher,
            memory,
            "background_description_get_body",
            unchecked((uint)backgroundTextRead.Int32Value),
            ReadTimeout
        );

        return new BackgroundLogbookPageSnapshot(
            backgroundRead.Int32Value,
            backgroundTextRead.Int32Value,
            FirstNonEmpty(nativeName, background.Name, background.SummaryLabel),
            FirstNonEmpty(nativeBody, background.Body),
            NullIfWhiteSpace(background.Name),
            NullIfWhiteSpace(background.Body),
            backgroundRead,
            backgroundTextRead
        );
    }

    private static KeyringLogbookPageSnapshot ReadKeyring(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        WorkspaceTextCatalog catalog,
        ulong handle
    )
    {
        var keyIds = ReadKeyIds(dispatcher, memory, handle);
        var entries = keyIds
            .Select(
                (keyId, index) =>
                    new KeyringLogbookEntrySnapshot(
                        index,
                        keyId,
                        FirstNonEmpty(catalog.ResolveKeyName(keyId), $"Key {keyId}")!
                    )
            )
            .ToArray();
        return new KeyringLogbookPageSnapshot(entries);
    }

    private static TimedIdReadResult ReadTimedIdEntries(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        ulong handle,
        string functionKey
    )
    {
        using var remoteBuffer = new RemoteAllocation(memory, MaxTimedLogbookEntries * TimedLogbookEntrySize);
        memory.WriteBytes(remoteBuffer.Address, new byte[MaxTimedLogbookEntries * TimedLogbookEntrySize]);
        var read = NativeInvoker
            .Invoke(
                dispatcher,
                memory,
                functionKey,
                [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), remoteBuffer.Address32],
                ReadTimeout
            )
            .Snapshot;
        var count = Math.Clamp(read.Int32Value, 0, MaxTimedLogbookEntries);
        var bytes = memory.ReadBytes(remoteBuffer.Address, count * TimedLogbookEntrySize);
        var entries = Enumerable
            .Range(0, count)
            .Select(index =>
            {
                var span = bytes.AsSpan(index * TimedLogbookEntrySize, TimedLogbookEntrySize);
                return new TimedIdEntry(
                    BinaryPrimitives.ReadInt32LittleEndian(span[..sizeof(int)]),
                    ReadDateTime(span, TimedLogbookDateTimeOffset)
                );
            })
            .ToArray();

        return new TimedIdReadResult(read, entries);
    }

    private static int[] ReadKeyIds(RuntimeCallDispatcher dispatcher, ProcessMemory memory, ulong handle)
    {
        var countRead = ReadInt32Value(
            dispatcher,
            memory,
            "item_get_keys",
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), 0]
        );
        var count = Math.Clamp(countRead, 0, MaxKeyRingEntries);
        if (count == 0)
            return [];

        using var remoteBuffer = new RemoteAllocation(memory, count * sizeof(int));
        memory.WriteBytes(remoteBuffer.Address, new byte[count * sizeof(int)]);
        _ = ReadInt32Value(
            dispatcher,
            memory,
            "item_get_keys",
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), remoteBuffer.Address32]
        );
        var bytes = memory.ReadBytes(remoteBuffer.Address, count * sizeof(int));
        return Enumerable
            .Range(0, count)
            .Select(index => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(index * sizeof(int), sizeof(int))))
            .ToArray();
    }

    private static int ReadStatValue(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        ulong handle,
        int statId
    ) =>
        ReadInt32Value(
            dispatcher,
            memory,
            "stat_base_get",
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)statId)]
        );

    private static int ReadObjectFieldValue(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        ulong handle,
        int fieldId
    ) =>
        ReadInt32Value(
            dispatcher,
            memory,
            "obj_field_int32_get",
            [TargetResolver.ToLow32(handle), TargetResolver.ToHigh32(handle), unchecked((uint)fieldId)]
        );

    private static int ReadArrayInt32Value(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        ulong handle,
        int fieldId,
        int index
    ) =>
        ReadInt32Value(
            dispatcher,
            memory,
            "obj_array_field_int32_get",
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)fieldId),
                unchecked((uint)index),
            ]
        );

    private static int ReadInt32Value(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        string functionKey,
        IReadOnlyList<uint> stackArguments
    ) => NativeInvoker.Invoke(dispatcher, memory, functionKey, stackArguments, ReadTimeout).Snapshot.Int32Value;

    private static GameDateTimeSnapshot ReadDateTime(ReadOnlySpan<byte> span, int offset) =>
        new(
            BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset, sizeof(uint))),
            BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + sizeof(uint), sizeof(uint)))
        );

    private static string ResolveDescriptionBestEffort(WorkspaceTextCatalog catalog, int descriptionId)
    {
        var description = catalog.ResolveDescription(descriptionId);
        return string.IsNullOrWhiteSpace(description) ? $"Description {descriptionId}" : description;
    }

    private static string InjuryTypeName(int injuryType) =>
        injuryType switch
        {
            LbiBlinded => "Blinded",
            LbiCrippledArm => "Crippled arm",
            LbiCrippledLeg => "Crippled leg",
            LbiScarred => "Scarred",
            _ => $"Injury {injuryType}",
        };

    private static void MarkLatestActiveInjury(List<InjuryLogbookEntrySnapshot> injuries, int injuryType, bool active)
    {
        if (!active)
            return;

        for (var index = injuries.Count - 1; index >= 0; index--)
        {
            if (injuries[index].InjuryType != injuryType)
                continue;

            injuries[index] = injuries[index] with { Active = true, StateText = "Active" };
            break;
        }
    }

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(static candidate => !string.IsNullOrWhiteSpace(candidate));

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static int ResolveFieldId(string rawName)
    {
        if (ObjectFieldCatalog.TryGetFieldId(rawName, out var fieldId))
            return fieldId;

        throw new InvalidOperationException($"Unable to resolve runtime object field '{rawName}'.");
    }

    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(1);
    private const int IntelligenceStatId = 4;
    private const int LowIntelligence = 4;
    private const int MaxRumorLogbookEntries = 2000;
    private const int MaxQuestLogbookEntries = 2000;
    private const int MaxTimedLogbookEntries = 2000;
    private const int MaxKeyRingEntries = 512;
    private const int RumorLogbookEntrySize = 24;
    private const int QuestLogbookEntrySize = 24;
    private const int TimedLogbookEntrySize = 16;
    private const int RumorDateTimeOffset = 4;
    private const int RumorQuelledOffset = 16;
    private const int QuestDateTimeOffset = 8;
    private const int QuestStateOffset = 16;
    private const int TimedLogbookDateTimeOffset = 8;
    private const int KillStatCount = 13;
    private const int FirstInjuryRecordIndex = 64;
    private const int MaxLogbookFieldIndex = 1024;
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
    private const int LbiBlinded = 0;
    private const int LbiCrippledArm = 1;
    private const int LbiCrippledLeg = 2;
    private const int LbiScarred = 3;
    private const int OcfBlinded = 0x00000080;
    private const int OcfCrippledArmsOne = 0x00000100;
    private const int OcfCrippledArmsBoth = 0x00000200;
    private const int OcfCrippledLegsBoth = 0x00000400;
    private const uint EffectScarring = 50;
    private static readonly int s_logbookFieldId = ResolveFieldId("OBJ_F_PC_LOGBOOK_EGO_IDX");
    private static readonly int s_critterFlagsFieldId = ResolveFieldId("OBJ_F_CRITTER_FLAGS");

    private readonly record struct TimedIdEntry(int Id, GameDateTimeSnapshot DateTime);

    private readonly record struct TimedIdReadResult(
        NativeReadSnapshot NativeRead,
        IReadOnlyList<TimedIdEntry> Entries
    );
}
