using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.ViewModels;

public static class LogbookLiveStatusCatalog
{
    public static bool Supports(LogbookMutationKind kind) => true;

    public static DebuggerLogbookLiveStatus DescribeRead(
        LogbookMutationKind kind,
        string displayName,
        ReadSnapshot snapshot
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return kind switch
        {
            LogbookMutationKind.SetQuestState => DescribeQuestState(displayName, snapshot, isPcQuestState: true),
            LogbookMutationKind.SetQuestGlobalState => DescribeQuestState(displayName, snapshot, isPcQuestState: false),
            LogbookMutationKind.SetRumorKnown => DescribeRumorKnown(displayName, snapshot),
            LogbookMutationKind.QuellRumor => DescribeRumorQuelled(displayName, snapshot),
            _ => throw new InvalidOperationException($"Read-backed live status is not supported for '{kind}'."),
        };
    }

    public static DebuggerLogbookLiveStatus DescribeLogbook(
        LogbookMutationKind kind,
        string displayName,
        int entryId,
        int auxiliaryId,
        string? valueTokenText,
        LogbookSnapshot snapshot
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (KillLogbookSummaryCatalog.TryGetDefinition(kind, out var killSummaryDefinition))
        {
            return DescribeKillSummary(killSummaryDefinition, displayName, entryId, valueTokenText, snapshot);
        }

        return kind switch
        {
            LogbookMutationKind.AddKill => DescribeKill(displayName, snapshot),
            LogbookMutationKind.AddReputation or LogbookMutationKind.RemoveReputation => DescribeReputation(
                kind,
                displayName,
                entryId,
                snapshot
            ),
            LogbookMutationKind.AddBlessing or LogbookMutationKind.RemoveBlessing => DescribeBlessingOrCurse(
                kind,
                displayName,
                entryId,
                "blessing",
                snapshot
            ),
            LogbookMutationKind.AddCurse or LogbookMutationKind.RemoveCurse => DescribeBlessingOrCurse(
                kind,
                displayName,
                entryId,
                "curse",
                snapshot
            ),
            LogbookMutationKind.AddKey or LogbookMutationKind.RemoveKey => DescribeKey(
                kind,
                displayName,
                entryId,
                snapshot
            ),
            LogbookMutationKind.AddInjury => DescribeInjury(displayName, entryId, valueTokenText, snapshot),
            LogbookMutationKind.RemoveInjury => DescribeInjuryRemoval(
                displayName,
                entryId,
                auxiliaryId,
                valueTokenText,
                snapshot
            ),
            LogbookMutationKind.SetBackground or LogbookMutationKind.ClearBackground => DescribeBackground(
                kind,
                displayName,
                entryId,
                auxiliaryId,
                snapshot
            ),
            _ => throw new InvalidOperationException($"Logbook-page live status is not supported for '{kind}'."),
        };
    }

    private static DebuggerLogbookLiveStatus DescribeKillSummary(
        KillLogbookSummaryDefinition definition,
        string displayName,
        int entryId,
        string? valueTokenText,
        LogbookSnapshot snapshot
    )
    {
        var targetText = ResolveTargetText(snapshot.TargetText);
        var currentEntry = snapshot.Data.KillsAndInjuries?.Summary.FirstOrDefault(entry =>
            entry.Key.Equals(definition.Key, StringComparison.OrdinalIgnoreCase)
        );
        if (currentEntry is not { } summaryEntry)
        {
            return new DebuggerLogbookLiveStatus(
                "Kill ledger unavailable",
                $"The current kill ledger summary row for {definition.SummaryLabel} is not available on {targetText}. Reload the kills page before editing it.",
                null
            );
        }

        if (!TryParseNonNegativeInt32(valueTokenText, out var requestedValue))
        {
            return new DebuggerLogbookLiveStatus(
                $"{definition.SummaryLabel} value required",
                definition.RequiresDescription
                    ? $"{definition.SummaryLabel} currently records {FormatKillSummarySubject(summaryEntry)} with {FormatKillSummaryMetric(definition, summaryEntry.Value)} on {targetText}. Enter one numeric {definition.ValueLabel.ToLowerInvariant()} to compare or replace it."
                    : $"{targetText} currently shows {summaryEntry.Value.ToString(CultureInfo.InvariantCulture)} total kills. Enter one numeric total-kill value to compare or replace it.",
                null
            );
        }

        if (!definition.RequiresDescription)
        {
            return new DebuggerLogbookLiveStatus(
                requestedValue == summaryEntry.Value ? "Selected total already active" : "Current total kills",
                requestedValue == summaryEntry.Value
                    ? $"{targetText} already shows {requestedValue.ToString(CultureInfo.InvariantCulture)} total kills."
                    : $"{targetText} currently shows {summaryEntry.Value.ToString(CultureInfo.InvariantCulture)} total kills. {definition.OperationLabel} will change it to {requestedValue.ToString(CultureInfo.InvariantCulture)}.",
                null
            );
        }

        var selectedSubject = FormatSelectedKillSummarySubject(displayName, entryId);
        var selectedMetric = FormatKillSummaryMetric(definition, requestedValue);
        var currentSubject = FormatKillSummarySubject(summaryEntry);
        var currentMetric = FormatKillSummaryMetric(definition, summaryEntry.Value);
        var alreadyMatches = summaryEntry.DescriptionId == entryId && summaryEntry.Value == requestedValue;
        return new DebuggerLogbookLiveStatus(
            alreadyMatches ? "Selected kill summary already active" : "Current kill summary",
            alreadyMatches
                    ? $"{definition.SummaryLabel} already records {selectedSubject} with {selectedMetric} on {targetText}."
                : summaryEntry.DescriptionId > 0
                    ? $"{definition.SummaryLabel} currently records {currentSubject} with {currentMetric} on {targetText}. {definition.OperationLabel} will replace it with {selectedSubject} and {selectedMetric}."
                : $"No {definition.SummaryLabel.ToLowerInvariant()} row is currently recorded on {targetText}. {definition.OperationLabel} will set it to {selectedSubject} with {selectedMetric}.",
            null
        );
    }

    private static DebuggerLogbookLiveStatus DescribeKill(string displayName, LogbookSnapshot snapshot)
    {
        var targetText = ResolveTargetText(snapshot.TargetText);
        var totalKills =
            snapshot
                .Data.KillsAndInjuries?.Summary.FirstOrDefault(entry =>
                    entry.Key.Equals("total_kills", StringComparison.OrdinalIgnoreCase)
                )
                .Value
            ?? 0;
        var injuryCount = snapshot.Data.KillsAndInjuries?.Injuries.Count ?? 0;
        var victimText = string.IsNullOrWhiteSpace(displayName) ? "the selected victim" : displayName;
        return new DebuggerLogbookLiveStatus(
            "Current kill ledger",
            $"{targetText} currently shows {totalKills.ToString(CultureInfo.InvariantCulture)} total kills and {injuryCount.ToString(CultureInfo.InvariantCulture)} injury history entr{(injuryCount == 1 ? "y" : "ies")}. Record Kill will attempt to add one kill from {victimText} and advance total kills to {(totalKills + 1).ToString(CultureInfo.InvariantCulture)}.",
            null
        );
    }

    private static DebuggerLogbookLiveStatus DescribeQuestState(
        string displayName,
        ReadSnapshot snapshot,
        bool isPcQuestState
    )
    {
        var rawState = TryReadInt32(snapshot, "raw_state");
        var stateText =
            TryReadValueText(snapshot, "quest_state")
            ?? RuntimeWatchValueCatalog.QuestStateName(RuntimeWatchValueCatalog.QuestBaseState(rawState));
        return new DebuggerLogbookLiveStatus(
            isPcQuestState ? "Current PC quest state" : "Current global quest state",
            isPcQuestState
                ? $"{displayName} is currently {stateText} on {ResolveTargetText(snapshot.TargetText)}. The selector below now mirrors the live runtime state."
                : $"{displayName} is currently {stateText} in shared global quest state. The selector below now mirrors the live runtime state.",
            isPcQuestState
                ? LogbookQuestStateTokenCatalog.CreatePcMutationToken(rawState)
                : LogbookQuestStateTokenCatalog.CreateGlobalMutationToken(rawState)
        );
    }

    private static DebuggerLogbookLiveStatus DescribeRumorKnown(string displayName, ReadSnapshot snapshot)
    {
        var known = TryReadInt32(snapshot, "raw_state") != 0;
        return new DebuggerLogbookLiveStatus(
            "Current rumor-known flag",
            known
                ? $"{displayName} is already known on {ResolveTargetText(snapshot.TargetText)}. Mark Rumor Known will likely no-op."
                : $"{displayName} is not yet known on {ResolveTargetText(snapshot.TargetText)}. Mark Rumor Known will add it to this journal.",
            null
        );
    }

    private static DebuggerLogbookLiveStatus DescribeRumorQuelled(string displayName, ReadSnapshot snapshot)
    {
        var quelled = TryReadInt32(snapshot, "raw_state") != 0;
        return new DebuggerLogbookLiveStatus(
            "Current rumor-quelled flag",
            quelled
                ? $"{displayName} is already quelled globally. Quell Rumor will likely no-op."
                : $"{displayName} is not yet quelled globally. Quell Rumor will mark it quelled for the whole world state.",
            null
        );
    }

    private static DebuggerLogbookLiveStatus DescribeReputation(
        LogbookMutationKind kind,
        string displayName,
        int entryId,
        LogbookSnapshot snapshot
    )
    {
        var present = snapshot.Data.Reputations?.Entries.Any(entry => entry.ReputationId == entryId) == true;
        return DescribePresence(
            kind,
            displayName,
            present,
            ResolveTargetText(snapshot.TargetText),
            "Current reputation status",
            presentNoun: "present",
            missingNoun: "not present"
        );
    }

    private static DebuggerLogbookLiveStatus DescribeBlessingOrCurse(
        LogbookMutationKind kind,
        string displayName,
        int entryId,
        string entryKind,
        LogbookSnapshot snapshot
    )
    {
        var present =
            snapshot.Data.BlessingsAndCurses?.Entries.Any(entry =>
                entry.Id == entryId && entry.Kind.Equals(entryKind, StringComparison.OrdinalIgnoreCase)
            ) == true;
        var label = entryKind.Equals("blessing", StringComparison.OrdinalIgnoreCase)
            ? "Current blessing status"
            : "Current curse status";
        return DescribePresence(
            kind,
            displayName,
            present,
            ResolveTargetText(snapshot.TargetText),
            label,
            presentNoun: $"already active {entryKind}",
            missingNoun: $"not an active {entryKind}"
        );
    }

    private static DebuggerLogbookLiveStatus DescribeKey(
        LogbookMutationKind kind,
        string displayName,
        int entryId,
        LogbookSnapshot snapshot
    )
    {
        var present = snapshot.Data.KeyringContents?.Entries.Any(entry => entry.KeyId == entryId) == true;
        return DescribePresence(
            kind,
            displayName,
            present,
            ResolveTargetText(snapshot.TargetText),
            "Current keyring status",
            presentNoun: "already in the keyring",
            missingNoun: "not in the keyring"
        );
    }

    private static DebuggerLogbookLiveStatus DescribeInjury(
        string displayName,
        int entryId,
        string? valueTokenText,
        LogbookSnapshot snapshot
    )
    {
        if (!TryParseInjuryTypeToken(valueTokenText, out var injuryType, out var injuryTypeName))
        {
            return new DebuggerLogbookLiveStatus(
                "Injury type required",
                "Choose an injury type to compare against the target's active injury history.",
                null
            );
        }

        var activeMatch = snapshot.Data.KillsAndInjuries?.Injuries.Any(entry =>
            entry.Active && entry.DescriptionId == entryId && entry.InjuryType == injuryType
        );
        return new DebuggerLogbookLiveStatus(
            "Current injury status",
            activeMatch == true
                ? $"An active {injuryTypeName.ToLowerInvariant()} injury from {displayName} is already present on {ResolveTargetText(snapshot.TargetText)}. Add Injury History will still append another history row."
                : $"No active {injuryTypeName.ToLowerInvariant()} injury from {displayName} is currently present on {ResolveTargetText(snapshot.TargetText)}.",
            null
        );
    }

    private static DebuggerLogbookLiveStatus DescribeInjuryRemoval(
        string displayName,
        int entryId,
        int slotIndex,
        string? valueTokenText,
        LogbookSnapshot snapshot
    )
    {
        if (!TryParseInjuryTypeToken(valueTokenText, out var injuryType, out var injuryTypeName))
        {
            return new DebuggerLogbookLiveStatus(
                "Injury type required",
                "Choose an injury type before comparing this history row against the current injury ledger.",
                null
            );
        }

        if (slotIndex <= 0)
        {
            return new DebuggerLogbookLiveStatus(
                "History slot required",
                "Use one live injury shortcut so ArcNET can target the exact healed history slot to remove.",
                null
            );
        }

        var injuryEntry = snapshot.Data.KillsAndInjuries?.Injuries.FirstOrDefault(entry =>
            entry.SlotIndex == slotIndex
        );
        if (injuryEntry is not { } matchedEntry)
        {
            return new DebuggerLogbookLiveStatus(
                "History row missing",
                $"No injury history row currently occupies slot {slotIndex.ToString(CultureInfo.InvariantCulture)} on {ResolveTargetText(snapshot.TargetText)}. Reload the journal page to rebuild the shortcut list.",
                null
            );
        }

        if (matchedEntry.DescriptionId != entryId || matchedEntry.InjuryType != injuryType)
        {
            return new DebuggerLogbookLiveStatus(
                "History row changed",
                $"Slot {slotIndex.ToString(CultureInfo.InvariantCulture)} now points at {matchedEntry.SourceName} / {matchedEntry.InjuryTypeName} on {ResolveTargetText(snapshot.TargetText)}. Reload the journal page before removing it.",
                null
            );
        }

        if (matchedEntry.Active)
        {
            return new DebuggerLogbookLiveStatus(
                "Active injury protected",
                $"The {injuryTypeName.ToLowerInvariant()} row from {displayName} is still active on {ResolveTargetText(snapshot.TargetText)}. Heal the condition first, then remove the healed history row.",
                null
            );
        }

        return new DebuggerLogbookLiveStatus(
            "Healed injury history row",
            $"{displayName} / {injuryTypeName} currently sits in healed slot {slotIndex.ToString(CultureInfo.InvariantCulture)} on {ResolveTargetText(snapshot.TargetText)}. Remove Injury History will delete it and compact later injury rows.",
            null
        );
    }

    private static DebuggerLogbookLiveStatus DescribeBackground(
        LogbookMutationKind kind,
        string displayName,
        int entryId,
        int auxiliaryId,
        LogbookSnapshot snapshot
    )
    {
        var targetText = ResolveTargetText(snapshot.TargetText);
        var background = snapshot.Data.Background;
        var hasBackground = background is { BackgroundId: > 0 } || background is { BackgroundTextId: > 0 };
        if (kind == LogbookMutationKind.ClearBackground)
        {
            return new DebuggerLogbookLiveStatus(
                "Current background",
                hasBackground
                    ? $"{FormatBackgroundLabel(background!)} is currently active on {targetText}. Clear Background will remove it."
                    : $"No background is currently active on {targetText}. Clear Background will likely no-op.",
                null
            );
        }

        if (entryId <= 0 || auxiliaryId <= 0)
        {
            return new DebuggerLogbookLiveStatus(
                "Background selection required",
                hasBackground
                    ? $"{FormatBackgroundLabel(background!)} is currently active on {targetText}. Pick one background row to compare or replace it."
                    : $"No background is currently active on {targetText}. Pick one background row to compare or apply it.",
                null
            );
        }

        var selectedLabel =
            $"{displayName} [{entryId.ToString(CultureInfo.InvariantCulture)}] / text {auxiliaryId.ToString(CultureInfo.InvariantCulture)}";
        if (!hasBackground)
        {
            return new DebuggerLogbookLiveStatus(
                "Current background",
                $"No background is currently active on {targetText}. Set Background will apply {selectedLabel}.",
                null
            );
        }

        var currentMatches = background!.BackgroundId == entryId && background.BackgroundTextId == auxiliaryId;
        return new DebuggerLogbookLiveStatus(
            currentMatches ? "Selected background already active" : "Current background",
            currentMatches
                ? $"{selectedLabel} is already active on {targetText}. Set Background will likely no-op."
                : $"{FormatBackgroundLabel(background)} is currently active on {targetText}. Set Background will replace it with {selectedLabel}.",
            null
        );
    }

    private static DebuggerLogbookLiveStatus DescribePresence(
        LogbookMutationKind kind,
        string displayName,
        bool present,
        string targetText,
        string statusText,
        string presentNoun,
        string missingNoun
    )
    {
        var isAdd =
            kind
            is LogbookMutationKind.AddReputation
                or LogbookMutationKind.AddBlessing
                or LogbookMutationKind.AddCurse
                or LogbookMutationKind.AddKey;
        return new DebuggerLogbookLiveStatus(
            statusText,
            present
                ? isAdd
                    ? $"{displayName} is {presentNoun} on {targetText}. This add operation will likely no-op."
                    : $"{displayName} is {presentNoun} on {targetText}. This remove operation can clear it."
                : isAdd
                    ? $"{displayName} is {missingNoun} on {targetText}. This add operation can apply it."
                    : $"{displayName} is {missingNoun} on {targetText}. This remove operation will likely no-op.",
            null
        );
    }

    private static bool TryParseInjuryTypeToken(string? valueTokenText, out int injuryType, out string injuryTypeName)
    {
        injuryTypeName = string.Empty;
        if (string.IsNullOrWhiteSpace(valueTokenText))
        {
            injuryType = 0;
            return false;
        }

        switch (Normalize(valueTokenText))
        {
            case "blinded":
                injuryType = 0;
                injuryTypeName = "Blinded";
                return true;
            case "crippledarm":
                injuryType = 1;
                injuryTypeName = "Crippled Arm";
                return true;
            case "crippledleg":
                injuryType = 2;
                injuryTypeName = "Crippled Leg";
                return true;
            case "scarred":
                injuryType = 3;
                injuryTypeName = "Scarred";
                return true;
            default:
                injuryType = 0;
                return false;
        }
    }

    private static string FormatBackgroundLabel(BackgroundLogbookPageSnapshot background)
    {
        var name =
            FirstNonEmpty(background.Name, background.CatalogName)
            ?? $"Background {background.BackgroundId.ToString(CultureInfo.InvariantCulture)}";
        return $"{name} [{background.BackgroundId.ToString(CultureInfo.InvariantCulture)}] / text {background.BackgroundTextId.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string ResolveTargetText(string? targetText) =>
        string.IsNullOrWhiteSpace(targetText) ? "the selected target" : targetText;

    private static bool TryParseNonNegativeInt32(string? valueText, out int value)
    {
        if (int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0)
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static string FormatSelectedKillSummarySubject(string displayName, int entryId)
    {
        if (entryId <= 0)
            return "no description";

        if (
            string.IsNullOrWhiteSpace(displayName)
            || displayName.Equals("No description", StringComparison.OrdinalIgnoreCase)
        )
        {
            return $"Description {entryId.ToString(CultureInfo.InvariantCulture)}";
        }

        return displayName.Contains($"[{entryId.ToString(CultureInfo.InvariantCulture)}]", StringComparison.Ordinal)
            ? displayName
            : $"{displayName} [{entryId.ToString(CultureInfo.InvariantCulture)}]";
    }

    private static string FormatKillSummarySubject(KillLogbookSummaryEntrySnapshot entry)
    {
        if (entry.DescriptionId <= 0)
            return "no description";

        var name = string.IsNullOrWhiteSpace(entry.Name)
            ? $"Description {entry.DescriptionId.ToString(CultureInfo.InvariantCulture)}"
            : entry.Name;
        return name.Contains(
            $"[{entry.DescriptionId.ToString(CultureInfo.InvariantCulture)}]",
            StringComparison.Ordinal
        )
            ? name
            : $"{name} [{entry.DescriptionId.ToString(CultureInfo.InvariantCulture)}]";
    }

    private static string FormatKillSummaryMetric(KillLogbookSummaryDefinition definition, int value) =>
        definition.MutationKind == LogbookMutationKind.SetTotalKills
            ? value.ToString(CultureInfo.InvariantCulture)
            : $"{definition.ValueLabel} {value.ToString(CultureInfo.InvariantCulture)}";

    private static int TryReadInt32(ReadSnapshot snapshot, string key)
    {
        var valueText = TryReadValueText(snapshot, key);
        return int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static string? TryReadValueText(ReadSnapshot snapshot, string key)
    {
        var value = snapshot.Values.FirstOrDefault(candidate => candidate.Key == key);
        return string.IsNullOrWhiteSpace(value.Key) ? null : value.ValueText;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

    private static string Normalize(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (!char.IsLetterOrDigit(ch))
                continue;

            buffer[count++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..count]);
    }
}

public sealed record class DebuggerLogbookLiveStatus(string StatusText, string SummaryText, string? PrefillValueToken);
