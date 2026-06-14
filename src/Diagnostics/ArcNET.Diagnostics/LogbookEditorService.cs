using System.Globalization;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed class LogbookEditorService(ILogbookEditorBackend backend)
{
    public async Task<LogbookEditorCatalogSnapshot> LoadCatalogAsync(LogbookEditorCatalogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var workspacePath = request.WorkspacePath;
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return CreateUnavailableCatalogSnapshot(
                "Logbook catalog unavailable",
                "The request does not expose a usable local workspace path, so ArcNET cannot load the local journal and source catalog."
            );
        }

        try
        {
            var entries = await backend.LoadCatalogAsync(workspacePath).ConfigureAwait(false);
            List<string> notes = [];
            if (request.Session.HasExited)
                notes.Add("The process has exited, but the local journal and source catalog remains browsable.");

            return new LogbookEditorCatalogSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Logbook catalog loaded",
                $"Loaded {entries.Count.ToString(CultureInfo.InvariantCulture)} journal and source entries from the local workspace catalog.",
                entries,
                notes
            );
        }
        catch (Exception ex)
        {
            return CreateUnavailableCatalogSnapshot(
                "Logbook catalog unavailable",
                $"Unable to load the local journal and source catalog ({ex.GetType().Name}: {ex.Message})."
            );
        }
    }

    public async Task<LogbookMutationSnapshot> WriteAsync(LogbookMutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutate(request.Session))
            return CreateUnavailableMutationSnapshot(
                request.Kind,
                "Logbook editor unavailable",
                CreateAvailabilitySummary(request.Session)
            );

        try
        {
            var catalogEntries = await TryLoadCatalogEntriesAsync(request.WorkspacePath).ConfigureAwait(false);
            return await Task.Run(() => ExecuteWrite(request, catalogEntries)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableMutationSnapshot(request.Kind, "Invalid logbook edit request", ex.Message);
        }
    }

    private LogbookMutationSnapshot ExecuteWrite(
        LogbookMutationRequest request,
        IReadOnlyList<LogbookCatalogEntrySnapshot> catalogEntries
    )
    {
        var timeout = ParseTimeout(request.TimeoutMillisecondsText);
        var target = TargetResolver.Resolve(backend, request.Session, request.TargetHandleToken, "logbook target");
        var victimTarget =
            request.Kind == LogbookMutationKind.AddKill ? ResolveKillVictim(request, backend) : (ResolvedTarget?)null;
        var categoryToken = CategoryToken(request.Kind);
        var entryId = RequiresEntry(request.Kind) ? ParseEntryId(request.EntryTokenText, request.Kind) : 0;
        var catalogEntry =
            entryId > 0
                ? catalogEntries.FirstOrDefault(entry =>
                    entry.CategoryToken.Equals(categoryToken, StringComparison.OrdinalIgnoreCase)
                    && entry.EntryId == entryId
                )
                : null;
        var execution = request.Kind switch
        {
            LogbookMutationKind.SetQuestState => backend.SetQuestState(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                ParseQuestPcState(request.ValueTokenText),
                timeout
            ),
            LogbookMutationKind.SetQuestGlobalState => backend.SetQuestGlobalState(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                entryId,
                ParseQuestGlobalState(request.ValueTokenText),
                timeout
            ),
            LogbookMutationKind.SetRumorKnown => backend.SetRumorKnown(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                timeout
            ),
            LogbookMutationKind.QuellRumor => backend.QuellRumor(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                entryId,
                timeout
            ),
            LogbookMutationKind.AddReputation => backend.AddReputation(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                timeout
            ),
            LogbookMutationKind.RemoveReputation => backend.RemoveReputation(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                timeout
            ),
            LogbookMutationKind.AddBlessing => backend.AddBlessing(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                timeout
            ),
            LogbookMutationKind.RemoveBlessing => backend.RemoveBlessing(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                timeout
            ),
            LogbookMutationKind.AddCurse => backend.AddCurse(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                timeout
            ),
            LogbookMutationKind.RemoveCurse => backend.RemoveCurse(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                timeout
            ),
            LogbookMutationKind.AddKey => backend.AddKey(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                timeout
            ),
            LogbookMutationKind.RemoveKey => backend.RemoveKey(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                timeout
            ),
            LogbookMutationKind.AddInjury => backend.AddInjury(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                ParseInjuryType(request.ValueTokenText),
                timeout
            ),
            LogbookMutationKind.RemoveInjury => backend.RemoveInjury(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                ParseInjuryType(request.ValueTokenText),
                ParseInjurySlotIndex(request.AuxiliaryTokenText),
                timeout
            ),
            LogbookMutationKind.AddKill => backend.AddKill(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                victimTarget!.Value.Handle,
                timeout
            ),
            LogbookMutationKind.SetTotalKills
            or LogbookMutationKind.SetMostPowerfulKill
            or LogbookMutationKind.SetLeastPowerfulKill
            or LogbookMutationKind.SetMostGoodKill
            or LogbookMutationKind.SetMostEvilKill
            or LogbookMutationKind.SetMostMagicalKill
            or LogbookMutationKind.SetMostTechKill => backend.SetKillSummary(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                request.Kind,
                ParseKillSummaryDescriptionId(request),
                ParseKillSummaryValue(request),
                timeout
            ),
            LogbookMutationKind.SetBackground => backend.SetBackground(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                entryId,
                ResolveBackgroundTextId(request, catalogEntry),
                timeout
            ),
            LogbookMutationKind.ClearBackground => backend.ClearBackground(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                timeout
            ),
            _ => throw new InvalidOperationException($"Unsupported logbook mutation '{request.Kind}'."),
        };

        var valueText = FormatValueText(request.Kind, request.ValueTokenText);
        var auxiliaryText = FormatAuxiliaryText(
            request.Kind,
            request.AuxiliaryTokenText,
            catalogEntry,
            victimTarget?.HandleText
        );
        var subjectText = FormatSubjectText(
            request.Kind,
            entryId,
            auxiliaryText,
            catalogEntry,
            victimTarget?.TargetText
        );
        IReadOnlyList<string> notes = victimTarget is { } resolvedVictim
            ? [.. target.Notes, .. resolvedVictim.Notes]
            : [.. target.Notes];
        return new LogbookMutationSnapshot(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            execution.NoMutation
                ? $"{OperationText(request.Kind)} unchanged"
                : $"{OperationText(request.Kind)} applied",
            CreateSummary(request.Kind, execution.NoMutation, target.TargetText, subjectText, valueText),
            request.Kind,
            OperationText(request.Kind),
            target.HandleText,
            target.TargetText,
            subjectText,
            valueText,
            auxiliaryText,
            $"{execution.DispatcherMode} · {execution.DispatcherSite}",
            execution.ExecutionDetailText,
            execution.ResultText,
            notes
        );
    }

    private static bool CanMutate(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.RuntimeProfile.SupportsCatalogRvas
        && session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions);

    private static string CreateAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live journal edits are unavailable until a new session is attached.";

        return "Live journal edits require a validated runtime profile with live function invocation support.";
    }

    private async Task<IReadOnlyList<LogbookCatalogEntrySnapshot>> TryLoadCatalogEntriesAsync(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            return [];

        try
        {
            return await backend.LoadCatalogAsync(workspacePath).ConfigureAwait(false);
        }
        catch
        {
            return [];
        }
    }

    private static int ResolveBackgroundTextId(
        LogbookMutationRequest request,
        LogbookCatalogEntrySnapshot? catalogEntry
    )
    {
        if (
            int.TryParse(
                request.AuxiliaryTokenText.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var textId
            )
            && textId > 0
        )
        {
            return textId;
        }

        if (catalogEntry is { AuxiliaryId: > 0 })
            return catalogEntry.AuxiliaryId;

        throw new InvalidOperationException(
            "Background set requires a background text id. Pick one background from the local journal and source catalog or enter the text id manually."
        );
    }

    private static int ParseKillSummaryDescriptionId(LogbookMutationRequest request)
    {
        if (
            !KillLogbookSummaryCatalog.TryGetDefinition(request.Kind, out var definition)
            || !definition.RequiresDescription
        )
            return 0;

        if (string.IsNullOrWhiteSpace(request.EntryTokenText))
        {
            throw new InvalidOperationException(
                $"{definition.OperationLabel} requires a description id. Pick one source description from the local journal catalog or enter 0 to clear the current name slot."
            );
        }

        if (
            int.TryParse(
                request.EntryTokenText.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var descriptionId
            )
            && descriptionId >= 0
        )
        {
            return descriptionId;
        }

        throw new InvalidOperationException(
            $"Description id '{request.EntryTokenText}' is not a valid non-negative 32-bit integer."
        );
    }

    private static int ParseKillSummaryValue(LogbookMutationRequest request)
    {
        if (!KillLogbookSummaryCatalog.TryGetDefinition(request.Kind, out var definition))
            throw new InvalidOperationException($"Unsupported kill-summary mutation '{request.Kind}'.");

        if (string.IsNullOrWhiteSpace(request.ValueTokenText))
        {
            throw new InvalidOperationException(
                $"{definition.OperationLabel} requires a numeric {definition.ValueLabel.ToLowerInvariant()}."
            );
        }

        if (
            int.TryParse(
                request.ValueTokenText.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value
            )
            && value >= 0
        )
        {
            return value;
        }

        throw new InvalidOperationException(
            $"{definition.ValueLabel} '{request.ValueTokenText}' is not a valid non-negative 32-bit integer."
        );
    }

    private static int ParseEntryId(string? value, LogbookMutationKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{OperationText(kind)} requires one entry id.");

        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Entry id '{value}' is not a valid signed 32-bit integer.");
    }

    private static int ParseQuestPcState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                "Quest state is required. Use unknown, mentioned, accepted, achieved, completed, other-completed, botched, or one explicit botched variant such as accepted-botched."
            );

        var normalized = Normalize(value);
        return normalized switch
        {
            "0" or "unknown" => 0,
            "1" or "mentioned" => 1,
            "2" or "accepted" => 2,
            "3" or "achieved" => 3,
            "4" or "completed" => 4,
            "5" or "othercompleted" or "othercomplete" or "completedelsewhere" => 5,
            "6" or "botched" => 6,
            "mentionedbotched" or "botchedmentioned" => RuntimeWatchValueCatalog.QuestBotchedModifier | 1,
            "acceptedbotched" or "botchedaccepted" => RuntimeWatchValueCatalog.QuestBotchedModifier | 2,
            "achievedbotched" or "botchedachieved" => RuntimeWatchValueCatalog.QuestBotchedModifier | 3,
            "completedbotched" or "botchedcompleted" => RuntimeWatchValueCatalog.QuestBotchedModifier | 4,
            "othercompletedbotched"
            or "botchedothercompleted"
            or "completedelsewherebotched"
            or "botchedcompletedelsewhere" => RuntimeWatchValueCatalog.QuestBotchedModifier | 5,
            _ => throw new InvalidOperationException(
                $"Unknown quest state '{value}'. Use unknown, mentioned, accepted, achieved, completed, other-completed, botched, or one explicit botched variant such as accepted-botched."
            ),
        };
    }

    private static int ParseQuestGlobalState(string? value)
    {
        var parsed = ParseQuestPcState(value);
        if (RuntimeWatchValueCatalog.QuestHasBotchedModifier(parsed))
        {
            throw new InvalidOperationException(
                "Global quest state does not support explicit botched-base variants. Use unknown, mentioned, accepted, achieved, completed, other-completed, or botched."
            );
        }

        return parsed;
    }

    private static TimeSpan ParseTimeout(string? timeoutText)
    {
        if (string.IsNullOrWhiteSpace(timeoutText))
            return TimeSpan.FromSeconds(1);

        if (
            int.TryParse(timeoutText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds)
            && milliseconds > 0
        )
        {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        throw new InvalidOperationException($"Timeout '{timeoutText}' is not a valid positive millisecond value.");
    }

    private static ResolvedTarget ResolveKillVictim(LogbookMutationRequest request, ILogbookEditorBackend backend)
    {
        if (string.IsNullOrWhiteSpace(request.AuxiliaryTokenText))
        {
            throw new InvalidOperationException(
                "Record Kill requires one live victim handle. Use Roster to copy a mobile handle or enter one manually."
            );
        }

        return TargetResolver.Resolve(backend, request.Session, request.AuxiliaryTokenText, "kill victim");
    }

    private static bool RequiresEntry(LogbookMutationKind kind) =>
        kind
            is not LogbookMutationKind.ClearBackground
                and not LogbookMutationKind.AddKill
                and not LogbookMutationKind.SetTotalKills;

    private static string CategoryToken(LogbookMutationKind kind) =>
        KillLogbookSummaryCatalog.TryGetDefinition(kind, out var killSummaryDefinition)
            ? killSummaryDefinition.CatalogCategoryToken
            : kind switch
            {
                LogbookMutationKind.SetQuestState or LogbookMutationKind.SetQuestGlobalState => "quest",
                LogbookMutationKind.SetRumorKnown or LogbookMutationKind.QuellRumor => "rumor",
                LogbookMutationKind.AddReputation or LogbookMutationKind.RemoveReputation => "reputation",
                LogbookMutationKind.AddBlessing or LogbookMutationKind.RemoveBlessing => "blessing",
                LogbookMutationKind.AddCurse or LogbookMutationKind.RemoveCurse => "curse",
                LogbookMutationKind.AddKey or LogbookMutationKind.RemoveKey => "key",
                LogbookMutationKind.AddInjury or LogbookMutationKind.RemoveInjury => "injury",
                LogbookMutationKind.AddKill => "kill",
                LogbookMutationKind.SetBackground or LogbookMutationKind.ClearBackground => "background",
                _ => string.Empty,
            };

    private static string OperationText(LogbookMutationKind kind) =>
        KillLogbookSummaryCatalog.TryGetDefinition(kind, out var killSummaryDefinition)
            ? killSummaryDefinition.OperationLabel
            : kind switch
            {
                LogbookMutationKind.SetQuestState => "Quest State",
                LogbookMutationKind.SetQuestGlobalState => "Quest Global State",
                LogbookMutationKind.SetRumorKnown => "Mark Rumor Known",
                LogbookMutationKind.QuellRumor => "Quell Rumor",
                LogbookMutationKind.AddReputation => "Add Reputation",
                LogbookMutationKind.RemoveReputation => "Remove Reputation",
                LogbookMutationKind.AddBlessing => "Add Blessing",
                LogbookMutationKind.RemoveBlessing => "Remove Blessing",
                LogbookMutationKind.AddCurse => "Add Curse",
                LogbookMutationKind.RemoveCurse => "Remove Curse",
                LogbookMutationKind.AddKey => "Add Key",
                LogbookMutationKind.RemoveKey => "Remove Key",
                LogbookMutationKind.AddInjury => "Add Injury History",
                LogbookMutationKind.RemoveInjury => "Remove Injury History",
                LogbookMutationKind.AddKill => "Record Kill",
                LogbookMutationKind.SetBackground => "Set Background",
                LogbookMutationKind.ClearBackground => "Clear Background",
                _ => kind.ToString(),
            };

    private static string FormatSubjectText(
        LogbookMutationKind kind,
        int entryId,
        string auxiliaryText,
        LogbookCatalogEntrySnapshot? catalogEntry,
        string? resolvedSubjectText = null
    )
    {
        if (kind == LogbookMutationKind.ClearBackground)
            return "Current background";

        if (kind == LogbookMutationKind.AddKill)
            return string.IsNullOrWhiteSpace(resolvedSubjectText) ? "Kill victim" : resolvedSubjectText;

        if (KillLogbookSummaryCatalog.TryGetDefinition(kind, out var killSummaryDefinition))
        {
            if (!killSummaryDefinition.RequiresDescription)
                return killSummaryDefinition.SummaryLabel;

            if (catalogEntry is not null)
            {
                return $"{catalogEntry.DisplayName} [{catalogEntry.EntryId.ToString(CultureInfo.InvariantCulture)}]";
            }

            return entryId > 0 ? $"Description {entryId.ToString(CultureInfo.InvariantCulture)}" : "No description";
        }

        if (catalogEntry is null)
        {
            if (kind is LogbookMutationKind.AddInjury or LogbookMutationKind.RemoveInjury)
                return $"Description {entryId.ToString(CultureInfo.InvariantCulture)}";

            return kind == LogbookMutationKind.SetBackground && auxiliaryText.Length != 0
                ? $"Background {entryId.ToString(CultureInfo.InvariantCulture)} ({auxiliaryText})"
                : $"{CategoryToken(kind)} {entryId.ToString(CultureInfo.InvariantCulture)}";
        }

        return kind == LogbookMutationKind.SetBackground
            ? $"{catalogEntry.DisplayName} [{catalogEntry.EntryId.ToString(CultureInfo.InvariantCulture)}]"
            : $"{catalogEntry.DisplayName} [{catalogEntry.EntryId.ToString(CultureInfo.InvariantCulture)}]";
    }

    private static string FormatValueText(LogbookMutationKind kind, string? rawValue) =>
        KillLogbookSummaryCatalog.TryGetDefinition(kind, out var killSummaryDefinition)
            ? FormatKillSummaryValue(killSummaryDefinition, rawValue)
            : kind switch
            {
                LogbookMutationKind.SetQuestState => RuntimeWatchValueCatalog.QuestPcStateName(
                    ParseQuestPcState(rawValue)
                ),
                LogbookMutationKind.SetQuestGlobalState => RuntimeWatchValueCatalog.QuestStateName(
                    ParseQuestGlobalState(rawValue)
                ),
                LogbookMutationKind.AddInjury or LogbookMutationKind.RemoveInjury => FormatInjuryType(
                    ParseInjuryType(rawValue)
                ),
                _ => string.Empty,
            };

    private static string FormatKillSummaryValue(KillLogbookSummaryDefinition definition, string? rawValue)
    {
        var value = ParseKillSummaryValueToken(rawValue, definition);
        return definition.MutationKind == LogbookMutationKind.SetTotalKills
            ? value.ToString(CultureInfo.InvariantCulture)
            : $"{definition.ValueLabel} {value.ToString(CultureInfo.InvariantCulture)}";
    }

    private static int ParseKillSummaryValueToken(string? rawValue, KillLogbookSummaryDefinition definition)
    {
        if (
            int.TryParse(rawValue?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            && value >= 0
        )
        {
            return value;
        }

        throw new InvalidOperationException(
            $"{definition.ValueLabel} '{rawValue}' is not a valid non-negative 32-bit integer."
        );
    }

    private static string FormatAuxiliaryText(
        LogbookMutationKind kind,
        string? rawAuxiliary,
        LogbookCatalogEntrySnapshot? catalogEntry,
        string? resolvedAuxiliaryText = null
    ) =>
        kind == LogbookMutationKind.AddKill ? resolvedAuxiliaryText ?? rawAuxiliary?.Trim() ?? string.Empty
        : kind == LogbookMutationKind.SetBackground
            ? $"Text {ResolveBackgroundAuxiliaryDisplayId(rawAuxiliary, catalogEntry).ToString(CultureInfo.InvariantCulture)}"
        : kind == LogbookMutationKind.RemoveInjury
            ? $"Slot {ParseInjurySlotIndex(rawAuxiliary).ToString(CultureInfo.InvariantCulture)}"
        : string.Empty;

    private static int ResolveBackgroundAuxiliaryDisplayId(
        string? rawAuxiliary,
        LogbookCatalogEntrySnapshot? catalogEntry
    )
    {
        if (
            int.TryParse(rawAuxiliary?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var textId)
            && textId > 0
        )
        {
            return textId;
        }

        if (catalogEntry is { AuxiliaryId: > 0 })
            return catalogEntry.AuxiliaryId;

        return ParseEntryId(rawAuxiliary ?? string.Empty, LogbookMutationKind.SetBackground);
    }

    private static string CreateSummary(
        LogbookMutationKind kind,
        bool noMutation,
        string targetText,
        string subjectText,
        string valueText
    ) =>
        KillLogbookSummaryCatalog.TryGetDefinition(kind, out var killSummaryDefinition)
            ? CreateKillSummary(killSummaryDefinition, noMutation, targetText, subjectText, valueText)
            : kind switch
            {
                LogbookMutationKind.SetQuestState => noMutation
                    ? $"{subjectText} already reads {valueText} for {targetText}."
                    : $"Set {subjectText} to {valueText} for {targetText}.",
                LogbookMutationKind.SetQuestGlobalState => noMutation
                    ? $"{subjectText} global state already reads {valueText}."
                    : $"Set {subjectText} global state to {valueText}.",
                LogbookMutationKind.SetRumorKnown => noMutation
                    ? $"{subjectText} was already known by {targetText}."
                    : $"Marked {subjectText} known for {targetText}.",
                LogbookMutationKind.QuellRumor => noMutation
                    ? $"{subjectText} was already quelled."
                    : $"Marked {subjectText} quelled.",
                LogbookMutationKind.AddReputation => noMutation
                    ? $"{subjectText} was already present on {targetText}."
                    : $"Added {subjectText} to {targetText}.",
                LogbookMutationKind.RemoveReputation => noMutation
                    ? $"{subjectText} was not present on {targetText}."
                    : $"Removed {subjectText} from {targetText}.",
                LogbookMutationKind.AddBlessing => noMutation
                    ? $"{subjectText} was already present on {targetText}."
                    : $"Added {subjectText} to {targetText}.",
                LogbookMutationKind.RemoveBlessing => noMutation
                    ? $"{subjectText} was not present on {targetText}."
                    : $"Removed {subjectText} from {targetText}.",
                LogbookMutationKind.AddCurse => noMutation
                    ? $"{subjectText} was already present on {targetText}."
                    : $"Added {subjectText} to {targetText}.",
                LogbookMutationKind.RemoveCurse => noMutation
                    ? $"{subjectText} was not present on {targetText}."
                    : $"Removed {subjectText} from {targetText}.",
                LogbookMutationKind.AddKey => noMutation
                    ? $"{subjectText} was already present in the keyring for {targetText}."
                    : $"Added {subjectText} to the keyring for {targetText}.",
                LogbookMutationKind.RemoveKey => noMutation
                    ? $"{subjectText} was not present in the keyring for {targetText}."
                    : $"Removed {subjectText} from the keyring for {targetText}.",
                LogbookMutationKind.AddInjury => noMutation
                    ? $"{subjectText} was already recorded on {targetText}."
                    : $"Recorded {valueText} from {subjectText} on {targetText}.",
                LogbookMutationKind.RemoveInjury => noMutation
                    ? $"{subjectText} was already absent from {targetText}."
                    : $"Removed {valueText} history from {subjectText} on {targetText}.",
                LogbookMutationKind.AddKill => $"Recorded one kill from {subjectText} for {targetText}.",
                LogbookMutationKind.SetBackground => noMutation
                    ? $"{subjectText} was already active on {targetText}."
                    : $"Applied {subjectText} to {targetText}.",
                LogbookMutationKind.ClearBackground => noMutation
                    ? $"{targetText} already had no active background."
                    : $"Cleared the current background from {targetText}.",
                _ => $"{OperationText(kind)} finished.",
            };

    private static string CreateKillSummary(
        KillLogbookSummaryDefinition definition,
        bool noMutation,
        string targetText,
        string subjectText,
        string valueText
    )
    {
        if (definition.MutationKind == LogbookMutationKind.SetTotalKills)
        {
            return noMutation
                ? $"{definition.SummaryLabel} already reads {valueText} for {targetText}."
                : $"Set {definition.SummaryLabel.ToLowerInvariant()} to {valueText} for {targetText}.";
        }

        return noMutation
            ? $"{definition.SummaryLabel} already records {subjectText} with {valueText} on {targetText}."
            : $"Set {definition.SummaryLabel.ToLowerInvariant()} to {subjectText} with {valueText} for {targetText}.";
    }

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

    private static int ParseInjuryType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                "Injury type is required. Use blinded, crippled-arm, crippled-leg, or scarred."
            );

        return Normalize(value) switch
        {
            "0" or "blinded" => 0,
            "1" or "crippledarm" => 1,
            "2" or "crippledleg" => 2,
            "3" or "scarred" => 3,
            _ => throw new InvalidOperationException(
                $"Unknown injury type '{value}'. Use blinded, crippled-arm, crippled-leg, or scarred."
            ),
        };
    }

    private static int ParseInjurySlotIndex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Injury history slot is required. Use one live injury shortcut so ArcNET can target the exact healed row."
            );
        }

        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new InvalidOperationException(
                $"Injury history slot '{value}' is not a valid signed 32-bit integer."
            );
    }

    private static string FormatInjuryType(int injuryType) =>
        injuryType switch
        {
            0 => "Blinded",
            1 => "Crippled arm",
            2 => "Crippled leg",
            3 => "Scarred",
            _ => $"Injury {injuryType.ToString(CultureInfo.InvariantCulture)}",
        };

    private static LogbookEditorCatalogSnapshot CreateUnavailableCatalogSnapshot(string status, string summary) =>
        new(DateTimeOffset.UtcNow, false, status, summary, [], []);

    private static LogbookMutationSnapshot CreateUnavailableMutationSnapshot(
        LogbookMutationKind kind,
        string status,
        string summary
    ) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            kind,
            OperationText(kind),
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "Dispatcher result unavailable.",
            "Target address and hook details will appear here after a live journal mutation.",
            "Mutation result values will appear here after a live journal mutation.",
            []
        );
}
