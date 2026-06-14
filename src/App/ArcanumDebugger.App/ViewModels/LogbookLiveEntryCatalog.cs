using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.ViewModels;

public static class LogbookLiveEntryCatalog
{
    public static IReadOnlyList<DebuggerLogbookEditableEntry> BuildEntries(LogbookSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        List<DebuggerLogbookEditableEntry> entries = [];
        var payload = snapshot.Data;
        if (payload.Quests is { } quests)
            entries.AddRange(quests.Entries.SelectMany(CreateQuestEntries));

        if (payload.RumorsAndNotes is { } rumors)
            entries.AddRange(rumors.Entries.Where(static entry => !entry.Quelled).Select(CreateRumorEntry));

        if (payload.Reputations is { } reputations)
            entries.AddRange(reputations.Entries.Select(CreateReputationEntry));

        if (payload.BlessingsAndCurses is { } blessingsAndCurses)
            entries.AddRange(blessingsAndCurses.Entries.Select(CreateBlessingOrCurseEntry));

        if (payload.KillsAndInjuries is { } killsAndInjuries)
        {
            entries.AddRange(CreateKillSummaryEntries(killsAndInjuries));
            entries.Add(CreateKillEntry(killsAndInjuries));
            entries.AddRange(
                killsAndInjuries.Injuries.Where(static entry => entry.Active).Select(CreateActiveInjuryEntry)
            );
            entries.AddRange(
                killsAndInjuries.Injuries.Where(static entry => !entry.Active).Select(CreateHealedInjuryRemovalEntry)
            );
        }

        if (payload.Background is { BackgroundId: > 0 } background)
        {
            entries.Add(CreateBackgroundEntry(background));
            entries.Add(CreateClearBackgroundEntry(background));
        }

        if (payload.KeyringContents is { } keyring)
            entries.AddRange(keyring.Entries.Select(CreateKeyEntry));

        return
        [
            .. entries
                .OrderBy(static entry => entry.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.EntryKey, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static IReadOnlyList<DebuggerLogbookEditableEntry> CreateQuestEntries(QuestLogbookEntrySnapshot entry)
    {
        var displayName = string.IsNullOrWhiteSpace(entry.Label) ? $"Quest {entry.QuestId}" : entry.Label;
        var description = FirstNonEmpty(entry.Description, entry.NormalDescription, entry.DumbDescription);
        var detailText = description is null
            ? $"Id {entry.QuestId.ToString(CultureInfo.InvariantCulture)} · {FormatLogbookDate(entry.DateTime)}"
            : $"Id {entry.QuestId.ToString(CultureInfo.InvariantCulture)} · {FormatLogbookDate(entry.DateTime)} · {description}";
        return
        [
            new DebuggerLogbookEditableEntry(
                $"quest:{entry.QuestId.ToString(CultureInfo.InvariantCulture)}",
                "Quest",
                displayName,
                entry.StateName,
                detailText,
                "Prefills Quest State with the current live quest record so you can advance, rewind, or botch it from the editor below.",
                "quest",
                LogbookMutationKind.SetQuestState,
                entry.QuestId.ToString(CultureInfo.InvariantCulture),
                LogbookQuestStateTokenCatalog.CreatePcMutationToken(entry.State),
                string.Empty
            ),
            new DebuggerLogbookEditableEntry(
                $"quest-global:{entry.QuestId.ToString(CultureInfo.InvariantCulture)}",
                "Quest",
                $"{displayName} (Global)",
                "Inspect shared global state",
                $"{detailText} · current PC state {entry.StateName}",
                "Prefills Quest Global State for this quest, then lets the live inspector read the current shared world-state value before you apply a change.",
                "quest",
                LogbookMutationKind.SetQuestGlobalState,
                entry.QuestId.ToString(CultureInfo.InvariantCulture),
                string.Empty,
                string.Empty
            ),
        ];
    }

    private static DebuggerLogbookEditableEntry CreateRumorEntry(RumorLogbookEntrySnapshot entry)
    {
        var text = FirstNonEmpty(entry.Text, entry.NormalText, entry.DumbText) ?? $"Rumor {entry.RumorId}";
        return new DebuggerLogbookEditableEntry(
            $"rumor:{entry.RumorId.ToString(CultureInfo.InvariantCulture)}",
            "Rumor",
            text,
            "Active",
            $"Id {entry.RumorId.ToString(CultureInfo.InvariantCulture)} · {FormatLogbookDate(entry.DateTime)}",
            "Prefills Quell Rumor for this currently active rumor.",
            "rumor",
            LogbookMutationKind.QuellRumor,
            entry.RumorId.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty
        );
    }

    private static DebuggerLogbookEditableEntry CreateReputationEntry(ReputationLogbookEntrySnapshot entry) =>
        new(
            $"reputation:{entry.ReputationId.ToString(CultureInfo.InvariantCulture)}",
            "Reputation",
            entry.Name,
            "Present",
            $"Id {entry.ReputationId.ToString(CultureInfo.InvariantCulture)} · {FormatLogbookDate(entry.DateTime)}",
            "Prefills Remove Reputation for this currently present reputation entry.",
            "reputation",
            LogbookMutationKind.RemoveReputation,
            entry.ReputationId.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty
        );

    private static DebuggerLogbookEditableEntry CreateBlessingOrCurseEntry(BlessingCurseLogbookEntrySnapshot entry)
    {
        var isBlessing = entry.Kind.Equals("blessing", StringComparison.OrdinalIgnoreCase);
        return new DebuggerLogbookEditableEntry(
            $"{entry.Kind}:{entry.Id.ToString(CultureInfo.InvariantCulture)}",
            isBlessing ? "Blessing" : "Curse",
            entry.Name,
            isBlessing ? "Active blessing" : "Active curse",
            $"Id {entry.Id.ToString(CultureInfo.InvariantCulture)} · {FormatLogbookDate(entry.DateTime)}",
            isBlessing
                ? "Prefills Remove Blessing for this currently active blessing."
                : "Prefills Remove Curse for this currently active curse.",
            isBlessing ? "blessing" : "curse",
            isBlessing ? LogbookMutationKind.RemoveBlessing : LogbookMutationKind.RemoveCurse,
            entry.Id.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty
        );
    }

    private static DebuggerLogbookEditableEntry CreateActiveInjuryEntry(InjuryLogbookEntrySnapshot entry) =>
        new(
            $"injury:{entry.DescriptionId.ToString(CultureInfo.InvariantCulture)}:{entry.InjuryType.ToString(CultureInfo.InvariantCulture)}:{entry.SlotIndex.ToString(CultureInfo.InvariantCulture)}",
            "Injury",
            entry.SourceName,
            entry.InjuryTypeName,
            $"{entry.StateText} · description {entry.DescriptionId.ToString(CultureInfo.InvariantCulture)} · slot {entry.SlotIndex.ToString(CultureInfo.InvariantCulture)}",
            "Prefills Add Injury History with the same source and injury type so you can copy it onto another target.",
            "injury",
            LogbookMutationKind.AddInjury,
            entry.DescriptionId.ToString(CultureInfo.InvariantCulture),
            CreateInjuryTypeToken(entry.InjuryType),
            string.Empty
        );

    private static DebuggerLogbookEditableEntry CreateHealedInjuryRemovalEntry(InjuryLogbookEntrySnapshot entry) =>
        new(
            $"injury-remove:{entry.DescriptionId.ToString(CultureInfo.InvariantCulture)}:{entry.InjuryType.ToString(CultureInfo.InvariantCulture)}:{entry.SlotIndex.ToString(CultureInfo.InvariantCulture)}",
            "Injury",
            entry.SourceName,
            entry.InjuryTypeName,
            $"{entry.StateText} · description {entry.DescriptionId.ToString(CultureInfo.InvariantCulture)} · slot {entry.SlotIndex.ToString(CultureInfo.InvariantCulture)}",
            "Prefills Remove Injury History for this healed row. ArcNET keeps active injuries protected until the condition is actually healed.",
            "injury",
            LogbookMutationKind.RemoveInjury,
            entry.DescriptionId.ToString(CultureInfo.InvariantCulture),
            CreateInjuryTypeToken(entry.InjuryType),
            entry.SlotIndex.ToString(CultureInfo.InvariantCulture)
        );

    private static IReadOnlyList<DebuggerLogbookEditableEntry> CreateKillSummaryEntries(
        KillsAndInjuriesLogbookPageSnapshot entry
    ) => [.. entry.Summary.Select(CreateKillSummaryEntry).OfType<DebuggerLogbookEditableEntry>()];

    private static DebuggerLogbookEditableEntry? CreateKillSummaryEntry(KillLogbookSummaryEntrySnapshot entry)
    {
        if (!KillLogbookSummaryCatalog.TryGetDefinition(entry.Key, out var definition))
            return null;

        if (!definition.RequiresDescription)
        {
            return new DebuggerLogbookEditableEntry(
                $"kill-summary:{entry.Key}",
                "Kill Summary",
                definition.SummaryLabel,
                $"{entry.Value.ToString(CultureInfo.InvariantCulture)} total kills",
                "Prefills Set Total Kills with the current live counter from the loaded kill ledger.",
                "Prefills Set Total Kills with the current live counter so you can correct or clone it onto another player or companion.",
                string.Empty,
                definition.MutationKind,
                string.Empty,
                entry.Value.ToString(CultureInfo.InvariantCulture),
                string.Empty
            );
        }

        var subjectName =
            !string.IsNullOrWhiteSpace(entry.Name) ? entry.Name
            : entry.DescriptionId > 0 ? $"Description {entry.DescriptionId.ToString(CultureInfo.InvariantCulture)}"
            : "No description";
        var displayName = $"{definition.SummaryLabel}: {subjectName}";
        var detailText =
            entry.DescriptionId > 0
                ? $"Description {entry.DescriptionId.ToString(CultureInfo.InvariantCulture)} · {definition.ValueLabel} {entry.Value.ToString(CultureInfo.InvariantCulture)}"
                : $"No description selected · {definition.ValueLabel} {entry.Value.ToString(CultureInfo.InvariantCulture)}";
        return new DebuggerLogbookEditableEntry(
            $"kill-summary:{entry.Key}:{entry.DescriptionId.ToString(CultureInfo.InvariantCulture)}",
            "Kill Summary",
            displayName,
            $"{definition.ValueLabel} {entry.Value.ToString(CultureInfo.InvariantCulture)}",
            detailText,
            $"Prefills {definition.OperationLabel} with the current live kill-ledger row so you can replace or clone it from the editor below.",
            definition.CatalogCategoryToken,
            definition.MutationKind,
            entry.DescriptionId.ToString(CultureInfo.InvariantCulture),
            entry.Value.ToString(CultureInfo.InvariantCulture),
            string.Empty
        );
    }

    private static DebuggerLogbookEditableEntry CreateKillEntry(KillsAndInjuriesLogbookPageSnapshot entry)
    {
        var totalKills = entry
            .Summary.FirstOrDefault(summary => summary.Key.Equals("total_kills", StringComparison.OrdinalIgnoreCase))
            .Value;
        return new DebuggerLogbookEditableEntry(
            "kill:record",
            "Kill",
            "Record Kill",
            $"Total kills {totalKills.ToString(CultureInfo.InvariantCulture)}",
            $"{totalKills.ToString(CultureInfo.InvariantCulture)} total kills · {entry.Injuries.Count.ToString(CultureInfo.InvariantCulture)} injury histor{(entry.Injuries.Count == 1 ? "y" : "ies")}",
            "Prefills Record Kill from the current kills page so you can use one live victim handle from the roster without manually switching the editor operation.",
            string.Empty,
            LogbookMutationKind.AddKill,
            string.Empty,
            string.Empty,
            string.Empty
        );
    }

    private static DebuggerLogbookEditableEntry CreateBackgroundEntry(BackgroundLogbookPageSnapshot entry)
    {
        var displayName =
            entry.Name
            ?? entry.CatalogName
            ?? $"Background {entry.BackgroundId.ToString(CultureInfo.InvariantCulture)}";
        var detailText =
            $"Background {entry.BackgroundId.ToString(CultureInfo.InvariantCulture)} · text {entry.BackgroundTextId.ToString(CultureInfo.InvariantCulture)}";
        var body = FirstNonEmpty(entry.Body, entry.CatalogBody);
        if (!string.IsNullOrWhiteSpace(body))
            detailText = $"{detailText} · {body}";

        return new DebuggerLogbookEditableEntry(
            $"background:{entry.BackgroundId.ToString(CultureInfo.InvariantCulture)}",
            "Background",
            displayName,
            "Active background",
            detailText,
            "Prefills Set Background with the current background id and text id so you can reapply or swap it quickly. Clear Background stays available in the editor dropdown.",
            "background",
            LogbookMutationKind.SetBackground,
            entry.BackgroundId.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            entry.BackgroundTextId.ToString(CultureInfo.InvariantCulture)
        );
    }

    private static DebuggerLogbookEditableEntry CreateClearBackgroundEntry(BackgroundLogbookPageSnapshot entry)
    {
        var displayName =
            FirstNonEmpty(entry.Name, entry.CatalogName)
            ?? $"Background {entry.BackgroundId.ToString(CultureInfo.InvariantCulture)}";
        return new DebuggerLogbookEditableEntry(
            $"background-clear:{entry.BackgroundId.ToString(CultureInfo.InvariantCulture)}",
            "Background",
            $"Clear {displayName}",
            "Clear active background",
            $"Background {entry.BackgroundId.ToString(CultureInfo.InvariantCulture)} · text {entry.BackgroundTextId.ToString(CultureInfo.InvariantCulture)}",
            "Prefills Clear Background so you can remove the current background from this player or companion without manually switching the editor mode.",
            "background",
            LogbookMutationKind.ClearBackground,
            string.Empty,
            string.Empty,
            string.Empty
        );
    }

    private static DebuggerLogbookEditableEntry CreateKeyEntry(KeyringLogbookEntrySnapshot entry) =>
        new(
            $"key:{entry.KeyId.ToString(CultureInfo.InvariantCulture)}",
            "Key",
            entry.Name,
            "Present in keyring",
            $"Key {entry.KeyId.ToString(CultureInfo.InvariantCulture)} · slot {entry.Index.ToString(CultureInfo.InvariantCulture)}",
            "Prefills Remove Key for this currently present keyring entry.",
            "key",
            LogbookMutationKind.RemoveKey,
            entry.KeyId.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty
        );

    private static string CreateInjuryTypeToken(int injuryType) =>
        injuryType switch
        {
            0 => "blinded",
            1 => "crippled-arm",
            2 => "crippled-leg",
            3 => "scarred",
            _ => injuryType.ToString(CultureInfo.InvariantCulture),
        };

    private static string FormatLogbookDate(GameDateTimeSnapshot dateTime) =>
        RuntimeWatchValueCatalog.FormatGameDateTime(((ulong)dateTime.Milliseconds << 32) | dateTime.Days);

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
}

public sealed record class DebuggerLogbookEditableEntry(
    string EntryKey,
    string Category,
    string DisplayName,
    string CurrentValueText,
    string DetailText,
    string SuggestedOperationText,
    string CatalogCategoryToken,
    LogbookMutationKind MutationKind,
    string EntryText,
    string ValueText,
    string AuxiliaryText
);
